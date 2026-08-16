using System;
using System.Collections.Generic;
using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// The build a character begins the game with, used to seed a profile that has never been saved.
    /// Skills, the starter card deck and the starter tile deck are authored as assets here and stored
    /// as ids in the profile.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStartConfig", menuName = "M3P/Player Start Config", order = 2)]
    public class PlayerStartConfig : ScriptableObject
    {
        [SerializeField] HardStats _hardStats = new HardStats(1, 1, 1, 1);

        [Tooltip("Skills the character owns from the first battle. Each must be registered in the skill config.")]
        [SerializeField] SkillDefinition[] _skills = Array.Empty<SkillDefinition>();

        [Tooltip("Copied into a new profile as owned cards. Each copy becomes its own entry with empty upgrades.")]
        [SerializeField] DeckDefinition _starterDeck;

        [Tooltip("Copied into a new profile as owned tiles. Each copy becomes its own entry with empty upgrades.")]
        [SerializeField] TileDeckDefinition _starterTileDeck;

        public HardStats HardStats => _hardStats;

        public SkillDefinition[] Skills => _skills ?? Array.Empty<SkillDefinition>();

        public DeckDefinition StarterDeck => _starterDeck;

        public TileDeckDefinition StarterTileDeck => _starterTileDeck;

        public PlayerProfile CreateProfile(SkillConfig skillConfig, CardConfig cardConfig, TileConfig tileConfig)
        {
            PlayerProfile profile = new PlayerProfile { HardStats = _hardStats };
            CopyStartingSkills(profile, skillConfig);
            CopyStarterDeck(profile, cardConfig);
            CopyStarterTileDeck(profile, tileConfig);
            return profile;
        }

        /// <summary>
        /// Fills an empty card list from the starter deck. Used for new profiles and for saves written
        /// before cards lived on the profile.
        /// </summary>
        public void EnsureStarterCards(PlayerProfile profile, CardConfig cardConfig)
        {
            if (profile == null)
                return;

            profile.Cards ??= new List<OwnedCard>();
            if (profile.Cards.Count > 0)
                return;

            CopyStarterDeck(profile, cardConfig);
        }

        /// <summary>
        /// Fills an empty tile list from the starter tile deck. Used for new profiles and for saves
        /// written before tiles lived on the profile.
        /// </summary>
        public void EnsureStarterTiles(PlayerProfile profile, TileConfig tileConfig)
        {
            if (profile == null)
                return;

            profile.Tiles ??= new List<OwnedTile>();
            if (profile.Tiles.Count > 0)
                return;

            CopyStarterTileDeck(profile, tileConfig);
        }

        void CopyStartingSkills(PlayerProfile profile, SkillConfig skillConfig)
        {
            SkillDefinition[] skills = Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                SkillDefinition skill = skills[i];
                if (skill == null)
                    continue;

                int skillId = skillConfig != null ? skillConfig.GetSkillId(skill) : SkillConfig.InvalidSkillId;
                if (skillId == SkillConfig.InvalidSkillId)
                {
                    Debug.LogError(
                        $"{nameof(PlayerStartConfig)} '{name}': skill '{skill.name}' is not registered in {nameof(SkillConfig)}, so it cannot be saved to a profile.",
                        this);
                    continue;
                }

                profile.Skills.Add(new CharacterSkill(skillId, 1, skill.name));
            }
        }

        void CopyStarterDeck(PlayerProfile profile, CardConfig cardConfig)
        {
            profile.Cards.Clear();

            if (_starterDeck == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerStartConfig)} '{name}': assign {nameof(_starterDeck)} or new characters begin with no cards.",
                    this);
                return;
            }

            if (cardConfig == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerStartConfig)} '{name}': assign {nameof(CardConfig)} on {nameof(GameConfig)} or starter cards cannot be saved to a profile.",
                    this);
                return;
            }

            DeckDefinition.Entry[] entries = _starterDeck.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                BoardActionCardDefinition card = entries[i].Card;
                if (card == null)
                    continue;

                int cardId = cardConfig.GetCardId(card);
                if (cardId == CardConfig.InvalidCardId)
                {
                    Debug.LogError(
                        $"{nameof(PlayerStartConfig)} '{name}': card '{card.name}' is not registered in {nameof(CardConfig)}, so it cannot be saved to a profile.",
                        this);
                    continue;
                }

                int copies = Mathf.Max(1, entries[i].Copies);
                for (int copy = 0; copy < copies; copy++)
                    profile.Cards.Add(new OwnedCard(cardId));
            }

            profile.FillDeckWithAllOwnedCards();
        }

        void CopyStarterTileDeck(PlayerProfile profile, TileConfig tileConfig)
        {
            profile.Tiles.Clear();

            if (_starterTileDeck == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerStartConfig)} '{name}': assign {nameof(_starterTileDeck)} or new characters begin with no tiles.",
                    this);
                return;
            }

            if (tileConfig == null)
            {
                Debug.LogError(
                    $"{nameof(PlayerStartConfig)} '{name}': assign {nameof(TileConfig)} on {nameof(GameConfig)} or starter tiles cannot be saved to a profile.",
                    this);
                return;
            }

            TileDeckDefinition.Entry[] entries = _starterTileDeck.Entries;
            for (int i = 0; i < entries.Length; i++)
            {
                Match3TileTypeDefinition tile = entries[i].Tile;
                if (tile == null)
                    continue;

                int tileId = tileConfig.GetTileId(tile);
                if (tileId == TileConfig.InvalidTileId)
                {
                    Debug.LogError(
                        $"{nameof(PlayerStartConfig)} '{name}': tile '{tile.name}' is not registered in {nameof(TileConfig)}, so it cannot be saved to a profile.",
                        this);
                    continue;
                }

                int copies = Mathf.Max(1, entries[i].Copies);
                for (int copy = 0; copy < copies; copy++)
                    profile.Tiles.Add(new OwnedTile(tileId));
            }

            profile.FillTileDeckWithAllOwnedTiles();
        }
    }
}
