TARGETNAME=FooSyncEngine.dll
TARGETTYPE=library
TARGETDEST=../lib
TARGET=$(TARGETDEST)/$(TARGETNAME)

MCS=dmcs -debug+
SDK=4

SOURCES=\
    CopyEngine.cs \
    FooChangeSet.cs \
    FooFileInfo.cs \
    FooSync.cs \
    FooSyncUrl.cs \
    FooTree.cs \
    NativeMethods.cs \
    NetClient.cs \
    NetUtil.cs \
    Options.cs \
    RepositoryState.cs \
    XmlConfigLoader.cs \
    Properties/AssemblyInfo.cs

REFERENCES=

#BINPLACE=\
    RepositoryConfig.xsd

LIBDIR=../lib

ifeq ($(REFERENCES),)
	REFSFLAG=
else
	REFSFLAG=-references:$(REFERENCES)
endif

all: $(TARGET)

$(TARGET): $(SOURCES)
	[ ! -x $(TARGETDEST) ] && mkdir $(TARGETDEST) || true
	$(MCS) -target:$(TARGETTYPE) -out:$(TARGET) -sdk:$(SDK) $(REFSFLAG) $(DEFINES) $(SOURCES)
#	cp -v $(BINPLACE) $(TARGETDEST)

clean:
	rm -f $(TARGET)
