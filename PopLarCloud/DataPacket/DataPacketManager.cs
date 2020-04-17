using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Reflection;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.DataPacket;
using PoplarCloud.Interfaces;
namespace PoplarCloud
{
    internal class YDataPacketManager : IAnalysis
    {

        private static object lockObj = new object();
        /// <summary>
        /// 收到无效数据包时发生
        /// </summary>
        public event EventHandler InvalidPacketReceived;
        /// <summary>
        /// 当收到完整数据包时发生
        /// </summary>
        public event DataPacketHandler DataPacketReceived;
        /// <summary>
        /// 数据包注册集合
        /// </summary>
        private static Dictionary<string, Type> dicType = new Dictionary<string, Type>();
        private readonly int dataLenth = 4;
        private List<byte> bufferList = new List<byte>();
        public void ReceivedData(byte[] buffer)
        {

            lock (lockObj)
            {
                if (buffer != null && buffer.Length > 0)
                {
                    bufferList.AddRange(buffer);
                }

                if (bufferList.Count < 5)
                {
                    return;
                }

                byte[] tempbuffer = bufferList.ToArray();
                int dataPacketSize = BitConverter.ToInt32(tempbuffer, 0);
                if (bufferList.Count < dataPacketSize)
                {
                    return;
                }
                MemoryStream ms = new MemoryStream();
                ms.Write(tempbuffer, dataLenth, dataPacketSize - dataLenth);
                ms.Position = 0;
                ReceivedPacket(ms);
                ms.Close();
                if (dataPacketSize != bufferList.Count)
                {
                    bufferList.RemoveRange(0, dataPacketSize);
                    ReceivedData(null);
                }
                else
                {
                    bufferList.Clear();
                }
            }
        }
        internal void NoListWrite(byte[] buffer)
        {
              if (buffer != null && buffer.Length > 0)
              {
                  if (bufferList.Count < 5)
                  {
                      return;
                  }
                  int dataPacketSize = BitConverter.ToInt32(buffer, 0);
                  if (buffer.Length < dataPacketSize)
                  {
                      return;
                  }
                  MemoryStream ms = new MemoryStream();
                  ms.Write(buffer, dataLenth, dataPacketSize - dataLenth);
                  ms.Position = 0;
                  ReceivedPacket(ms);
                  ms.Close();
              }
        }
        public static bool RegisterDataPacket(Type dataPacketType)
        {
            if(dicType.ContainsKey(dataPacketType.Name))
            {
                return false;
            }
            else
            {
                dicType.Add(dataPacketType.Name, dataPacketType);
            }
            return true;
        }
        public static bool DeleteDataPacket(Type dataPacketType)
        {
            try
            {
                dicType.Remove(dataPacketType.Name);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public static void ClearDataPacket()
        {
            dicType.Clear();
        }
        private void ReceivedPacket(Stream stream)
        {
            DataPacketBase dataPacketBase = null;
            BinaryReader br=null;
            try
            {
                br = new BinaryReader(stream, Encoding.UTF8);
                string packetType = br.ReadString();
                dataPacketBase = GetDataPacketBase(packetType);
                if (dataPacketBase == null)
                {
                    OnInvalidPacketReceived();
                    return;
                }
            }
            catch (Exception ex)
            {
                OnInvalidPacketReceived();
            }
            if (dataPacketBase.Decoder(br) == true)
            {
                if (DataPacketReceived != null)
                {
                    DataPacketReceived(this,dataPacketBase);
                }
            }
        }
        private DataPacketBase GetDataPacketBase(string packetType)
        {
            Type type = null;
            if (!dicType.TryGetValue(packetType, out type))
            {
                return null;
            }
            DataPacketBase dataPacketBase = CreateDataPack(type);
            dataPacketBase.PacketType = packetType;
            return dataPacketBase;
        }
        private DataPacketBase CreateDataPack(Type dataPacketType)
        {
            ConstructorInfo constructor = dataPacketType.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new InvalidOperationException("数据包必须有默认构造函数。");
            DataPacketBase packet = constructor.Invoke(null) as DataPacketBase;
            
            return packet;
        }
        protected virtual void OnInvalidPacketReceived()
        {
            if(InvalidPacketReceived!=null)
            {
                InvalidPacketReceived(this, new EventArgs());
            }
        }

    }
}
