using PoplarCloud.EventsAndEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoplarCloud.DataPacket
{
    public class SocketRegisterPacket:DataPacketBase
    {
        public SocketRegisterPacket()
        {
            Error = "";
        }
        public SocketRegisterPacket(RegisterType rType,string data,string error="")
        {
            this.Data = data;
            this.RType = rType;
            Error = error;
        }
        public RegisterType RType { get; set; }
        public string Data { get; set; }
        public string Error { get; set; }
        protected override void EncoderData(System.IO.BinaryWriter bw)
        {
            bw.Write((int)RType);
            bw.Write(Data);
            bw.Write(Error);
        }

        protected override void DecoderData(System.IO.BinaryReader br)
        {
            RType = (RegisterType)br.ReadInt32();
            Data = br.ReadString();
            Error = br.ReadString();
        }
    }
}
