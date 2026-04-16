namespace OnlineGame
{
    internal class Quest
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public string QuestType { get; set; }

        public string RequiredItemId { get; set; }
        public string TargetNpcId { get; set; }
        public string TargetRoomId { get; set; }

        public string RewardItemId { get; set; }
        public int RewardGold { get; set; }

        public string UnlocksRoomId { get; set; }
        public bool MarksGameProgress { get; set; }
    }
}