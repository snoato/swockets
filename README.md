![swockets logo](https://github.com/snoato/swockets/blob/master/misc/swockets_logo.png)
# swockets
swockets is a socket wrapper library intended to make it easy to set up a server and corresponding clients, while still giving the user of the library room for individualizing.

swockets is created to send and receive JSON messages encoded as UTF8 strings. swockets takes care of message fragmentation and automatically stitches messages together. It also takes care of managing the connected clients in server mode, threading, and so on.

With the use of your own handler you can insert your own code at various places. You can add the handshake of your own protocol, write functions that are executed whenever a message is received, a client disconnects, connection drops and when the handshake is unsuccessful. 

##Availablity
swockets is available for Python (2.7), C# (mono compatible), C++ (only in client mode) and will be released for Rust, Ruby and Swift. 

##Usage
!todo!
