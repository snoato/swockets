from swockets import swockets, SwocketError, SwocketClientSocket, SwocketHandler
import thread

class BasicHandler(SwocketHandler):
	def __init__(self):
		SwocketBasicHandler.__init__(self)
		self.connected = True

	def disconnect(self):
		self.connected = False
		print "Server disconnected"

	def handshake_unsuccessful(self):
		self.connected = False
		print "Handshake unsuccessful"

handler = BasicHandler()
client = swockets(swockets.ISCLIENT, handler, 'localhost')

while(handler.connected):
	client.send({"message":raw_input("")})