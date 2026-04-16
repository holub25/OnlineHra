using System.Collections.Generic;

namespace OnlineGame
{
    internal class PlayerState
    {
        public string Name { get; set; }
        public string CurrentRoomId { get; set; }

        public List<string> Inventory { get; set; } = new();
        public int InventoryCapacity { get; set; } = 5;

        public int Health { get; set; } = 100;
        public int MaxHealth { get; set; } = 100;
        public int BaseAttack { get; set; } = 5;
        public int Gold { get; set; } = 0;

        public bool GameCompleted { get; set; } = false;

        public HashSet<string> CompletedQuests { get; set; } = new();

        public int GetAttack(GameWorld world)
        {
            int attack = BaseAttack;

            foreach (string itemId in Inventory)
            {
                Item item = world.GetItem(itemId);

                if (item != null && item.Type == "weapon")
                {
                    attack += item.Damage;
                }
            }

            return attack;
        }

        public bool HasItem(string itemId)
        {
            return Inventory.Contains(itemId);
        }

        public bool HasCompletedQuest(string questId)
        {
            return CompletedQuests.Contains(questId);
        }
    }
}