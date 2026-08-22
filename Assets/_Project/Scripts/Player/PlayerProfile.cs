using System;
using System.Collections.Generic;
using UnityEngine;

namespace M3P
{
    [Serializable]
    public class PlayerProfile
    {
        public int Level = LevelProgressionConfig.FirstLevel;

        /// <summary>Lifetime experience. <see cref="Level"/> is what the curve has already paid out for it.</summary>
        public int Experience;

        public int UnspentStatPoints;
        public List<CharacterSkill> Skills = new List<CharacterSkill>();

        /// <summary>
        /// Up to <see cref="SkillConfig.MaxLoadoutSize"/> owned skill ids taken into battle.
        /// Null or empty falls back to the first owned skills.
        /// </summary>
        public List<int> SkillLoadout;
        public List<OwnedCard> Cards = new List<OwnedCard>();

        /// <summary>
        /// Indices into <see cref="Cards"/> that form the battle deck. Null means every owned copy
        /// is in the deck, which is how saves written before the deck was separate behave.
        /// </summary>
        public List<int> Deck;

        public List<OwnedTile> Tiles = new List<OwnedTile>();

        /// <summary>
        /// Indices into <see cref="Tiles"/> that form the board tile deck. Null means every owned
        /// copy is in the deck, which is how saves written before the tile deck existed behave.
        /// </summary>
        public List<int> TileDeck;

        public List<ShardAmount> Shards = new List<ShardAmount>();
        public List<int> UnlockedTalentIds = new List<int>();
        public PendingTalentChoice PendingTalent;
        public HardStats HardStats;

        /// <summary>Current dungeon-map run: floor graph, player node and cleared encounters.</summary>
        public MapRunSave MapRun;

        public PlayerProfile()
        {
            HardStats = new HardStats(1, 1, 1, 1);
        }

        public CharacterStats CreateBattleStats(StatProgressionConfig progression, TalentConfig talentConfig)
        {
            TalentBonuses talentBonuses = talentConfig != null
                ? talentConfig.BuildBonuses(UnlockedTalentIds)
                : TalentBonuses.None;

            CharacterStats stats = new CharacterStats(HardStats, progression, talentBonuses);
            stats.RecalculateSoftStatsForBattle();
            return stats;
        }

        public bool HasTalentForMilestone(EStatType stat, int milestoneTier, TalentConfig talentConfig)
        {
            if (UnlockedTalentIds == null || talentConfig == null || milestoneTier <= 0)
                return false;

            for (int i = 0; i < UnlockedTalentIds.Count; i++)
            {
                if (!talentConfig.TryGetTalent(UnlockedTalentIds[i], out TalentDefinition talent))
                    continue;

                if (talent.Stat == stat && talent.MilestoneTier == milestoneTier)
                    return true;
            }

            return false;
        }

        public bool TryUnlockTalent(int talentId, TalentConfig talentConfig)
        {
            if (talentConfig == null || !PendingTalent.IsValid)
                return false;

            if (!talentConfig.TryGetTalent(talentId, out TalentDefinition talent))
                return false;

            if (talent.Stat != PendingTalent.Stat || talent.MilestoneTier != PendingTalent.MilestoneTier)
                return false;

            UnlockedTalentIds ??= new List<int>();
            UnlockedTalentIds.Add(talentId);
            PendingTalent = default;
            return true;
        }

        public bool HasSkill(int skillId)
        {
            if (Skills == null || skillId == SkillConfig.InvalidSkillId)
                return false;

            for (int i = 0; i < Skills.Count; i++)
            {
                if (Skills[i].SkillId == skillId)
                    return true;
            }

            return false;
        }

        /// <summary>Adds a skill the profile does not already own. Returns false when the id is invalid or already present.</summary>
        public bool TryAddSkill(int skillId, string name = "", int skillLevel = 1)
        {
            if (skillId == SkillConfig.InvalidSkillId || HasSkill(skillId))
                return false;

            Skills ??= new List<CharacterSkill>();
            Skills.Add(new CharacterSkill(skillId, Math.Max(1, skillLevel), name ?? ""));
            TryAddSkillToLoadout(skillId);
            return true;
        }

        /// <summary>Adds an owned skill to the first empty loadout slot. Ignored when the loadout is already full.</summary>
        public bool TryAddSkillToLoadout(int skillId)
        {
            if (!HasSkill(skillId))
                return false;

            SkillLoadout ??= new List<int>();
            if (SkillLoadout.Contains(skillId) || SkillLoadout.Count >= SkillConfig.MaxLoadoutSize)
                return false;

            SkillLoadout.Add(skillId);
            return true;
        }

