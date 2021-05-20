bin_PROGRAMS = banana_server 

BOOST_LIB = -L/usr/local/Cellar/boost/1.75.0_3/lib -lboost_unit_test_framework
OPENSSL_LIB = -L/usr/local/opt/openssl@1.1/lib -lssl -lcrypto 

banana_server_SOURCES = banana_server.cpp banana_server.h banana_message
banana_server_LDADD=$(BOOST_LIB) $(OPENSSL_LIB)

banana_message_SOURCES = banana_message.cpp banana_message.h 
banana_message_LDADD=$(BOOST_LIB) $(OPENSSL_LIB)

AM_CPPFLAGS = -I/usr/local/Cellar/boost/1.75.0_3/include -I/usr/local/opt/openssl@1.1/include -I$(PWD)
AM_CFLAGS = -mthreads -O0 -w -Wno-invalid-offsetof -Wformat
AM_LDFLAGS = -L/usr/local/Cellar/boost/1.75.0_3/lib -L/usr/local/opt/openssl@1.1/lib