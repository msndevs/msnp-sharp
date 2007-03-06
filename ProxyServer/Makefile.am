
# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/ProxyServer.dll
COMPILE_TARGET = library
PC_FILES_IN = msnp-sharp.ProxyServer.pc.in
PROJECT_REFERENCES = 
BUILD_DIR = ./bin/Debug/
endif
if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = mcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4
ASSEMBLY = ./bin/Release/ProxyServer.dll
COMPILE_TARGET = library
PC_FILES_IN = msnp-sharp.ProxyServer.pc.in
PROJECT_REFERENCES = 
BUILD_DIR = ./bin/Release/
endif

	
all: $(ASSEMBLY)

FILES = \
	./AssemblyInfo.cs \
	./AuthBase.cs \
	./AuthenticationList.cs \
	./AuthNone.cs \
	./AuthUserPass.cs \
	./Client.cs \
	./ConsoleAttributes.cs \
	./FtpClient.cs \
	./FtpDataConnection.cs \
	./FtpListener.cs \
	./HttpClient.cs \
	./HttpListener.cs \
	./Listener.cs \
	./PortMapClient.cs \
	./PortMapListener.cs \
	./Proxy.cs \
	./ProxyConfig.cs \
	./Socks4Handler.cs \
	./Socks5Handler.cs \
	./SocksClient.cs \
	./SocksHandler.cs \
	./SocksListener.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES =  \
	-r:System \
	-r:System.Data \
	-r:System.Xml

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include
