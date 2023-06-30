#!/sbin/openrc-run
#copyright 2021 Gentoo Authors

# Distributed under the terms of the GNU General Public License v2

name="Cerberus"
description="https://github.com/b1tcr4sh/cerberus"
command="/path/to/ecex"
command_args="${service_args}"
 #command_user="user:user"

depend() {
	need net, redis
}
