using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UISkillVisuals : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI labelSKillName;
        [SerializeField] Image skillArtwork;
        [SerializeField] TextMeshProUGUI labelDescription;

        void Awake()
        {
            ResolveRefs();
        }

        void OnValidate()
        {
            ResolveRefs();
        }

        public void SetSkill(SkillDefinition skill)
        {
            ResolveRefs();

            if (labelSKillName != null)
                labelSKillName.text = skill != null ? skill.DisplayName : string.Empty;

            if (labelDescription != null)
                labelDescription.text = skill != null ? skill.Description : string.Empty;

            if (skillArtwork != null)
            {
                Sprite artwork = skill != null ? skill.Artwork : null;
                skillArtwork.sprite = artwork;
                skillArtwork.enabled = artwork != null;
            }
        }

        void ResolveRefs()
        {
            if (labelSKillName == null)
                labelSKillName = FindDescendantComponent<TextMeshProUGUI>("labelSKillName")
                    ?? FindDescendantComponent<TextMeshProUGUI>("LabelName")
                    ?? FindDescendantComponent<TextMeshProUGUI>("SkillName");

            if (skillArtwork == null)
            {
                Transform artwork = FindDescendant("skillArtwork")
                    ?? FindDescendant("SkillArtwork")
                    ?? FindDescendant("Image");
                if (artwork != null)
                    skillArtwork = artwork.GetComponent<Image>();
            }

            if (labelDescription == null)
                labelDescription = FindDescendantComponent<TextMeshProUGUI>("labelDescription")
                    ?? FindDescendantComponent<TextMeshProUGUI>("LabelDescription")
                    ?? FindDescendantComponent<TextMeshProUGUI>("Description");
        }

        T FindDescendantComponent<T>(string childName) where T : Component
        {
            Transform child = FindDescendant(childName);
            return child != null ? child.GetComponentInChildren<T>(true) : null;
        }

        Transform FindDescendant(string childName)
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name == childName)
                    return children[i];
            }

            return null;
        }
    }
}
