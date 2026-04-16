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

                GameEngine engine = new GameEngine(world);
                AccountService accountService = new AccountService(logger);
                SaveService saveService = new SaveService(logger);

                int port = 5050;

                Server server = new Server(
                    logger,
                    world,
                    engine,
                    accountService,
                    saveService);

                await server.StartAsync(port);
            }
            catch (Exception ex)
            {
                logger.LogError("Neočekávaná chyba v hlavním programu", ex);
                Console.WriteLine("Nastala neočekávaná chyba. Podívej se do logu.");
            }
        }
    }
}
