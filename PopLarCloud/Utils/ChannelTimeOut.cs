using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PoplarCloud.Utils
{
    internal class ChannelTimeOut:TimeOutManager<YChannel>
    {
        public YChannel Find(string id)
        {
            return base.WaitData.FirstOrDefault(p => p.Id == id);
        }
    }

}
