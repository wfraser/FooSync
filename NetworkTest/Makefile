TARGETNAME=NetworkTest.exe
TARGETTYPE=exe
TARGETDEST=../lib
TARGET=$(TARGETDEST)/$(TARGETNAME)

MCS=dmcs -debug+
SDK=4

SOURCES=\
    Program.cs \
    Properties/AssemblyInfo.cs

REFERENCES=\
    FooSyncEngine.dll

LIBDIR=../lib

ifeq ($(REFERENCES),)
	REFSFLAG=
else
	REFSFLAG=-reference:$(REFERENCES)
endif

all: $(TARGET)

$(TARGET): $(SOURCES)
	[ ! -x $(TARGETDEST) ] && mkdir $(TARGETDEST) || true
	$(MCS) -target:$(TARGETTYPE) -out:$(TARGET) -sdk:$(SDK) -lib:$(LIBDIR) $(REFSFLAG) $(SOURCES)

clean:
	rm -f $(TARGET)
