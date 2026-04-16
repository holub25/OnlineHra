using System.Collections.Generic;

namespace OnlineGame
{
    internal class PlayerSaveData
    {
        public string Name { get; set; }
        public string CurrentRoomId { get; set; }

        public List<string> Inventory { get; set; } = new();
        public int InventoryCapacity { get; set; }

        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public int BaseAttack { get; set; }
        public int Gold { get; set; }

        public bool GameCompleted { get; set; }

        public List<string> CompletedQuests { get; set; } = new();
        public List<string> ActiveQuests { get; set; } = new();
    }
}