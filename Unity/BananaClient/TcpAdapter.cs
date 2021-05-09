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
using UnityEngine;

namespace BananaClient
{
    public class TcpAdapter : INetworkAdapter
    {
        private string host;
        private ushort port;
        private Stream networkStream;
        private bool running;

        private string error = "";

        private ConcurrentQueue<object> sendingQueue = new ConcurrentQueue<object>();
        private Thread sendingThread;
        private ConcurrentQueue<object> receivingQueue = new ConcurrentQueue<object>();
        private Thread receivingThread;

        // True for debugging only.
        private bool SkipSsl = false;


        public TcpAdapter(string host, ushort port)
        {
            this.host = host;
            this.port = port;
            sendingQueue = new ConcurrentQueue<object>();
            receivingQueue = new ConcurrentQueue<object>();
            sendingThread = new Thread(new ThreadStart(this.Sending));
            receivingThread = new Thread(new ThreadStart(this.Receiving));
        }

        // For Unittest and debugging.
        public TcpAdapter(Stream networkStream) {
            this.networkStream = networkStream;
            sendingQueue = new ConcurrentQueue<object>();
            receivingQueue = new ConcurrentQueue<object>();
            sendingThread = new Thread(new ThreadStart(this.Sending));
            receivingThread = new Thread(new ThreadStart(this.Receiving));
            this.SkipSsl = true;
        }

        private void Sending()
        {
            try 
            {
                if (networkStream == null) 
                    networkStream = Connect(host, port, "commonName");

                byte[] buffer = new byte[NetworkMessage.MaxMessageSize];
                while (running && error == "")
                {
                    // Sending messages stored in the sending queue.
                    object request = null;
                    if (sendingQueue.Count > 0)
                        sendingQueue.TryDequeue(out request);

                    if (request == null) {
                        Thread.Sleep(0);
                        continue;
                    }

                    int offset = 0;
                    if (request.GetType() == typeof(ConnectServerRequest))
                    {
                        ConnectServerRequest connectServerRequest = (ConnectServerRequest) request;
                        offset = writePrimitiveBytes(buffer, offset, connectServerRequest.eventType);
                        offset = writePrimitiveBytes(buffer, offset, connectServerRequest.magicNumber);
                        offset = writePrimitiveBytes(buffer, offset, connectServerRequest.authType); 

                        offset = writeArrayBytes(buffer, offset, connectServerRequest.username);
                        offset = writeBytes(buffer, offset, Zeros(256 - connectServerRequest.username.Length * 2));

                        offset = writeArrayBytes(buffer, offset, connectServerRequest.password);
                        offset = writeBytes(buffer, offset, Zeros(256 - connectServerRequest.password.Length * 2));

                    } else {
                        throw new Exception("Not support request: " + request.GetType());
                    }
                    
                    networkStream.Write(buffer, 0, offset);
                    ZeroOut(buffer, offset);
                }      
            } 
            catch (Exception e) 
            {
                error = e.ToString();
            } 
            finally 
            {
                if (networkStream != null)
                {
                    networkStream.Close();
                }
            }
        }

        private byte[] Zeros(int n) {
            if (n < 0) {
                throw new Exception("Negative byte array");
            }
            byte[] zeros = new byte[n];
            return zeros;
        }

        private void ZeroOut(byte[] buffer, int len) {
            for (int i = 0; i < len; i++)
                buffer[i] = 0x00;
        }

        private int writeBytes(byte[] dest, int offset, byte[] data)
        {
            if (offset + data.Length >= dest.Length) {
                throw new Exception("data too big for dest.");
            }

            for (int i = 0; i < data.Length; i++) {
                dest[offset + i] = data[i];
            }
            return offset + data.Length;
        }

        private int writeArrayBytes(byte[] dest, int offset, char[] data) {
            for (int i = 0; i < data.Length; i++) {
                offset = writePrimitiveBytes(dest, offset, data[i]);
            }
            return offset;
        }

        private int writePrimitiveBytes(byte[] dest, int offset, char data) {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return writeBytes(dest, offset, bytes);
        }

        private int writePrimitiveBytes(byte[] dest, int offset, ushort data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return writeBytes(dest, offset, bytes);
        }

        private int writePrimitiveBytes(byte[] dest, int offset, uint data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return writeBytes(dest, offset, bytes);
        }

        private void Receiving() {
            try 
            {
                // Waiting for sendingThread to establish the connnection.
                while (running && error == "" && networkStream == null)
                    Thread.Sleep(0);

                ushort eventType = 0;
                while (running && error == "") {
                    try
                    {
                        eventType = ReadUInt16(networkStream);
                    }
                    catch (Exception)
                    {
                        Thread.Sleep(100);
                        continue;
                    }
                    
                    if (eventType == NetworkMessage.EventType_ConnectServer) 
                    {
                        ConnectServerResponse response;
                        response.eventType = eventType;
                        response.sessionId = ReadBytes(networkStream, 20);
                        response.userId = ReadUInt64(networkStream);
                        response.errorCode = ReadByte(networkStream);
                        Byte[] buf = new byte[520-31];
                        networkStream.Read(buf, 0, buf.Length); // dump redundant bytes
                        receivingQueue.Enqueue(response);
                    } 
                    else 
                    {
                        //throw new Exception("Unknown error: " + this.error);
                        Thread.Sleep(1000);
                        continue;
                    }
                }

            } catch (Exception e) {
                this.error = e.ToString();
                throw e;
            }
        }

        public byte[] ReadBytes(Stream stream, int typeSize) {
            byte[] data = new byte[typeSize];
            for (int i = 0; i < typeSize; i++) {
                int b = stream.ReadByte();
                if (b == -1)
                {
                    throw new Exception("byte is -1");
                }
                data[i] = (byte) b;
            }
            // The network stream is agreed (assumed) on Big-Endian. So we
            // reverse data if the system is Little-Endian. 
            if (BitConverter.IsLittleEndian)
                Array.Reverse(data);
            return data;
        }

        public ushort ReadUInt16(Stream stream)
        {
            return BitConverter.ToUInt16(ReadBytes(stream, 2), 0);
        }

        public byte ReadByte(Stream stream) {
            int b = stream.ReadByte();
            if (b == -1)
                throw new Exception("byte is -1");
            return (byte)b;
        }

        public ulong ReadUInt64(Stream stream)
        {
            return BitConverter.ToUInt64(ReadBytes(stream, 8), 0);
        }

        public short ReadInt16(Stream stream)
        {
            return BitConverter.ToInt16(ReadBytes(stream, 2), 0);
        }

        public void Send(object request) {
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

        public object Receive() {
            Console.WriteLine("Receive");
            if (receivingQueue.Count > 100)
            {
                throw new IndexOutOfRangeException("Too many responses queued...");
            }
            if (error != "") {
                throw new Exception("Underlying network error: " + error);
            }

            object response;
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
