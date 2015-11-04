This folder contains sample scripts for the 2015-16 DAD SESDAD project.
The *-config.txt files contain the operation and pub-sub network configuration.
The *-commands.txt files contain the test commands for the puppetmaster.
In the Remoting URLs in these scripts, the localhost address is used instead of the real machine's IP. In a real deployment, localhost should be replaced with the real IP addresses.
 
Test T1 creates a two node network with a pub and a sub. The sub subscribes to the topic the pub publishes, listens on events for 30 secs. and then unsubscribes.

Test T2 creates a three node tree with two pubs at the tree's top node and a sub at each of the child nodes. Both pubs publish two topics (p00-0, p00-1 p01-0 and p01-1) and each of the subs subscribes to a different topic from each of the pubs. After 30secs. they unsubscribe from one of the topics.
