///////////////////////////////////////////////////////////
//swockets.cpp - a swockets client implementation for c++
//
//by Daniel Swoboda
//MIT License
//2017
///////////////////////////////////////////////////////////
#ifndef SWOCKETS_CPP
#define SWOCKETS_CPP

#include "json.hpp"
#include <exception>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>
#include <netdb.h>
#include <thread>
#include <unistd.h>
#include <string.h>

enum  SwocketMode { ISSERVER, ISCLIENT};

class Swockets;

class SwocketHandler
{
public:
	SwocketHandler(){}
	~SwocketHandler(){}

	Swockets *swocket;
	
	virtual bool handshake(int sock) {
		std::cout << "handshaking" << std::endl;
		return true;
	}

	virtual void recv(nlohmann::json recvObj) {
		std::cout << recvObj << std::endl;
	}

	virtual void disconnect() {
		std::cout << "disconnected" << std::endl;
	}

	virtual void connect(int sock) {
		std::cout << "connected" << std::endl;
	}

	virtual void handshake_unsuccessful() {
		std::cout << "handshake unsuccessful" << std::endl;
	}
};

class Swockets
{
private:
	enum SwocketMode mode_;
	SwocketHandler *handle_;
	bool RUNNING = true;
	int sock_;
	std::thread receive_thread_;

	char recv_buffer[1024]; 
	int bytes_recvd{};
 
	void client_negotiate() {
		if(handle_->handshake(sock_)) {
			handle_->connect(sock_);
			
			receive_thread_ = std::thread(&Swockets::receive_thread, this, sock_);
		} else {
			handle_->handshake_unsuccessful();
			close(sock_);
		}
	}

	nlohmann::json receive_one_message(int sock) {
		nlohmann::json j;
		std::string msg{};
		while(RUNNING) {
			try {
				bytes_recvd = recv(sock,recv_buffer,1024, 0);
				if(bytes_recvd <= 0) {
					close(sock);
					handle_->disconnect();
					RUNNING = false;
				}
				msg+=recv_buffer;

				j = nlohmann::json::parse(msg);
				return j;
			} catch (const std::invalid_argument& e) {
			}
		}
		return j;
	} 

	void receive_thread(int sock) {
		while(RUNNING) {
			nlohmann::json recvdObj = receive_one_message(sock);
	
			handle_->recv(recvdObj);
		}
	}
public:
	Swockets(SwocketMode mode, SwocketHandler *handle, std::string host, int port = 6666, int backlog = 1) {
		mode_ = mode;
		handle_ = handle;
		handle_->swocket = this;

		if (mode_ == SwocketMode::ISSERVER) {
			throw("NOT YET SUPPORTED");
		} else {
        	sock_ = socket(AF_INET , SOCK_STREAM , 0);

    		struct sockaddr_in server;
			unsigned long addr;

			memset( &server, 0, sizeof (server));

			addr = inet_addr( host.c_str() );
			memcpy( (char *)&server.sin_addr, &addr, sizeof(addr));
			server.sin_family = AF_INET;
			server.sin_port = htons(port);
	
			if (connect(sock_,(struct sockaddr*)&server, sizeof(server)) < 0){
				throw("Connect error");
			}

			client_negotiate();
		}
	}

	nlohmann::json receive(int sock) {
		return receive_one_message(sock);
	}

	void send(nlohmann::json msg, int sock = -1) {
		if (mode_ == SwocketMode::ISCLIENT) {
			sock = sock_;
		}

		std::string sendStr = msg.dump(0);

		sendStr.append(std::string((sendStr.size()/1024+1)*1024-sendStr.size(), ' '));

		for(int i = 0; i < sendStr.size()/1024; i++) {
			std::string part = sendStr.substr(i*1024,1024);
			::send(sock, part.c_str(), 1024, 0);
		}


	    /*char* message = (char*)malloc(1024);
	    strcpy(message, msg);
	    for(i=lenghtOfMsg; i<1024; i++){
	        message[i] = ' ';
	    }
	    message[1024] = '\0';
	    if (send(sock, message, lenghtOfMsg, 0) == -1) {
	        perror("send_error");
	        running = false;
	    }
	    free(message);
	    message = 0;*/

		//send(sock, buf, len, flags);

	}

	~Swockets(){}
};
#endif