using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace RSU_Server
{
    public class HandleData
    {
        Socket socket;
        Server_Client client;

        int bufferSize = 1024;
        byte[] byteBuffer;
        List<byte> byteList = new List<byte>();
        int currentDataType = 0;
        bool dataPending = false;
        int numberOfBytesPending = 0;
        int currentDataLength = 0;

        public HandleData(Socket socket1, Server_Client client1)
        {
            socket = socket1; client = client1;
            socket.ReceiveBufferSize = bufferSize;
            socket.SendBufferSize = bufferSize;
        }

        public void BeginReceiveData()
        {
            byteBuffer = new byte[bufferSize];
            socket.BeginReceive(byteBuffer, 0, byteBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveDataCallBack), null);
        }

        private void ReceiveDataCallBack(IAsyncResult ar)
        {
            //deal with disconnects from here.
            int bytesReceived = socket.EndReceive(ar);
            Console.WriteLine("Bytes received: " + bytesReceived);
            for (int i = 0; i < bytesReceived; i++)
            {
                byteList.Add(byteBuffer[i]);
            }

            byteBuffer = null;

            if (!dataPending) { IdentifyData(); }

            else if (dataPending && numberOfBytesPending <= bytesReceived)
            {
                if (bytesReceived - numberOfBytesPending > 0)
                {
                    HandOverData(currentDataType, currentDataLength);
                    currentDataType = 0;
                    dataPending = false;
                    numberOfBytesPending = 0;
                    currentDataLength = 0;
                    IdentifyData();
                }
                else if (bytesReceived == numberOfBytesPending)
                {
                    HandOverData(currentDataType, currentDataLength);
                    currentDataType = 0;
                    dataPending = false;
                    numberOfBytesPending = 0;
                    currentDataLength = 0;
                }
            }
            else if (dataPending && numberOfBytesPending > bytesReceived)
            {
                numberOfBytesPending -= bytesReceived;
            }
            else { Console.WriteLine("Data received does not have a designated processing protocol."); }

            BeginReceiveData();
        }
        private void IdentifyData()
        {
            byte[] idBytes = new byte[4];
            byteList.CopyTo(0, idBytes, 0, 4);
            byteList.RemoveRange(0, 4);
            int id = BitConverter.ToInt32(idBytes, 0);

            byte[] lengthBytes = new byte[4];
            byteList.CopyTo(0, lengthBytes, 0, 4);
            byteList.RemoveRange(0, 4);
            int length = BitConverter.ToInt32(lengthBytes, 0);

            byteList.TrimExcess();
            int byteListCount = byteList.Count;

            Console.WriteLine("Receiving data with length " + length + " and id " + id + ". BytesReceived: " + byteListCount);

            if (byteListCount < length) //if we didnt receive all the data
            {
                currentDataType = id;
                dataPending = true;
                numberOfBytesPending = length - byteListCount;
                currentDataLength = length;
            }

            else if (byteListCount == length) //if we received only that piece of data
            {
                HandOverData(id, length);
            }

            else if (byteListCount > length) //it probably means other data also came with it
            {
                HandOverData(id, length);
                IdentifyData(); //we run this code cause there is a new piece of data to be identified
            }
        }

        private List<byte> CutFromByteList(int length)
        {
            List<byte> bytes = new List<byte>();

            lock (byteList)
            {
                for (int i = 0; i < length; i++)
                {
                    bytes.Add(byteList[i]);
                }
                byteList.RemoveRange(0, length);
                byteList.TrimExcess();
            }

            return bytes;
        }

        private void HandOverData(int id, int length)
        {
            List<byte> bytes = CutFromByteList(length);
            client.ProcessData(id, length, bytes);
        }
    }
}