        public bool TryRemoveLoadoutAt(int index)
        {
            if (SkillLoadout == null || index < 0 || index >= SkillLoadout.Count)
                return false;

            SkillLoadout.RemoveAt(index);
            return true;
        }

        /// <summary>Owned skill ids taken into battle, capped at <see cref="SkillConfig.MaxLoadoutSize"/>.</summary>
        public void CollectBattleSkillIds(List<int> destination)
        {
            destination.Clear();
            if (Skills == null)
                return;

            if (SkillLoadout != null)
            {
                for (int i = 0; i < SkillLoadout.Count && destination.Count < SkillConfig.MaxLoadoutSize; i++)
                {
                    int skillId = SkillLoadout[i];
                    if (skillId == SkillConfig.InvalidSkillId || !HasSkill(skillId) || destination.Contains(skillId))
                        continue;

                    destination.Add(skillId);
                }
            }

            if (destination.Count > 0)
                return;

            for (int i = 0; i < Skills.Count && destination.Count < SkillConfig.MaxLoadoutSize; i++)
            {
                int skillId = Skills[i].SkillId;
                if (skillId == SkillConfig.InvalidSkillId || destination.Contains(skillId))
                    continue;

                destination.Add(skillId);
            }
        }

        void DropInvalidLoadoutIds()
        {
            if (SkillLoadout == null)
                return;

            for (int i = SkillLoadout.Count - 1; i >= 0; i--)
            {
                int skillId = SkillLoadout[i];
                if (skillId == SkillConfig.InvalidSkillId || !HasSkill(skillId) || SkillLoadout.IndexOf(skillId) != i)
                    SkillLoadout.RemoveAt(i);
            }

            while (SkillLoadout.Count > SkillConfig.MaxLoadoutSize)
                SkillLoadout.RemoveAt(SkillLoadout.Count - 1);
        }

        /// <summary>Spends one level-up point on a stat. Returns false when there is nothing to spend.</summary>
        public bool TrySpendStatPoint(EStatType stat)
        {
            if (UnspentStatPoints <= 0)
                return false;

            HardStats = HardStats.WithPointsAdded(stat);
            UnspentStatPoints--;
            return true;
        }

        /// <summary>Shards of one colour currently banked, or zero for a colour never earned.</summary>
        public int GetShards(string tileType)
        {
            int index = IndexOfShards(tileType);
            return index >= 0 ? Shards[index].Amount : 0;
        }

        /// <summary>Banks shards of one colour. Amounts of zero or less are ignored.</summary>
        public void AddShards(string tileType, int amount)
        {
            if (amount <= 0 || string.IsNullOrEmpty(tileType))
                return;

            int index = IndexOfShards(tileType);
            if (index >= 0)
                Shards[index] = new ShardAmount(tileType, Shards[index].Amount + amount);
            else
                Shards.Add(new ShardAmount(tileType, amount));
        }

