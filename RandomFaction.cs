using Oxide.Core.Configuration;
using Oxide.Core;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Random Faction", "NoSoyLito", "0.0.1")]
    [Description("Server PVP Gamemode that randomly divides the server population in two factions")]
    public class RandomFaction : RustPlugin
    {
        #region "Fields"
        private PluginConfig config;
        #endregion

        #region "Oxide Hooks"

        void Init()
        {
            // Initiate config system
            config = Config.ReadObject<PluginConfig>();
            // Initiate data system
            InitiateData();
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("RandomFaction");
        }

        void OnNewSave(string filename)
        {
            if (config.ResetDataOnWipe)
            {
                WipeDataFile();
                Puts("Data file has been cleared because of a map wipe.");
            }
        }

        object OnPlayerDeath(BasePlayer victim, HitInfo info)
        {
            if (victim != null && info != null && info.InitiatorPlayer != null && !info.InitiatorPlayer.IsNpc)
            {
                PlayerData victimData = GetPlayerData(victim);
                PlayerData killerData = GetPlayerData(info.InitiatorPlayer);

                if (victimData.faction == 0 && killerData.faction == 0)
                {
                    victimData = SetRandomFaction(victimData);
                    switch (victimData.faction)
                    {
                        case 1:
                            killerData = SetFaction(killerData, 2);
                            break;
                        case 2:
                            killerData = SetFaction(killerData, 1);
                            break;
                    }
                }
                else if (victimData.faction == 0 && killerData.faction != 0)
                {
                    switch (killerData.faction)
                    {
                        case 1:
                            victimData = SetFaction(victimData, 2);
                            break;
                        case 2:
                            victimData = SetFaction(victimData, 1);
                            break;
                    }
                }
                else if (victimData.faction != 0 && killerData.faction == 0)
                {
                    switch (victimData.faction)
                    {
                        case 1:
                            killerData = SetFaction(killerData, 2);
                            break;
                        case 2:
                            killerData = SetFaction(killerData, 1);
                            break;
                    }
                }
                AddKillToPlayer(info.InitiatorPlayer);
                AddDeathToPlayer(victim);
            }
            return null;
        }

        bool CanBeWounded(BasePlayer player, HitInfo info)
        {
            if (player != null && info != null && info.InitiatorPlayer != null && !info.InitiatorPlayer.IsNpc)
            {
                PlayerData victimData = GetPlayerData(player);
                PlayerData attackerData = GetPlayerData(info.InitiatorPlayer);
                if (victimData.faction == attackerData.faction)
                {
                    if (victimData.faction != 0 || attackerData.faction != 0)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        object OnPlayerAssist(BasePlayer target, BasePlayer player)
        {
            if (target != null && player != null)
            {
                PlayerData targetData = GetPlayerData(target);
                PlayerData playerData = GetPlayerData(player);
                if (targetData.faction != playerData.faction && targetData.faction != 0 && playerData.faction != 0)
                {
                    rust.SendChatMessage(target, ColorString("[RandomFactions]", "#cc99ff"), "You can't be assisted by members of the opposite faction!");
                    rust.SendChatMessage(player, ColorString("[RandomFactions]", "#cc99ff"), "You can't assist members of the opposite faction!");
                    return false;
                }
            }
            return null;
        }

        object OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            // IF PLAYERS ARE FROM THE SALME FACTION THEY CAN'T DAMAGE EACH OTHER'S BUILDING
            Puts("OnEntityTakeDamage works!");
            return null;
        }

        #endregion

        #region "Factions"

        #endregion

        #region "OnPlayerDeath"

        #endregion

        #region "Config logic"
        private class PluginConfig
        {
            public bool ResetDataOnWipe;
            public string FactionAName;
            public string FactionBName;
            public int BountyPerOppositeFactionHead;
        }

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                ResetDataOnWipe = true,
                FactionAName = "Faction A",
                FactionBName = "Faction B",
                BountyPerOppositeFactionHead = 50
            };
        }
        #endregion

        #region "DataFile logic"

        private DynamicConfigFile dataFile;
        private StoredData storedData;

        // StoreData class to store list of players on memory
        private class StoredData
        {
            public Dictionary<ulong, PlayerData> playerList = new Dictionary<ulong, PlayerData>();

            public StoredData() { }
        }

        // PlayerData class to manage player data
        private class PlayerData
        {
            public ulong id;
            public string name;
            public int faction;
            public int kills;
            public int deaths;

            public PlayerData() { }

            public PlayerData(BasePlayer player)
            {
                id = player.userID;
                name = player.displayName;
                faction = 0;
                kills = 0;
                deaths = 0;
            }
        }

        private void InitiateData()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("RandomFaction");
        }

        private bool DataFileExists()
        {
            if (Interface.Oxide.DataFileSystem.ExistsDatafile("RandomFaction"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void WipeDataFile()
        {
            if (DataFileExists())
            {
                dataFile = Interface.Oxide.DataFileSystem.GetDatafile("RandomFaction");
                dataFile.Clear();
                dataFile.Save();
            }
        }

        private void LoadDataFile()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("RandomFaction");
        }

        private void SaveDataFile()
        {
            Interface.Oxide.DataFileSystem.WriteObject("RandomFaction", storedData);
        }

        private bool IsPlayerInFaction(BasePlayer player)
        {
            if (storedData.playerList.ContainsKey(player.userID))
            {
                return true;
            }
            return false;
        }

        private bool ArePlayersFriends(BasePlayer player1, BasePlayer player2)
        {
            if (GetPlayerData(player1).faction == GetPlayerData(player2).faction)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private PlayerData GetPlayerData(BasePlayer player)
        {
            if (storedData.playerList.ContainsKey(player.userID))
            {
                return storedData.playerList[player.userID];
            }
            else
            {
                PlayerData playerData = new PlayerData(player);
                storedData.playerList.Add(player.userID, playerData);
                SaveDataFile();
                return playerData;
            }
        }

        private PlayerData SetRandomFaction(PlayerData playerData)
        {
            playerData.faction = Random.Range(1, 2);
            storedData.playerList.Add(playerData.id, playerData);
            SaveDataFile();
            return playerData;
        }

        private PlayerData SetFaction(PlayerData playerData, int fact)
        {
            playerData.faction = fact;
            storedData.playerList.Add(playerData.id, playerData);
            SaveDataFile();
            return playerData;
        }

        private void AddKillToPlayer(BasePlayer player)
        {
            storedData.playerList[player.userID].kills += 1;
            SaveDataFile();
        }
        private void AddDeathToPlayer(BasePlayer player)
        {
            storedData.playerList[player.userID].deaths += 1;
            SaveDataFile();
        }

        #endregion

        #region "Helpers"

        string ColorString(string text, string color)
        {
            return "<color=" + color + ">" + text + "</color>";
        }

        #endregion
    }
}
