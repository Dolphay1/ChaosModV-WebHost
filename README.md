## What is Chaos Mod V?

A replica of the chaos mods found on previous GTA games for GTA V.

See the [GTA5-Mods mod page](https://www.gta5-mods.com/scripts/chaos-mod-v-beta) and [GitHub page](https://github.com/gta-chaos-mod/ChaosModV) for more information and instructions on how to install it.

Also make sure to check the [Wiki](https://github.com/gta-chaos-mod/ChaosModV/wiki)!

[![](https://discord.com/api/guilds/785656433529716757/widget.png)](https://discord.gg/w2tDeKVaF9)

# What is Chaos Mod V-WebHost
It is an altered fork of the original mod that allows you to host your own voting platform for people to vote with instead of using twitch, it has a built in version which allows for multivoting and single voting.

# Multivoting vs. Singlevoting

## Multivoting
Multivoting allows a single person to vote for more options than just one, it is recommended for smaller groups as its more reliably gives an answer everyone is ok with.

## Singlevoting
Single voting only lets each individual vote for one answer, and never lets them vote for a second one, the more traditional, and easier to understand voting system

# Random Effect Votable Option
This replaces the 4th voting option with a Random Effect option allowing users to pick this if they feel the other options aren't adequate.

# Using the mod as a host vs. setting up your own host

## Using the Mod as a Host
The mod comes incorporated with a system that allows you to vote for options without any other setup. To do this just disable the "Use a seperate host" option.

If you need to access the host from outside the current network you will have to port forward the ports used. The mod will always use 2 ports, the entered port + 1, meaning if you set the port to 3003 (by default) it will use the ports 3003 and 3004.

## Using a Separate Host
Using a separate host allows you to setup your own server to vote on and the mod will automatically call and request the parts needed to run.

To set this up you check the "Use a separate host" box. Then you put in your Host and Port. The directory used will be the path to access the file, and the request parameter will be the parameter used to request information.

Example:

http://request.com/api/?request=EXAMPLE

request.com would be the host
/api/ would be the directory
request would be the parameter

the ? and = are added automatically

EXAMPLE is the request made by the mod, this will usually be "result" as that is currently the only implemented request. 
