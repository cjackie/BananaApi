using System;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System.Net;
using System.Collections.Concurrent;
using System.Collections;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace BananaClient
{
    public class TcpAdapter : INetworkAdapter
    {
        private string host;
        private ushort port;
        private Stream networkStream;
        private bool running;

        private string error = "";

        private ConcurrentQueue<NetworkMessage> sendingQueue =
            new ConcurrentQueue<NetworkMessage>();
        private Thread sendingThread;
        private ConcurrentQueue<NetworkMessage> receivingQueue =
            new ConcurrentQueue<NetworkMessage>();
        private Thread receivingThread;

        // True for debugging only.
        private bool SkipSsl = false;
        private List<IMessageBytesFiller> messageBytesFillers;

        public TcpAdapter(string host, ushort port)
        {
            this.host = host;
            this.port = port;
            sendingQueue = new ConcurrentQueue<NetworkMessage>();
            receivingQueue = new ConcurrentQueue<NetworkMessage>();
            sendingThread = new Thread(new ThreadStart(this.Sending));
            receivingThread = new Thread(new ThreadStart(this.Receiving));
            messageBytesFillers = new List<IMessageBytesFiller>() {
                new ConnectServerResponseBytesFiller(),
                new SampleRoomsResponseBytesFiller()
            };
        }

        
        // For Unittest and debugging.
        public TcpAdapter(Stream networkStream, IMessageBytesFiller messageByteFiller) {
            this.networkStream = networkStream;
            sendingQueue = new ConcurrentQueue<NetworkMessage>();
            receivingQueue = new ConcurrentQueue<NetworkMessage>();
            sendingThread = new Thread(new ThreadStart(this.Sending));
            receivingThread = new Thread(new ThreadStart(this.Receiving));
            messageBytesFillers = new List<IMessageBytesFiller>() { messageByteFiller };
            this.SkipSsl = true;
        }

        private void Sending()
        {
            try {
                if (networkStream == null) 
                    networkStream = Connect(host, port, "commonName");

                byte[] buffer = new byte[NetworkMessage.MaxMessageSize];
                while (running && error == "")
                {
                    // Sending messages stored in the sending queue.
                    NetworkMessage request = null;
                    if (sendingQueue.Count > 0)
                        sendingQueue.TryDequeue(out request);

                    if (request == null) {
                        Thread.Sleep(0);
                        continue;
                    }
                    
                    byte[] data = SerializationUtils.SerializeRequest(request);                    
                    networkStream.Write(data, 0, data.Length);
                }      
            } catch (Exception e) {
                error = e.ToString();
            } finally {
                if (networkStream != null) {
                    networkStream.Close();
                }
            }
        }

        private void Receiving() {
            try {
                // Waiting for sendingThread to establish the connnection.
                while (running && error == "" && networkStream == null)
                    Thread.Sleep(0);

                IMessageBytesFiller filler = null;
                while (running && error == "") {
                    if (filler == null)
                    {
                        ushort eventType = BytesUtils.ReadUInt16(networkStream);
                        // Find filler.
                        foreach (var eachFiller in messageBytesFillers)
                        {
                            if (eachFiller.EventType() == eventType)
                            {
                                filler = eachFiller;
                            }
                        }
                        if (filler == null)
                        {
                            throw new Exception("Could not find a filler for " + eventType.ToString());
                        }

                        // Add the event type into the filler.
                        var raw = BitConverter.GetBytes(eventType);
                        filler.Fill(raw[0]);
                        filler.Fill(raw[1]);
                    } else
                    {
                        byte b = BytesUtils.ReadByte(networkStream);
                        byte[] buffer = filler.Fill(b);
                        if (buffer != null)
                        {
                            receivingQueue.Enqueue(SerializationUtils.DeserializeResponse(buffer));
                            // A new filler will be picked for the next message.
                            filler = null; 
                        }
                    }
                }
            } catch (Exception e) {
                this.error = e.ToString();
                throw e;
            }
        }

        public void Send(NetworkMessage request) {
            if (sendingQueue.Count > 100)
            {
                throw new IndexOutOfRangeException("Too many request queued...");
            }
            if (error != "")
            {
                throw new Exception("Underlying network error: " + error);
            }

            sendingQueue.Enqueue(request);
        }

        public NetworkMessage Receive() {
            Console.WriteLine("Receive");
            if (receivingQueue.Count > 100)
            {
                throw new IndexOutOfRangeException("Too many responses queued...");
            }
            if (error != "") {
                throw new Exception("Underlying network error: " + error);
            }

            NetworkMessage response;
            if (receivingQueue.TryDequeue(out response)) {
                return response;
            } else {
                return null;
            }
        }

        public void Start()
        {
            this.running = true;
            this.sendingThread.Start();
            this.receivingThread.Start();
        }

        public void Stop()
        {
            this.running = false;
        }

        // It is a blocking call, and returns a TCP socket.

        // serverName is the Common Name (AKA CN) that represents the server name protected by the SSL certificate. 
        // The common name is technically represented by the `commonName` field in the X.509 certificate specification,
        // e.g "*.google.com".
        private Stream Connect(string server, ushort port, string serverName)
        {
            TcpClient client = new TcpClient(server, port);
            Console.WriteLine("Client connected.");
            if (SkipSsl) {
                return client.GetStream();
            }

            // Create an SSL stream that will close the client's stream.
            SslStream sslStream = new SslStream(
                client.GetStream(),
                false,
                new RemoteCertificateValidationCallback(ValidateServerCertificate),
                null
                );
            // The server name must match the name on the server certificate.
            try
            {
                sslStream.AuthenticateAsClient(serverName);
                Console.WriteLine("AuthenticateAsClient succeeded");
                return sslStream;
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                client.Close();
                throw e;
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate.
        private static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers.
            return false;
        }

        public string Error { get => error; }
    }
}
