using System.Text;

namespace OnlineGame
{
    internal class GameEngine
    {
        private readonly GameWorld world;

        public GameEngine(GameWorld world)
        {
            this.world = world;
        }

        public string HandleCommand(PlayerState player, string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return "Zadej příkaz.";
            }

            string[] parts = input.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string command = parts[0].ToLower();
            string argument = parts.Length > 1 ? parts[1].Trim().ToLower() : "";

            switch (command)
            {
                case "pomoc":
                    return Help();

                case "prozkoumej":
                    return Look(player);

                case "jdi":
                    return Move(player, argument);

                case "vezmi":
                    return TakeItem(player, argument);

                case "poloz":
                    return DropItem(player, argument);

                case "inventar":
                    return ShowInventory(player);

                case "mluv":
                    return TalkToNpc(player, argument);

                default:
                    return "Neznámý příkaz. Napiš pomoc.";
            }
        }

        public string Help()
        {
            return
@"Dostupné příkazy:
pomoc - zobrazí nápovědu
prozkoumej - zobrazí aktuální místnost
jdi <směr> - přesun do jiné místnosti
vezmi <předmět> - vezme předmět z místnosti
poloz <předmět> - položí předmět do místnosti
inventar - zobrazí inventář
mluv <npc> - promluví s NPC";
        }

        public string Look(PlayerState player)
        {
            Room room = world.GetRoom(player.CurrentRoomId);

            if (room == null)
            {
                return "Aktuální místnost neexistuje.";
            }

            string exits = room.Exits.Count > 0
                ? string.Join(", ", room.Exits.Keys)
                : "žádné";

            string items = room.Items.Count > 0
                ? string.Join(", ", room.Items.Select(GetItemDisplayName))
                : "žádné";

            string npcs = room.Npcs.Count > 0
                ? string.Join(", ", room.Npcs.Select(GetNpcDisplayName))
                : "žádné";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Jsi v místnosti: {room.Name}");
            sb.AppendLine($"Popis: {room.Description}");
            sb.AppendLine($"Východy: {exits}");
            sb.AppendLine($"Předměty: {items}");
            sb.AppendLine($"NPC: {npcs}");

            return sb.ToString().TrimEnd();
        }

        public string Move(PlayerState player, string direction)
        {
            if (string.IsNullOrWhiteSpace(direction))
            {
                return "Musíš zadat směr.";
            }

            Room currentRoom = world.GetRoom(player.CurrentRoomId);

            if (currentRoom == null)
            {
                return "Aktuální místnost neexistuje.";
            }

            if (!currentRoom.Exits.ContainsKey(direction))
            {
                return "Tímto směrem se nedá jít.";
            }

            string targetRoomId = currentRoom.Exits[direction];
            Room targetRoom = world.GetRoom(targetRoomId);

            if (targetRoom == null)
            {
                return "Cílová místnost neexistuje.";
            }

            if (targetRoom.IsLocked)
            {
                if (!string.IsNullOrWhiteSpace(targetRoom.RequiredItemId) && !player.HasItem(targetRoom.RequiredItemId))
                {
                    Item requiredItem = world.GetItem(targetRoom.RequiredItemId);
                    string itemName = requiredItem != null ? requiredItem.Name : targetRoom.RequiredItemId;

                    return $"Nemůžeš vstoupit. Potřebuješ předmět: {itemName}";
                }

                if (!string.IsNullOrWhiteSpace(targetRoom.RequiredQuestId) && !player.HasCompletedQuest(targetRoom.RequiredQuestId))
                {
                    Quest requiredQuest = world.GetQuest(targetRoom.RequiredQuestId);
                    string questName = requiredQuest != null ? requiredQuest.Name : targetRoom.RequiredQuestId;

                    return $"Nemůžeš vstoupit. Nejprve musíš splnit úkol: {questName}";
                }
            }

            if (!string.IsNullOrWhiteSpace(targetRoom.RequiredQuestId) && !player.HasCompletedQuest(targetRoom.RequiredQuestId))
            {
                Quest requiredQuest = world.GetQuest(targetRoom.RequiredQuestId);
                string questName = requiredQuest != null ? requiredQuest.Name : targetRoom.RequiredQuestId;

                return $"Nemůžeš pokračovat. Nejprve musíš splnit úkol: {questName}";
            }

            player.CurrentRoomId = targetRoom.Id;
            return Look(player);
        }

        public string TakeItem(PlayerState player, string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return "Musíš zadat název předmětu.";
            }

            Room room = world.GetRoom(player.CurrentRoomId);

            if (room == null)
            {
                return "Aktuální místnost neexistuje.";
            }

