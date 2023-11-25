#!/bin/sh

[ "$UID" -eq 0 ] || exec sudo bash "$0" "$@"

TTY=$(tty)
STD_OUT_DEST=$(readlink /proc/$$/fd/1)  # Find where stdout is directed

# Decide where to tee based on whether the script's stdout is the same as TTY
if [ "$TTY" = "$STD_OUT_DEST" ]; then
    TEE_DEST=/dev/null
else
    TEE_DEST=$TTY
fi

CURRENT_DIR=$(pwd)
PARENT_DIR="$(dirname "$CURRENT_DIR")"
LOGS_DIR="$CURRENT_DIR/logs"

setcap cap_net_admin,cap_net_raw+ep $PARENT_DIR/ExtravaWallSetup/bin/Debug/net7.0/linux-x64/ExtravaWallSetup

{
    echo "Starting Packet Routing in 5 seconds" | tee $TEE_DEST
    sleep 5
    iptables -A INPUT -j NFQUEUE --queue-num 0
    iptables -A OUTPUT -j NFQUEUE --queue-num 0
    echo "Started Packet Routing" | tee $TEE_DEST
} &

{
    for ((n=0;n<15;n++))
    do
        iptables -vL -n
        ping 1.1.1.1 -c 1
        sleep 1
    done
   
} &

{
    echo "Stopping Packet Routing in 30 seconds" | tee $TEE_DEST
    sleep 30
    iptables -D INPUT -j NFQUEUE --queue-num 0
    iptables -D OUTPUT -j NFQUEUE --queue-num 0
    echo "Stopped Packet Routing" | tee $TEE_DEST
} &

wait
