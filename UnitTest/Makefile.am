
EXTRA_DIST = 

# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/NUNit2.dll
COMPILE_TARGET = library
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
PROJECT_REFERENCES =  \
	../ProxyServer/bin/Release/ProxyServer.dll \
	../msnp-sharp/bin/Debug/MSNPSharp.dll
BUILD_DIR = ./bin/Release/
endif


PROGRAMFILES = \
	$(PROXYSERVER_DLL) \
	$(MSNPSHARP_DLL) \
	$(MSNPSHARP_DLL_MDB)  
LINUX_PKGCONFIG = \
	$(MSNP_SHARP_NUNIT_PC)  

	
all: $(ASSEMBLY) $(PROGRAMFILES) $(LINUX_PKGCONFIG) 

FILES = \
	DataTransferTests.cs \
	MobileMessage.cs \
	NameserverTests.cs \
	ProxyConnection.cs \
	SerializableTest.cs \
	SwitchboardTests.cs \
	TestBase.cs 

DATA_FILES = 

RESOURCES = 

EXTRAS = \
	./msnp-sharp-nunit.pc.in 

REFERENCES =  \
	$(MONO_NUNIT_LIBS) \
	-r:System \
	-r:System.Data \
	-r:System.Xml \
	-r:System.Drawing

DLL_REFERENCES = 

PROXYSERVER_DLL = $(BUILD_DIR)/ProxyServer.dll
$(PROXYSERVER_DLL): ../ProxyServer/bin/Debug/ProxyServer.dll
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'

MSNPSHARP_DLL = $(BUILD_DIR)/MSNPSharp.dll
$(MSNPSHARP_DLL): ../msnp-sharp/bin/Debug/MSNPSharp.dll
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'

MSNPSHARP_DLL_MDB = $(BUILD_DIR)/MSNPSharp.dll.mdb
$(MSNPSHARP_DLL_MDB): ../msnp-sharp/bin/Debug/MSNPSharp.dll.mdb
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'

MSNP_SHARP_NUNIT_PC = $(BUILD_DIR)/msnp-sharp-nunit.pc
$(MSNP_SHARP_NUNIT_PC): msnp-sharp-nunit.pc
	mkdir -p $(BUILD_DIR)
	cp '$<' '$@'



$(build_resx_resources) : %.resources: %.resx
	resgen2 '$<' '$@'

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) $(DLL_REFERENCES) $(PROJECT_REFERENCES)
	mkdir -p $(dir $(ASSEMBLY))
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources_embed) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include