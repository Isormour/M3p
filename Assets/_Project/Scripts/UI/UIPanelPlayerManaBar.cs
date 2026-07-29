using Match3;
using UnityEngine;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UIPanelPlayerManaBar : MonoBehaviour
    {
        [SerializeField] Image _manaIcon;
        [SerializeField] UISimpleIndicator _indicator;
        int _tileTypeId;
        int _currentAmount = 0;

        public int TileTypeId => _tileTypeId;

        public void Configure(int tileTypeId, Sprite icon, Material material = null)
        {
            _tileTypeId = tileTypeId;

            if (_manaIcon == null || _indicator == null)
            {
                Debug.LogError($"{nameof(UIPanelPlayerManaBar)}: assign {nameof(_manaIcon)} and {nameof(_indicator)} on the prefab.", this);
                return;
            }

            _manaIcon.sprite = icon != null
                ? icon
                : Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
            
            if (material != null)
            {
                _manaIcon.material = material;
            }
            
            _manaIcon.enabled = true;

            _indicator.Bind(
                getCurrent: () => _currentAmount,
                getMax: () => 0,
                formatText: (current, _) => current.ToString()
            );
        }

        public void SetAmount(int amount)
        {
            if (_currentAmount == amount) return;
            _currentAmount = amount;
            
            if (_indicator != null)
            {
                _indicator.ManualRefresh();
            }
        }

        private void OnDestroy()
        {
            if (_indicator != null)
            {
                _indicator.Unbind();
            }
        }
    }
}
