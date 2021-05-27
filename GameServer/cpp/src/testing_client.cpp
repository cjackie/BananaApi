#include <iostream>
#include <cstdint>
#include <thread>
#include <csignal>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <unistd.h>

int main() {
    int sfd = socket(AF_INET, SOCK_STREAM, 0);

    sockaddr_in remote;
    remote.sin_family = AF_INET;
    remote.sin_addr.s_addr = INADDR_ANY;
    remote.sin_port = htons(8080);

    if (connect(sfd, (sockaddr*) &remote, sizeof(remote)) == -1) {
        std::cout << "connection fails" << std::endl;
        return 1;
    }

    char buffer[256] = "hello world\n";
    int written = write(sfd, (void*)buffer, strlen(buffer));
    std::cout << "Write: " << written << std::endl;

    close(sfd);
}