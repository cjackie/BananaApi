AC_PREREQ(2.59)
AC_INIT([BananaGameServer], [2.0])
AC_CONFIG_HEADERS([config.h])
AC_CONFIG_MACRO_DIR([m4])
LT_INIT
AM_INIT_AUTOMAKE([foreign subdir-objects]) # Does not require NEWS, COPYING, AUTHORS, ChangeLog or README

AC_PROG_CXX([g++])

dnl check for c++11 is supported
DIALECT="-std=c++0x"
echo 'int main() {return 0;}' > ./log.cpp && $CXX -std=c++0x ./log.cpp || $DIALECT="no"
if test $DILAECT = no; then
    AC_MSG_ERROR([c++ compiler does not support c++11])
else
    echo $DILAECT
fi

CXXFLAGS="$CXXFLAGS -std=c++0x"

dnl Makes sure that the OS is LE
AC_C_BIGENDIAN(AC_MSG_ERROR([The OS must be Little Endian]))

AC_PROG_CC
AC_ENABLE_SHARED
AC_DISABLE_STATIC
AC_PROG_LIBTOOL(libtool)
AC_CONFIG_FILES([Makefile src/Makefile])

AC_OUTPUT