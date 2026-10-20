using UnityEngine;
using UnityEngine.Serialization;

namespace M3P
{
    [CreateAssetMenu(fileName = "PerkDefinition", menuName = "M3P/Perk Definition", order = 6)]
    public class PerkDefinition : ScriptableObject
    {
        [FormerlySerializedAs("_displayName")]
        [SerializeField] string _name;
        [TextArea, SerializeField] string _description;
        [SerializeField] Sprite _artwork;
        [SerializeField, SerializeReference] PerkLogic _logic;

        public string Name => string.IsNullOrEmpty(_name) ? name : _name;
        public string Description => _description;
        public Sprite Artwork => _artwork;
        public PerkLogic Logic => _logic;

        /// <summary>One short line for an unlock offer. Falls back to <see cref="Name"/> when the logic has no wording.</summary>
        public string OfferText
        {
            get
            {
                string offer = _logic != null ? _logic.FormatOffer() : string.Empty;
                return string.IsNullOrEmpty(offer) ? Name : offer;
            }
        }

        public void Apply(ref SoftStatValues stats)
        {
            _logic?.Apply(ref stats);
        }
    }
}
