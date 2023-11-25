#!/bin/bash
output_path=$(dotnet publish -c Debug --no-build | grep -oP '\-\> .+\K(bin/.+)(?=/publish)')
app_dll="$PWD/$output_path/ExtravaWallSetup.dll"
#rider_exec=$(which rider)
DOTNET_ROOT=$(dirname $(which dotnet))

pkexec env DISPLAY=$DISPLAY XAUTHORITY=$XAUTHORITY dotnet $app_dll
