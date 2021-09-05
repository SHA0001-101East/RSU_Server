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
        public void SendData(int id, byte[] bytes)
        {
            List<byte> byteList = new List<byte>();
            byteList.AddRange(BitConverter.GetBytes(id));
            byteList.AddRange(BitConverter.GetBytes(bytes.Length));
            byteList.AddRange(bytes);

            byte[] byteArray = byteList.ToArray();
            socket.BeginSend(byteArray, 0, byteArray.Length, SocketFlags.None, null, null);
        }

        public void AcknowledgeVerification()
        {
            int id = 2;
            SendData(id, new byte[0]);
        }

        public void DisconnectClient()
        {

        }

        public void SendOtherPlayerData()
        {
            int id = 4; //id for sending other player data;
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
                    bytes.AddRange(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(clients[i].name))); //gets name length
                    bytes.AddRange(Encoding.ASCII.GetBytes(clients[i].name)); //gets name
                    bytes.AddRange(BitConverter.GetBytes(clients[i].id)); //gets client's id
                }
            }

            bytes.AddRange(BitConverter.GetBytes(Server.maxClients)); //sends max number of players
            bytes.AddRange(BitConverter.GetBytes(this.id)); //gives the player his/her/it/they/them/xe/xae/zum ID
            SendData(id, bytes.ToArray());
        }

        public void SendSpawnInt(int spawnInt)
        {
            int id = 9; //id for sending spawn
            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(spawnInt));
            SendData(id, bytes.ToArray());
        }

        public void SendMapData()
        {
            List<byte> bytes = new List<byte>();

            GameObject[] nodes = game.nodes;
            for (int i = 0; i < nodes.Length; i++)
            {
                Vector3 vector3 = nodes[i].transform.position;
                bytes.AddRange(BitConverter.GetBytes(vector3.x));
                bytes.AddRange(BitConverter.GetBytes(vector3.y));
                bytes.AddRange(BitConverter.GetBytes(vector3.z));
                bytes.AddRange(BitConverter.GetBytes(nodes[i].GetComponent<Node>().nodeTeam));
            }

            SendData(6, bytes.ToArray());
            Console.WriteLine("Map data sent. " + bytes.Count.ToString() + " bytes. Has been sent to client no. " + id);
        }

        public void SendMessageAcross(string name, string message)
        {
            Client[] clients;
            lock (Server.clients) // you know the deal
            {
                clients = Server.clients;
            }

            List<byte> bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(name)));
            bytes.AddRange(Encoding.ASCII.GetBytes(name));
            bytes.AddRange(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(message)));
            bytes.AddRange(Encoding.ASCII.GetBytes(message));

            for (int i = 0; i < clients.Length; i++)
            {
                if (clients[i] != null && clients[i].id != id) { clients[i].SendData(7, bytes.ToArray()); }
            }
        }

        public void ProcessData(int id, int length, List<byte> bytes)
        {
            switch (id)
            {
                case 0:
                    {
                        Console.WriteLine("Data type with ID 0."); //properly disconnect client
                        break;
                    }

                case 1: //check verification key
                    {
                        byte[] dataBytes = new byte[length];

                        bytes.CopyTo(0, dataBytes, 0, length);
                        bytes.RemoveRange(0, length);
                        bytes.TrimExcess();
                        ulong key = BitConverter.ToUInt64(dataBytes, 0);

                        if (key == verificationKey) { AcknowledgeVerification(); }

                        else { DisconnectClient(); }


                        if (id == 2) { Console.WriteLine("Data type only for clients."); } //probably should disconnect client
                        break;
                    }

                case 3:
                    {
                        byte[] dataBytes = new byte[length];

                        bytes.CopyTo(0, dataBytes, 0, length);
                        bytes.RemoveRange(0, length);
                        bytes.TrimExcess();
                        name = Encoding.ASCII.GetString(dataBytes);
                        SendOtherPlayerData();

                        //TODO: Send other player data about client.

                        Console.WriteLine("Client name recieved: " + name);
                        break;
                    }

                case 5: //send map packet
                    {
                        Console.WriteLine("Player has asked for map data.");
                        SendMapData();
                        break;
                    }

                case 7:
                    {
                        Console.WriteLine("Message Recieved. Processing Message.");
                        Console.WriteLine(bytes.Count.ToString());

                        string name;
                        string message;

                        byte[] dataBytes = new byte[4];

                        bytes.CopyTo(0, dataBytes, 0, 4);
                        bytes.RemoveRange(0, 4);
                        int nameLength = BitConverter.ToInt32(dataBytes, 0);

                        dataBytes = new byte[nameLength];
                        bytes.CopyTo(0, dataBytes, 0, nameLength);
                        bytes.RemoveRange(0, nameLength);
                        name = Encoding.ASCII.GetString(dataBytes);

                        dataBytes = new byte[4];
                        bytes.CopyTo(0, dataBytes, 0, 4);
                        bytes.RemoveRange(0, 4);
                        int messageLength = BitConverter.ToInt32(dataBytes, 0);

                        dataBytes = new byte[messageLength];
                        bytes.CopyTo(0, dataBytes, 0, messageLength);
                        bytes.RemoveRange(0, messageLength);
                        message = Encoding.ASCII.GetString(dataBytes);

                        SendMessageAcross(name, message);
                        break;
                    }

                case 8:
                    {
                        Console.WriteLine("Client has asked for game data.");
                        int spawnInt = 5 * (this.id + 1);
                        if (Server.inGame) { Server.game.Spawn(spawnInt, this.id + 2); SendSpawnInt(spawnInt); }  //make it so that in the future, if we are in game then player is a spectator
                        if (!Server.inGame) { Server.game.Spawn(spawnInt, this.id + 2); SendSpawnInt(spawnInt); } //spawn player.
                        break;
                    }

                default:
                    {
                        Console.WriteLine("Data type only for clients.");
                        //disconnect client
                        break;
                    }
            }
        }

    }
}
