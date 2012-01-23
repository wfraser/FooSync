TARGETNAME=FooSync.dll
TARGETTYPE=library
TARGETDEST=../lib
TARGET=$(TARGETDEST)/$(TARGETNAME)

MCS=dmcs
SDK=4

SOURCES=\
    FooFileInfo.cs \
    FooSync.cs \
    FooTree.cs \
    Options.cs \
    RepositoryConfig.cs \
    RepositoryConfigLoader.cs \
    RepositoryState.cs

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
