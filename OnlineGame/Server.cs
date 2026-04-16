using System.Net;
using System.Net.Sockets;

namespace OnlineGame
{
    internal class Server
    {
        private readonly Logger logger;
        private readonly GameWorld world;
        private readonly GameEngine engine;
        private readonly AccountService accountService;
        private readonly SaveService saveService;

        private TcpListener listener;
        private readonly List<ClientSession> clients = new();
        private readonly object clientsLock = new object();

        public Server(
            Logger logger,
            GameWorld world,
            GameEngine engine,
            AccountService accountService,
            SaveService saveService)
        {
            this.logger = logger;
            this.world = world;
            this.engine = engine;
            this.accountService = accountService;
            this.saveService = saveService;
        }

        public async Task StartAsync(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            logger.Log($"Server spuštěn na portu {port}");
            Console.WriteLine($"Server běží na portu {port}");

            while (true)
            {
                TcpClient tcpClient = await listener.AcceptTcpClientAsync();

                ClientSession session = new ClientSession(
                    tcpClient,
                    this,
                    logger,
                    world,
                    engine,
                    accountService,
                    saveService);

                AddClient(session);

                _ = session.HandleAsync();
            }
        }

        public void AddClient(ClientSession client)
        {
            lock (clientsLock)
            {
                clients.Add(client);
            }
        }

        public void RemoveClient(ClientSession client)
        {
            lock (clientsLock)
            {
                clients.Remove(client);
            }
        }

        public List<ClientSession> GetClientsInRoom(string roomId)
        {
            lock (clientsLock)
            {
                return clients
                    .Where(c => c.Player != null && c.Player.CurrentRoomId == roomId)
                    .ToList();
            }
        }

        public List<ClientSession> GetAllAuthenticatedClients()
        {
            lock (clientsLock)
            {
                return clients
                    .Where(c => c.Player != null)
                    .ToList();
            }
        }

        public void BroadcastToRoom(string roomId, string message, ClientSession sender = null)
        {
            List<ClientSession> roomClients = GetClientsInRoom(roomId);

            foreach (ClientSession client in roomClients)
            {
                if (client != sender)
                {
                    client.Send(message);
                }
            }
        }

        public void BroadcastToAll(string message, ClientSession sender = null)
        {
            List<ClientSession> allClients = GetAllAuthenticatedClients();

            foreach (ClientSession client in allClients)
            {
                if (client != sender)
                {
                    client.Send(message);
                }
            }
        }
    }
}