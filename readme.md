# NAT Punchthrough for TugBoat / FishNet

### Proposed Approach

1. Create a standalone NAT Punchthrough Server
1. Create Tests against the punchthrough server that:
	- Test multiple clients
	- Test multiple session keys
1. Create a client library that aligns with the TugBoat transport
1. Create a Unity project that uses this library
1. embed this in TugBoat


## Getting started doing dev

1. create a 'lib' directory in the root of this project
2. cd into the directory
3. git clone https://github.com/RevenantX/LiteNetLib.git

