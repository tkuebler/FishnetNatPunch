# NAT Punchthrough for TugBoat / FishNet

### Dependency
https://github.com/RevenantX/LiteNetLib
## Proposed Approach

1. Create a standalone NAT Punchthrough Server based on LiteNetLib
1. create a client for testing
1. Create Tests against the punchthrough server that:
	- Test multiple clients
	- Test multiple session keys
1. Create a client library that aligns with the TugBoat transport
1. Create a Unity project that uses this library
1. embed this in TugBoat


## Getting started doing dev

1. The repo is organized as a Solution, with multiple projects.  It should import into any C# IDE without a problem.  
1. Please .gitignore your IDE poop
1. Run the server either in your IDE or via
	1. ```cd NATPunchServer```
	1. ```dotnet run NATPunchServer```
1. Same for NATPunchClient 
	- You must run at least two clients with the same token to see the punchthrough conversation happen

	
## Resources:
- [https://github.com/RevenantX/LiteNetLib/blob/master/LibSample/HolePunchServerTest.cs](https://github.com/RevenantX/LiteNetLib/blob/master/LibSample/HolePunchServerTest.cs)
- [https://anyconnect.com/stun-turn-ice/](https://anyconnect.com/stun-turn-ice/)
- [https://mirror-networking.gitbook.io/docs/transports/litenetlib-transport](https://mirror-networking.gitbook.io/docs/transports/litenetlib-transport)
