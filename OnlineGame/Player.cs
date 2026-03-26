using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OnlineGame
{
    internal class Player
    {
        private TcpClient client;
        private Server server;

        private StreamReader reader;
        private StreamWriter writer;

        public string name;

        public Player(TcpClient client, Server server)
        {
            this.client = client;
            this.server = server;

            var stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream)
            {
                AutoFlush = true
            };
        }

        public async Task JoinChat()
        {
            try
            {
                await writer.WriteLineAsync("Zadej jmeno");
                name = await reader.ReadLineAsync();

                await writer.WriteLineAsync("Vítej " + name);

                while (true)
                {
                    string msg = await reader.ReadLineAsync();

                    if (msg == null) break;

                    Console.WriteLine(name + ": " + msg);

                    server.BroadCast(name + ": " + msg, this);
                }
            }
            catch
            {

            }
            finally
            {
                server.Remove(this);
                client.Close();
            }
        }

        public void Send(string msg)
        {
            writer.WriteLine(msg);
        }
    }

    
}
