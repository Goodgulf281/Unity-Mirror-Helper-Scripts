# Unity Networking - Using a Linux Server in the Cloud for Hosting a Game - Part 2

## Updating Linux and Firewall Setup

Once you logged on to the Linux server using Putty use these commands to update Ubuntu:

```
sudo apt update
sudo apt upgrade
```

Enable the Firewall using these commands:

```
sudo ufw enable
sudo ufw allow 22
sudo ufw allow 7770/udp

```

Note that port 22 is used for SSH and 7770 is the default listening port for Fishnet networking. If you change the port in your NetworkManager you also need to change it here.
The same goes for adding multiple game servers on a single VM, add the ports you'll be adding in the startup scripts (below) here too.

## Uploading a build to your server

If you already uploaded a build to your server before then stop the startup service first using:

```
sudo systemctl stop wiba4.service
```

Where "wiba4.service" is the name of the startup service you use (see below).
Then copy the files and make the main game executable using:

```
chmod +x WiBa4.x86_64
```

Where "WiBa4.x86_64" is the filename Unity created for my build.


## Create the startup service file

First start the Nano editor:

```
sudo nano /etc/systemd/system/wiba4.service
```

You can replace name "wiba4" with your own game's name here. Then write (or copy using right mouse click) the following contents into the file:

```
[Unit]
Description=Wiba4 Auto Start
After=network.target

[Service]
ExecStart=/home/admindude/run-server.sh Island 7770 2
Restart=always
User=admindude
Group=adm
StandardOutput=append:/var/log/wiba4.log
StandardError=append:/var/log/wiba4error.log

[Install]
WantedBy=multi-user.target

```
Here you'll want to make another few replacements: change the name of your game and change the name of your admin user in both the User and ExecStart path properties.

Once the service script is ready you can enable it by using these commands:
```
sudo systemctl daemon-reload
sudo systemctl start wiba4.service
sudo systemctl enable wiba4.service

```

You can check the status of the service with this command:
```
sudo systemctl status wiba4.service
```

...and check the logs using the tail command:

```
tail -f /var/log/wiba4.log 
```

## The run-server script

The last element to be finished is creating the bash script run-server.sh. It is called from the service file and starts the game executable with some command line parameters:

```
#!/bin/bash

serverip=$( curl https://ipinfo.io/ip )
echo Server IP=$serverip

scene=$1
port=$2
serverid=$3

echo Scene=$scene
echo Port=$port
echo ServerID=$serverid

/home/admindude/Wiba4-linux-server/WiBa4.x86_64 -scene $scene -port $port -ipaddress $serverip -serverid $serverid

```

Again, make the changes based on the name of your game. This script uses a command to get the IP address of the server which is passes to the game executable. The other parameters (a scene name, listening port and a unique server id) are passed to this script by the service we created earlier. So this script rarely needs to change and we can run additional game servers by simply adding a new service file with some new parameters.

