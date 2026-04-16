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

                    if (msg.StartsWith("/"))
                    {
                        CommandSelect(msg);
                    }
                    else
                    {
                        Console.WriteLine(name + ": " + msg);
                        server.BroadCast(name + ": " + msg, this);
                    }
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

        public void CommandSelect(string command)
        {
            string[] msg = command.Split(" ");
            switch (msg[0])
            {
                case "/pomoc":
                    Send("Dostupne prikazy:");
                    Console.WriteLine(this.name + " Zazadal o pomoc");
                    break;
                case "/jdi":
                    Send("Sel jsi do: " + msg[1]);
                    Console.WriteLine(this.name + " Sel do " + msg[1]);
                    break;
                case "/vezmi":
                    Send("Sebral jsi: " + msg[1]);
                    Console.WriteLine(this.name + " Sebral " + msg[1]);
                    break;
                case "/poloz":
                    Send("Polozil jsi: "+ msg[1]);
                    Console.WriteLine(this.name + " Polozil " + msg[1]);
                    break;
                case "/inventar":
                    Send("Otevrel jsi inv");
                    Console.WriteLine(this.name + " Otevrel inv");
                    break;
                case "/mluv":
                    Send("Mluvis s " + msg[1]);
                    Console.WriteLine(this.name + " Mluvi s " + msg[1]);
                    break;
                case "/utoc":
                    Send("Utocis na " + msg[1]);
                    Console.WriteLine(this.name + " Utoci na " + msg[1]);
                    break;

            }
        }

        public void Send(string msg)
        {
            writer.WriteLine(msg);
        }
    }

    
}
