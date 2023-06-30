#!/bin/sh

git fetch
git pull
dotnet restore
dotnet build

cd ./bin/Debug/net7.0/
./cerebus-cs
