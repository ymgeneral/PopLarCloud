using PoplarCloud.EventsAndEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoplarCloud.Interfaces
{
	/// <summary>
	/// 包解析接口
	/// </summary>
    public interface IAnalysis
    {
		/// <summary>
		/// 收到无效数据包事件
		/// </summary>
        event EventHandler InvalidPacketReceived;
		/// <summary>
		/// 解析完后抛出此事件
		/// </summary>
		event DataPacketHandler DataPacketReceived;
		/// <summary>
		/// 接收到的数据流
		/// </summary>
		/// <param name="buffer"></param>
        void ReceivedData(byte[] buffer);
    }
}
