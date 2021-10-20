using System;
using System.Collections.Generic;
using System.Text;

namespace RSU_Server
{
    public class Packet : IDisposable
    {
        private List<byte> byteList;
        private int readPos;

        public Packet()
        {
            byteList = new List<byte>();
            readPos = 0;
        }

        public Packet(List<byte> vs)
        {
            byteList = vs;
            readPos = 0;
        }

        public byte[] GetBytes(int id)
        {
            int length = byteList.Count;
            byteList.InsertRange(0, BitConverter.GetBytes(length));
            byteList.InsertRange(0, BitConverter.GetBytes(id));
            return byteList.ToArray();
        }

        public int Length()
        {
            return byteList.Count;
        }

        public int RemainingBytes()
        {
            return byteList.Count - readPos;
        }

        public void WriteInt(int num)
        {
            byteList.AddRange(BitConverter.GetBytes(num));
        }

        public void WriteFloat(float num)
        {
            byteList.AddRange(BitConverter.GetBytes(num));
        }

        public void WriteULong(ulong num)
        {
            byteList.AddRange(BitConverter.GetBytes(num));
        }

        public void WriteString(string s)
        {
            byteList.AddRange(BitConverter.GetBytes(Encoding.ASCII.GetByteCount(s)));
            byteList.AddRange(Encoding.ASCII.GetBytes(s));
        }

        public void WriteVector3(Vector3 vector3)
        {
            byteList.AddRange(BitConverter.GetBytes(vector3.x));
            byteList.AddRange(BitConverter.GetBytes(vector3.y));
            byteList.AddRange(BitConverter.GetBytes(vector3.z));
        }

        public int ReadInt()
        {
            int x = BitConverter.ToInt32(byteList.GetRange(readPos, 4).ToArray(), 0);
            readPos += 4;
            return x;
        }

        public float ReadFloat()
        {
            float x = BitConverter.ToSingle(byteList.GetRange(readPos, 4).ToArray(), 0);
            readPos += 4;
            return x;
        }
        public ulong ReadULong()
        {
            ulong x = BitConverter.ToUInt64(byteList.GetRange(readPos, 8).ToArray(), 0);
            readPos += 8;
            return x;
        }

        public string ReadString()
        {
            int length = BitConverter.ToInt32(byteList.GetRange(readPos, 4).ToArray(), 0);
            readPos += 4;
            string s = Encoding.ASCII.GetString(byteList.GetRange(readPos, length).ToArray(), 0, length);
            readPos += length;
            return s;
        }

        public Vector3 ReadVector3()
        {
            return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
        }


        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                    byteList = null;
                    readPos = 0;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}