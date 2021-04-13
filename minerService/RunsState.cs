using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace minerService
{
    static class RunsState
    {
        private static int runs;
        private static TcpListener server;
        private static UdpClient receiver;

        public static TcpListener Server { get => server; set => server = value; }
        public static UdpClient Receiver { get => receiver; set => receiver = value; }
        public static int Runs { get => runs; set => runs = value; }
    }
}
