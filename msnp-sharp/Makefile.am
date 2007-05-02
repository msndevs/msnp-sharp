
# Warning: This is an automatically generated file, do not edit!

if ENABLE_DEBUG
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4 -debug -d:DEBUG
ASSEMBLY = ./bin/Debug/MSNPSharp.dll
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ./bin/Debug/
endif
if ENABLE_RELEASE
ASSEMBLY_COMPILER_COMMAND = gmcs
ASSEMBLY_COMPILER_FLAGS =  -noconfig -codepage:utf8 -warn:4
ASSEMBLY = ./bin/Release/MSNPSharp.dll
COMPILE_TARGET = library
PROJECT_REFERENCES = 
BUILD_DIR = ./bin/Release/
endif

LINUX_PKGCONFIG = \
	$(MSNP_SHARP_PC)  

	
all: $(ASSEMBLY) $(LINUX_PKGCONFIG) 

FILES = \
	./AssemblyInfo.cs \
	./Configuration.cs \
	./ConnectivityException.cs \
	./ConnectivitySettings.cs \
	./Contact.cs \
	./ContactGroup.cs \
	./ContactGroupList.cs \
	./ContactList.cs \
	./Conversation.cs \
	./Credentials.cs \
	./DisplayImage.cs \
	./MSNPSharpException.cs \
	./Emoticon.cs \
	./EmoticonMessage.cs \
	./enums.cs \
	./EventArgs.cs \
	./Factory.cs \
	./IMessageHandler.cs \
	./MailEventArgs.cs \
	./MessagePool.cs \
	./MessageProcessor.cs \
	./Messenger.cs \
	./MobileMessage.cs \
	./MSNMessage.cs \
	./MSNObject.cs \
	./MSNSLPHandler.cs \
	./MSNSLPMessage.cs \
	./NetworkMessage.cs \
	./NotificationMessage.cs \
	./NS11MessageHandler.cs \
	./NSMessage.cs \
	./NSMessagePool.cs \
	./NSMessageProcessor.cs \
	./P2PDCPool.cs \
	./P2PDirectProcessor.cs \
	./P2PHandler.cs \
	./P2PMessage.cs \
	./P2PMessagePool.cs \
	./P2PMessageSession.cs \
	./P2PTransferSession.cs \
	./SB11MessageHandler.cs \
	./SBMessage.cs \
	./SBMessagePool.cs \
	./SBMessageProcessor.cs \
	./SBMSGMessage.cs \
	./SocketMessageProcessor.cs \
	./TextMessage.cs \
	./UnauthorizedException.cs \
	./ProxySocket/AuthMethod.cs \
	./ProxySocket/AuthNone.cs \
	./ProxySocket/AuthUserPass.cs \
	./ProxySocket/IAsyncProxyResult.cs \
	./ProxySocket/ProxyException.cs \
	./ProxySocket/ProxySocket.cs \
	./ProxySocket/Socks4Handler.cs \
	./ProxySocket/Socks5Handler.cs \
	./ProxySocket/SocksHandler.cs \
	./QRYFactory.cs \
	./PersonalMessage.cs \
	./Wink.cs 

DATA_FILES = 

RESOURCES = \
	./owner.png 

EXTRAS = \
	./msnp-sharp.pc.in 

REFERENCES =  \
	-r:System \
	-r:System.Data \
	-r:System.Xml \
	-r:System.Web \
	-r:System.Drawing

DLL_REFERENCES = 

MSNP_SHARP_PC = $(BUILD_DIR)/msnp-sharp.pc
$(MSNP_SHARP_PC): msnp-sharp.pc
	mkdir -p $(BUILD_DIR)
	cp msnp-sharp.pc $(MSNP_SHARP_PC)



$(ASSEMBLY) $(ASSEMBLY).mdb: $(build_sources) $(build_resources) $(build_datafiles) 
	mkdir -p $(dir $(ASSEMBLY))		
	$(ASSEMBLY_COMPILER_COMMAND) $(ASSEMBLY_COMPILER_FLAGS) -out:$@ -target:$(COMPILE_TARGET) $(build_sources) $(build_resources_embed) $(build_references_ref)

include $(top_srcdir)/Makefile.include
