#!/bin/sh

MONO_SERVICE=/usr/bin/mono-service2
PREFIX=/usr/local

if [ ! -x $MONO_SERVICE ]; then
    echo "Fata: this program requires Mono"
    exit -1
fi

$MONO_SERVICE $PREFIX/lib/foosync/FooSync.Daemon.exe "$@"

