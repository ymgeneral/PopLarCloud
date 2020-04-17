using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
namespace PoplarCloud.UdpGeneral
{
    internal class UpdClientTimeOut:TimeOutManager<UdpClientInfo>
    {

    }
    internal class UdpClientInfo
    {
        public IPEndPoint IpEnd { get; set; }
        public string Ip { get; set; }
        public int Port { get; set; }
    }
}
