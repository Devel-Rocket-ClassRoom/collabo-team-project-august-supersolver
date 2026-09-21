using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PPS.MapEditor
{
    public sealed class MapNumericDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public Action BeginScrub;
        public Action<float> Scrub;
        bool _horizontal;
        ScrollRect _scroll;
        float _scale;

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            Vector2 delta = eventData.position - eventData.pressPosition;
            _horizontal = Mathf.Abs(delta.x) >= Mathf.Abs(delta.y);
            _scroll = GetComponentInParent<ScrollRect>();
            _scale = GetComponentInParent<Canvas>().scaleFactor;
            if (_horizontal) BeginScrub?.Invoke();
            else if (_scroll != null) _scroll.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (_horizontal) Scrub?.Invoke((eventData.position.x - eventData.pressPosition.x) / _scale);
            else if (_scroll != null) _scroll.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_horizontal && _scroll != null) _scroll.OnEndDrag(eventData);
            _horizontal = false;
        }
    }
}
