#include "swockets.hpp"
#include <iostream>
using namespace std;

int main() {
	SwocketHandler handle{};
	Swockets swocket{SwocketMode::ISCLIENT, &handle, "127.0.0.1"};

	while(true) {
		string s;
		getline(cin, s, '\n');
		string msg = "{\"message\":\""+s+"\"}";
		nlohmann::json j = nlohmann::json::parse(msg);
		swocket.send(j);
	}
	return 0;
}