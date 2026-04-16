using System.Text;

namespace OnlineGame
{
    internal class GameEngine
    {
        private readonly GameWorld world;
        private readonly Dictionary<string, int> npcCurrentHealth = new();

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

                case "utoc":
                    return AttackNpc(player, argument);

                case "pouzij":
                    return UseItem(player, argument);

                case "kup":
                    return BuyItem(player, argument);

                case "questy":
                    return ShowQuests(player);

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
mluv <npc> - promluví s NPC
utoc <npc> - zaútočí na NPC
pouzij <předmět> - použije předmět z inventáře
kup <předmět> - koupí předmět od obchodníka
questy - zobrazí úkoly";
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

            sb.AppendLine($"Životy: {player.Health}/{player.MaxHealth}");
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

            TryActivateTalkQuest(player, targetNpc, room);

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

        public string AttackNpc(PlayerState player, string npcName)
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

            Npc npc = world.GetNpc(npcId);

            if (npc == null)
            {
                return "NPC neexistuje.";
            }

            if (!npc.IsHostile && !npc.IsFinalBoss)
            {
                return "Na tuto postavu nemůžeš útočit.";
            }

            int npcHealth = GetNpcHealth(npc);
            int playerDamage = player.GetAttack(world);

            npcHealth -= playerDamage;
            npcCurrentHealth[npc.Id] = npcHealth;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"Zaútočil jsi na {npc.Name} a způsobil jsi {playerDamage} poškození.");

            if (npcHealth > 0)
            {
                player.Health -= npc.Attack;
                sb.AppendLine($"{npc.Name} přežil. Zbývá mu {npcHealth} životů.");
                sb.AppendLine($"{npc.Name} ti oplatil útok za {npc.Attack} poškození.");

                if (player.Health <= 0)
                {
                    HandlePlayerDeath(player);
                    sb.AppendLine("Byl jsi poražen. Probudil ses znovu na palubě se 100 životy.");
                }

                return sb.ToString().TrimEnd();
            }

            room.Npcs.Remove(npc.Id);
            npcCurrentHealth.Remove(npc.Id);

            sb.AppendLine($"Porazil jsi {npc.Name}.");

            string dropResult = GiveNpcDrops(room, npc);
            if (!string.IsNullOrWhiteSpace(dropResult))
            {
                sb.AppendLine(dropResult);
            }

            string questResult = TryCompleteKillQuest(player, npc, room);
            if (!string.IsNullOrWhiteSpace(questResult))
            {
                sb.AppendLine(questResult);
            }

            if (npc.IsFinalBoss)
            {
                player.GameCompleted = true;
                sb.AppendLine("VYHRÁL JSI HRU. Převzal jsi kontrolu nad pirátskou lodí.");
            }

