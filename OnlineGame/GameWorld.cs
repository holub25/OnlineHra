using System.Collections.Generic;

namespace OnlineGame
{
    internal class GameWorld
    {
        public Dictionary<string, Room> Rooms { get; private set; } = new();
        public Dictionary<string, Item> Items { get; private set; } = new();
        public Dictionary<string, Npc> Npcs { get; private set; } = new();
        public Dictionary<string, Quest> Quests { get; private set; } = new();

        public void SetRooms(List<Room> rooms)
        {
            Rooms.Clear();

            foreach (Room room in rooms)
            {
                Rooms[room.Id] = room;
            }
        }

        public void SetItems(List<Item> items)
        {
            Items.Clear();

            foreach (Item item in items)
            {
                Items[item.Id] = item;
            }
        }

        public void SetNpcs(List<Npc> npcs)
        {
            Npcs.Clear();

            foreach (Npc npc in npcs)
            {
                Npcs[npc.Id] = npc;
            }
        }

        public void SetQuests(List<Quest> quests)
        {
            Quests.Clear();

            foreach (Quest quest in quests)
            {
                Quests[quest.Id] = quest;
            }
        }

        public Room GetRoom(string id)
        {
            return Rooms.ContainsKey(id) ? Rooms[id] : null;
        }

        public Item GetItem(string id)
        {
            return Items.ContainsKey(id) ? Items[id] : null;
        }

        public Npc GetNpc(string id)
        {
            return Npcs.ContainsKey(id) ? Npcs[id] : null;
        }

        public Quest GetQuest(string id)
        {
            return Quests.ContainsKey(id) ? Quests[id] : null;
        }
    }
}