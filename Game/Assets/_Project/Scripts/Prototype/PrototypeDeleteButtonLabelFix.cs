using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Final fail-safe for the compact DEL control. The button itself lives inside the decorative app chrome,
    /// while worksheet/formula presentation is reparented directly under Background. Keep the visible label
    /// as a top-level, non-raycast overlay so later presentation sibling ordering can never hide it.
    /// </summary>
    [DefaultExecutionOrder(2450)]
    public sealed class PrototypeDeleteButtonLabelFix : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private Canvas canvas;
        private RectTransform background;
        private RectTransform deleteReserved;
        private Text visibleLabel;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeDeleteButtonLabelFix>() != null) return;

            var root = new GameObject("[PRESENTATION] DEL Label Fix");
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
            if (background == null || deleteReserved == null) return;

            EnsureVisibleLabel();
            PositionVisibleLabel();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            DestroyVisibleLabel();
            prototype = owner;
            canvas = null;
            background = null;
            deleteReserved = null;

            if (prototype != null)
                canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        }

        private void ResolveUi()
        {
            if (canvas == null) return;
            if (background == null) background = FindRect(canvas.transform, "Background");
            if (deleteReserved == null) deleteReserved = FindRect(canvas.transform, "Delete Reserved");
        }

        private void EnsureVisibleLabel()
        {
            if (visibleLabel != null) return;

            var go = new GameObject("DEL Visible Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(background, false);
            visibleLabel = go.GetComponent<Text>();
            visibleLabel.text = "DEL";
            visibleLabel.font = PrototypeVisualTheme.UiFont;
            visibleLabel.fontSize = 16;
            visibleLabel.fontStyle = FontStyle.Bold;
            visibleLabel.alignment = TextAnchor.MiddleCenter;
            visibleLabel.color = PrototypeSpreadsheetRedesign.CellText;
            visibleLabel.horizontalOverflow = HorizontalWrapMode.Overflow;
            visibleLabel.verticalOverflow = VerticalWrapMode.Overflow;
            visibleLabel.raycastTarget = false;
        }

        private void PositionVisibleLabel()
        {
            visibleLabel.gameObject.SetActive(deleteReserved.gameObject.activeInHierarchy);
            if (!visibleLabel.gameObject.activeSelf) return;

            // Place the label from the actual button bounds instead of duplicating shell coordinates.
            // This keeps it correct if the final shell geometry changes again.
            var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(background, deleteReserved);
            var rect = visibleLabel.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localPosition = new Vector3(bounds.center.x, bounds.center.y, 0f);
            rect.sizeDelta = new Vector2(Mathf.Max(1f, bounds.size.x - 8f), Mathf.Max(1f, bounds.size.y - 4f));
            rect.localScale = Vector3.one;

            // Background-level last sibling guarantees visibility above the reparented formula bar/grid,
            // while raycastTarget=false preserves the real button underneath.
            visibleLabel.transform.SetAsLastSibling();
            visibleLabel.text = "DEL";
            visibleLabel.font = PrototypeVisualTheme.UiFont;
            visibleLabel.fontSize = 16;
            visibleLabel.fontStyle = FontStyle.Bold;
            visibleLabel.color = PrototypeSpreadsheetRedesign.CellText;
        }

        private void DestroyVisibleLabel()
        {
            if (visibleLabel != null) Destroy(visibleLabel.gameObject);
            visibleLabel = null;
        }

        private void OnDisable() => DestroyVisibleLabel();

        private static RectTransform FindRect(Transform root, string objectName)
        {
            if (root == null) return null;
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.name == objectName) return rect;
            return null;
        }
    }
}
