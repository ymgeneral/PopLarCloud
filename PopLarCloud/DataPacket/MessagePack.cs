using PoplarCloud.EventsAndEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoplarCloud.DataPacket
{
    public class MessagePack:DataPacketBase
    {
        public MessagePack()
        {
        }
        public MessagePack(string targetId,MessageType msgType, byte[] data)
        {
            this.Data = data;
            this.MsgType = msgType;
            this.TargetId = targetId;
        }
        public string TargetId { get; set; }
        public string FromId { get; internal set; }
        public MessageType MsgType { get; set; }
        public byte[] Data { get; set; }


        protected override void EncoderData(System.IO.BinaryWriter bw)
        {
            bw.Write(TargetId);
            bw.Write(FromId);
            bw.Write((byte)this.MsgType);
            bw.Write(Data.Length);
            bw.Write(Data);
        }

        protected override void DecoderData(System.IO.BinaryReader br)
        {
            TargetId = br.ReadString();
            FromId = br.ReadString();
            this.MsgType = (MessageType)br.ReadByte();
            int leng = br.ReadInt32();
            Data = br.ReadBytes(leng);
        }
    }
}
