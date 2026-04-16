namespace OnlineGame
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var server = new Server();

            //await server.Start(5050);

            Logger logger = new Logger();

            try
            {
                GameDataLoader loader = new GameDataLoader();
                loader.LoadAll();
                logger.Log("Herní data byla úspěšně načtena.");

                GameWorld world = new GameWorld();
                world.SetRooms(loader.Rooms);
                world.SetItems(loader.Items);
                world.SetNpcs(loader.Npcs);
                world.SetQuests(loader.Quests);

                AccountService accountService = new AccountService(logger);
                SaveService saveService = new SaveService(logger);

                PlayerState player = AuthenticateUser(accountService, saveService, logger);

                GameEngine engine = new GameEngine(world);

                logger.Log($"Hráč vstoupil do hry: {player.Name}");

                Console.WriteLine();
                Console.WriteLine("Hra spuštěna.");
                Console.WriteLine("Napiš pomoc pro seznam příkazů.");
                Console.WriteLine();
                Console.WriteLine(engine.Look(player));

                while (true)
                {
                    if (player.GameCompleted)
                    {
                        saveService.SavePlayer(player);
                        logger.Log($"Hráč dokončil hru: {player.Name}");
                        Console.WriteLine();
                        Console.WriteLine("Hra byla dokončena.");
                        break;
                    }

                    Console.WriteLine();
                    Console.Write("> ");
                    string input = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        continue;
                    }

                    logger.Log($"Příkaz hráče {player.Name}: {input}");

                    if (input.ToLower() == "konec")
                    {
                        saveService.SavePlayer(player);
                        logger.Log($"Hráč ukončil hru: {player.Name}");
                        Console.WriteLine("Hra byla uložena.");
                        break;
                    }

                    string result = engine.HandleCommand(player, input);
                    Console.WriteLine(result);

                    saveService.SavePlayer(player);
                }
            }
            catch (Exception ex)
            {
                logger.LogError("Neočekávaná chyba v hlavním programu", ex);
                Console.WriteLine("Nastala neočekávaná chyba. Podívej se do logu.");
            }
        }

        static PlayerState AuthenticateUser(AccountService accountService, SaveService saveService, Logger logger)
        {
            while (true)
            {
                Console.WriteLine("Napiš:");
                Console.WriteLine("1 - registrace");
                Console.WriteLine("2 - přihlášení");
                Console.Write("> ");

                string choice = Console.ReadLine();

                if (choice == "1")
                {
                    Console.Write("Zadej uživatelské jméno: ");
                    string username = Console.ReadLine()?.Trim();

                    Console.Write("Zadej heslo: ");
                    string password = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    {
                        Console.WriteLine("Jméno ani heslo nesmí být prázdné.");
                        continue;
                    }

                    bool success = accountService.Register(username, password, out string message);
                    Console.WriteLine(message);

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
                    Console.Write("Zadej uživatelské jméno: ");
                    string username = Console.ReadLine()?.Trim();

                    Console.Write("Zadej heslo: ");
                    string password = Console.ReadLine();

                    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    {
                        Console.WriteLine("Jméno ani heslo nesmí být prázdné.");
                        continue;
                    }

                    bool success = accountService.Login(username, password, out string message);
                    Console.WriteLine(message);

                    if (success)
                    {
                        PlayerState loadedPlayer = saveService.LoadPlayer(username);

                        if (loadedPlayer != null)
                        {
                            Console.WriteLine("Načten uložený stav hráče.");
                            return loadedPlayer;
                        }

                        Console.WriteLine("Nebyl nalezen uložený stav. Vytváří se nová hra.");
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
                    Console.WriteLine("Neplatná volba.");
                    logger.Log($"Uživatel zadal neplatnou volbu v menu autentizace: {choice}");
                }

                Console.WriteLine();
            }
        }
    }
}
