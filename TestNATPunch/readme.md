Some Details you need to know:

- some routers refuse to do upnp if the WAN address is [BOGON](https://en.wikipedia.org/wiki/Bogon_filtering).  For the pfsense routers in my testbed this can be overcome by filling in the **Override WAN address** setting with a valid public ip address.
- example command line to run the upnp test and pass it a different router ip
```dotnet test --filter Name~UPnP -- TestRunParameters.Parameter\(name=\"DefaultRouter\",value=\"192.168.1.1\"\)```