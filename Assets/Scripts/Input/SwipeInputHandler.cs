using UnityEngine;
using UnityEngine.EventSystems;

namespace VisionsOfGenesis.InputSystem
{
    public enum SwipeDirection
    {
        Tap,
        Up,
        Down,
        Left,
        Right
    }

    [RequireComponent(typeof(RectTransform))]
    public class SwipeInputHandler : MonoBehaviour,
        IPointerDownHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerUpHandler
    {
        [Tooltip("Minimum pixel distance for a gesture to count as a swipe instead of a tap.")]
        public float swipeThreshold = 50f;

        public event System.Action<SwipeDirection> OnSwipe;

        private Vector2 _downPos;
        private bool _swipeFired;

        public void OnPointerDown(PointerEventData eventData)
        {
            _downPos = eventData.position;
            _swipeFired = false;
        }

        public void OnBeginDrag(PointerEventData eventData) { }
        public void OnDrag(PointerEventData eventData) { }

        public void OnEndDrag(PointerEventData eventData)
        {
            Vector2 delta = eventData.position - _downPos;
            if (delta.magnitude < swipeThreshold) return;
            EmitSwipe(delta);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_swipeFired) return;

            Vector2 delta = eventData.position - _downPos;
            if (delta.magnitude < swipeThreshold)
                OnSwipe?.Invoke(SwipeDirection.Tap);
            else
                EmitSwipe(delta);
        }

        private void EmitSwipe(Vector2 delta)
        {
            SwipeDirection dir;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
                dir = delta.x > 0 ? SwipeDirection.Right : SwipeDirection.Left;
            else
                dir = delta.y > 0 ? SwipeDirection.Up : SwipeDirection.Down;

            _swipeFired = true;
            OnSwipe?.Invoke(dir);
        }
    }
}
