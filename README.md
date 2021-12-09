
### Goal

Create a framework for reliable UDP communication between two applications adhering to .NET Standard 2.0.  
Future project to expand this to maintain a persistent state between applications using deltas.

### Background

- Prior to 2021-11: I became proficient with Unity, a graphics and scripting engine used primarily for making games. However, I wasn't able to find any **good** solutions for fine control of communications between application instances.
- 2021-11: Purchased and began reading *Development and Deployment of Multiplayer Online Games, Volume I*. `ISBN: 9783903213067`  
This led me to reading several fantastic articles at <https://gafferongames.com/>, most pertinent to this project:
  - <https://gafferongames.com/post/udp_vs_tcp/>
  - <https://gafferongames.com/post/sending_and_receiving_packets/>
  - <https://gafferongames.com/post/virtual_connection_over_udp/>
  - <https://gafferongames.com/post/client_server_connection/>
  - <https://gafferongames.com/post/reliable_ordered_messages/>

### Log

- 2021-12-07
  - Set up IDE: Visual Studio 2019
  - Read up on Unity standards: Supports up to the .NET Standard 2.0 specification. This is several years behind the base class library of today's .NET 6. However, this shouldn't be much of an issue since we're dealing with UDP network communications, which *shouldn't* have changed much.
  - Set up repository and C# projects
- 2021-12-08: Deep dive into System.Net and System.Net.Sockets libraries at <https://docs.microsoft.com/en-us/dotnet/api/>
