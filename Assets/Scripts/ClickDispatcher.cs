using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace RollAndEarn
{
    public class ClickDispatcher : MonoBehaviour
    {
        private EventSystem eventSystem;
        private Canvas canvas;

        private void Start()
        {
            canvas = FindAnyObjectByType<Canvas>();
            var es = FindAnyObjectByType<EventSystem>();
            if (es == null && canvas != null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.transform.SetParent(canvas.transform, false);
                eventSystem = esGo.AddComponent<EventSystem>();
            }
            else
            {
                eventSystem = es;
            }
        }

        private void Update()
        {
            if (Mouse.current?.leftButton.wasPressedThisFrame == true)
            {
                ProcessClick();
            }
        }

        private Vector2 GetMousePosition() => Mouse.current?.position.ReadValue() ?? Vector2.zero;

        private void ProcessClick()
        {
            if (canvas == null || eventSystem == null) return;

            var mousePosition = GetMousePosition();
            var raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null) return;

            var results = new List<RaycastResult>();
            var pointerData = new PointerEventData(eventSystem) { position = mousePosition };
            raycaster.Raycast(pointerData, results);

            for (int i = 0; i < results.Count; i++)
            {
                var go = results[i].gameObject;
                var button = go.GetComponent<Button>();

                if (button != null)
                {
                    button.OnPointerClick(pointerData);
                    return;
                }
            }
        }
    }
}