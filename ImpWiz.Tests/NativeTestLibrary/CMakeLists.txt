cmake_minimum_required(VERSION 3.10)

# set the project name
project(NativeTestLibrary)

# add the executable
add_library(NativeTestLibrary SHARED MarshalingTests.c)
