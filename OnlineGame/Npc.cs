using System.Collections.Generic;

namespace OnlineGame
{
    internal class Npc
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public string Description { get; set; }

        public List<string> Dialogues { get; set; } = new();

        public int Health { get; set; }
        public int Attack { get; set; }

        public bool IsHostile { get; set; }
        public bool IsMerchant { get; set; }
        public bool IsFinalBoss { get; set; }

        public List<string> Drops { get; set; } = new();
        public List<string> ShopItems { get; set; } = new();
    }
}