            string itemId = room.Items.FirstOrDefault(id =>
            {
                Item item = world.GetItem(id);
                return item != null && item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase);
            });

            if (itemId == null)
            {
                return "Takový předmět tu není.";
            }

            if (player.Inventory.Count >= player.InventoryCapacity)
            {
                return "Inventář je plný.";
            }

            Item pickedItem = world.GetItem(itemId);

            room.Items.Remove(itemId);

            if (pickedItem.Type == "currency")
            {
                player.Gold += pickedItem.GoldValue;
                return $"Sebral jsi {pickedItem.Name}. Získal jsi {pickedItem.GoldValue} zlata.";
            }

            player.Inventory.Add(itemId);
            return $"Sebral jsi předmět: {pickedItem.Name}";
        }

        public string DropItem(PlayerState player, string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName))
            {
                return "Musíš zadat název předmětu.";
            }

            string itemId = player.Inventory.FirstOrDefault(id =>
            {
                Item item = world.GetItem(id);
                return item != null && item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase);
            });

            if (itemId == null)
            {
                return "Takový předmět nemáš.";
            }

            Item itemToDrop = world.GetItem(itemId);

            if (itemToDrop == null)
            {
                return "Předmět neexistuje.";
            }

            if (!itemToDrop.CanBeDropped)
            {
                return "Tento předmět nelze odložit.";
            }

            Room room = world.GetRoom(player.CurrentRoomId);

            if (room == null)
            {
                return "Aktuální místnost neexistuje.";
            }

            player.Inventory.Remove(itemId);
            room.Items.Add(itemId);

            return $"Položil jsi předmět: {itemToDrop.Name}";
        }

        public string ShowInventory(PlayerState player)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"Inventář ({player.Inventory.Count}/{player.InventoryCapacity}):");

            if (player.Inventory.Count == 0)
            {
                sb.AppendLine("prázdný");
            }
            else
            {
                foreach (string itemId in player.Inventory)
                {
                    Item item = world.GetItem(itemId);

                    if (item != null)
                    {
                        sb.AppendLine($"- {item.Name}");
                    }
                }
            }

            sb.AppendLine($"Životy: {player.Health}");
            sb.AppendLine($"Síla: {player.GetAttack(world)}");
            sb.AppendLine($"Zlato: {player.Gold}");

            return sb.ToString().TrimEnd();
        }

        public string TalkToNpc(PlayerState player, string npcName)
        {
            if (string.IsNullOrWhiteSpace(npcName))
            {
                return "Musíš zadat jméno NPC.";
            }

            Room room = world.GetRoom(player.CurrentRoomId);

            if (room == null)
            {
                return "Aktuální místnost neexistuje.";
            }

            string npcId = room.Npcs.FirstOrDefault(id =>
            {
                Npc npc = world.GetNpc(id);
                return npc != null && npc.Name.Equals(npcName, StringComparison.OrdinalIgnoreCase);
            });

            if (npcId == null)
            {
                return "Taková postava tu není.";
            }

            Npc targetNpc = world.GetNpc(npcId);

            if (targetNpc == null)
            {
                return "NPC neexistuje.";
            }

            string questResult = TryCompleteTalkQuest(player, targetNpc, room);
            if (!string.IsNullOrWhiteSpace(questResult))
            {
                return questResult;
            }

            if (targetNpc.Dialogues.Count == 0)
            {
                return $"{targetNpc.Name} mlčí.";
            }

            return $"{targetNpc.Name}: {targetNpc.Dialogues[0]}";
        }

        private string TryCompleteTalkQuest(PlayerState player, Npc npc, Room room)
        {
            foreach (Quest quest in world.Quests.Values)
            {
                if (player.HasCompletedQuest(quest.Id))
                {
                    continue;
                }

                if (quest.QuestType != "talk_after_item")
                {
                    continue;
                }

                if (quest.TargetNpcId != npc.Id)
                {
                    continue;
                }

                if (quest.TargetRoomId != room.Id)
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(quest.RequiredItemId) && !player.HasItem(quest.RequiredItemId))
                {
                    continue;
                }

                player.CompletedQuests.Add(quest.Id);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine($"Splnil jsi úkol: {quest.Name}");

                if (!string.IsNullOrWhiteSpace(quest.RewardItemId))
                {
                    Item rewardItem = world.GetItem(quest.RewardItemId);

                    if (rewardItem != null)
                    {
                        player.Inventory.Add(rewardItem.Id);
                        sb.AppendLine($"Získal jsi předmět: {rewardItem.Name}");
                    }
                }

                if (quest.RewardGold > 0)
                {
                    player.Gold += quest.RewardGold;
                    sb.AppendLine($"Získal jsi {quest.RewardGold} zlata.");
                }

                sb.AppendLine($"{npc.Name}: {npc.Dialogues.FirstOrDefault() ?? "Děkuji."}");

                return sb.ToString().TrimEnd();
            }

            return null;
        }

        private string GetItemDisplayName(string itemId)
        {
            Item item = world.GetItem(itemId);
            return item != null ? item.Name : itemId;
        }

        private string GetNpcDisplayName(string npcId)
        {
            Npc npc = world.GetNpc(npcId);
            return npc != null ? npc.Name : npcId;
        }
    }
}