using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace RSU_Server
{
    public class Client
    {
        public Socket socket;
        public HandleData dataHandler;

        public string name = null;
        public int id;
        public int teamid;

        ulong verificationKey = 17439573912022222222;

        public DateTime timeOut_initialTime;

        public Client(Socket socket1, int index)
        {
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
            socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, null, null);
        }

        public void AcknowledgeVerification()
        {
            using Packet packet = new Packet();
            SendData(packet.GetBytes(Server.Server_AckVerfication));
        }

        public void DisconnectClient()
        {
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
            Client[] clients;

            lock (Server.clients) //locking server.clients as a client might be attempting to connect or be in the process of verifying that connection whilst we are accessing this data
            {
                clients = Server.clients;
            }

            for (int i = 0; i < clients.Length; i++) //itterates and gets the information of every client
            {
                if (clients[i] != null)
                {
                    packet.WriteString(clients[i].name); //writes clients name
                    packet.WriteInt(clients[i].id); //writes client's id
                }
            }

            packet.WriteInt(Server.maxClients); //sends max number of players
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

            Node[] nodes = Server.game.nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 position = nodes[i].position;
                packet.WriteFloat(position.x);
                packet.WriteFloat(position.y);
                packet.WriteFloat(position.z);
                packet.WriteInt(nodes[i].CurrentTeam());
            }

            SendData(packet.GetBytes(Server.Server_SendMap));
            Console.WriteLine("Map data sent. " + packet.Length() + " bytes. Has been sent to client no. " + id);
            
        }

        public void SendMessage(string name, string message)
        {
            using Packet packet = new Packet();

            Client[] clients;
            lock (Server.clients) // you know the deal
            {
                clients = Server.clients;
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
                        SendOtherPlayerData();
                        Console.WriteLine("Client name recieved: " + name);
                        break;
                    }

                case Server.Client_RequestMap: //send map packet
                    {
                        Console.WriteLine("Player has asked for map data.");
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

                /*case 8: //edit this later
                    {
                        Console.WriteLine("Client has asked for game data.");
                        int spawnInt = 5 * (this.id + 1);
                        if (Server.inGame) { Server.game.Spawn(spawnInt, this.id + 2); SendSpawnInt(spawnInt); }  //make it so that in the future, if we are in game then player is a spectator
                        if (!Server.inGame) { Server.game.Spawn(spawnInt, this.id + 2); SendSpawnInt(spawnInt); } //spawn player.
                        break;
                    }*/

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
