using System.Collections.Generic;

namespace OnlineGame
{
    internal class Room
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public Dictionary<string, string> Exits { get; set; } = new();
        public List<string> Items { get; set; } = new();
        public List<string> Npcs { get; set; } = new();

        public bool IsLocked { get; set; }
        public string RequiredItemId { get; set; }
        public string RequiredQuestId { get; set; }
    }
}