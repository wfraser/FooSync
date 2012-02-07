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
    FooTree.cs \
    NativeMethods.cs \
    Options.cs \
    RepositoryConfig.cs \
    RepositoryConfigLoader.cs \
    RepositoryState.cs \
    Properties/AssemblyInfo.cs

REFERENCES=

BINPLACE=\
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
	$(MCS) -target:$(TARGETTYPE) -out:$(TARGET) -sdk:$(SDK) $(REFSFLAG) $(SOURCES)
	cp -v $(BINPLACE) $(TARGETDEST)

clean:
	rm -f $(TARGET)
