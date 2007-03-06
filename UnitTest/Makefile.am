
# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/NUNit2.dll
COMPILE_TARGET = library
PC_FILES_IN = msnp-sharp.msnp-sharp-nunit.pc.in
PROJECT_REFERENCES =  \
	../ProxyServer/bin/Debug/ProxyServer.dll \
	../msnp-sharp/bin/Debug/MSNPSharp.dll
BUILD_DIR = ./bin/Debug/
endif
if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4
ASSEMBLY = ./bin/Release/NUNit2.dll
COMPILE_TARGET = library
PC_FILES_IN = msnp-sharp.msnp-sharp-nunit.pc.in
PROJECT_REFERENCES =  \
	../ProxyServer/bin/Release/ProxyServer.dll \
	../msnp-sharp/bin/Release/XihSolutions.DotMSN.dll
BUILD_DIR = ./bin/Release/
endif

	
all: $(ASSEMBLY)

FILES = \
	./DataTransferTests.cs \
	./MobileMessage.cs \
	./NameserverTests.cs \
	./ProxyConnection.cs \
	./SerializableTest.cs \
	./SwitchboardTests.cs \
	./TestBase.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = 

REFERENCES =  \
	$(NUNIT_22_LIBS) \
	-r:System \
	-r:System.Data \
	-r:System.Xml \
	-r:System.Drawing

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include
