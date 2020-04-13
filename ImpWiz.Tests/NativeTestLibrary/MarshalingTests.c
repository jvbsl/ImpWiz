#include <stdlib.h>
#include <stdio.h>
#include "MarshalingTests.h"

char* GetLPSTR()
{
    return "ASCII Test String";
}

int32_t GetInt32()
{
    return 11;
}

int32_t Add(int32_t a, int32_t b)
{
    return a+b;
}

int32_t ParseInt32(char* ptr)
{
    return atoi(ptr);
}
char buffer[256];
char* Combine(char* a, char* b)
{
    snprintf(buffer, 256, "%s%s", a, b);
    return buffer;
}