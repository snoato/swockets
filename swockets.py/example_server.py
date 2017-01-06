from swockets import swockets, SwocketError, SwocketClientSocket, SwocketHandler

handle = SwocketHandler()
server = swockets(swockets.ISSERVER, handle)
handle.sock = server

while(True):
	user_input = {"message":raw_input("")}

	if len(server.clients) > 0:
		server.send(user_input, server.clients[0], server.clients[0].sock)