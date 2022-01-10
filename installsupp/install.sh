#!/bin/bash


declare INSTALL_ROOT='/opt/santesuite/santedb/www'
declare SUDO=''

set -o history -o histexpand 

exit_on_error() {
    exit_code=$1
    last_command=${@:2}
    if [ $exit_code -ne 0 ]; then
        >&2 echo "\"${last_command}\" command failed with exit code ${exit_code}."
        echo "Installation failed"; 

        exit $exit_code
    fi
}

read_yesno() {
    local inp=''
    while ! [[ "$inp" =~ ^[ynYN]$ ]]
    do
        read -p "$1 [y/n]:" inp
    done 

    eval $2="$inp"
}

install_mono() {
    local install=""
    read_yesno "You don't appear to have MONO installed, SanteDB dCDR Requires Mono - do you want me to install it?" install
    if [[ "$install" =~ ^[nN]$ ]]; then 
        echo "Won't install mono - SanteDB may not work properly!"
    else
		$SUDO apt update
        $SUDO apt install -y mono-complete || exit_on_error $? !!
        echo "Mono installed"
    fi
}


if (( $EUID != 0 )); then
    read_yesno "You don't appear to be running this script as root, do you mind if I use sudo?" useSudo
    if [[ "$useSudo" =~ ^[nN]$ ]] 
    then 
        echo "You must run this script as root"
        exit
    fi

    SUDO='sudo'
fi

mono --version || install_mono

echo -e "\n
SanteDB dCDR Web Access Gateway Installation Script
Copyright (c) 2021 - 2022 SanteSuite Inc. and the SanteSuite Contributors
Portions Copyright (C) 2019-2021 Fyfe Software Inc.
Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology

 Licensed under the Apache License, Version 2.0 (the "License"); you 
 may not use this file except in compliance with the License. You may 
 obtain a copy of the License at 
 
 http://www.apache.org/licenses/LICENSE-2.0 
 
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 License for the specific language governing permissions and limitations under 
 the License.
 
 "
 
read_yesno "Do you accept the terms of the LICENSE?" eula

if [[ "$eula" =~ ^[nN]$ ]] 
then 
    echo "You must accept the terms of the license agreement"
    exit
fi

read -p "Where would you like to install SanteDB dCDR? [Default: $INSTALL_ROOT]" installAlt

if [[ "$installAlt" =~ ^.{1,}$ ]]
then 
    $INSTALL_ROOT = $installAlt
fi

echo "Installing at $INSTALL_ROOT"
$SUDO mkdir -p $INSTALL_ROOT

echo "Copying Files"
$SUDO cp -rf * $INSTALL_ROOT

read_yesno "Do you want me to install SanteDB Web Access Gateway as a daemon?" daemon

if [[ "$daemon" =~ ^[yY]$ ]]
then 
    cat > /tmp/santedb-www.service <<EOF
[Unit]
Description=SanteDB dCDR Web Access Gateway

[Service]
Type=simple
RemainAfterExit=yes
ExecStart=/usr/bin/mono-service -d:$INSTALL_ROOT $INSTALL_ROOT/santedb-www.exe --daemon --console --dllForce
ExecStop=kill \`cat /tmp/santedb-www.exe.lock\`

[Install]
WantedBy=multi-user.target
EOF

    $SUDO mv /tmp/santedb.service /etc/systemd/system/santedb-www.service

    read_yesno "Do you want SanteDB Web Access Gateway to start when the system starts?" autostart
    if [[ "$autostart" =~ ^[Yy]$ ]]
    then 
        $SUDO systemctl enable santedb
    fi

    echo -e "\n

    SanteDB Web Access Gateway is now installed in $INSTALL_ROOT

    START SANTEDB: 
    systemctl start santedb-www

    STOP SANTEDB: 
    systemctl stop santedb-www
	
	Visit http://127.0.0.1:9200 to configure SanteDB Web Access Gateway
    "
	$SUDO systemctl start santedb-www
else 

    echo -e "\n

    SanteDB is now installed in $INSTALL_ROOT

    START SANTEDB: 
    sudo mono-service -d:$INSTALL_ROOT $INSTALL_ROOT/santedb-www.exe --console --daemon --dllForce

    STOP SANTEDB: 
    kill \`cat /tmp/santedb-www.exe.lock\`
	
	Visit http://127.0.0.1:9200 to configure SanteDB Web Access Gateway
    "

fi
