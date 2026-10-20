using UnityEngine;

namespace M3P
{
    /// <summary>One diamond in the "trees owned" row. The fill is the tree this profile already has.</summary>
    public sealed class UIPerkTreeCountPip : MonoBehaviour
    {
        [SerializeField] GameObject _fill;

        public void SetFilled(bool filled)
        {
            if (_fill != null)
                _fill.SetActive(filled);
        }
    }
}
