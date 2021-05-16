ifneq "$(BUILD)" "debug"
ifneq "$(BUILD)" "release"
BUILD=debug
endif
endif
ifeq "$(BUILD)" "debug"
DEBUGFLAGS=-g -gdwarf
endif

mkdir -p obj

INCLUDEPATHS=-I"/usr/local/Cellar/boost/1.75.0_3/include" -I"/usr/local/opt/openssl@1.1/include"
LIBPATHS=-L"/usr/local/Cellar/boost/1.75.0_3/lib" -L"/usr/local/opt/openssl@1.1/lib"
LIBS= \
 -l boost_unit_test_framework \
 -l ssl -l crypto 
CFLAGS=-mthreads -O0 -w -Wno-invalid-offsetof -Wformat $(DEBUGFLAGS) $(INCLUDEPATHS)
HEADERS=banana_message.h 

all: banana_server

obj/banana_message.o: banana_message.cpp $(HEADERS)
	g++ -c $(CFLAGS) -o $@ $<

obj/banana_server.o: banana_server.cpp $(HEADERS)
	g++ -c $(CFLAGS) -o $@ $<


OBJS=obj/banana_message.o obj/banana_server.o

banana_server: $(OBJS)
	g++ $(CFLAGS) -o $@ $(LIBPATHS) $(OBJS) $(LIBS)

clean:
	rm -rf obj/
