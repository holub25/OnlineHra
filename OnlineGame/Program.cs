namespace OnlineGame
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            //var server = new Server();

            //await server.Start(5050);

            GameDataLoader loader = new GameDataLoader();
            loader.LoadAll();

            GameWorld world = new GameWorld();
            world.SetRooms(loader.Rooms);
            world.SetItems(loader.Items);
            world.SetNpcs(loader.Npcs);
            world.SetQuests(loader.Quests);

            PlayerState player = new PlayerState
            {
                Name = "Tomáš",
                CurrentRoomId = "paluba"
            };

            GameEngine engine = new GameEngine(world);

            Console.WriteLine("Hra spuštěna.");
            Console.WriteLine("Napiš pomoc pro seznam příkazů.");
            Console.WriteLine();
            Console.WriteLine(engine.Look(player));

            while (true)
            {
                Console.WriteLine();
                Console.Write("> ");
                string input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input))
                {
                    continue;
                }

                if (input.ToLower() == "konec")
                {
                    break;
                }

                string result = engine.HandleCommand(player, input);
                Console.WriteLine(result);
            }
        }
    }
}
