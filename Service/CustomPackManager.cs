using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    class CustomPackManager : IAnalysis
    {
        public event EventHandler InvalidPacketReceived;

        public event DataPacketHandler DataPacketReceived;

		public void ReceivedData(byte[] buffer)
		{
			DataPacketReceived?.Invoke(this,new CustomPack { Text = Encoding.Default.GetString(buffer) });
		}
    }

    class CustomPack : IPacket
    {
        public string Text { get; set; }
        public byte[] Encoder()
        {
            return Encoding.Default.GetBytes(Text);
        }
    }
}
