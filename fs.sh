#!/bin/sh

PREFIX=/usr/local
MONO=/usr/bin/mono

if [ ! -x $MONO ]; then
    echo "Fatal: this program requires Mono"
    exit -1
fi

exec $MONO $PREFIX/lib/foosync/fs.exe "$@"

