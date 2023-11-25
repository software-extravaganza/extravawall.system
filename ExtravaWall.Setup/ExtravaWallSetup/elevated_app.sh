#!/bin/bash
echo elevatedApp_2d39d0f0-6277-48d7-9464-873c4c641283; pkexec env DISPLAY=$DISPLAY XAUTHORITY=$XAUTHORITY dotnet /home/phil/src/extrava/extravawall/ExtravaWallSetup/bin/Debug/net7.0/ExtravaWallSetup.dll & PID=\$!; echo 'Application started with PID:' \$PID; echo \$PID > pid.txt; sleep infinity
