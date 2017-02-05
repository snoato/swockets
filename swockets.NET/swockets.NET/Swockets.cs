using System;
using System.Net.Sockets;
using System.Net;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace swockets
{
	public class SwocketError : Exception
	{
		public SwocketError()
		{

		}

		public SwocketError(string value)
		{
		}
	}

	public class SwocketClientSocket
	{
		public Socket sock;
		public string address;

		public SwocketClientSocket(Socket sock, string address)
		{
			this.sock = sock;
			this.address = address;
		}
	}

	public class SwocketHandler
	{
		/*
		 * 
		 */
		public bool handshake(Socket sock)
		{
			return true;
		}

		/*
		 * 
		 */
		public void recv(JObject recvObj, SwocketClientSocket sock = null)
		{
			Console.WriteLine(recvObj);
		}

		/*
		 *
		 */
		public void connect(SwocketClientSocket sock = null)
		{
			Console.WriteLine("connect");
		}

		/*
		 * 
		 */
		public void disconnect(SwocketClientSocket sock = null)
		{
			Console.WriteLine("disconnect");
		}

		/*
		 * 
		 */
		public void handshake_unsuccessful()
		{
			Console.WriteLine("handshake unsuccessful");
		}
	}

	public enum SwocketMode
	{
		ISSERVER, ISCLIENT
	}

	public class Swockets
	{
		private SwocketHandler handler;
		private SwocketMode mode;
		private Socket sock;
		private Thread serverConnectionThread;
		private bool RUNNING = true;
		private List<SwocketClientSocket> clients;

		public Swockets(SwocketMode mode, SwocketHandler handler, string host, int port = 6666, int backlog = 1)
		{
			this.mode = mode;
			this.handler = handler;


			//initialize as Swockets Server
			if (this.mode == SwocketMode.ISSERVER)
			{
				IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
				sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				sock.Bind(localEndPoint);
				sock.Listen(backlog);

				clients = new List<SwocketClientSocket>();

				serverConnectionThread = new Thread(server_connection_thread);
				serverConnectionThread.Start();
			}
			//initialize as Swockets Client
			else
			{
				IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(host), port);
				sock = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

				sock.Connect(ipe);

				if (sock.Connected)
				{
					client_negotiate();
				}
			}
		}

		/*
		 * 
		 */
		public void stop()
		{
			RUNNING = false;

			if (mode == SwocketMode.ISSERVER)
			{
				foreach (SwocketClientSocket client in clients)
				{
					client.sock.Dispose();
				}
			}
			sock.Dispose();
		}

		/*
		 * 
		 */
		private void server_connection_thread()
		{
			while (RUNNING)
			{
				Socket handler = sock.Accept();
				server_negotiate(handler, ((IPEndPoint)handler.RemoteEndPoint).Address.ToString());
			}
		}

		/*
		 * 
		 */
		private void server_negotiate(Socket clientsocket, string address)
		{
			if (handler.handshake(clientsocket))
			{
				SwocketClientSocket client = new SwocketClientSocket(clientsocket, address);
				handler.connect(client);
				clients.Add(client);

				Thread t = new Thread(() => receive_thread(clientsocket, client));
				t.Start();
			}
			else
			{
				clientsocket.Dispose();
				handler.handshake_unsuccessful();
			}
		}

		/*
		 * 
		 */
		private void client_negotiate()
		{
			if (handler.handshake(sock))
			{
				handler.connect(new SwocketClientSocket(sock, ""));
				Thread t = new Thread(() => receive_thread(sock));
				t.Start();
			}
			else
			{
				sock.Dispose();
				handler.handshake_unsuccessful();
			}

		}

		/*
		 * 
		 */
		private JObject receive_one_message(Socket sock, SwocketClientSocket clsock = null)
		{
			string recvdMsg = "";

			while (true)
			{
				try
				{
					Byte[] bytesReceived = new Byte[1024];

					int bytes = sock.Receive(bytesReceived);
					recvdMsg += Encoding.ASCII.GetString(bytesReceived, 0, bytes);

					JObject recvdObj = JObject.Parse(recvdMsg);
					recvdMsg = "";

					return recvdObj;
				}
				catch (Newtonsoft.Json.JsonReaderException)
				{
					//continue receiving
				}
				catch (Exception e)
				{
					Console.Error.WriteLine("Error while receiving message: " + e);
					if (mode == SwocketMode.ISSERVER)
					{
						clients.Remove(clsock);
						sock.Dispose();
						handler.disconnect(clsock);
					}
					else
					{
						this.sock.Dispose();
						handler.disconnect();
					}
					return null;
				}
			}

		}

		/*
		 * 
		 */
		private void receive_thread(Socket sock, SwocketClientSocket clsock = null)
		{
			while (RUNNING)
			{
				JObject recvdObj = receive_one_message(sock, clsock);

				if (recvdObj == null)
				{
					return;
				}
				handler.recv(recvdObj, clsock);
			}
		}

		/*
		 * 
		 */
		public JObject receive(Socket sock)
		{
			return receive_one_message(sock);
		}

		/*
		 * 
		 */
		public void send(JObject msg, Socket sock = null, SwocketClientSocket clsock = null)
		{
			if (mode == SwocketMode.ISCLIENT)
			{
				sock = this.sock;
			}
			try
			{
				string sendStr = msg.ToString();

				if (sendStr != "")
				{
					sock.Send(Encoding.ASCII.GetBytes(msg.ToString() + new string(' ', (1024 - (System.Text.ASCIIEncoding.ASCII.GetByteCount(msg.ToString())) % 1024))));
				}
			}
			catch (Exception e)
			{
				Console.Error.WriteLine("Error while receiving message: " + e);
				if (mode == SwocketMode.ISSERVER)
				{
					clients.Remove(clsock);
					sock.Dispose();
					handler.disconnect(clsock);
				}
				else
				{
					this.sock.Dispose();
					handler.disconnect();
				}
			}
		}
	}
}
