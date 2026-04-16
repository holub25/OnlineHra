using System.Text.Json;

namespace OnlineGame
{
    internal class GameDataLoader
    {
        public List<Room> Rooms { get; private set; } = new();
        public List<Item> Items { get; private set; } = new();
        public List<Npc> Npcs { get; private set; } = new();
        public List<Quest> Quests { get; private set; } = new();

        public void LoadAll()
        {
            Rooms = Load<List<Room>>("Data/rooms.json");
            Items = Load<List<Item>>("Data/items.json");
            Npcs = Load<List<Npc>>("Data/npcs.json");
            Quests = Load<List<Quest>>("Data/quests.json");
        }

        private T Load<T>(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Soubor nebyl nalezen: {path}");
            }

            string json = File.ReadAllText(path);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            T data = JsonSerializer.Deserialize<T>(json, options);

            if (data == null)
            {
                throw new Exception($"Nepodařilo se načíst data ze souboru: {path}");
            }

            return data;
        }
    }
}