        /// <summary>True when the wallet covers every positive entry in <paramref name="costs"/>.</summary>
        public bool CanAffordCraftCost(IReadOnlyList<TileTypeShardCost> costs)
        {
            if (costs == null)
                return true;

            for (int i = 0; i < costs.Count; i++)
            {
                TileTypeShardCost cost = costs[i];
                if (cost.Amount <= 0 || cost.TileType == null)
                    continue;

                if (GetShards(cost.TileType.name) < cost.Amount)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Spends the craft cost and adds one owned copy. Returns false when the wallet cannot cover it.
        /// The new copy is not added to the battle deck; the cards panel does that.
        /// </summary>
        public bool TryCraftCard(int cardId, IReadOnlyList<TileTypeShardCost> costs)
        {
            if (cardId == CardConfig.InvalidCardId || !CanAffordCraftCost(costs))
                return false;

            SpendCraftCost(costs);
            Cards ??= new List<OwnedCard>();
            Cards.Add(new OwnedCard(cardId));
            return true;
        }

        /// <summary>
        /// Spends the craft cost and adds one owned tile copy, optionally with upgrades already
        /// attached. Returns false when the wallet cannot cover it. The new copy is added to the
        /// board tile deck when a deck list already exists.
        /// </summary>
        public bool TryCraftTile(int tileId, IReadOnlyList<TileTypeShardCost> costs, int[] upgradeIds = null)
        {
            if (tileId == TileConfig.InvalidTileId || !CanAffordCraftCost(costs))
                return false;

            SpendCraftCost(costs);
            Tiles ??= new List<OwnedTile>();
            Tiles.Add(new OwnedTile(tileId, upgradeIds));
            if (TileDeck != null)
                TileDeck.Add(Tiles.Count - 1);
            return true;
        }

        /// <summary>
        /// Spends the scaled craft cost and attaches one upgrade to an owned tile. Returns false when
        /// the slot is full or the wallet cannot cover it. <paramref name="costs"/> is the base cost;
        /// the spend is that amount times (existing upgrades + 1).
        /// </summary>
        public bool TryAddTileUpgrade(int ownedIndex, int upgradeId, IReadOnlyList<TileTypeShardCost> costs)
        {
            if (Tiles == null || ownedIndex < 0 || ownedIndex >= Tiles.Count)
                return false;

            if (upgradeId == TileUpgradeConfig.InvalidUpgradeId)
                return false;

            TileTypeShardCost[] scaledCosts = TileUpgradeDefinition.ScaleCraftCost(
                costs,
                TileUpgradeDefinition.GetCraftCostMultiplier(Tiles[ownedIndex].UpgradeCount));

            if (!CanAffordCraftCost(scaledCosts))
                return false;

            if (!Tiles[ownedIndex].TryAddUpgrade(upgradeId, out OwnedTile updated))
                return false;

            SpendCraftCost(scaledCosts);
            Tiles[ownedIndex] = updated;
            return true;
        }

        void SpendCraftCost(IReadOnlyList<TileTypeShardCost> costs)
        {
            if (costs == null)
                return;

            for (int i = 0; i < costs.Count; i++)
            {
                TileTypeShardCost cost = costs[i];
                if (cost.Amount <= 0 || cost.TileType == null)
                    continue;

                TrySpendShards(cost.TileType.name, cost.Amount);
            }
        }

        bool TrySpendShards(string tileType, int amount)
        {
            if (amount <= 0)
                return true;

            int index = IndexOfShards(tileType);
            if (index < 0 || Shards[index].Amount < amount)
                return false;

            Shards[index] = new ShardAmount(tileType, Shards[index].Amount - amount);
            return true;
        }

        int IndexOfShards(string tileType)
        {
            if (Shards == null || string.IsNullOrEmpty(tileType))
                return -1;

            for (int i = 0; i < Shards.Count; i++)
            {
                if (string.Equals(Shards[i].TileType, tileType, StringComparison.Ordinal))
                    return i;
            }

            return -1;
        }

        /// <summary>Fills in fields a save written before levels or cards existed never stored.</summary>
        public void NormalizeAfterLoad()
        {
            Level = Math.Max(LevelProgressionConfig.FirstLevel, Level);
            Experience = Math.Max(0, Experience);
            UnspentStatPoints = Math.Max(0, UnspentStatPoints);
            Skills ??= new List<CharacterSkill>();
            SkillLoadout ??= new List<int>();
            DropInvalidLoadoutIds();
            Cards ??= new List<OwnedCard>();
            for (int i = 0; i < Cards.Count; i++)
                Cards[i] = Cards[i].Normalized();
            DropInvalidDeckIndices();
            Tiles ??= new List<OwnedTile>();
            for (int i = 0; i < Tiles.Count; i++)
                Tiles[i] = Tiles[i].Normalized();
            DropInvalidTileDeckIndices();
            Shards ??= new List<ShardAmount>();
            UnlockedTalentIds ??= new List<int>();
        }

        public string ToJson(bool prettyPrint = true)
        {
            return JsonUtility.ToJson(PlayerProfileSaveData.FromProfile(this), prettyPrint);
        }

        public static PlayerProfile FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return new PlayerProfile();

            PlayerProfileSaveData data = JsonUtility.FromJson<PlayerProfileSaveData>(json);
            return data.ToProfile();
        }

        public void CopyFrom(PlayerProfile source)
        {
            if (source == null)
                return;

            Level = source.Level;
            Experience = source.Experience;
            UnspentStatPoints = source.UnspentStatPoints;
            Skills = source.Skills != null
                ? new List<CharacterSkill>(source.Skills)
                : new List<CharacterSkill>();
            SkillLoadout = source.SkillLoadout != null
                ? new List<int>(source.SkillLoadout)
                : null;
            Cards = CloneCards(source.Cards);
            Deck = source.Deck != null ? new List<int>(source.Deck) : null;
            Tiles = CloneTiles(source.Tiles);
            TileDeck = source.TileDeck != null ? new List<int>(source.TileDeck) : null;
            Shards = source.Shards != null
                ? new List<ShardAmount>(source.Shards)
                : new List<ShardAmount>();
            UnlockedTalentIds = source.UnlockedTalentIds != null
                ? new List<int>(source.UnlockedTalentIds)
                : new List<int>();
            PendingTalent = source.PendingTalent;
            HardStats = source.HardStats;
            MapRun = source.MapRun != null ? source.MapRun.Clone() : null;
        }

        [Serializable]
        struct PlayerProfileSaveData
        {
            public int Level;
            public int Experience;
            public int UnspentStatPoints;
            public CharacterSkill[] Skills;
            public int[] SkillLoadout;
            public OwnedCard[] Cards;
            public int[] Deck;
            public OwnedTile[] Tiles;
            public int[] TileDeck;
            public ShardAmount[] Shards;
            public int[] UnlockedTalentIds;
            public PendingTalentChoice PendingTalent;
            public HardStats HardStats;
            public MapRunSave MapRun;

            public static PlayerProfileSaveData FromProfile(PlayerProfile profile)
            {
                return new PlayerProfileSaveData
                {
                    Level = profile.Level,
                    Experience = profile.Experience,
                    UnspentStatPoints = profile.UnspentStatPoints,
                    Skills = profile.Skills != null ? profile.Skills.ToArray() : Array.Empty<CharacterSkill>(),
                    SkillLoadout = profile.SkillLoadout != null ? profile.SkillLoadout.ToArray() : null,
                    Cards = CloneCardArray(profile.Cards),
                    Deck = profile.Deck != null ? profile.Deck.ToArray() : null,
                    Tiles = CloneTileArray(profile.Tiles),
                    TileDeck = profile.TileDeck != null ? profile.TileDeck.ToArray() : null,
                    Shards = profile.Shards != null ? profile.Shards.ToArray() : Array.Empty<ShardAmount>(),
                    UnlockedTalentIds = profile.UnlockedTalentIds != null
                        ? profile.UnlockedTalentIds.ToArray()
                        : Array.Empty<int>(),
                    PendingTalent = profile.PendingTalent,
                    HardStats = profile.HardStats,
                    MapRun = profile.MapRun != null ? profile.MapRun.Clone() : null,
                };
            }

            public PlayerProfile ToProfile()
            {
                return new PlayerProfile
                {
                    Level = Level,
                    Experience = Experience,
                    UnspentStatPoints = UnspentStatPoints,
                    Skills = Skills != null
                        ? new List<CharacterSkill>(Skills)
                        : new List<CharacterSkill>(),
                    SkillLoadout = SkillLoadout != null ? new List<int>(SkillLoadout) : null,
                    Cards = CloneCards(Cards),
                    Deck = Deck != null ? new List<int>(Deck) : null,
                    Tiles = CloneTiles(Tiles),
                    TileDeck = TileDeck != null ? new List<int>(TileDeck) : null,
                    Shards = Shards != null
                        ? new List<ShardAmount>(Shards)
                        : new List<ShardAmount>(),
                    UnlockedTalentIds = UnlockedTalentIds != null
                        ? new List<int>(UnlockedTalentIds)
                        : new List<int>(),
                    PendingTalent = PendingTalent,
                    HardStats = HardStats,
                    MapRun = MapRun != null ? MapRun.Clone() : null,
                };
            }
        }

        /// <summary>
        /// Owned copies currently in the battle deck. Null deck means every owned copy is included.
        /// </summary>
        public IReadOnlyList<int> GetDeckIndices()
        {
            if (Deck != null)
                return Deck;

            if (Cards == null || Cards.Count == 0)
                return Array.Empty<int>();

            int[] indices = new int[Cards.Count];
            for (int i = 0; i < Cards.Count; i++)
                indices[i] = i;

            return indices;
        }

        public bool IsOwnedCardInDeck(int ownedIndex)
        {
            if (Cards == null || ownedIndex < 0 || ownedIndex >= Cards.Count)
                return false;

            if (Deck == null)
                return true;

            return Deck.Contains(ownedIndex);
        }

        public bool TryAddOwnedCardToDeck(int ownedIndex)
        {
            if (Cards == null || ownedIndex < 0 || ownedIndex >= Cards.Count)
                return false;

            EnsureDeckList();
            if (Deck.Contains(ownedIndex))
                return false;

            Deck.Add(ownedIndex);
            return true;
        }

        public bool TryRemoveDeckCardAt(int deckIndex)
        {
            EnsureDeckList();
            if (deckIndex < 0 || deckIndex >= Deck.Count)
                return false;

            Deck.RemoveAt(deckIndex);
            return true;
        }

        public void FillDeckWithAllOwnedCards()
        {
            Deck = new List<int>(Cards != null ? Cards.Count : 0);
            if (Cards == null)
                return;

            for (int i = 0; i < Cards.Count; i++)
                Deck.Add(i);
        }

        void EnsureDeckList()
        {
            if (Deck != null)
                return;

            FillDeckWithAllOwnedCards();
        }

        void DropInvalidDeckIndices()
        {
            if (Deck == null)
                return;

            int cardCount = Cards != null ? Cards.Count : 0;
            for (int i = Deck.Count - 1; i >= 0; i--)
            {
                if (Deck[i] < 0 || Deck[i] >= cardCount)
                    Deck.RemoveAt(i);
            }
        }

        /// <summary>
        /// Owned copies currently in the board tile deck. Null deck means every owned copy is included.
        /// </summary>
        public IReadOnlyList<int> GetTileDeckIndices()
        {
            if (TileDeck != null)
                return TileDeck;

            if (Tiles == null || Tiles.Count == 0)
                return Array.Empty<int>();

            int[] indices = new int[Tiles.Count];
            for (int i = 0; i < Tiles.Count; i++)
                indices[i] = i;

            return indices;
        }

        public bool IsOwnedTileInDeck(int ownedIndex)
        {
            if (Tiles == null || ownedIndex < 0 || ownedIndex >= Tiles.Count)
                return false;

            if (TileDeck == null)
                return true;

            return TileDeck.Contains(ownedIndex);
        }

        public bool TryAddOwnedTileToDeck(int ownedIndex)
        {
            if (Tiles == null || ownedIndex < 0 || ownedIndex >= Tiles.Count)
                return false;

            EnsureTileDeckList();
            if (TileDeck.Contains(ownedIndex))
                return false;

            TileDeck.Add(ownedIndex);
            return true;
        }

        public bool TryRemoveTileDeckAt(int deckIndex)
        {
            EnsureTileDeckList();
            if (deckIndex < 0 || deckIndex >= TileDeck.Count)
                return false;

            TileDeck.RemoveAt(deckIndex);
            return true;
        }

        public void FillTileDeckWithAllOwnedTiles()
        {
            TileDeck = new List<int>(Tiles != null ? Tiles.Count : 0);
            if (Tiles == null)
                return;

            for (int i = 0; i < Tiles.Count; i++)
                TileDeck.Add(i);
        }

        public bool TryRemoveOwnedTileAt(int ownedIndex)
        {
            if (Tiles == null || ownedIndex < 0 || ownedIndex >= Tiles.Count)
                return false;

            Tiles.RemoveAt(ownedIndex);
            if (TileDeck == null)
                return true;

            for (int i = TileDeck.Count - 1; i >= 0; i--)
            {
                if (TileDeck[i] == ownedIndex)
                    TileDeck.RemoveAt(i);
                else if (TileDeck[i] > ownedIndex)
                    TileDeck[i]--;
            }

            return true;
        }

        void EnsureTileDeckList()
        {
            if (TileDeck != null)
                return;

            FillTileDeckWithAllOwnedTiles();
        }

        void DropInvalidTileDeckIndices()
        {
            if (TileDeck == null)
                return;

            int tileCount = Tiles != null ? Tiles.Count : 0;
            for (int i = TileDeck.Count - 1; i >= 0; i--)
            {
                if (TileDeck[i] < 0 || TileDeck[i] >= tileCount)
                    TileDeck.RemoveAt(i);
            }
        }

        static List<OwnedCard> CloneCards(IReadOnlyList<OwnedCard> source)
        {
            if (source == null || source.Count == 0)
                return new List<OwnedCard>();

            List<OwnedCard> copy = new List<OwnedCard>(source.Count);
            for (int i = 0; i < source.Count; i++)
                copy.Add(source[i].Clone());

            return copy;
        }

        static OwnedCard[] CloneCardArray(List<OwnedCard> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<OwnedCard>();

            OwnedCard[] copy = new OwnedCard[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i].Clone();

            return copy;
        }

        static List<OwnedTile> CloneTiles(IReadOnlyList<OwnedTile> source)
        {
            if (source == null || source.Count == 0)
                return new List<OwnedTile>();

            List<OwnedTile> copy = new List<OwnedTile>(source.Count);
            for (int i = 0; i < source.Count; i++)
                copy.Add(source[i].Clone());

            return copy;
        }

        static OwnedTile[] CloneTileArray(List<OwnedTile> source)
        {
            if (source == null || source.Count == 0)
                return Array.Empty<OwnedTile>();

            OwnedTile[] copy = new OwnedTile[source.Count];
            for (int i = 0; i < source.Count; i++)
                copy[i] = source[i].Clone();

            return copy;
        }
    }
}
