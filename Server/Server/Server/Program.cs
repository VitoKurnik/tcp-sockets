using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        private static readonly Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static readonly List<Socket> clientSockets = new List<Socket>();
        private const int BUFFER_SIZE = 2048;
        private const int PORT = 100;
        private static readonly byte[] buffer = new byte[BUFFER_SIZE];
        static void Main(string[] args)
        {
            Console.Title = "Server";
            ServerSetup();
            Console.ReadLine(); // closes the program when ENTER is pressed
            CloseAllSockets();
        }

        private static void ServerSetup()
        {
            Console.WriteLine("Server startup...");
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, PORT));
            serverSocket.Listen(0); // backlog
            serverSocket.BeginAccept(AcceptCallback, null); 
        }

        private static void CloseAllSockets()
        {
            foreach (Socket socket in clientSockets)
            {
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
        }

        private static void AcceptCallback(IAsyncResult AR)
        {
            Socket socket; 

            try
            {
                socket = serverSocket.EndAccept(AR);
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            clientSockets.Add(socket); 
            string ip = IPAddress.Loopback.ToString();
            Console.WriteLine("Client has connected (" + ip + ":" + PORT + ")");
            socket.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, socket);  
            serverSocket.BeginAccept(AcceptCallback, null); 
        }

        private static void ReceiveCallback (IAsyncResult AR)
        {
            Socket current = (Socket)AR.AsyncState;
            int received; 

            try
            {
                received = current.EndReceive(AR);
            }
            catch
            {
                Console.WriteLine("Client has disconnected");
                current.Close();
                clientSockets.Remove(current);
                return;
            }

            byte[] dataBuff = new byte[received];
            Array.Copy(buffer, dataBuff, received); 
            string text = Encoding.ASCII.GetString(dataBuff);
            Console.WriteLine("Received a message: " + text);
            if (text[0] == 'A')
            {
                string ip = IPAddress.Loopback.ToString();
                Console.WriteLine("Answer: Hello " + ip + ":" + PORT);
                byte[] data = Encoding.ASCII.GetBytes("Hello " + ip + ":" + PORT);
                current.Send(data);
            }
            else if (text[0] == 'B')
            {
                string datetime = DateTime.Now.ToString();
                Console.WriteLine("Answer: " + datetime);
                byte[] data = Encoding.ASCII.GetBytes(datetime);
                current.Send(data);

            }
            else if (text[0] == 'C')
            {
                string path = Directory.GetCurrentDirectory();
                Console.WriteLine("Answer: " + path);
                byte[] data = Encoding.ASCII.GetBytes(path);
                current.Send(data);

            }
            else if (text[0] == 'D')
            {           
                Console.Write("Answer: ");
                for (int i = 1; i < text.Length; i++)
                {
                    Console.Write(text[i]);
                    
                }
                Console.WriteLine();

                char[] toTrim = {'D'};
                string answer = text.Trim(toTrim);
                byte[] data = Encoding.ASCII.GetBytes(answer);
                current.Send(data);
               
            }
            else if (text == "exit")
            {
                current.Shutdown(SocketShutdown.Both);
                current.Close();
                clientSockets.Remove(current);
                Console.WriteLine("Client has disconnected");
                return;
            }
            else
            {
                byte[] data = Encoding.ASCII.GetBytes("Invalid argument");
                current.Send(data);
            }

            current.BeginReceive(buffer, 0, BUFFER_SIZE, SocketFlags.None, ReceiveCallback, current); 
        }

   
    }
}
