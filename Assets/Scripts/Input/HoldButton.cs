using UnityEngine;
using UnityEngine.EventSystems;

namespace HeadSoccer
{
    /// <summary>
    /// A UI button that reports press and release instead of click, so holding the
    /// left arrow on a phone keeps the character running.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private TouchInputSource target;
        [SerializeField] private TouchAction action;

        public void Setup(TouchInputSource source, TouchAction touchAction)
        {
            target = source;
            action = touchAction;
        }

        public void OnPointerDown(PointerEventData eventData) => target?.Press(action);

        public void OnPointerUp(PointerEventData eventData) => target?.Release(action);

        private void OnDisable() => target?.Release(action);
    }
}
