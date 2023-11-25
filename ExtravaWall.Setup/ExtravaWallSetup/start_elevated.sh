#!/bin/bash
konsole -- bash -e "sudo dotnet ExtravaWallSetup.dll & PID=\$!; echo 'Application started with PID:' \$PID; sleep infinity" &
konsole_pid=$!

while true; do
  PID=$(pgrep -f ExtravaWallSetup)
  if [ -n "$PID" ]; then
    break
  fi

  if ! kill -0 $konsole_pid 2>/dev/null; then
    echo "Konsole process has exited."
    exit 1
  fi

  sleep 1
done

echo "Application started with PID: $PID"
echo $PID > pid.txt
