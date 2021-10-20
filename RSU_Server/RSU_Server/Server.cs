using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

namespace RSU_Server
{
    public class Server
    {
        public string name = "Server";

        public Server_Game game;

        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        public int maxClients = 6;
        public int connectedPlayers = 0;
        public Server_Client[] clients;

        public int gameState;

        public const int
        GameState_Intermission = 1,
        GameState_InGame = 2,

            Client_Token = 1,
            Server_AckVerfication = 2,
            All_Message = 3,
            Client_Name = 4,
            Server_PlayersInfo = 5,
            Server_SendMap = 6,
            Server_GameData = 7,
            Server_SpawnInt = 8,
            Client_Request = 9,
                Request_Build = 1,
                Request_Move = 2,
                Request_Retreat = 3,

            Server_StartGame = 10,
            Server_GameAction = 11
        ;

        public void CreateServer()
        {
            game = new Server_Game(this); //creates an instance of the game class to run the game off

            clients = new Server_Client[maxClients]; //remember that the is already connected
            game.CreateMap();

            StartServer();
            gameState = GameState_Intermission;
        }

        public void StartServer()
        {
            Console.WriteLine("Starting Server");
            socket.Bind(new IPEndPoint(IPAddress.Any, 9000));
            socket.Listen(128);

            Console.WriteLine("Waiting for a connection...");

            socket.BeginAccept(new AsyncCallback(TPCCallback), null);

            Console.ReadLine();
        }

        public void StartGame()
        {
            for (int i = 0; i < clients.Length; i+=10) game.Spawn(i, clients[i].teamid);
            foreach (Server_Client client in clients)
            {
                if (client != null)
                {
                    client.SendGameData();
                }
            }
            foreach (Server_Client client in clients)
            {
                if (client != null)
                {
                    client.SendStartGame();
                }
            }
            game.clock.StartInternalClock();
        }

        public Server_Client[] GetClientsInTeam(int team)
        {
            List<Server_Client> server_Clients = new List<Server_Client>();
            foreach (Server_Client client in clients)
            {
                if (client != null && client.teamid == team)
                {
                    server_Clients.Add(client);
                }
            }
            //perhaps throw an exception if no teams are found
            return server_Clients.ToArray();
        }

        void TPCCallback(IAsyncResult result)
        {
            int index = ServerAvailability();

            if (index < 0)
            {
                Console.WriteLine("Server Full");
                //TODO: Disconnect Client
            }

            else
            {
                connectedPlayers++;
                Server_Client client = new Server_Client(this, socket.EndAccept(result), index);
                clients[index] = client;
                Console.WriteLine("Client Connected.");
                client.InitialiseDataTransfer();
                LookForMoreConnections();
            }
        }

        void LookForMoreConnections() { socket.BeginAccept(new AsyncCallback(TPCCallback), null); }

        int ServerAvailability()
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

        public void CheckForTimeouts()
        {
            List<int> delete = new List<int>();

            for (int i = 0; i < clients.Length; i++)
            {
                TimeSpan dt = DateTime.Now - clients[i].timeOut_initialTime;
                if (dt.TotalMilliseconds >= 5000)
                {
                    delete.Add(i);
                }
            }

            //delete clients
            for (int i = 0; i < delete.Count; i++)
            {
                //should implement IDisposable
                connectedPlayers--;
                clients[delete[i]].DisconnectClient();
                clients[delete[i]] = null;
            }
        }
    }
}
