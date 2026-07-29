using Match3;
using UnityEngine;

namespace M3P
{
    /// <summary>
    /// A card the player plays to reshape the board. Cards never deal damage directly — damage comes
    /// from the matches they set up and from skills paid for with the mana those matches produce.
    /// </summary>
    [CreateAssetMenu(fileName = "Card", menuName = "M3P/Board Action Card", order = 20)]
    public class BoardActionCardDefinition : ScriptableObject
    {
        [SerializeField] string _displayName = "Card";
        [TextArea, SerializeField] string _description;
        [SerializeField] Sprite _artwork;

        [Tooltip("Action points spent when this card is played.")]
        [Min(0), SerializeField] int _actionPointCost = 1;

        [SerializeReference] BoardActionLogic _logic;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;

        public string Description => _description;

        public Sprite Artwork => _artwork;

        public int ActionPointCost => _actionPointCost;

        public BoardActionLogic Logic => _logic;

        public CardTargeting Targeting => _logic != null ? _logic.Targeting : CardTargeting.None;
    }
}
