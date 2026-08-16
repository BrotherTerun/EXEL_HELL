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
    /// Final non-invasive polish over Spreadsheet Redesign v1.
    /// Presentation only: replaces offset Outline borders with true pixel edges, adds bounded typography,
    /// strengthens report/deleted semantics and snaps the existing FC2 MOVE ghost to worksheet cells.
    /// </summary>
    [DefaultExecutionOrder(2400)]
    public sealed class PrototypeSpreadsheetRedesignPolish : MonoBehaviour
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", PrivateInstance);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", PrivateInstance);
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", PrivateInstance);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", PrivateInstance);
        private static readonly FieldInfo CurrentIntentField = typeof(ExcelHellPrototype).GetField("currentIntent", PrivateInstance);

        private static readonly FieldInfo FormulaDraggingMoveField = typeof(PrototypeFormulaCells).GetField("draggingMove", PrivateInstance);
        private static readonly FieldInfo FormulaDraggingFormulaField = typeof(PrototypeFormulaCells).GetField("draggingFormula", PrivateInstance);
        private static readonly FieldInfo FormulaHoverCellField = typeof(PrototypeFormulaCells).GetField("hoverCell", PrivateInstance);
        private static readonly FieldInfo FormulaGhostRectField = typeof(PrototypeFormulaCells).GetField("dragGhostRect", PrivateInstance);
        private static readonly FieldInfo FormulaGhostImageField = typeof(PrototypeFormulaCells).GetField("dragGhostImage", PrivateInstance);
        private static readonly FieldInfo FormulaGhostTextField = typeof(PrototypeFormulaCells).GetField("dragGhostText", PrivateInstance);

        private static readonly Color ReportHeader = new Color32(86, 67, 31, 255);       // #56431F
        private static readonly Color ReportHeaderText = new Color32(242, 219, 158, 255); // #F2DB9E
        private static readonly Color DeletedWell = new Color32(7, 11, 16, 255);           // #070B10
        private static readonly Color DeletedMark = new Color32(112, 126, 140, 145);

        private readonly Dictionary<int, PixelBorder> borders = new();
        private readonly Dictionary<int, DeletedRecess> deletedRecesses = new();
        private readonly List<GameObject> generated = new();

        private ExcelHellPrototype prototype;
        private PrototypeFormulaCells formulaCells;
        private RectTransform background;
        private RectTransform spreadsheet;
        private RectTransform formulaBar;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<CellModel> selection;
        private List<ReportGoal> goals;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetRedesignPolish>() != null) return;

            var root = new GameObject("[PRESENTATION] Spreadsheet Redesign Polish");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetRedesignPolish>();
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
            if (prototype == null || cells == null || views == null) return;

            ResolveUi();
            if (spreadsheet == null || background == null) return;

            StyleFormulaBar();
            StyleHeaders();
            StyleCells();
            StyleControls();
            SnapMoveGhost();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            DestroyGenerated();

            prototype = owner;
            formulaCells = null;
            background = null;
            spreadsheet = null;
            formulaBar = null;
            cells = null;
            views = null;
            selection = null;
            goals = null;

            if (prototype == null) return;

            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
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

            if (formulaCells == null) formulaCells = FindFirstObjectByType<PrototypeFormulaCells>();
        }

        private void StyleFormulaBar()
        {
            if (formulaBar == null) return;

            ClearOffsetOutline(formulaBar.gameObject);
            SetBorder(formulaBar.gameObject, PrototypeSpreadsheetRedesign.Metal, 2f);

            foreach (var text in formulaBar.GetComponentsInChildren<Text>(true))
            {
                if (text == null) continue;
                text.font = PrototypeVisualTheme.MonoFont;
                text.fontStyle = string.Equals((text.text ?? string.Empty).Trim(), "fx", StringComparison.OrdinalIgnoreCase)
                    ? FontStyle.Bold
                    : FontStyle.Normal;
                ApplyBestFit(text, 13, 20, 20);
            }
        }

        private void StyleHeaders()
        {
            foreach (Transform child in spreadsheet)
            {
                if (child == null || child.gameObject.name != "Header") continue;

                ClearOffsetOutline(child.gameObject);
                SetBorder(child.gameObject, PrototypeSpreadsheetRedesign.Grid, 2f);

                foreach (var text in child.GetComponentsInChildren<Text>(true))
                {
                    if (text == null) continue;
                    text.font = PrototypeVisualTheme.MonoFont;
                    text.fontStyle = FontStyle.Bold;
                    ApplyBestFit(text, 16, 26, 26);
                }
            }
        }

        private void StyleCells()
        {
            var selected = selection != null ? new HashSet<CellModel>(selection) : null;
            var intent = CurrentIntentField?.GetValue(prototype);
            var hasIntent = intent is AnomalyIntent;
            var currentIntent = hasIntent ? (AnomalyIntent)intent : default;

            var rowCount = cells.GetLength(0);
            var columnCount = cells.GetLength(1);
            for (var row = 0; row < rowCount; row++)
            for (var column = 0; column < columnCount; column++)
            {
                var cell = cells[row, column];
                var view = views[row, column];
                if (cell == null || view == null) continue;

                var isSelected = selected != null && selected.Contains(cell);
                var isReportTarget = goals != null && goals.Any(goal => goal.TargetRow == row && goal.TargetColumn == column);
                var isReportHeader = column == columnCount - 1 && cell.Occupant?.Kind == ContentKind.Label;
                var isIntent = hasIntent && currentIntent.TargetRow == row && currentIntent.TargetColumn == column;

                ClearOffsetOutline(view.gameObject);

                var borderColor = PrototypeSpreadsheetRedesign.Grid;
                var borderWidth = 2f;
                if (cell.State == CellState.Destroyed)
                {
                    borderColor = PrototypeSpreadsheetRedesign.DeletedGrid;
                    borderWidth = 1f;
                }
                else if (cell.State == CellState.Corrupted)
                {
                    borderColor = PrototypeSpreadsheetRedesign.RefAccent;
                }
                else if (isIntent)
                {
                    borderColor = PrototypeSpreadsheetRedesign.IntentAccent;
                }
                else if (isReportHeader || isReportTarget)
                {
                    borderColor = PrototypeSpreadsheetRedesign.ReportAccent;
                }
                else if (cell.IsFormula)
                {
                    borderColor = PrototypeSpreadsheetRedesign.FormulaText;
                }

                // Selected range is drawn once by PrototypeSpreadsheetRedesignCorrections; do not create a
                // second cyan border on every selected cell. A slight cell tint from that pass remains visible.
                SetBorder(view.gameObject, borderColor, borderWidth);
                SetDeletedRecess(view.gameObject, cell.State == CellState.Destroyed);

                if (isReportHeader)
                    StyleReportHeader(view);

                StyleCellTypography(view, cell, isReportHeader, isSelected);
            }
        }

        private void StyleReportHeader(ExcelHellCellView view)
        {
            var image = view.GetComponent<Image>();
            if (image != null) image.color = ReportHeader;

            foreach (var text in view.GetComponentsInChildren<Text>(true))
            {
                if (text == null || HasAncestorStartingWith(text.transform, "Cell Message")) continue;
                if (text.transform.parent != view.transform) continue;

                text.font = PrototypeVisualTheme.MonoFont;
                text.fontStyle = FontStyle.Bold;
                text.color = ReportHeaderText;
                ApplyBestFit(text, 14, 22, 22);
            }
        }

        private void StyleCellTypography(ExcelHellCellView view, CellModel cell, bool isReportHeader, bool isSelected)
        {
            var directLabel = view.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text != null && text.transform.parent == view.transform);

            foreach (var text in view.GetComponentsInChildren<Text>(true))
            {
                if (text == null || HasAncestorStartingWith(text.transform, "Cell Message")) continue;

                // #REF! owns its living typography in PrototypeRefGlitchPresenter. Do not fight that renderer.
                if (cell.State == CellState.Corrupted && text == directLabel)
                {
                    text.resizeTextForBestFit = false;
                    continue;
                }

                text.font = PrototypeVisualTheme.MonoFont;
                var parentName = text.transform.parent != null ? text.transform.parent.name : string.Empty;
                var value = (text.text ?? string.Empty).Trim();

                if (isReportHeader && text == directLabel)
                {
                    text.fontStyle = FontStyle.Bold;
                    text.color = ReportHeaderText;
                    ApplyBestFit(text, 14, 22, 22);
                }
                else if (parentName == "Report Goal Caption")
                {
                    text.fontStyle = FontStyle.Bold;
                    text.color = PrototypeSpreadsheetRedesign.ReportAccent;
                    ApplyBestFit(text, 9, 12, 12);
                }
                else if (parentName == "Formula 2.0 Interaction")
                {
                    text.fontStyle = FontStyle.Bold;
                    text.color = isReportHeader ? ReportHeaderText : PrototypeSpreadsheetRedesign.FormulaText;
                    ApplyBestFit(text, 13, 22, 22);
                }
                else if (cell.State == CellState.Destroyed && text == directLabel)
                {
                    text.fontStyle = FontStyle.Bold;
                    text.color = isSelected ? PrototypeSpreadsheetRedesign.Selection : DeletedMark;
                    ApplyBestFit(text, 16, 24, 22);
                }
                else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
                         double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out _))
                {
                    text.fontStyle = FontStyle.Bold;
                    ApplyBestFit(text, 20, 30, 30);
                }
                else
                {
                    text.fontStyle = FontStyle.Bold;
                    ApplyBestFit(text, 12, 19, 19);
                }
            }
        }

        private void StyleControls()
        {
            if (background == null) return;

            var names = new[] { "Tasks Reserved", "Help Reserved", "Chat Reserved", "Menu Reserved", "Delete Reserved" };
            foreach (var name in names)
            {
                var rect = FindRect(background, name);
                if (rect == null) continue;

                ClearOffsetOutline(rect.gameObject);
                SetBorder(rect.gameObject, PrototypeSpreadsheetRedesign.Metal, 2f);

                foreach (var text in rect.GetComponentsInChildren<Text>(true))
                {
                    if (text == null) continue;
                    ApplyBestFit(text, 11, name == "Delete Reserved" ? 16 : 15,
                        name == "Delete Reserved" ? 16 : 15);
                }
            }
        }

        private void SnapMoveGhost()
        {
            if (formulaCells == null || views == null) return;
            if (FormulaDraggingMoveField == null || FormulaHoverCellField == null || FormulaGhostRectField == null) return;

            var dragging = FormulaDraggingMoveField.GetValue(formulaCells) is bool value && value;
            if (!dragging) return;

            var target = FormulaHoverCellField.GetValue(formulaCells) as CellModel;
            var ghostRect = FormulaGhostRectField.GetValue(formulaCells) as RectTransform;
            if (target == null || ghostRect == null || !ghostRect.gameObject.activeInHierarchy) return;
            if (target.Row < 0 || target.Column < 0 || target.Row >= views.GetLength(0) || target.Column >= views.GetLength(1)) return;

            var targetView = views[target.Row, target.Column];
            if (targetView == null) return;
            var targetRect = targetView.GetComponent<RectTransform>();
            if (targetRect == null) return;

            // The FC2 runtime still owns drag/drop and hover state. This late pass only renders its ghost at the
            // currently hovered worksheet cell, preserving the exact gameplay hit target while removing floaty UI.
            ghostRect.pivot = new Vector2(0.5f, 0.5f);
            ghostRect.position = targetRect.TransformPoint(targetRect.rect.center);
            ghostRect.sizeDelta = new Vector2(
                Mathf.Max(28f, targetRect.rect.width - 6f),
                Mathf.Max(24f, targetRect.rect.height - 6f));
            ghostRect.localScale = Vector3.one;
            ghostRect.transform.SetAsLastSibling();

            var image = FormulaGhostImageField?.GetValue(formulaCells) as Image;
            var text = FormulaGhostTextField?.GetValue(formulaCells) as Text;
            var draggingFormula = FormulaDraggingFormulaField?.GetValue(formulaCells) is bool formula && formula;

            if (image != null)
            {
                image.color = draggingFormula
                    ? new Color(PrototypeSpreadsheetRedesign.FormulaCell.r, PrototypeSpreadsheetRedesign.FormulaCell.g,
                        PrototypeSpreadsheetRedesign.FormulaCell.b, 0.96f)
                    : new Color(PrototypeSpreadsheetRedesign.CellRaised.r, PrototypeSpreadsheetRedesign.CellRaised.g,
                        PrototypeSpreadsheetRedesign.CellRaised.b, 0.96f);
            }

            ClearOffsetOutline(ghostRect.gameObject);
            SetBorder(ghostRect.gameObject, PrototypeSpreadsheetRedesign.Selection, 2f);

            if (text != null)
            {
                text.font = PrototypeVisualTheme.MonoFont;
                text.fontStyle = FontStyle.Bold;
                text.color = draggingFormula ? PrototypeSpreadsheetRedesign.FormulaText : PrototypeSpreadsheetRedesign.CellText;
                ApplyBestFit(text, 11, 20, 20);
            }
        }

        private void SetBorder(GameObject owner, Color color, float thickness)
        {
            if (owner == null) return;
            var id = owner.GetInstanceID();
            if (!borders.TryGetValue(id, out var border) || border.Root == null)
            {
                border = CreatePixelBorder(owner.transform);
                borders[id] = border;
            }

            border.Set(color, thickness);
            border.Root.transform.SetAsLastSibling();
        }

        private PixelBorder CreatePixelBorder(Transform owner)
        {
            var root = new GameObject("Redesign Pixel Border", typeof(RectTransform));
            root.transform.SetParent(owner, false);
            Stretch(root.GetComponent<RectTransform>());
            generated.Add(root);

            return new PixelBorder(
                root,
                CreateEdge(root.transform, "Top"),
                CreateEdge(root.transform, "Bottom"),
                CreateEdge(root.transform, "Left"),
                CreateEdge(root.transform, "Right"));
        }

        private void SetDeletedRecess(GameObject owner, bool visible)
        {
            if (owner == null) return;
            var id = owner.GetInstanceID();
            if (!deletedRecesses.TryGetValue(id, out var recess) || recess.Root == null)
            {
                recess = CreateDeletedRecess(owner.transform);
                deletedRecesses[id] = recess;
            }

            recess.Root.SetActive(visible);
            if (!visible) return;

            recess.Root.transform.SetAsFirstSibling();
            recess.Fill.color = DeletedWell;
            recess.Top.color = PrototypeSpreadsheetRedesign.ShellOuter;
            recess.Left.color = PrototypeSpreadsheetRedesign.ShellOuter;
            recess.Bottom.color = PrototypeSpreadsheetRedesign.DeletedGrid;
            recess.Right.color = PrototypeSpreadsheetRedesign.DeletedGrid;
        }

        private DeletedRecess CreateDeletedRecess(Transform owner)
        {
            var root = new GameObject("Deleted Recess", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(owner, false);
            var rect = root.GetComponent<RectTransform>();
            Stretch(rect, 4f);
            generated.Add(root);

            var fill = root.GetComponent<Image>();
            fill.color = DeletedWell;
            fill.raycastTarget = false;

            var top = CreateEdge(root.transform, "Inset Top");
            var bottom = CreateEdge(root.transform, "Inset Bottom");
            var left = CreateEdge(root.transform, "Inset Left");
            var right = CreateEdge(root.transform, "Inset Right");

            SetHorizontalEdge(top.rectTransform, true, 1f);
            SetHorizontalEdge(bottom.rectTransform, false, 1f);
            SetVerticalEdge(left.rectTransform, true, 1f);
            SetVerticalEdge(right.rectTransform, false, 1f);

            return new DeletedRecess(root, fill, top, bottom, left, right);
        }

        private static Image CreateEdge(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            return image;
        }

        private static void ClearOffsetOutline(GameObject owner)
        {
            if (owner == null) return;
            var outline = owner.GetComponent<Outline>();
            if (outline == null) return;
            outline.effectColor = Color.clear;
            outline.effectDistance = Vector2.zero;
        }

        private static void ApplyBestFit(Text text, int min, int max, int preferred)
        {
            if (text == null) return;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(1, min);
            text.resizeTextMaxSize = Mathf.Max(text.resizeTextMinSize, max);
            text.fontSize = Mathf.Clamp(preferred, text.resizeTextMinSize, text.resizeTextMaxSize);
        }

        private void DestroyGenerated()
        {
            foreach (var go in generated)
                if (go != null) Destroy(go);
            generated.Clear();
            borders.Clear();
            deletedRecesses.Clear();
        }

        private void OnDisable() => DestroyGenerated();

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

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
            rect.localScale = Vector3.one;
        }

        private static void SetHorizontalEdge(RectTransform rect, bool top, float thickness)
        {
            rect.anchorMin = new Vector2(0f, top ? 1f : 0f);
            rect.anchorMax = new Vector2(1f, top ? 1f : 0f);
            rect.pivot = new Vector2(0.5f, top ? 1f : 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, thickness);
        }

        private static void SetVerticalEdge(RectTransform rect, bool left, float thickness)
        {
            rect.anchorMin = new Vector2(left ? 0f : 1f, 0f);
            rect.anchorMax = new Vector2(left ? 0f : 1f, 1f);
            rect.pivot = new Vector2(left ? 0f : 1f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(thickness, 0f);
        }

        private sealed class PixelBorder
        {
            public readonly GameObject Root;
            private readonly Image top;
            private readonly Image bottom;
            private readonly Image left;
            private readonly Image right;

            public PixelBorder(GameObject root, Image top, Image bottom, Image left, Image right)
            {
                Root = root;
                this.top = top;
                this.bottom = bottom;
                this.left = left;
                this.right = right;
            }

            public void Set(Color color, float thickness)
            {
                if (Root == null) return;
                Root.SetActive(true);
                top.color = bottom.color = left.color = right.color = color;
                SetHorizontalEdge(top.rectTransform, true, thickness);
                SetHorizontalEdge(bottom.rectTransform, false, thickness);
                SetVerticalEdge(left.rectTransform, true, thickness);
                SetVerticalEdge(right.rectTransform, false, thickness);
            }
        }

        private sealed class DeletedRecess
        {
            public readonly GameObject Root;
            public readonly Image Fill;
            public readonly Image Top;
            public readonly Image Bottom;
            public readonly Image Left;
            public readonly Image Right;

            public DeletedRecess(GameObject root, Image fill, Image top, Image bottom, Image left, Image right)
            {
                Root = root;
                Fill = fill;
                Top = top;
                Bottom = bottom;
                Left = left;
                Right = right;
            }
        }
    }
}
