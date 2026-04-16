using System.Text.Json;

namespace OnlineGame
{
    internal class SaveService
    {
        private readonly string savesDirectory = "Saves";
        private readonly Logger logger;

        public SaveService(Logger logger)
        {
            this.logger = logger;
            EnsureSaveDirectoryExists();
        }

        public void SavePlayer(PlayerState player)
        {
            try
            {
                PlayerSaveData data = new PlayerSaveData
                {
                    Name = player.Name,
                    CurrentRoomId = player.CurrentRoomId,
                    Inventory = new List<string>(player.Inventory),
                    InventoryCapacity = player.InventoryCapacity,
                    Health = player.Health,
                    MaxHealth = player.MaxHealth,
                    BaseAttack = player.BaseAttack,
                    Gold = player.Gold,
                    GameCompleted = player.GameCompleted,
                    CompletedQuests = player.CompletedQuests.ToList(),
                    ActiveQuests = player.ActiveQuests.ToList()
                };

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(GetSavePath(player.Name), json);

                logger.Log($"Uložen stav hráče: {player.Name}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Chyba při ukládání hráče {player.Name}", ex);
                throw;
            }
        }

        public PlayerState LoadPlayer(string username)
        {
            try
            {
                string path = GetSavePath(username);

                if (!File.Exists(path))
                {
                    logger.Log($"Save hráče neexistuje: {username}");
                    return null;
                }

                string json = File.ReadAllText(path);

                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                PlayerSaveData data = JsonSerializer.Deserialize<PlayerSaveData>(json, options);

                if (data == null)
                {
                    logger.Log($"Save hráče je prázdný nebo neplatný: {username}");
                    return null;
                }

                PlayerState player = new PlayerState
                {
                    Name = data.Name,
                    CurrentRoomId = data.CurrentRoomId,
                    Inventory = data.Inventory ?? new List<string>(),
                    InventoryCapacity = data.InventoryCapacity,
                    Health = data.Health,
                    MaxHealth = data.MaxHealth,
                    BaseAttack = data.BaseAttack,
                    Gold = data.Gold,
                    GameCompleted = data.GameCompleted
                };

                player.CompletedQuests = new HashSet<string>(data.CompletedQuests ?? new List<string>());
                player.ActiveQuests = new HashSet<string>(data.ActiveQuests ?? new List<string>());

                logger.Log($"Načten stav hráče: {username}");
                return player;
            }
            catch (Exception ex)
            {
                logger.LogError($"Chyba při načítání hráče {username}", ex);
                throw;
            }
        }

        private string GetSavePath(string username)
        {
            return Path.Combine(savesDirectory, $"{username}.json");
        }

        private void EnsureSaveDirectoryExists()
        {
            if (!Directory.Exists(savesDirectory))
            {
                Directory.CreateDirectory(savesDirectory);
            }
        }
    }
}