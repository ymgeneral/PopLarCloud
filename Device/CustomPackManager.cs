using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
namespace Device
{
    /// <summary>
    /// 自定义协议解析类
    /// </summary>
    class CustomPackManager:IAnalysis
    {
        /// <summary>
        /// 无效的包事件
        /// </summary>
        public event EventHandler InvalidPacketReceived;
        /// <summary>
        /// 解析完后抛出此事件
        /// </summary>

        public event DataPacketHandler DataPacketReceived;

		public void ReceivedData(byte[] buffer)
		{
			DataPacketReceived?.Invoke(this,new CustomPack { Text = Encoding.Default.GetString(buffer) });
		}

    }
    /// <summary>
    /// 自定义协议类
    /// </summary>
    class CustomPack:IPacket
    {
        public string Text { get; set; }
        /// <summary>
        /// 打包方法
        /// </summary>
        /// <returns></returns>
        public byte[] Encoder()
        {
            return Encoding.Default.GetBytes(Text);
        }
    }
}
