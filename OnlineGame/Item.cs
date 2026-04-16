namespace OnlineGame
{
    internal class Item
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string Type { get; set; }
        public int Damage { get; set; }
        public int Heal { get; set; }
        public int GoldValue { get; set; }

        public bool CanBeDropped { get; set; }
        public bool QuestItem { get; set; }
    }
}