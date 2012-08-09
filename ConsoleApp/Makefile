TARGETNAME=fs.exe
TARGETTYPE=exe
TARGETDEST=../lib
TARGET=$(TARGETDEST)/$(TARGETNAME)

MCS=dmcs -debug+
SDK=4

SOURCES=\
    Program.cs \
    ProgramArguments.cs \
    SyncGroupConfig.cs \
    Properties/AssemblyInfo.cs

REFERENCES=\
    FooSyncEngine.dll

BINPLACE=\
    SyncGroupConfig.xsd

LIBDIR=../lib

ifeq ($(REFERENCES),)
	REFSFLAG=
else
	REFSFLAG=-reference:$(REFERENCES)
endif

all: $(TARGET)

$(TARGET): $(SOURCES)
	[ ! -x $(TARGETDEST) ] && mkdir $(TARGETDEST) || true
	$(MCS) -target:$(TARGETTYPE) -out:$(TARGET) -sdk:$(SDK) -lib:$(LIBDIR) $(REFSFLAG) $(DEFINES) $(SOURCES)
	cp -v $(BINPLACE) $(TARGETDEST)

clean:
	rm -f $(TARGET)
