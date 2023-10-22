#!/bin/sh

[ "$(id -u)" -eq 0 ] || exec sudo bash "$0" "$@"

TTY=$(tty)
STD_OUT_DEST=$(readlink /proc/$$/fd/1)  # Find where stdout is directed

# sudo dnf install libnetfilter_queue libnetfilter_queue-devel
# Decide where to tee based on whether the script's stdout is the same as TTY
if [ "$TTY" = "$STD_OUT_DEST" ]; then
    TEE_DEST=/dev/null
else
    TEE_DEST=$TTY
fi

CURRENT_DIR=$(pwd)
PARENT_DIR="$(dirname "$CURRENT_DIR")"
LOGS_DIR="$CURRENT_DIR/logs"
LOGS_FILE_RUN_TIMESTAMP="$(date +%Y-%m-%d_%H-%M-%S)"

# Find the process IDs of ExtravaWall.Watch
pids=$(ps aux | grep ExtravaWall.Watch | awk '{print $2}')

if [ -z "$pids" ]; then
  echo "No processes found" | tee "$TEE_DEST"
fi

# Kill the processes
echo "$pids" | xargs kill -9

echo "Processes killed successfully" | tee "$TEE_DEST"

cd "$CURRENT_DIR" || exit 1
mkdir -p "$LOGS_DIR"
gcc -shared -o libnetlink.so libnetlink.c -fPIC
make clean
make
cd "$PARENT_DIR" || exit 1
./build.sh compile > /dev/null
cd "$CURRENT_DIR" || exit 1

{
  echo "Starting Ping in 1 second" | tee "$TEE_DEST"

  # n=0
  # while [ $n -lt 15 ]
  # do
  #   #iptables -vL -n >> $LOGS_DIR/iptables_dump_$LOGS_FILE_RUN_TIMESTAMP.log
  #   ping 1.1.1.1 -c 1 | tee -a "$LOGS_DIR"/iptables_dump_"$LOGS_FILE_RUN_TIMESTAMP".log | tee -a "$TEE_DEST"
  #   sleep 1
  #   n=$((n+1))
  # done
  echo "Stopped Ping" | tee "$TEE_DEST"
} &

{
  echo "Starting ExtravaWall.Watch in 10 seconds" | tee "$TEE_DEST"
  sleep 10
  cd "$PARENT_DIR"/ExtravaWall.Watch || exit 1
  dotnet run -- --no-root > /dev/null 2>&1 &
  echo "Started ExtravaWall.Watch" | tee "$TEE_DEST"
} &

{
  echo "Starting Extrava Kernel Module in 5 seconds" | tee "$TEE_DEST"
  sleep 5
  insmod "$CURRENT_DIR"/extrava.ko log_level=1 default_packet_response=2 force_icmp=0 | tee "$TEE_DEST"
  echo "Started Extrava Kernel Module" | tee "$TEE_DEST"
} &

{
  echo "Stopping ExtravaWall.Watch in 45 seconds" | tee "$TEE_DEST"
  sleep 45
  # Find the process IDs of ExtravaWall.Watch
  pids=$(ps aux | grep ExtravaWall.Watch | awk '{print $2}')

  if [ -z "$pids" ]; then
    echo "No processes found" | tee "$TEE_DEST"
  fi

  # Kill the processes
  echo "$pids" | xargs kill -9
} &

{
  echo "Stopping Extrava Kernel Module in 60 seconds" | tee "$TEE_DEST"
  sleep 60
  rmmod extrava | tee "$TEE_DEST"
  echo "Stopped Extrava Kernel Module" | tee "$TEE_DEST"
} &

wait

dmesg > "$LOGS_DIR"/dmesg_dump_"$LOGS_FILE_RUN_TIMESTAMP".log