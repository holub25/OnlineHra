using System.Net.Sockets;
using System.Text;

namespace OnlineGame
{
    internal class ClientSession
    {
        private readonly TcpClient client;
        private readonly Server server;
        private readonly Logger logger;
        private readonly GameWorld world;
        private readonly GameEngine engine;
        private readonly AccountService accountService;
        private readonly SaveService saveService;

        private readonly StreamReader reader;
        private readonly StreamWriter writer;

        public PlayerState Player { get; private set; }

        public ClientSession(
            TcpClient client,
            Server server,
            Logger logger,
            GameWorld world,
            GameEngine engine,
            AccountService accountService,
            SaveService saveService)
        {
            this.client = client;
            this.server = server;
            this.logger = logger;
            this.world = world;
            this.engine = engine;
            this.accountService = accountService;
            this.saveService = saveService;

            NetworkStream stream = client.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8)
            {
                AutoFlush = true
            };
        }

        public async Task HandleAsync()
        {
            try
            {
                await SendAsync("Vítej ve hře Vzpoura na pirátské lodi.");
                await SendAsync("Připojení proběhlo úspěšně.");

                Player = await AuthenticateAsync();

                if (Player == null)
                {
                    return;
                }

                logger.Log($"Hráč vstoupil do hry přes TCP: {Player.Name}");

                await SendAsync("");
                await SendAsync("Hra spuštěna.");
                await SendAsync("Napiš pomoc pro seznam příkazů.");
                await SendAsync("");
                await SendAsync(GetLookWithPlayers());

                server.BroadcastToRoom(Player.CurrentRoomId, $"{Player.Name} přišel do místnosti.", this);

                while (true)
                {
                    string input = await reader.ReadLineAsync();

                    if (input == null)
                    {
                        break;
                    }

                    input = input.Trim();

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        continue;
                    }

                    logger.Log($"Příkaz hráče {Player.Name}: {input}");

                    string oldRoomId = Player.CurrentRoomId;

                    if (input.Equals("konec", StringComparison.OrdinalIgnoreCase))
                    {
                        saveService.SavePlayer(Player);
                        await SendAsync("Hra byla uložena. Odpojuješ se.");
                        logger.Log($"Hráč ukončil hru: {Player.Name}");
                        break;
                    }

                    string lowerInput = input.ToLower();

                    if (lowerInput.StartsWith("rekni "))
                    {
                        string message = input.Substring(6).Trim();

                        if (string.IsNullOrWhiteSpace(message))
                        {
                            await SendAsync("Napiš zprávu.");
                        }
                        else
                        {
                            server.BroadcastToRoom(Player.CurrentRoomId, $"{Player.Name} říká: {message}", this);
                            await SendAsync($"Řekl jsi: {message}");
                        }

                        saveService.SavePlayer(Player);
                        continue;
                    }

                    if (lowerInput.StartsWith("krik "))
                    {
                        string message = input.Substring(5).Trim();

                        if (string.IsNullOrWhiteSpace(message))
                        {
                            await SendAsync("Napiš zprávu.");
                        }
                        else
                        {
                            server.BroadcastToAll($"{Player.Name} křičí: {message}", this);
                            await SendAsync($"Zakřičel jsi: {message}");
                        }

                        saveService.SavePlayer(Player);
                        continue;
                    }

                    string result = engine.HandleCommand(Player, input);
                    await SendAsync(result);

                    if (oldRoomId != Player.CurrentRoomId)
                    {
                        server.BroadcastToRoom(oldRoomId, $"{Player.Name} opustil místnost.", this);
                        server.BroadcastToRoom(Player.CurrentRoomId, $"{Player.Name} přišel do místnosti.", this);
                    }

                    if (Player.GameCompleted)
                    {
                        saveService.SavePlayer(Player);
                        logger.Log($"Hráč dokončil hru: {Player.Name}");
                        await SendAsync("");
                        await SendAsync("Hra byla dokončena.");
                        break;
                    }

                    saveService.SavePlayer(Player);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Chyba při obsluze klienta", ex);
            }
            finally
            {
                try
                {
                    if (Player != null)
                    {
                        saveService.SavePlayer(Player);
                        server.BroadcastToRoom(Player.CurrentRoomId, $"{Player.Name} opustil místnost.", this);
                        logger.Log($"Hráč byl odpojen: {Player.Name}");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError("Chyba při ukládání nebo odpojování hráče", ex);
                }

                server.RemoveClient(this);
                client.Close();
            }
        }

        public void Send(string message)
        {
            try
            {
                writer.WriteLine(message);
            }
            catch
            {
            }
        }

        private async Task SendAsync(string message)
        {
            await writer.WriteLineAsync(message);
        }

        private async Task<PlayerState> AuthenticateAsync()
        {
            while (true)
            {
                await SendAsync("");
                await SendAsync("Napiš:");
                await SendAsync("1 - registrace");
                await SendAsync("2 - přihlášení");

                string choice = await reader.ReadLineAsync();

                if (choice == null)
                {
                    return null;
                }

                choice = choice.Trim();

                if (choice == "1")
                {
                    await SendAsync("Zadej uživatelské jméno:");
                    string username = await reader.ReadLineAsync();

                    await SendAsync("Zadej heslo:");
                    string password = await reader.ReadLineAsync();

                    username = username?.Trim();

                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    {
                        await SendAsync("Jméno ani heslo nesmí být prázdné.");
                        continue;
                    }

                    bool success = accountService.Register(username, password, out string message);
                    await SendAsync(message);

                    if (success)
                    {
                        PlayerState loadedPlayer = saveService.LoadPlayer(username);

                        if (loadedPlayer != null)
                        {
                            logger.Log($"Po registraci byl nalezen existující save: {username}");
                            return loadedPlayer;
                        }

                        logger.Log($"Vytvořena nová hra pro hráče: {username}");
                        return new PlayerState
                        {
                            Name = username,
                            CurrentRoomId = "paluba"
                        };
                    }
                }
                else if (choice == "2")
                {
                    await SendAsync("Zadej uživatelské jméno:");
                    string username = await reader.ReadLineAsync();

                    await SendAsync("Zadej heslo:");
                    string password = await reader.ReadLineAsync();

                    username = username?.Trim();

                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    {
                        await SendAsync("Jméno ani heslo nesmí být prázdné.");
                        continue;
                    }

                    bool success = accountService.Login(username, password, out string message);
                    await SendAsync(message);

                    if (success)
                    {
                        PlayerState loadedPlayer = saveService.LoadPlayer(username);

                        if (loadedPlayer != null)
                        {
                            await SendAsync("Načten uložený stav hráče.");
                            return loadedPlayer;
                        }

                        await SendAsync("Nebyl nalezen uložený stav. Vytváří se nová hra.");
                        logger.Log($"Pro přihlášeného hráče nebyl nalezen save, vytvořena nová hra: {username}");

                        return new PlayerState
                        {
                            Name = username,
                            CurrentRoomId = "paluba"
                        };
                    }
                }
                else
                {
                    await SendAsync("Neplatná volba.");
                    logger.Log($"Uživatel zadal neplatnou volbu v menu autentizace: {choice}");
                }
            }
        }
        private string GetLookWithPlayers()
        {
            List<string> otherPlayers = server.GetClientsInRoom(Player.CurrentRoomId)
                .Where(c => c != this && c.Player != null)
                .Select(c => c.Player.Name)
                .ToList();

            return engine.Look(Player, otherPlayers);
        }
    }
}