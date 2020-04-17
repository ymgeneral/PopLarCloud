using PoplarCloud;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Device
{
    public class Device:DeviceBase
    {
        public event DataPacketHandler DataPackReceived;
        public override void AbsInit()
        {
        }

        public override void AbsUnInit()
        {
        }


        public override void ServiceDataPacketReceived(YChannel sender, IPacket pack)
        {
            if (DataPackReceived != null)
            {
                DataPackReceived(sender, pack);
            }
        }
    }
}
