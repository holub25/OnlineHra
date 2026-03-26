using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame
{
    internal class Server
    {
        private TcpListener listener;
        private List<Player> players = new();

        public async Task Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine("SERVER BEZI");

            while (true)
            {
                var tcpClient = await listener.AcceptTcpClientAsync();

                var client = new Player(tcpClient, this);

                players.Add(client);

                _ = client.JoinChat();
            }
        }

        public void BroadCast(string msg, Player sender)
        {
            foreach(var client in players)
            {
                if(client != sender)
                {
                    client.Send(msg);
                }
            }
        }

        public void Remove(Player player)
        {
            players.Remove(player);
        }
    }
}
