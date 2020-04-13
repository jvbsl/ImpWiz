#include <string.h>
#include <stddef.h>
#include <stdint.h>
#include "LibraryBasics.h"

#ifdef __cplusplus
extern "C"
{
#endif
DLLEXPORT char* GetLPSTR();

DLLEXPORT int32_t GetInt32();

DLLEXPORT int32_t Add(int32_t a, int32_t b);

DLLEXPORT int32_t ParseInt32(char* ptr);
#ifdef __cplusplus
};
#endif