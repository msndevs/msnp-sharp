
# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/DotMSNClient.exe
COMPILE_TARGET = exe
ASSEMBLY_WRAPPER = dotmsnclient
ASSEMBLY_WRAPPER_IN = dotmsnclient.in
PROJECT_REFERENCES =  \
	../msnp-sharp/bin/Debug/MSNPSharp.dll
BUILD_DIR = ./bin/Debug/
endif
if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4
ASSEMBLY = ./bin/Release/DotMSNClient.exe
COMPILE_TARGET = winexe
ASSEMBLY_WRAPPER = dotmsnclient
ASSEMBLY_WRAPPER_IN = dotmsnclient.in
PROJECT_REFERENCES =  \
	../msnp-sharp/bin/Release/XihSolutions.DotMSN.dll
BUILD_DIR = ./bin/Release/
endif

	
all: $(ASSEMBLY)

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

EXTRAS = 

REFERENCES =  \
	-r:System \
	-r:System.Data \
	-r:System.Drawing \
	-r:System.Xml \
	-r:System.Windows.Forms

DLL_REFERENCES = 

$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		

	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include
