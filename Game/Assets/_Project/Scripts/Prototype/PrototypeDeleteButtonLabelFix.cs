using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Final skin/fail-safe for the compact DEL control. The shell now lifts the real interactive button
    /// beside the formula bar; this pass gives that same object a physical pixel bevel and press feedback.
    /// </summary>
    [DefaultExecutionOrder(2450)]
    public sealed class PrototypeDeleteButtonLabelFix : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private Canvas canvas;
        private RectTransform deleteReserved;
        private Text label;
        private PrototypeDeleteButtonPressFeedback feedback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeDeleteButtonLabelFix>() != null) return;

            var root = new GameObject("[PRESENTATION] DEL Button Skin");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeDeleteButtonLabelFix>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active)
            {
                if (prototype != null) Bind(null);
                return;
            }

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null) return;

            ResolveUi();
            if (deleteReserved == null) return;

            EnsureButtonSkin();
            RefreshButtonSkin();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            canvas = null;
            deleteReserved = null;
            label = null;
            feedback = null;

            if (prototype != null)
                canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        }

        private void ResolveUi()
        {
            if (canvas == null) return;
            if (deleteReserved == null) deleteReserved = FindRect(canvas.transform, "Delete Reserved");
        }

        private void EnsureButtonSkin()
        {
            var image = deleteReserved.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                image.color = PrototypeSpreadsheetRedesign.CellRaised;
            }

            var button = deleteReserved.GetComponent<Button>();
            if (button != null)
            {
                button.targetGraphic = image;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
                colors.pressedColor = new Color(0.82f, 0.86f, 0.90f, 1f);
                colors.selectedColor = Color.white;
                colors.disabledColor = new Color(0.55f, 0.58f, 0.62f, 0.55f);
                colors.colorMultiplier = 1f;
                colors.fadeDuration = 0.04f;
                button.colors = colors;
            }

            label = deleteReserved.GetComponentsInChildren<Text>(true).FirstOrDefault();
            if (label == null)
            {
                var labelObject = new GameObject("DEL Label", typeof(RectTransform), typeof(Text));
                labelObject.transform.SetParent(deleteReserved, false);
                label = labelObject.GetComponent<Text>();
                Stretch(label.rectTransform, 6f);
            }

            label.gameObject.SetActive(true);
            label.text = "DEL";
            label.font = PrototypeVisualTheme.UiFont;
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = PrototypeSpreadsheetRedesign.CellText;
            label.raycastTarget = false;

            var top = EnsureEdge("DEL Bevel Top");
            var left = EnsureEdge("DEL Bevel Left");
            var bottom = EnsureEdge("DEL Bevel Bottom");
            var right = EnsureEdge("DEL Bevel Right");

            ConfigureEdge(top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 2f));
            ConfigureEdge(left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(2f, 0f));
            ConfigureEdge(bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f));
            ConfigureEdge(right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(3f, 0f));

            feedback = deleteReserved.GetComponent<PrototypeDeleteButtonPressFeedback>();
            if (feedback == null) feedback = deleteReserved.gameObject.AddComponent<PrototypeDeleteButtonPressFeedback>();
            feedback.Bind(label, top, left, bottom, right);

            top.transform.SetAsLastSibling();
            left.transform.SetAsLastSibling();
            bottom.transform.SetAsLastSibling();
            right.transform.SetAsLastSibling();
            label.transform.SetAsLastSibling();
        }

        private void RefreshButtonSkin()
        {
            if (label == null || feedback == null) return;
            label.text = "DEL";
            label.font = PrototypeVisualTheme.UiFont;
            label.fontSize = 16;
            label.fontStyle = FontStyle.Bold;
            label.color = PrototypeSpreadsheetRedesign.CellText;
            label.transform.SetAsLastSibling();
            feedback.RefreshVisual();
        }

        private Image EnsureEdge(string name)
        {
            var existing = FindRect(deleteReserved, name);
            if (existing != null)
            {
                var existingImage = existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
                existingImage.raycastTarget = false;
                return existingImage;
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(deleteReserved, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void ConfigureEdge(Image image, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta)
        {
            var rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sizeDelta;
            rect.localScale = Vector3.one;
        }

        private static void Stretch(RectTransform rect, float padding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            rect.localScale = Vector3.one;
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            if (root == null) return null;
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.name == objectName) return rect;
            return null;
        }
    }

    public sealed class PrototypeDeleteButtonPressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private Text label;
        private Image top;
        private Image left;
        private Image bottom;
        private Image right;
        private bool pressed;
        private bool bound;

        public void Bind(Text targetLabel, Image topEdge, Image leftEdge, Image bottomEdge, Image rightEdge)
        {
            label = targetLabel;
            top = topEdge;
            left = leftEdge;
            bottom = bottomEdge;
            right = rightEdge;
            bound = label != null && top != null && left != null && bottom != null && right != null;
            RefreshVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pressed = true;
            RefreshVisual();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            pressed = false;
            RefreshVisual();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!pressed) return;
            pressed = false;
            RefreshVisual();
        }

        public void RefreshVisual()
        {
            if (!bound) return;

            var light = PrototypeSpreadsheetRedesign.MetalHighlight;
            var dark = PrototypeSpreadsheetRedesign.ShellOuter;
            top.color = pressed ? dark : light;
            left.color = pressed ? dark : light;
            bottom.color = pressed ? light : dark;
            right.color = pressed ? light : dark;

            var rect = label.rectTransform;
            rect.anchoredPosition = pressed ? new Vector2(1f, -1f) : Vector2.zero;
        }
    }
}
