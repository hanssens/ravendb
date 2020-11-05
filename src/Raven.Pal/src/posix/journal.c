#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif 

#include <unistd.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <errno.h>
#include <stdio.h>
#include <assert.h>
#include <stdlib.h>
#include <string.h>

#include "rvn.h"
#include "status_codes.h"
#include "internal_posix.h"

struct journal_handle
{
    int fd;
    const char *path;
    bool delete_on_close;
};

PRIVATE void 
_free_journal_handle(struct journal_handle* handle)
{
    if (handle->path != NULL)
    {
        free((void *)(handle->path));
        (handle)->path = NULL;
    }

    free((void*)handle);
}

EXPORT int32_t
rvn_open_journal_for_writes(const char *file_name, int32_t transaction_mode, int64_t initial_file_size, int32_t durability_support, void **handle, int64_t *actual_size, int32_t *detailed_error_code)
{
    assert(initial_file_size > 0);

    int32_t rc;
    
    int32_t flags = O_DSYNC | O_DIRECT;
    if (durability_support == DURABILITY_NOT_SUPPORTED)
        flags = O_DSYNC;

    if (transaction_mode == JOURNAL_MODE_DANGER || transaction_mode == JOURNAL_MODE_PURE_MEMORY)
        flags = 0;
    if (sizeof(int) == 4) /* 32 bits */
        flags |= O_LARGEFILE;

    struct journal_handle *jfh = calloc(1, sizeof(struct journal_handle));
    
    *handle = jfh;
    if (jfh == NULL)
    {
        rc = FAIL_CALLOC;
        goto error_cleanup;
    }

    jfh->delete_on_close = transaction_mode == JOURNAL_MODE_PURE_MEMORY;


    jfh->path = strdup(file_name);
    if (jfh->path == NULL)
    {
        rc = FAIL_CALLOC;
        goto error_cleanup;
    }

    jfh->fd = open(file_name, flags | O_WRONLY | O_CREAT, S_IWUSR | S_IRUSR);

    if (jfh->fd == -1)
    {
        rc = FAIL_OPEN_FILE;
        goto error_cleanup;
    }

    if ((flags & O_DIRECT) && _finish_open_file_with_odirect(jfh->fd) == -1)
    {
        rc = FAIL_SYNC_FILE;
        goto error_cleanup;
    }

    struct stat fs;
    if (fstat(jfh->fd, &fs) == -1)
    {
        rc = FAIL_STAT_FILE;
        goto error_cleanup;
    }

    if (fs.st_size < initial_file_size)
    {
        rc = _resize_file(jfh->fd, initial_file_size, detailed_error_code);
        if (rc != SUCCESS)
            goto error_clean_With_error;

        *actual_size = initial_file_size;
    }
    else
    {
        *actual_size = fs.st_size;
    }    
    return SUCCESS;

error_cleanup:
    *detailed_error_code = errno;
error_clean_With_error:
    if (jfh != NULL)
    {
        if (jfh->fd != -1)
        {
            if (jfh->delete_on_close == true)
            {
                int32_t unlink_rc = unlink(jfh->path);
                if (unlink_rc != 0)
                {
                    /* record the error and continue to close */
                    rc = FAIL_UNLINK;
                    *detailed_error_code = errno;
                }
            }
            close(jfh->fd);
        }

        _free_journal_handle(*handle);
        *handle = NULL;
    }

    return rc;
}

EXPORT int32_t
rvn_close_journal(void *handle, int32_t *detailed_error_code)
{
    int32_t rc;
    struct journal_handle* jfh = (struct journal_handle*)handle;
    if (jfh->delete_on_close == true)
    {
        int32_t unlink_rc = unlink(jfh->path);
        if (unlink_rc != 0)
        {
            /* record the error and continue to close */
            rc = FAIL_UNLINK;
            *detailed_error_code = errno;
        }
    }

    if (close(jfh->fd) == -1)
    {
        rc = FAIL_CLOSE;
        goto error_cleanup;
    }

    rc = SUCCESS;
    goto cleanup;

error_cleanup :
    *detailed_error_code = errno;
cleanup:
    _free_journal_handle(jfh);
    return rc;
}

EXPORT int32_t
rvn_write_journal(void *handle, void *buffer, int64_t size, int64_t offset, int32_t *detailed_error_code)
{
    struct journal_handle *jfh = (struct journal_handle *)handle;
    return _pwrite(jfh->fd, buffer, size, offset, detailed_error_code);
}

EXPORT int32_t
rvn_open_journal_for_reads(const char *file_name, void **handle, int32_t *detailed_error_code)
{
    int32_t rc;
    struct journal_handle *jfh = calloc(1, sizeof(struct journal_handle));
    *handle = jfh;
    if (jfh == NULL)
    {
        *detailed_error_code = errno;
        return FAIL_CALLOC;
    }

    jfh->path = NULL;
    rc = _open_file_to_read(file_name, &(jfh->fd), detailed_error_code);
    if(rc != SUCCESS)
    {
        if (jfh->fd != -1)
        {
            if (jfh->delete_on_close == true)
            {
                int32_t unlink_rc = unlink(jfh->path);
                if (unlink_rc != 0)
                {
                    /* record the error and continue to close */
                    rc = FAIL_UNLINK;
                    *detailed_error_code = errno;
                }
            }
            close(jfh->fd);
        }

        _free_journal_handle(jfh);
        *handle = NULL;
    }

    return rc;
}

EXPORT int32_t
rvn_read_journal(void *handle, void *buffer, int64_t required_size, int64_t offset, int64_t *actual_size, int32_t *detailed_error_code)
{
    struct journal_handle *jfh = (struct journal_handle *)handle;
    return _read_file(jfh->fd, buffer, required_size, offset, actual_size, detailed_error_code);
}

EXPORT int32_t
rvn_truncate_journal(void *handle, int64_t size, int32_t *detailed_error_code)
{
    int32_t rc;
    struct journal_handle *jfh = (struct journal_handle *)handle;

    if (_flush_file(jfh->fd) == -1)
    {
        rc = FAIL_SYNC_FILE;
        goto error_cleanup;
    }

    rc = _resize_file(jfh->fd, size, detailed_error_code);
    if(rc != SUCCESS)
        return rc;

    return _sync_directory_for(jfh->path, detailed_error_code);

error_cleanup:
    *detailed_error_code = errno;
    return rc;
}
