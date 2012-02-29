#!/bin/sh

PREFIX=/usr/local

if [ `id -u` != "0" ]; then
    echo "This script must be run as root."
    exit -1
fi

mkdir -p $PREFIX/lib/foosync
mkdir -p $PREFIX/bin

cp -v lib/* $PREFIX/lib/foosync
cp -v fs.sh $PREFIX/bin/fs

