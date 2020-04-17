using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace PoplarCloud
{
    internal class General
    {
        /// <summary>
        /// 判断给定的一个字符串是否为IP v4地址。
        /// </summary>
        /// <param name="address">要检查的字符串。</param>
        /// <returns>如果是IPV4地址则为True，否则为False。</returns>
        public static bool IsIPv4Address(string address)
        {
            if (address == null)
                return false;

            string pattern = @"^(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])\.(\d{1,2}|1\d\d|2[0-4]\d|25[0-5])$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(address);
        }

        public static byte[] PackHandShakeData(string secKeyAccept)
        {
            var responseBuilder = new StringBuilder();
            responseBuilder.Append("HTTP/1.1 101 Switching Protocols" + Environment.NewLine);
            responseBuilder.Append("Upgrade: websocket" + Environment.NewLine);
            responseBuilder.Append("Connection: Upgrade" + Environment.NewLine);
            responseBuilder.Append("Sec-WebSocket-Accept: " + secKeyAccept + Environment.NewLine + Environment.NewLine);
            //如果把上一行换成下面两行，才是thewebsocketprotocol-17协议，但居然握手不成功，目前仍没弄明白！
            //responseBuilder.Append("Sec-WebSocket-Accept: " + secKeyAccept + Environment.NewLine);
            //responseBuilder.Append("Sec-WebSocket-Protocol: chat" + Environment.NewLine);

            return Encoding.UTF8.GetBytes(responseBuilder.ToString());
        }

        /// <summary>
        /// 生成Sec-WebSocket-Accept
        /// </summary>
        /// <param name="handShakeText">客户端握手信息</param>
        /// <returns>Sec-WebSocket-Accept</returns>
        public static string GetSecKeyAccetp(byte[] handShakeBytes, int bytesLength)
        {
            string handShakeText = Encoding.UTF8.GetString(handShakeBytes, 0, bytesLength);
            string key = string.Empty;
            System.Text.RegularExpressions.Regex r = new System.Text.RegularExpressions.Regex(@"Sec-WebSocket-Key:(.*?)\r\n");
            System.Text.RegularExpressions.Match m = r.Match(handShakeText);
            if (m.Groups.Count != 0)
            {
                key = System.Text.RegularExpressions.Regex.Replace(m.Value, @"Sec-WebSocket-Key:(.*?)\r\n", "$1").Trim();
            }
            byte[] encryptionString = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
            return Convert.ToBase64String(encryptionString);
        }
        /// <summary>
        /// 解析DNS。
        /// </summary>
        public static IPAddress ResolveDns(string netAddress)
        {
            try
            {
               IPAddress ipAddress = Dns.GetHostEntry(netAddress).AddressList[0];
               return ipAddress;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
