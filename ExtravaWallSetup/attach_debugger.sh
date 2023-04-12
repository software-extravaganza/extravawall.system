#!/bin/bash
# Variables
unique_string="elevatedApp_$(uuidgen)"
output_path=$(dotnet publish -c Debug --no-build | grep -oP '\-\> .+\K(bin/.+)(?=/publish)')
app_dll="$PWD/$output_path/ExtravaWallSetup.dll"
port="12345"
elevated_script="elevated_app.sh"

echo "Will attach to: $app_dll"

# Create elevated_app.sh script
echo "#!/bin/bash" > $elevated_script
echo "echo $unique_string; pkexec env DISPLAY=\$DISPLAY XAUTHORITY=\$XAUTHORITY dotnet $app_dll & PID=\\\$!; echo 'Application started with PID:' \\\$PID; echo \\\$PID > pid.txt; sleep infinity" >> $elevated_script
chmod +x $elevated_script

# Start the elevated application in konsole
#konsole -e "bash -c './$elevated_script'" &
terminator -e "bash -c './$elevated_script'" &

# Find the correct process ID
while true; do
  PID=$(pgrep -f "$unique_string")
  if [ -n "$PID" ]; then
    break
  fi

  sleep 1
done

echo "Application started with PID: $PID"



#
## Create temporary run configuration file
#config_file="attach_elevated_debugger.run.xml"
#
#cat > $config_file <<EOL
#<component name="ProjectRunConfigurationManager">
#  <configuration default="false" name="Attach Elevated Debugger" type="AttachToProcessDebugConfigurationType" factoryName="Attach to Process">
#    <option name="mode" value="LOCAL" />
#    <option name="pid" value="$PID" />
#    <option name="host" value="127.0.0.1" />
#    <method v="2" />
#  </configuration>
#</component>
#EOL
#
## Start Rider with the temporary run configuration file
#RIDER_REVISION=$(snap list rider | awk '/rider/ {print $3}' | tr -d '()' )
#RIDER_PATH="/snap/rider/$RIDER_REVISION"
#"$RIDER_PATH/bin/rider.sh" --debugger-agent=transport=dt_socket,address=127.0.0.1:$port,server=y,suspend=n --run-configurations $config_file
#
## Clean up
#rm $config_file
