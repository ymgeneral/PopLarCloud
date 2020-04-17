using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoplarCloud.Interfaces
{

	/// <summary>
	/// 包接口
	/// </summary>
    public interface IPacket
    {
        /// <summary>
        /// 打包数据
        /// </summary>
        /// <returns></returns>
        byte[] Encoder();
    }
}
