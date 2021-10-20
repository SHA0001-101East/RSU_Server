using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace RSU_Server
{
    public class Server_Client
    {
        public Server server;
        public Socket socket;
        public HandleData dataHandler;

        public string name = null;
        public int id; //unique
        public int teamid;
        public int playerState = Server.GameState_Intermission; //player is spectating by default

        ulong verificationKey = 17439573912022222222;

        public DateTime timeOut_initialTime;
        public List<byte[]> packets = new List<byte[]>();
        public bool isSending = false;

        public Server_Client(Server server1, Socket socket1, int index)
        {
            server = server1;
            socket = socket1;
            id = index;
            dataHandler = new HandleData(socket, this);
        }
        public void InitialiseDataTransfer()
        {
            dataHandler.BeginReceiveData();
        }
        public void SendData(byte[] bytes)
        {
            packets.Add(bytes);

            if (isSending)
            {
                Console.WriteLine("some data is already sending...");
                //some items that are sent might need a callback to the game
                return;
            }
            else
            {
                byte[] sendbytes;
                lock (packets)
                {
                    if (packets.Count > 0)
                    {
                        sendbytes = packets[0];
                        packets.RemoveAt(0);
                        packets.TrimExcess();
                        isSending = true;
                    }
                    else { return; }
                }
                Console.WriteLine($"Sending ... {sendbytes.Length.ToString()}");
                socket.BeginSend(sendbytes, 0, sendbytes.Length, SocketFlags.None, SendCallback, null);
            }
        }

        public void SendData()
        {
            if (isSending)
            {
                //some items that are sent might need a callback to the game
                return;
            }
            else
            {
                byte[] sendbytes;
                lock (packets)
                {
                    if (packets.Count > 0)
                    {
                        sendbytes = packets[0];
                        packets.RemoveAt(0);
                        packets.TrimExcess();
                        isSending = true;
                    }
                    else { return; }
                }

                socket.BeginSend(sendbytes, 0, sendbytes.Length, SocketFlags.None, SendCallback, null);
            }
        }

        public void SendCallback(IAsyncResult result)
        {
            isSending = false;
            SendData();
        }

        public void AcknowledgeVerification()
        {
            using Packet packet = new Packet();
            SendData(packet.GetBytes(Server.Server_AckVerfication));
        }

        public void DisconnectClient()
        {
            Console.WriteLine("Disconnecting client.");
            //this is actual garbage code i have no idea what i am doing
            //i am simply exerting all forms of destruction upon my poor socket
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
            socket.Close();
            socket.Dispose();
        }

        public void SendOtherPlayerData() //--------------------------------------------fix this-------------------------------------------------------------//
        {
            using Packet packet = new Packet();

            List<byte> bytes = new List<byte>();
            Server_Client[] clients;

            lock (server.clients) //locking server.clients as a client might be attempting to connect or be in the process of verifying that connection whilst we are accessing this data
            {
                clients = server.clients;
            }

            for (int i = 0; i < clients.Length; i++) //itterates and gets the information of every client
            {
                if (clients[i] != null)
                {
                    packet.WriteString(clients[i].name); //writes clients name
                    packet.WriteInt(clients[i].id); //writes client's id
                }
            }

            packet.WriteInt(server.maxClients); //sends max number of players
            packet.WriteInt(this.id); //gives the player his/her/it/they/them/xe/xae/zum ID
            SendData(packet.GetBytes(Server.Server_PlayersInfo));
        }

        public void SendSpawnInt(int spawnInt) //--------------------fix this spawn ID rubbish
        {
            using Packet packet = new Packet();

            packet.WriteInt(spawnInt);
            SendData(packet.GetBytes(Server.Server_SpawnInt));
        }

        public void SendMapData()
        {
            //this code needs to be changed
            //nah this code is fine

            using Packet packet = new Packet();

            Server_Nodes[] nodes = server.game.nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 position = nodes[i].position;
                packet.WriteVector3(position);
                packet.WriteInt(nodes[i].CurrentTeam());
            }

            SendData(packet.GetBytes(Server.Server_SendMap));
            Console.WriteLine("Map data sent. " + packet.Length() + " bytes. Has been sent to client no. " + id);
            //SendGameData();
        }

        public void SendGameData()
        {
            using Packet packet = new Packet();
            packet.WriteInt(server.gameState); //first we write the game state and then send the id and teams of each player
            packet.WriteInt(server.connectedPlayers);

            for (int i = 0; i < server.clients.Length; i++)
            {
                if (server.clients[i] != null)
                {
                    packet.WriteInt(server.clients[i].id);
                    packet.WriteInt(server.clients[i].teamid);
                }
            }

            switch (playerState)
            {
                case Server.GameState_Intermission: //if player is spectating
                    {
                        packet.WriteInt(server.game.nodes.Length);
                        for (int i = 0; i < server.game.nodes.Length; i++)
                        {
                            packet.WriteInt(i); //write node
                            packet.WriteInt(server.game.nodes[i].building); //write building type

                            int mTeam = server.game.nodes[i].CurrentTeam(); //get the team
                            packet.WriteInt(mTeam);
                            packet.WriteInt(server.game.nodes[i].teamDictionary.Count); //write the number of teams on the node

                            //if there is no war on the node, that is there is only 1 team on it //write down the team
                            if (mTeam == -2) //else if there is another team on it
                            {
                                foreach (KeyValuePair<int, Team> keyValuePair in server.game.nodes[i].teamDictionary) //foreach time, write team and numb of troops and team
                                {
                                    packet.WriteInt(keyValuePair.Value.team);
                                    packet.WriteFloat(keyValuePair.Value.troops);
                                }
                            }
                        }

                        break;
                    }

                case Server.GameState_InGame: //else if the player is in game
                    {
                        int[] connectedNodes = server.game.GetNodesInFOV(teamid);

                        packet.WriteInt(connectedNodes.Length);
                        for (int i = 0; i < connectedNodes.Length; i++) //foreach node in it's scope, write the following properties.
                        {
                            packet.WriteInt(connectedNodes[i]);
                            packet.WriteInt(server.game.nodes[connectedNodes[i]].building);

                            int mTeam = server.game.nodes[connectedNodes[i]].CurrentTeam();

                            packet.WriteInt(mTeam);
                            packet.WriteInt(server.game.nodes[i].teamDictionary.Count); //write the number of teams on the node

                            if (mTeam > -2)
                            {
                                packet.WriteFloat(server.game.nodes[connectedNodes[i]].teamDictionary[mTeam].troops);
                            }

                            else if (mTeam == -2)
                            {
                                foreach (KeyValuePair<int, Team> keyValuePair in server.game.nodes[connectedNodes[i]].teamDictionary)
                                {
                                    packet.WriteInt(keyValuePair.Value.team);
                                    packet.WriteFloat(keyValuePair.Value.troops);
                                }
                            }
                        }

                        break;
                    }
            }

            int[] blobsInView;
            if (server.gameState == Server.GameState_Intermission)
            {
                blobsInView = new int[server.game.blobIDPairs.Count];
                int k = 0;
                foreach (KeyValuePair<int, Blob> item in server.game.blobIDPairs)
                {
                    blobsInView[k] = item.Key;
                    k++;
                }
            } else { blobsInView = server.game.GetBlobsInFOV(teamid); }

            packet.WriteInt(blobsInView.Length); //writes number of blobs

            for (int i = 0; i < blobsInView.Length; i++)
            {
                packet.WriteInt(blobsInView[i]); //blob id
                packet.WriteInt(server.game.blobIDPairs[blobsInView[i]].team); //team
                packet.WriteFloat(server.game.blobIDPairs[blobsInView[i]].troops); //troops
                packet.WriteVector3(server.game.blobIDPairs[blobsInView[i]].position); //position
                packet.WriteInt(server.game.blobIDPairs[blobsInView[i]].index); //index
                packet.WriteInt(server.game.blobIDPairs[blobsInView[i]].path.Length); //array length
                for (int j = 0; j < server.game.blobIDPairs[blobsInView[i]].path.Length; j++) //array elements
                {
                    packet.WriteInt(server.game.blobIDPairs[blobsInView[i]].path[j]);
                }
            }

            SendData(packet.GetBytes(Server.Server_GameData));
        }

        public void SendStartGame()
        {
            using Packet packet = new Packet();
            packet.WriteInt(server.game.clock.ticksSinceStartUp);
            SendData(packet.GetBytes(Server.Server_StartGame));
        }

        public void SendGameAction(GameAction gameAction, int ticksSinceStartUp)
        {
            using Packet packet = new Packet();
            packet.WriteInt(ticksSinceStartUp);
            packet.WriteInt(gameAction.action);
            switch (gameAction.action)
            {
                case GameAction.buildingAction:
                    {
                        packet.WriteInt(gameAction.team);
                        packet.WriteInt(gameAction.node);
                        packet.WriteInt(gameAction.building);
                        break;
                    }

                case GameAction.moveAction:
                    {
                        packet.WriteInt(gameAction.team);
                        if (playerState != Server.GameState_Intermission) packet.WriteFloat(gameAction.troops);
                        packet.WriteInt(gameAction.node);
                        packet.WriteInt(gameAction.destinationNode);
                        break;
                    }

                case GameAction.retreatAction:
                    {
                        packet.WriteInt(gameAction.team);
                        packet.WriteInt(gameAction.blobID);
                        break;
                    }
            }
            SendData(packet.GetBytes(Server.Server_GameAction));
        }

        public void SendMessage(string name, string message)
        {
            using Packet packet = new Packet();
            Server_Client[] clients;
            lock (server.clients) // you know the deal
            {
                clients = server.clients;
            }

            packet.WriteString(name);
            packet.WriteString(message);

            for (int i = 0; i < clients.Length; i++)
            {
                if (clients[i] != null && clients[i].id != id)
                {
                    clients[i].SendData(packet.GetBytes(Server.All_Message));
                }
            }
        }

        public void ProcessData(int id, int length, List<byte> bytes)
        {
            using Packet packet = new Packet(bytes);
            switch (id)
            {
                case 0:
                    {
                        Console.WriteLine("Data type with ID 0."); //properly disconnect client
                        break;
                    }

                case Server.Client_Token: //check verification key
                    {
                        ulong key = packet.ReadULong();
                        if (key == verificationKey) { AcknowledgeVerification(); }
                        else { DisconnectClient(); }
                        break;
                    }

                case Server.Client_Name: //player name
                    {
                        name = packet.ReadString();
                        Console.WriteLine("Client name recieved: " + name);
                        SendOtherPlayerData();
                        SendMapData();
                        break;
                    }

                case Server.All_Message:
                    {
                        Console.WriteLine("Message Recieved. Processing Message.");
                        Console.WriteLine(bytes.Count.ToString());

                        string name = packet.ReadString();
                        string message = packet.ReadString();

                        SendMessage(name, message);
                        break;
                    }

                case Server.Client_Request:
                    {
                        int request = packet.ReadInt();
                        switch (request)
                        {
                            case Server.Request_Build:
                                {
                                    List<GameAction> gameActions = new List<GameAction>();
                                    int _building = packet.ReadInt();
                                    int _length = packet.ReadInt();

                                    for (int i = 0; i < _length; i++)
                                    {
                                        GameAction action = new GameAction();
                                        action.Build(teamid, packet.ReadInt(), _building);
                                        gameActions.Add(action);
                                    }
                                    for (int i = 0; i < gameActions.Count; i++) server.game.QueueGameAction(gameActions[i]);
                                    break;
                                }

                            case Server.Request_Move:
                                {
                                    List<GameAction> gameActions = new List<GameAction>();
                                    int _final = packet.ReadInt();
                                    float _troops = packet.ReadFloat();
                                    int _length = packet.ReadInt();

                                    for (int i = 0; i < _length; i++)
                                    {
                                        GameAction action = new GameAction();
                                        action.Move(teamid, _troops, packet.ReadInt(), _final);
                                        gameActions.Add(action);
                                    }
                                    for (int i = 0; i < gameActions.Count; i++) server.game.QueueGameAction(gameActions[i]);
                                    break;
                                }

                            case Server.Request_Retreat:
                                {
                                    List<GameAction> gameActions = new List<GameAction>();
                                    int _length = packet.ReadInt();

                                    for (int i = 0; i < _length; i++)
                                    {
                                        GameAction action = new GameAction();
                                        action.Retreat(teamid, packet.ReadInt());
                                        gameActions.Add(action);
                                    }
                                    for (int i = 0; i < gameActions.Count; i++) server.game.QueueGameAction(gameActions[i]);
                                    break;
                                }
                        }
                        break;
                    }

                default:
                    {
                        Console.WriteLine("Data type only for clients.");
                        //disconnect client maybe
                        return;
                    }
            }

            timeOut_initialTime = DateTime.Now;
        }

    }
}
