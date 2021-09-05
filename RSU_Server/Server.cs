using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace RSU_Server
{
    public static class Server
    {
        public static Game game;

        private static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public static int maxClients = 6;
        public static Client[] clients;

        public static string name = "Server";
        public static bool inGame = false;

        static void Main(string[] args)
        {
            Console.WriteLine("Creating Local Server...");
            CreateServer();

        }

        static void CreateServer()
        {
            game = new Game(); //creates an instance of the game class to run the game off

            clients = new Client[maxClients]; //remember that the is already connected
            //game.CreateMap();

            StartServer();
            //game.InitialiseAndSpawn();
            inGame = true;
        }

        public static void StartServer()
        {
            Console.WriteLine("Starting Server");
            socket.Bind(new IPEndPoint(IPAddress.Any, 9000));
            socket.Listen(128);

            Console.WriteLine("Waiting for a connection...");

            socket.BeginAccept(new AsyncCallback(TPCCallback), null);
        }


        static void TPCCallback(IAsyncResult result)
        {
            int index = ServerAvailability();

            if (index < 0)
            {
                Console.WriteLine("Server Full");
                //TODO: Disconnect Client
            }

            else
            {
                Client client = new Client(socket.EndAccept(result), index);
                clients[index] = client;
                Console.WriteLine("Client Connected.");
                client.InitialiseDataTransfer();
                LookForMoreConnections();
            }
        }

        static void LookForMoreConnections() { socket.BeginAccept(new AsyncCallback(TPCCallback), null); }

        static int ServerAvailability()
        {
            for (int i = 0; i < clients.Length; i++)
            {
                if (clients[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
