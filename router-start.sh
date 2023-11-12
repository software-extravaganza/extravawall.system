#!/bin/sh
sessname="routersess"

sudo echo "Starting router and monitor..."
# Create a new session named "$sessname", and run command
tmux new-session -d -s "$sessname"
tmux send-keys -t "$sessname" "htop" Enter
tmux select-pane -t "$sessname":0.0
tmux split-window -h -p 50 -t "$sessname" 
tmux send-keys -t "$sessname" "dmesg --follow" Enter
tmux select-pane -t "$sessname":1.0
tmux split-window -v -p 50 -t "$sessname" 
tmux send-keys -t "$sessname" "sudo insmod ExtravaCore.KernelModule/extrava.ko log_level=1 default_packet_response=2 force_icmp=0; sudo ./ExtravaWall.Watch/bin/release/net7.0/linux-x64/ExtravaWall.Watch" Enter


# Attach to session named "$sessname"
tmux attach -t "$sessname"