            return sb.ToString().TrimEnd();
        }

        public string UseItem(PlayerState player, string itemName)
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

            Item itemToUse = world.GetItem(itemId);

            if (itemToUse == null)
            {
                return "Předmět neexistuje.";
            }

            if (itemToUse.Type != "consumable")
            {
                return "Tento předmět nejde použít.";
            }

            int oldHealth = player.Health;
            player.Health += itemToUse.Heal;

            if (player.Health > player.MaxHealth)
            {
                player.Health = player.MaxHealth;
            }

            player.Inventory.Remove(itemId);

            int healed = player.Health - oldHealth;
            return $"Použil jsi {itemToUse.Name}. Obnovil sis {healed} životů.";
        }

        public string BuyItem(PlayerState player, string itemName)
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

            Npc merchant = room.Npcs
                .Select(id => world.GetNpc(id))
                .FirstOrDefault(npc => npc != null && npc.IsMerchant);

            if (merchant == null)
            {
                return "V této místnosti není žádný obchodník.";
            }

            string itemId = merchant.ShopItems.FirstOrDefault(id =>
            {
                Item item = world.GetItem(id);
                return item != null && item.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase);
            });

            if (itemId == null)
            {
                return $"{merchant.Name} tento předmět neprodává.";
            }

            if (player.Inventory.Count >= player.InventoryCapacity)
            {
                return "Inventář je plný.";
            }

            Item itemToBuy = world.GetItem(itemId);

            if (itemToBuy == null)
            {
                return "Předmět neexistuje.";
            }

            int price = GetItemPrice(itemToBuy);

            if (player.Gold < price)
            {
                return $"Nemáš dost zlata. Potřebuješ {price} zlata.";
            }

            player.Gold -= price;
            player.Inventory.Add(itemId);

            return $"Koupil jsi {itemToBuy.Name} za {price} zlata.";
        }

        public string ShowQuests(PlayerState player)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Questy:");

            bool hasAny = false;

            foreach (Quest quest in world.Quests.Values)
            {
                string status;

                if (player.HasCompletedQuest(quest.Id))
                {
                    status = "splněn";
                    hasAny = true;
                }
                else if (player.HasActiveQuest(quest.Id))
                {
                    status = "aktivní";
                    hasAny = true;
                }
                else
                {
                    continue;
                }

                sb.AppendLine($"- {quest.Name} ({status})");
                sb.AppendLine($"  {quest.Description}");
            }

            if (!hasAny)
            {
                sb.AppendLine("Žádné aktivní ani splněné questy.");
            }

            return sb.ToString().TrimEnd();
        }

        private int GetNpcHealth(Npc npc)
        {
            if (!npcCurrentHealth.ContainsKey(npc.Id))
            {
                npcCurrentHealth[npc.Id] = npc.Health;
            }

            return npcCurrentHealth[npc.Id];
        }

        private void HandlePlayerDeath(PlayerState player)
        {
            player.Health = player.MaxHealth;
            player.CurrentRoomId = "paluba";
        }

        private string GiveNpcDrops(Room room, Npc npc)
        {
            if (npc.Drops == null || npc.Drops.Count == 0)
            {
                return null;
            }

            List<string> droppedNames = new List<string>();

            foreach (string dropId in npc.Drops)
            {
                Item item = world.GetItem(dropId);

                if (item != null)
                {
                    room.Items.Add(dropId);
                    droppedNames.Add(item.Name);
                }
            }

            if (droppedNames.Count == 0)
            {
                return null;
            }

            return $"Nepřítel upustil: {string.Join(", ", droppedNames)}";
        }

        private void TryActivateTalkQuest(PlayerState player, Npc npc, Room room)
        {
            foreach (Quest quest in world.Quests.Values)
            {
                if (player.HasCompletedQuest(quest.Id) || player.HasActiveQuest(quest.Id))
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

                player.ActiveQuests.Add(quest.Id);
            }
        }

        private string TryCompleteTalkQuest(PlayerState player, Npc npc, Room room)
        {
            foreach (Quest quest in world.Quests.Values)
            {
                if (player.HasCompletedQuest(quest.Id))
                {
                    continue;
                }

                if (!player.HasActiveQuest(quest.Id))
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
                    return $"Pro splnění úkolu ještě potřebuješ předmět: {GetItemDisplayName(quest.RequiredItemId)}";
                }

                player.ActiveQuests.Remove(quest.Id);
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

        private string TryCompleteKillQuest(PlayerState player, Npc npc, Room room)
        {
            foreach (Quest quest in world.Quests.Values)
            {
                if (player.HasCompletedQuest(quest.Id))
                {
                    continue;
                }

                if (quest.QuestType != "kill_npc")
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

                player.ActiveQuests.Remove(quest.Id);
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

                return sb.ToString().TrimEnd();
            }

            return null;
        }

        private int GetItemPrice(Item item)
        {
            if (item.Type == "consumable")
            {
                return 10;
            }

            if (item.Type == "weapon")
            {
                return 30;
            }

            if (item.Type == "key")
            {
                return 20;
            }

            return 15;
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