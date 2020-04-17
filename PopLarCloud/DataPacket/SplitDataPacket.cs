using System;
using System.Collections.Generic;
using System.Text;

namespace PoplarCloud.DataPacket
{
    internal class SplitDataPacket:DataPacketBase
    {
        public string SplitPackId { get; set; }
        public long PackLength { get; set; }
        public int PackCount { get; set; }
        public int PackIndex { get; set; }
        public byte[] Data { get; set; }
        protected override void EncoderData(System.IO.BinaryWriter bw)
        {
            bw.Write(SplitPackId);
            bw.Write(PackLength);
            bw.Write(PackCount);
            bw.Write(PackIndex);
            bw.Write(Data.Length);
            bw.Write(Data);
        }

        protected override void DecoderData(System.IO.BinaryReader br)
        {
            SplitPackId = br.ReadString();
            PackLength = br.ReadInt64();
            PackCount = br.ReadInt32();
            PackIndex = br.ReadInt32();
            int dataLength = br.ReadInt32();
            Data = br.ReadBytes(dataLength);
        }
    }
}
