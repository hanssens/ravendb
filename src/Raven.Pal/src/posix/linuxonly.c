#if defined(__unix__) && !defined(__APPLE__)

#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif

#include <unistd.h>
#include <sys/statfs.h>
#include <linux/magic.h>
#include <sys/syscall.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <errno.h>
#include <stdlib.h>
#include <assert.h>
#include <string.h>
#include <libgen.h>
#include <unistd.h>

#include "rvn.h"
#include "status_codes.h"
#include "internal_posix.h"

EXPORT uint64_t
rvn_get_current_thread_id(void)
{
  return syscall(SYS_gettid);
}

PRIVATE int32_t
_flush_file(int32_t fd)
{
  return fsync(fd);
}

#define SMB2_MAGIC_NUMBER 0xfe534d42

PRIVATE int32_t
_sync_directory_allowed(int32_t dir_fd)
{
  struct statfs buf;
  if (fstatfs(dir_fd, &buf) == -1)
    return FAIL;

  switch (buf.f_type)
  {
  case NFS_SUPER_MAGIC:
  case CIFS_MAGIC_NUMBER:
  case SMB_SUPER_MAGIC:
  case SMB2_MAGIC_NUMBER:
    return SYNC_DIR_NOT_ALLOWED;
  default:
    return SYNC_DIR_ALLOWED;
  }
}

PRIVATE int32_t
_finish_open_file_with_odirect(int32_t fd)
{
  /* nothing to do in posix, O_DIRECT is supported */
  return 0;
}

PRIVATE int32_t
_rvn_fallocate(int32_t fd, int64_t offset, int64_t size)
{
  return posix_fallocate64(fd, offset, size);
}

PRIVATE char*
_get_strerror_r(int32_t error, char* tmp_buff, int32_t buf_size)
{
  return strerror_r(error, tmp_buff, buf_size);
}

EXPORT int32_t
rvn_test_storage_durability(
    const char *temp_file_name,
    int32_t *detailed_error_code)
{
    int fd = open(temp_file_name, O_WRONLY | O_DSYNC | O_DIRECT | O_CREAT, S_IWUSR | S_IRUSR);
    int rc = SUCCESS;
    if (fd == -1)
    {
        rc = FAIL_OPEN_FILE;
        goto error_cleanup;
    }

    rc = _allocate_file_space(fd, 64 * 1024, detailed_error_code);
    if ( rc != SUCCESS )
    {
        rc = FAIL_ALLOC_FILE;
        if ( errno == EINVAL)
            rc = FAIL_TEST_DURABILITY;
        goto error_cleanup;        
    }

error_cleanup:
    if (rc != SUCCESS)
        *detailed_error_code = errno;
    if (fd != -1)
    {
        close(fd);
        unlink(temp_file_name);
    }

    return rc;
}


#endif
