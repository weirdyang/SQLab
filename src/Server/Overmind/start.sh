﻿#!/bin/bash
#chmod 777 start.sh & dos2unix start.sh
#chmod 777 start.sh
#dos2unix start.sh
#when executing .desktop files PATH is not set; so target Mono runtime temporarily 
#cd /home/ubuntu/SQServices/Server/Overmind/src
#echo "DNU Restore"
#/home/ubuntu/.dnx/runtimes/dnx-mono.1.0.0-rc1-update1/bin/dnu restore
#/home/ubuntu/.dnx/runtimes/dnx-coreclr-linux-x64.1.0.0-rc1-update1/bin/dnu restore
#echo "DNX Run"
#/home/ubuntu/.dnx/runtimes/dnx-mono.1.0.0-rc1-update1/bin/dnx run
#/home/ubuntu/.dnx/runtimes/dnx-coreclr-linux-x64.1.0.0-rc1-update1/bin/dnx run
#read

cd /home/ubuntu/SQ/Server/Overmind/src
echo "dotnet restore"
dotnet restore
cd /home/ubuntu/SQ/Server/Overmind/src/Server/Overmind
echo "dotnet run"
dotnet run
read


