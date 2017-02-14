#!/usr/bin/env python

"""
swockets by Daniel Swoboda (@snoato, swobo.space)
"""

import socket
import threading
import json
import select
import sys

#Exception for swockets
class SwocketError(IOError):
	def __init__(self, value):
		self.value = value

	def __str__(self):
		return self.value

#Class to store necessary client data for swockets in server mode
class SwocketClientSocket():
	pass

#Basic Handler as it can be used for swockets
class SwocketHandler(object):
	def __init__(self):
		self.sock = None
		self.connected = None

	#if in server mode, sock = SwocketClientSocket of connecting socket
	def handshake(self, sock):
		return True

	#if in server mode, sock = SwocketClientSocket of sending socket
	def recv(self, recvObj, sock = None):
		print recvObj

	#if in server mode, sock = SwocketClientSocket of disconnected socket
	def disconnect(self, sock = None):
		print "disconnect"

	def connect(self, sock):
		print "connect"

	def handshake_unsuccessful(self):
		print "handshake unsuccessful"


#Swockets, the Sw(obo - S)ockets
class swockets:
	ISSERVER = 1
	ISCLIENT = 2
	RUNNING = True

	def __init__(self, mode, handler, host=None, port=6666, backlog=1):
		self.handler = handler
		self.mode = mode
		self.handler.sock = self

		if mode == swockets.ISSERVER:
			self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
			self.sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1) 
			self.sock.bind(('', port))
			self.sock.listen(backlog)
			self.clients = []

			t=threading.Thread(target=self.server_connection_thread, args=())
			t.daemon = True  
			t.start()

		elif mode == swockets.ISCLIENT:
			self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
			self.sock.connect((host, port))

			self.client_negotiate()
		else:
			raise ValueError("You must provide a correct mode")

	def __del__(self):
		self.stop()

	#closes all connections and stops all running loops
	def stop(self):
		self.RUNNING = False
		if self.mode == swockets.ISSERVER:
			for client in self.clients:
				client.sock.close()
				
		self.sock.close()

	#thread that accepts new connections for server
	def server_connection_thread(self):
		while self.RUNNING:
			(clientsocket, address) = self.sock.accept()
			self.server_negotiate(clientsocket, address)

	#server negotiating function
	def server_negotiate(self, clientsocket, address):
		if self.handler.handshake(clientsocket):
			client = SwocketClientSocket()
			client.sock = clientsocket
			client.address = address
			self.clients.append(client)
			self.handler.connect(clientsocket)

			t=threading.Thread(target=self.receive_thread, args=(client, client.sock))
			t.daemon = True  
			t.start()
		else:
			clientsocket.close()
			self.handler.handshake_unsuccessful()

	#client negotiating function
	def client_negotiate(self):
		if self.handler.handshake(self.sock):
			self.handler.connect(self.sock)
			t=threading.Thread(target=self.receive_thread, args=(self.sock, self.sock))
			t.daemon = True  
			t.start()
		else:
			self.sock.close()
			self.handler.handshake_unsuccessful()

	#receives one json formatted message between two swockets
	def receive_one_message(self, sock, ssock):
		recvdStr = ""
		recvdMsg = ""

		while True:
			try:
				recvdStr = ssock.recv(1024)

				if recvdStr == "":
					if self.mode == swockets.ISSERVER:
						self.handler.disconnect(sock)
						ssock.close()
						self.clients.remove(sock)
						return None
					else: 
						self.handler.disconnect()
						self.sock.close()
						return None
				else:
					recvdMsg+=recvdStr

				recvdObj = json.loads(recvdMsg)
				recvdMsg = ""

				return recvdObj
			except ValueError:
				pass
			except socket.error:
				if self.mode == swockets.ISSERVER:
					ssock.close()
					try:
						self.handler.disconnect(sock)
						self.clients.remove(sock)
					except:
						pass
					return None
				else:
					self.handler.disconnect()
					self.sock.close()
					return None

	#thread that runs in background waiting for messages and forwarding
	#them to the handler for further processing by user
	def receive_thread(self, sock = None, ssock = None):
		if self.mode == swockets.ISCLIENT:
			sock = self.sock
			ssock = self.sock

		while self.RUNNING:
			recvdObj = self.receive_one_message(sock, ssock)

			if recvdObj == None:
				return
			self.handler.recv(recvdObj, sock)

		sock.sock.close()

	#user callable receive function, provide sock if in server mode
	def receive(self, sock = None, ssock = None):
		if self.mode == swockets.ISCLIENT:
			ssock = self.sock

		return self.receive_one_message(sock, ssock)

	#user callable send function, provide sock if in server mode, 
	#msg has to be json object
	#sock and ssock have to be given in server mode
	def send(self, msg, sock = None, ssock = None):
		try:	
			if self.mode == swockets.ISCLIENT:
				ssock = self.sock

			sendStr = json.dumps(msg)

			if sendStr != "":
				sendStr += ' ' * (1024-(len(sendStr.encode("utf8")) % 1024))
				ssock.sendall(sendStr)
		except socket.error:
			if self.mode == swockets.ISCLIENT:
				self.handler.disconnect()
			else:
				self.handler.disconnect(sock)
		except IndexError:
			if sock not in self.clients:
				SwocketError("Client not available")

