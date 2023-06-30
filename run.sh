#!/bin/ash
# #!/bin/bash
# #!/sbin/openrc-run

git fetch
git pull
dotnet restore
dotnet build

cd ./bin/Debug/net7.0/
./cerebus-cs