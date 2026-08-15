using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    public sealed class PrototypeMovementIntentOverlay : MonoBehaviour
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private readonly Dictionary<string, Image> overlays = new();
        private ExcelHellPrototype prototype;
        private FieldInfo currentIntentField;
        private string activeAddress;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeMovementIntentOverlay>() != null) return;
            new GameObject("EXCEL HELL Movement Intent Overlay").AddComponent<PrototypeMovementIntentOverlay>();
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current == null)
            {
                HideActive();
                prototype = null;
                currentIntentField = null;
                overlays.Clear();
                return;
            }

            if (prototype != current) Bind(current);
            if (currentIntentField == null) { HideActive(); return; }

            var intent = (AnomalyIntent?)currentIntentField.GetValue(prototype);
            if (!intent.HasValue) { HideActive(); return; }

            var address = ExcelHellPrototype.ColumnName(intent.Value.TargetColumn) + (intent.Value.TargetRow + 1);
            if (activeAddress == address && overlays.TryGetValue(address, out var activeOverlay) && activeOverlay != null) return;

            HideActive();
            if (overlays.TryGetValue(address, out var overlay) && overlay != null)
            {
                overlay.enabled = true;
                activeAddress = address;
            }
        }

        private void Bind(ExcelHellPrototype owner)
        {
            HideActive();
            overlays.Clear();
            prototype = owner;
            currentIntentField = typeof(ExcelHellPrototype).GetField("currentIntent", Flags);

            foreach (var view in owner.GetComponentsInChildren<ExcelHellCellView>(true))
            {
                var cellName = view.gameObject.name;
                if (!cellName.StartsWith("Cell ") || cellName.Length <= 5) continue;
                var address = cellName.Substring(5);
                var overlayObject = new GameObject("MovementIntentOverlay", typeof(RectTransform), typeof(Image));
                overlayObject.transform.SetParent(view.transform, false);
                overlayObject.transform.SetSiblingIndex(0);
                var rect = overlayObject.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                var image = overlayObject.GetComponent<Image>();
                image.color = new Color(1f, 0.62f, 0.12f, 0.38f);
                image.raycastTarget = false;
                image.enabled = false;
                overlays[address] = image;
            }
        }

        private void HideActive()
        {
            if (!string.IsNullOrEmpty(activeAddress) && overlays.TryGetValue(activeAddress, out var overlay) && overlay != null)
                overlay.enabled = false;
            activeAddress = null;
        }
    }
}
