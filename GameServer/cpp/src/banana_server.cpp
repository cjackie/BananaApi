#include <iostream>
#include <cstdint>
#include <thread>
#include <csignal>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <stdio.h>
#include <fcntl.h>

#include <banana_message.h>
#include <serialize.h>
#include "util.h"

class ConnectServerRequest {

public:
    ConnectServerRequest(){
        std::memset(username_, 0, 256);
    }

    IMPLEMENT_SERIALIZE(
        READWRITE(event_type_);
        READWRITE(magic_number_);
        READWRITE(auth_type_);
        READWRITE(FLATDATA(username_));)

    uint16_t event_type_;
    uint32_t magic_number_;
    char auth_type_;
    char username_[256];
};

void SignalCallbackHandler(int signum)
{
    std::cout << "Caught signal " << signum << std::endl;
    // Terminate program
    exit(signum);
}

void HandleConnection(int client_sfd) {
    std::cout
        << "Handle a connection: " << client_sfd << std::endl;

    // Set the socket to be in blocking mode.
    int opts = fcntl(client_sfd, F_GETFD);
    opts = opts & (~O_NONBLOCK);
    if (fcntl(client_sfd, F_SETFD, opts) == -1) {
        std::cout << "Setting to blocking mode fails for client: " << client_sfd << std::endl;
        return ;
    }

    const int buffer_len = 256;
    char buffer[buffer_len];
    while (true) {
        int r = read(client_sfd, (void *)buffer, buffer_len);
        if (r == 0) {
            std::cout << "[sdf = " << client_sfd << "] End of connection" << std::endl;
            close(client_sfd);
            break;
        }
        if (r == -1) {
            std::cout << "read error" << std::endl;
            close(client_sfd);
            break;
        }

        std::basic_string<char> str(buffer, 0, r);
        std::cout << "[sdf = " << client_sfd << "] Received: " << str << std::endl;
    }
}

int main()
{
    signal(SIGINT, &SignalCallbackHandler);
    // Create a TCP socket.
    int sfd = socket(AF_INET, SOCK_STREAM, 0);
    if (sfd == -1) {
        std::cout << "Failed to create a socket." << std::endl;
        return -1;
    }

    struct sockaddr_in server;
    server.sin_family = AF_INET;
    server.sin_addr.s_addr = INADDR_ANY;
    server.sin_port = htons(8080);

    if (bind(sfd, (struct sockaddr*) &server, (socklen_t) sizeof(server)) == -1) {
        std::cout << "Failed to bind socket." << std::endl;
        return -1;
    }

    // backlog is maximum number of pending connections.
    if (listen(sfd, /*backlog=*/100) == -1) {
        std::cout << "listen fails." << std::endl;
        return -1;
    }

    // Set the socket to be in blocking mode.
    int opts = fcntl(sfd, F_GETFD);
    opts = opts & (~O_NONBLOCK);
    fcntl(sfd, F_SETFD, opts);

    std::vector<std::thread> connectionHandlers;
    // Accepting connections
    while (true)
    {
        int socket_len;
        struct sockaddr_in client;
        int client_sfd = accept(sfd, (sockaddr *)&client, (socklen_t *)&socket_len);
        if (client_sfd == -1) {
            std::cerr << "Failed to accept a client." <<  std::endl;            
            continue;
        }
        std::cout << "Accepted a connection: " << client_sfd << std::endl;

        std::thread handler(HandleConnection, client_sfd);
        connectionHandlers.emplace_back(std::move(handler));
    }

    CDataStream c;
    ConnectServerRequest request;
    request.event_type_ = 0x1434;
    request.magic_number_ = 0x3233333;
    request.auth_type_ = 'A';
    std::string username("myUsername");
    strcpy(request.username_, username.c_str());
    c << request;

    std::cout << HexStr(c.begin(), c.end(), true) << std::endl;

    return 0;
}
