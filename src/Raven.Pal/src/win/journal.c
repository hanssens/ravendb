#include <windows.h>
#include <assert.h>

#include "rvn.h"
#include "status_codes.h"
#include "internal_win.h"

EXPORT int32_t
rvn_open_journal_for_writes(const char* file_name, int32_t transaction_mode, int64_t initial_file_size, int32_t durability_support, void** handle, int64_t* actual_size, int32_t* detailed_error_code)
{
    assert(initial_file_size > 0);

    DWORD access_flags;
    DWORD share_flags = FILE_SHARE_READ;
    switch (transaction_mode)
    {
        case JOURNAL_MODE_DANGER :
            access_flags = 0;
            break;
        case JOURNAL_MODE_PURE_MEMORY:
            access_flags = FILE_ATTRIBUTE_TEMPORARY | FILE_FLAG_DELETE_ON_CLOSE;
            share_flags |= FILE_SHARE_WRITE | FILE_SHARE_DELETE;
            break;
        default:
            access_flags = FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH;
            if (durability_support == DURABILITY_NOT_SUPPORTED)
                access_flags = 0;
            break;
    }

    int32_t rc;
    HANDLE h_file = CreateFileW(
        (LPCWSTR)file_name,
        GENERIC_WRITE,
        share_flags,
        NULL,
        OPEN_ALWAYS,
        access_flags,
        NULL);

    if (h_file == INVALID_HANDLE_VALUE)
    {
        rc = FAIL_OPEN_FILE;
        goto error_cleanup;
    }
    *handle = h_file;

    LARGE_INTEGER size;
    if (GetFileSizeEx(h_file, &size) == FALSE)
    {
        rc = FAIL_GET_FILE_SIZE;
        goto error_cleanup;
    }

    if (size.QuadPart <= initial_file_size)
    {
        rc = _resize_file(h_file, initial_file_size, detailed_error_code);
        if (rc != SUCCESS)
            goto error_clean_With_error;
        *actual_size = initial_file_size;
    }
    else
    {
        *actual_size = size.QuadPart;
    }

    return SUCCESS;

error_cleanup:
    *detailed_error_code = GetLastError();
error_clean_With_error:
    if (h_file != INVALID_HANDLE_VALUE)
        CloseHandle(h_file);
    return rc;
}

EXPORT int32_t
rvn_close_journal(void* handle, int32_t* detailed_error_code)
{
    if (CloseHandle(handle) != FALSE)
        return SUCCESS;

    *detailed_error_code = GetLastError();
    return FAIL_CLOSE;
}

EXPORT int32_t
rvn_write_journal(void* handle, void* buffer, int64_t size, int64_t offset, int32_t* detailed_error_code)
{
    return _write_file(handle, buffer, size, offset, detailed_error_code);
}

EXPORT int32_t
rvn_open_journal_for_reads(const char *file_name, void **handle, int32_t *detailed_error_code)
{
    return _open_file_to_read(file_name, handle, detailed_error_code);
}

EXPORT int32_t
rvn_read_journal(void* handle, void* buffer, int64_t required_size, int64_t offset, int64_t* actual_size, int32_t* detailed_error_code)
{
    return _read_file(handle, buffer, required_size, offset, actual_size, detailed_error_code);
}

EXPORT int32_t
rvn_truncate_journal(void* handle, int64_t size, int32_t* detailed_error_code)
{
    if (FlushFileBuffers(handle) == FALSE)
    {
        *detailed_error_code = GetLastError();
        return FAIL_FLUSH_FILE;
    }

    return _resize_file(handle, size, detailed_error_code);
}


