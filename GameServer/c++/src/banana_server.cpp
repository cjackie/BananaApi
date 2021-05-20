#include <iostream>
#include <cstdint>

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

int main()
{
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
