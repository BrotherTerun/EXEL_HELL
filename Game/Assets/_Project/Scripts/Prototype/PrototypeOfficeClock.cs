using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(1960)]
    public sealed class PrototypeOfficeClock : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);

        private ExcelHellPrototype prototype;
        private Canvas canvas;
        private RectTransform clockSlot;
        private Text clockText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeOfficeClock>() != null) return;
            var root = new GameObject("[PRESENTATION] Office Clock");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeOfficeClock>();
        }

        private void Update()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null) return;
            if (clockText == null) TryBuild();
            RefreshClock();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            canvas = null;
            clockSlot = null;
            if (clockText != null) Destroy(clockText.gameObject);
            clockText = null;

            if (prototype != null)
                canvas = prototype.GetComponentsInChildren<Canvas>(true).Length > 0
                    ? prototype.GetComponentsInChildren<Canvas>(true)[0]
                    : null;
        }

        private void TryBuild()
        {
            if (canvas == null) return;
            foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.gameObject.name != "Office Clock Display") continue;
                clockSlot = rect;
                break;
            }
            if (clockSlot == null) return;

            var go = new GameObject("Office Clock Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(clockSlot, false);
            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            clockText = go.GetComponent<Text>();
            clockText.font = PrototypeVisualTheme.MonoFont;
            clockText.fontSize = 34;
            clockText.fontStyle = FontStyle.Bold;
            clockText.alignment = TextAnchor.MiddleCenter;
            clockText.color = new Color(0.90f, 0.16f, 0.12f, 1f);
            clockText.raycastTarget = false;

            Debug.Log("[UI-CLOCK] Office wall clock presentation bound.");
        }

        private void RefreshClock()
        {
            if (clockText == null || prototype == null) return;
            if (TurnField?.GetValue(prototype) is not int turn) return;

            var maxTurns = Mathf.Max(1, PrototypeLevelRuntime.Current?.MaxTurns ?? 1);
            var clampedTurn = Mathf.Clamp(turn, 0, maxTurns);
            var minutes = Mathf.RoundToInt(540f * clampedTurn / maxTurns);
            var total = 9 * 60 + minutes;
            clockText.text = $"{total / 60:00}:{total % 60:00}";
        }
    }
}
