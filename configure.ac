dnl Warning: This is an automatically generated file, do not edit!
dnl Process this file with autoconf to produce a configure script.
AC_PREREQ([2.54])
AC_INIT([msnp-sharp], [0.1])
AM_INIT_AUTOMAKE([foreign])

dnl pkg-config
AC_PATH_PROG(PKG_CONFIG, pkg-config, no)
if test "x$PKG_CONFIG" = "xno"; then
        AC_MSG_ERROR([You need to install pkg-config])
fi

AC_PROG_INSTALL

AC_PATH_PROG(MCS, mcs, no)
if test "x$MCS" = "xno"; then
        AC_MSG_ERROR([mcs Not found])
fi
AC_PATH_PROG(GMCS, gmcs, no)
if test "x$GMCS" = "xno"; then
        AC_MSG_ERROR([gmcs Not found])
fi


PKG_CHECK_MODULES([NUNIT_22], [nunit-2.2])


AC_ARG_ENABLE(debug,
	AC_HELP_STRING([--enable-debug],
		[Use 'Debug' Configuration [default=NO]]),
		enable_debug=yes, enable_debug=no)
AM_CONDITIONAL(ENABLE_DEBUG, test x$enable_debug = xyes)
if test "x$enable_debug" = "xyes" ; then
	MSNP_SHARP_DEBUG_LIB='MSNPSharp.dll'
	AC_SUBST(MSNP_SHARP_DEBUG_LIB)
	MSNP_SHARP_NUNIT_DEBUG_LIB='NUNit2.dll'
	AC_SUBST(MSNP_SHARP_NUNIT_DEBUG_LIB)
	PROXYSERVER_DEBUG_LIB='ProxyServer.dll'
	AC_SUBST(PROXYSERVER_DEBUG_LIB)
	DEBUG_CONFIG_LIBRARIES=' ${pkglibdir}/MSNPSharp.dll ${pkglibdir}/NUNit2.dll ${pkglibdir}/ProxyServer.dll'
	DEBUG_CONFIG_LIBS=' -r:${pkglibdir}/MSNPSharp.dll -r:${pkglibdir}/NUNit2.dll -r:${pkglibdir}/ProxyServer.dll'
	AC_SUBST(DEBUG_CONFIG_LIBRARIES)
	AC_SUBST(DEBUG_CONFIG_LIBS)
	CONFIG_REQUESTED="yes"
fi
AC_ARG_ENABLE(release,
	AC_HELP_STRING([--enable-release],
		[Use 'Release' Configuration [default=YES]]),
		enable_release=yes, enable_release=no)
AM_CONDITIONAL(ENABLE_RELEASE, test x$enable_release = xyes)
if test "x$enable_release" = "xyes" ; then
	MSNP_SHARP_RELEASE_LIB='XihSolutions.DotMSN.dll'
	AC_SUBST(MSNP_SHARP_RELEASE_LIB)
	MSNP_SHARP_NUNIT_RELEASE_LIB='NUNit2.dll'
	AC_SUBST(MSNP_SHARP_NUNIT_RELEASE_LIB)
	PROXYSERVER_RELEASE_LIB='ProxyServer.dll'
	AC_SUBST(PROXYSERVER_RELEASE_LIB)
	RELEASE_CONFIG_LIBRARIES=' ${pkglibdir}/XihSolutions.DotMSN.dll ${pkglibdir}/NUNit2.dll ${pkglibdir}/ProxyServer.dll'
	RELEASE_CONFIG_LIBS=' -r:${pkglibdir}/XihSolutions.DotMSN.dll -r:${pkglibdir}/NUNit2.dll -r:${pkglibdir}/ProxyServer.dll'
	AC_SUBST(RELEASE_CONFIG_LIBRARIES)
	AC_SUBST(RELEASE_CONFIG_LIBS)
	CONFIG_REQUESTED="yes"
fi
if test -z "$CONFIG_REQUESTED" ; then
	MSNP_SHARP_RELEASE_LIB='XihSolutions.DotMSN.dll'
	AC_SUBST(MSNP_SHARP_RELEASE_LIB)
	MSNP_SHARP_NUNIT_RELEASE_LIB='NUNit2.dll'
	AC_SUBST(MSNP_SHARP_NUNIT_RELEASE_LIB)
	PROXYSERVER_RELEASE_LIB='ProxyServer.dll'
	AC_SUBST(PROXYSERVER_RELEASE_LIB)
	RELEASE_CONFIG_LIBRARIES=' ${pkglibdir}/XihSolutions.DotMSN.dll ${pkglibdir}/NUNit2.dll ${pkglibdir}/ProxyServer.dll'
	RELEASE_CONFIG_LIBS=' -r:${pkglibdir}/XihSolutions.DotMSN.dll -r:${pkglibdir}/NUNit2.dll -r:${pkglibdir}/ProxyServer.dll'
	AC_SUBST(RELEASE_CONFIG_LIBRARIES)
	AC_SUBST(RELEASE_CONFIG_LIBS)
	AM_CONDITIONAL(ENABLE_RELEASE, true)
fi


AC_CONFIG_FILES([
./ProxyServer/Makefile
./UnitTest/msnp-sharp.msnp-sharp-nunit.pc
./Example/dotmsnclient
./ProxyServer/msnp-sharp.ProxyServer.pc
./msnp-sharp/msnp-sharp.msnp-sharp.pc
./UnitTest/Makefile
./msnp-sharp/Makefile
./Example/Makefile
./Example/msnp-sharp.msnp-sharp-client.pc
./Makefile

])

AC_OUTPUT
