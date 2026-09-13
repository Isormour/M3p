using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace M3P
{
    public sealed class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        Func<TooltipContent> _provider;

        public static UITooltipTrigger Ensure(GameObject host, Func<TooltipContent> provider)
        {
            if (host == null)
                return null;

            UITooltipTrigger trigger = host.GetComponent<UITooltipTrigger>();
            if (trigger == null)
                trigger = host.AddComponent<UITooltipTrigger>();

            trigger.Bind(provider);
            trigger.EnsureRaycastTarget();
            return trigger;
        }

        public void Bind(Func<TooltipContent> provider)
        {
            _provider = provider;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_provider == null)
                return;

            UITooltipService service = UITooltipService.Instance;
            service?.RequestShow(this, _provider, transform as RectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideFromService();
        }

        void OnDisable()
        {
            HideFromService();
        }

        void OnDestroy()
        {
            HideFromService();
        }

        void HideFromService()
        {
            UITooltipService service = UITooltipService.Instance;
            service?.Hide(this);
        }

        void EnsureRaycastTarget()
        {
            Graphic graphic = GetComponent<Graphic>();
            if (graphic != null)
            {
                graphic.raycastTarget = true;
                return;
            }

            Image image = gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
        }
    }
}
