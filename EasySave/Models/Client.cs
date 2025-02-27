using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasySave.Models
{
    public class Client
    {
        public TcpClient TcpClient { get; private set; }
        public bool LoggedIn { get; set; } = false;

        public Client(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
        }

        public void Login(string hashedPassword)
        {
            if (LoggedIn) return;

            if (hashedPassword.Equals(Settings.Instance.ServerPasswordHash)) LoggedIn = true;
        }

        public void Close()
        {
            TcpClient.Close();
        }
    }
}
