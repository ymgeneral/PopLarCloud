using PoplarCloud;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PoplarCloud.EventsAndEnum;
using PoplarCloud.Interfaces;
namespace Service
{
    public class Service:ServiceBase
    {
        public event DataPacketHandler DataPackReceived;
        public override void AbsInit()
        {
        }

        public override void AbsUnInit()
        {
        }

        public override void DeviceDataPacketReceived(YChannel sender, IPacket pack)
        {
            if(DataPackReceived!=null)
            {
                DataPackReceived(sender, pack);
            }
        }

        public override void ServiceDataPacketReceived(YChannel sender, IPacket pack)
        {
        }

    }
}
