#!/usr/bin/env bash
set -eu -o pipefail


if [[ `dpkg-query --no-pager --showformat='${db:Status-Status}\n' -W libicu67:amd64 | grep -c installed` -le 0 ]]; then
    if  [[ "$EUID" -ne 0 ]] || [[ $(/usr/bin/id -u) -ne 0 ]]; then
        echo "$0 is not running as root. This script requires sudo privileges to run."
        exit 2
    fi
    echo Installing pre-requisites...
    PACKAGES_NEEDED="libicu67:amd64"
    apt-get update -y
    apt-get install -y $PACKAGES_NEEDED
fi

# todo: wget file(s)
chmod +x ExtravaWallSetup
./ExtravaWallSetup

sudo -k