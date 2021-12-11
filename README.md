
### Goal

Create a .NET Standard 2.0 framework for UDP communications that include:
- Create a UDP connection channel between two applications
- Allow sending optionally "reliable" messages that requests ACK or retransmits
- Track and update latency/RTT between the applications
- 


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
- 2021-12-09: Protocol research
  - IP (RFC 760, but wikipedia was more helfpul)
    - Good info here: <https://notes.shichao.io/tcpv1/ch10/>
    - Packets larger than the path's MTU will fragment.
	- If any fragments are lost, entire packet is lost.
	- IPv4 header is 20 bytes. MTU is typically 1500.
  - UDP (RFC 8085)
    - Congestion control: Ensure the transport protocol is applying
	- Latency: Maybe add option for how to track?
	- TFRC: Rate control, worth implementing? Useful for large bulk transfers
	- Low Data-Volume: Should not send on average more than one datagram per RTT. This should be an option.
	- Message size limits and how to handle. RFC recommends not sending packets >MTU, and references Path MTU Discovery (RFC 1191) and Packetization Layer Path MTU Discovery (RFC 4821). This may be something worth considering later.
	- Reliability: 2 min is standard Maximum Segment Lifetime in TCP, so plan on receiving duplicate packets up to 2 min later.
	- Rate: Should have a preferred rate and mechanism to back off? Future goal?
  - TCP Retransmission Timer RFC 6298: Contains guidance on how to handle calculating retransmission timer.
- 2021-12-10: Worked through getting UDP communications going in C#. Researched and implemented .NET thread pool functionality.
