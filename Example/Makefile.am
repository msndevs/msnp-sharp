
# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/DotMSNClient.exe
COMPILE_TARGET = exe
PROJECT_REFERENCES =  \
	../msnp-sharp/bin/Debug/MSNPSharp.dll
BUILD_DIR = ./bin/Debug/
endif
if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4
ASSEMBLY = ./bin/Release/DotMSNClient.exe
COMPILE_TARGET = winexe
PROJECT_REFERENCES =  \
	../msnp-sharp/bin/Release/MSNPSharp.dll
BUILD_DIR = ./bin/Release/
endif

BINARIES = \
	$(MSNP_SHARP_CLIENT)  
PROGRAMFILES = \
	$(MSNPSHARP_DLL) \
	$(MSNPSHARP_DLL_MDB)  

	
all: $(ASSEMBLY) $(BINARIES) $(PROGRAMFILES) 

FILES = \
	./AssemblyInfo.cs \
	./ConversationForm.cs \
	./DotMSNClient.cs 

DATA_FILES = 

RESOURCES = \
	./ConversationForm.resx \
	./DotMSNClient.resx \
	./listbg.png \
	./App.ico 

EXTRAS = \
	./msnp-sharp-client.in 

REFERENCES =  \
	-r:System \
	-r:System.Data \
	-r:System.Drawing \
	-r:System.Xml \
	-r:System.Windows.Forms

DLL_REFERENCES = 

MSNPSHARP_DLL = $(BUILD_DIR)/MSNPSharp.dll
$(MSNPSHARP_DLL): $(top_builddir)/msnp-sharp/bin/Debug/MSNPSharp.dll
	mkdir -p $(BUILD_DIR)
	cp $(top_builddir)/msnp-sharp/bin/Debug/MSNPSharp.dll $(MSNPSHARP_DLL)

MSNPSHARP_DLL_MDB = $(BUILD_DIR)/MSNPSharp.dll.mdb
$(MSNPSHARP_DLL_MDB): $(top_builddir)/msnp-sharp/bin/Debug/MSNPSharp.dll.mdb
	mkdir -p $(BUILD_DIR)
	cp $(top_builddir)/msnp-sharp/bin/Debug/MSNPSharp.dll.mdb $(MSNPSHARP_DLL_MDB)

MSNP_SHARP_CLIENT = $(BUILD_DIR)/msnp-sharp-client
$(MSNP_SHARP_CLIENT): msnp-sharp-client
	mkdir -p $(BUILD_DIR)
	cp msnp-sharp-client $(MSNP_SHARP_CLIENT)



$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include
