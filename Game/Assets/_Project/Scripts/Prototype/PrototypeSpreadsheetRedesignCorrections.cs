using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Corrective visual pass over Spreadsheet Redesign v1.
    /// Keeps the dark skin, but restores the production fonts/readability and replaces the offset
    /// Unity Outline selection cue with one clean range frame around the current selection.
    /// Presentation-only: no gameplay state is modified here.
    /// </summary>
    [DefaultExecutionOrder(2300)]
    public sealed class PrototypeSpreadsheetRedesignCorrections : MonoBehaviour
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", PrivateInstance);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", PrivateInstance);
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", PrivateInstance);

        private ExcelHellPrototype prototype;
        private RectTransform spreadsheet;
        private RectTransform formulaBar;
        private RectTransform background;
        private GameObject selectionFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetRedesignCorrections>() != null) return;
            var root = new GameObject("[PRESENTATION] Spreadsheet Redesign Corrections");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetRedesignCorrections>();
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
            if (spreadsheet == null || background == null) return;

            RestoreSpreadsheetTypography();
            RestoreDeletedMarkersAndSelectionCells();
            UpdateSelectionRangeFrame();
            RestoreControlTypography();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            spreadsheet = null;
            formulaBar = null;
            background = null;
            DestroySelectionFrame();
        }

        private void ResolveUi()
        {
            if (prototype == null) return;
            if (spreadsheet == null) spreadsheet = FindRect(prototype.transform, "Spreadsheet");
            if (formulaBar == null) formulaBar = FindRect(prototype.transform, "Formula Bar");

            if (background == null)
            {
                var canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
                if (canvas != null) background = FindRect(canvas.transform, "Background");
            }
        }

        private void RestoreSpreadsheetTypography()
        {
            var mono = PrototypeVisualTheme.MonoFont;

            foreach (Transform child in spreadsheet)
            {
                if (child == null || child.gameObject.name != "Header") continue;
                foreach (var text in child.GetComponentsInChildren<Text>(true))
                {
                    text.font = mono;
                    text.fontSize = 26;
                    text.fontStyle = FontStyle.Bold;
                }
            }

            foreach (var text in spreadsheet.GetComponentsInChildren<Text>(true))
            {
                if (text == null || HasAncestorStartingWith(text.transform, "Cell Message")) continue;

                var parentName = text.transform.parent != null ? text.transform.parent.name : string.Empty;
                if (parentName == "Header") continue;

                text.font = mono;
                text.fontStyle = FontStyle.Bold;

                var value = (text.text ?? string.Empty).Trim();
                if (parentName == "Report Goal Caption")
                {
                    text.fontSize = 11;
                }
                else if (parentName == "Formula 2.0 Interaction")
                {
                    text.fontSize = 22;
                }
                else if (value == "#REF!")
                {
                    text.fontSize = 24;
                }
                else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
                         double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out _))
                {
                    text.fontSize = 30;
                }
                else
                {
                    text.fontSize = 19;
                }
            }

            if (formulaBar != null)
            {
                foreach (var text in formulaBar.GetComponentsInChildren<Text>(true))
                {
                    text.font = mono;
                    text.fontSize = 20;
                    text.fontStyle = string.Equals((text.text ?? string.Empty).Trim(), "fx", StringComparison.OrdinalIgnoreCase)
                        ? FontStyle.Bold
                        : FontStyle.Normal;
                }
            }
        }

        private void RestoreDeletedMarkersAndSelectionCells()
        {
            var models = CellsField?.GetValue(prototype) as CellModel[,];
            var views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            var selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            if (models == null || views == null) return;

            var selected = selection != null ? new HashSet<CellModel>(selection) : null;
            var rowCount = models.GetLength(0);
            var columnCount = models.GetLength(1);

            for (var row = 0; row < rowCount; row++)
            for (var column = 0; column < columnCount; column++)
            {
                var cell = models[row, column];
                var view = views[row, column];
                if (cell == null || view == null) continue;

                var isSelected = selected != null && selected.Contains(cell);
                if (isSelected)
                {
                    // The previous 3 px Unity Outline reads as an offset corner/shadow. Hide it while selected;
                    // the range frame below supplies a true four-sided spreadsheet selection border.
                    var outline = view.GetComponent<Outline>();
                    if (outline != null)
                    {
                        outline.effectColor = Color.clear;
                        outline.effectDistance = Vector2.zero;
                    }

                    var image = view.GetComponent<Image>();
                    if (image != null)
                        image.color = Color.Lerp(image.color, PrototypeSpreadsheetRedesign.Selection, 0.10f);
                }

                if (cell.State != CellState.Destroyed) continue;

                var directLabel = view.GetComponentsInChildren<Text>(true)
                    .FirstOrDefault(text => text.transform.parent == view.transform);
                if (directLabel == null) continue;

                directLabel.text = "×";
                directLabel.font = PrototypeVisualTheme.MonoFont;
                directLabel.fontSize = 28;
                directLabel.fontStyle = FontStyle.Bold;
                directLabel.alignment = TextAnchor.MiddleCenter;
                directLabel.color = isSelected
                    ? PrototypeSpreadsheetRedesign.Selection
                    : PrototypeSpreadsheetRedesign.MutedText;
            }
        }

        private void UpdateSelectionRangeFrame()
        {
            var views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            var selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            if (views == null || selection == null || selection.Count == 0)
            {
                if (selectionFrame != null) selectionFrame.SetActive(false);
                return;
            }

            EnsureSelectionFrame();
            if (selectionFrame == null) return;

            var haveBounds = false;
            var combined = new Bounds();
            foreach (var cell in selection)
            {
                if (cell == null || cell.Row < 0 || cell.Column < 0 ||
                    cell.Row >= views.GetLength(0) || cell.Column >= views.GetLength(1)) continue;

                var view = views[cell.Row, cell.Column];
                if (view == null) continue;

                var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(background, view.transform);
                if (!haveBounds)
                {
                    combined = bounds;
                    haveBounds = true;
                }
                else
                {
                    combined.Encapsulate(bounds.min);
                    combined.Encapsulate(bounds.max);
                }
            }

            if (!haveBounds)
            {
                selectionFrame.SetActive(false);
                return;
            }

            selectionFrame.SetActive(true);
            selectionFrame.transform.SetAsLastSibling();

            var rect = selectionFrame.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localPosition = new Vector3(combined.center.x, combined.center.y, 0f);
            rect.sizeDelta = new Vector2(combined.size.x + 4f, combined.size.y + 4f);
            rect.localScale = Vector3.one;
        }

        private void EnsureSelectionFrame()
        {
            if (selectionFrame != null || background == null) return;

            selectionFrame = new GameObject("Spreadsheet Selection Range", typeof(RectTransform));
            selectionFrame.transform.SetParent(background, false);

            CreateEdge(selectionFrame.transform, "Selection Top",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 4f));
            CreateEdge(selectionFrame.transform, "Selection Bottom",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f));
            CreateEdge(selectionFrame.transform, "Selection Left",
                new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(4f, 0f));
            CreateEdge(selectionFrame.transform, "Selection Right",
                new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(4f, 0f));
        }

        private static void CreateEdge(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = PrototypeSpreadsheetRedesign.Selection;
            image.raycastTarget = false;
        }

        private void RestoreControlTypography()
        {
            if (background == null) return;

            var names = new[] { "Tasks Reserved", "Help Reserved", "Chat Reserved", "Menu Reserved", "Delete Reserved" };
            foreach (var name in names)
            {
                var control = FindRect(background, name);
                if (control == null) continue;

                if (name == "Delete Reserved")
                {
                    control.gameObject.SetActive(true);
                    control.transform.SetAsLastSibling();
                    var image = control.GetComponent<Image>();
                    if (image != null) image.color = PrototypeSpreadsheetRedesign.CellRaised;
                }

                foreach (var text in control.GetComponentsInChildren<Text>(true))
                {
                    text.gameObject.SetActive(true);
                    text.transform.SetAsLastSibling();
                    text.font = PrototypeVisualTheme.UiFont;
                    text.fontSize = name == "Delete Reserved" ? 16 : 14;
                    text.fontStyle = FontStyle.Bold;
                    text.color = PrototypeSpreadsheetRedesign.CellText;
                    text.raycastTarget = false;
                }
            }
        }

        private void DestroySelectionFrame()
        {
            if (selectionFrame != null) Destroy(selectionFrame);
            selectionFrame = null;
        }

        private void OnDisable() => DestroySelectionFrame();

        private static bool HasAncestorStartingWith(Transform transform, string prefix)
        {
            var current = transform.parent;
            while (current != null)
            {
                if (current.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
                current = current.parent;
            }
            return false;
        }

        private static RectTransform FindRect(Transform root, string objectName)
        {
            if (root == null) return null;
            foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                if (rect.gameObject.name == objectName) return rect;
            return null;
        }
    }
}
