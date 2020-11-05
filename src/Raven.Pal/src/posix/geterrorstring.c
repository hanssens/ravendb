#ifndef _GNU_SOURCE
#define _GNU_SOURCE
#endif

#include <unistd.h>
#include <errno.h>
#include <string.h>
#include <stdlib.h>

#include "rvn.h"
#include "status_codes.h"
#include "internal_posix.h"

EXPORT int32_t
rvn_get_error_string(int32_t error, 
					 char* buf, 
					 int32_t buf_size, 
					 int32_t* special_errno_flags) 
{
	char* tmp_buf = NULL;
	switch (error) {
		case ENOMEM:
			*special_errno_flags = ERRNO_SPECIAL_CODES_ENOMEM;
			break;
		case ENOENT:
			*special_errno_flags = ERRNO_SPECIAL_CODES_ENOENT;
			break;
		case ENOSPC:
			*special_errno_flags = ERRNO_SPECIAL_CODES_ENOSPC;
			break;
		default:
			*special_errno_flags = ERRNO_SPECIAL_CODES_NONE;
			break;
	}
	
	tmp_buf = malloc(buf_size);
	if(tmp_buf == NULL)
		goto error_cleanup;

	char* err = _get_strerror_r(error, tmp_buf, buf_size);
	if(err == NULL)
		goto error_cleanup;

	size_t size = strlen(err);
	
	size_t actual_size = size >  buf_size-1 ? buf_size-1 : size;
	memcpy(buf, err, actual_size);

	buf[actual_size] = 0;
	free(tmp_buf);

	return actual_size;


error_cleanup:
	if(tmp_buf != NULL)
		free(tmp_buf);
	return FAIL;
}
