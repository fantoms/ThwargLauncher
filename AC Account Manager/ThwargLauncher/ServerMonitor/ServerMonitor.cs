﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Threading;

namespace ThwargLauncher
{
    class ServerMonitor
    {
        public delegate void ReportSomethingDelegateMethod(string msg);

        private Thread _thread = null;
        private IList<Server.ServerItem> _items;
        private int _millisecondsDelay = 1000;
        private int index;
        public void StartMonitor(IList<Server.ServerItem> items)
        {
            _thread = new Thread(new ThreadStart(MonitorLoop));
            _items = items;
            _thread.Start();
        }
        public void StopMonitor()
        {
            _thread.Abort();
        }
        private void MonitorLoop()
        {
            Random random = new Random();
            while (true)
            {
                ++index;
                if (index >= _items.Count)
                {
                    index = 0;
                    _millisecondsDelay = 5000;
                }
                if (_items.Count > 0)
                {
                    var server = _items[index];
                    CheckServer(server);
                }
                Thread.Sleep(_millisecondsDelay);
            }
        }
        private void CheckServer(Server.ServerItem server)
        {
            var address = AddressParser.Parse(server.ServerIpAndPort);
            if (string.IsNullOrEmpty(address.Ip) || address.Port <= 0) { return; }
            bool up = IsUdpServerUp(address.Ip, address.Port);
            string status = GetStatusString(up);
            if (server.ConnectionStatus != status)
            {
                CallToUpdate(server, status);
            }
        }
        private bool IsUdpServerUp(string address, int port)
        {
            try
            {
                UdpClient udpClient = new UdpClient(port);
                udpClient.Client.ReceiveTimeout = 3000;
                udpClient.Connect(address, port);
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                Byte[] sendBytes = ConstructPacket();
                udpClient.Send(sendBytes, sendBytes.Length);
                Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                return true;
            }
            catch (SocketException e)
            {
                if (e.ErrorCode == 10054)
                {
                    return false;
                }
                else
                {
                    return false;
                }
            }
        }
        private byte[] ConstructPacket()
        {
            var data = new Packet.PacketHeader(Packet.PacketHeaderFlags.EchoRequest);
            uint checksum;
            data.CalculateHash32(out checksum);
            data.Checksum = checksum;
            return data.GetRaw();
        }
        private static string GetStatusString(bool up)
        {
            return (up ? "Online" : "Offline");
        }
        private bool IsTcpServerUp(string address, int port)
        {
            var tcpClient = new System.Net.Sockets.TcpClient();
            try
            {
                tcpClient.Connect(address, port);
                return true;
            }
            catch (Exception exc)
            {
                string debug = exc.ToString();
                return false;
            }
        }
        private void CallToUpdate(Server.ServerItem server, string status)
        {
            if (System.Windows.Application.Current == null) return;
            System.Windows.Application.Current.Dispatcher.BeginInvoke(
                DispatcherPriority.Normal, new Action(() =>
                    {
                        PerformUpdate(server, status);
                    }));
        }
        /// <summary>
        /// Called on UI thread
        /// </summary>
        private void PerformUpdate(Server.ServerItem server, string status)
        {
            if (server.ConnectionStatus != status)
            {
                server.ConnectionStatus = status;
            }

        }
    }
}