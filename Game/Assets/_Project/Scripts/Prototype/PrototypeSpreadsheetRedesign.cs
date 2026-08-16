using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Spreadsheet skin v1. Presentation-only: interprets the frozen CellModel state after the ordinary
    /// gameplay presenters have refreshed it. No cell state, selection, formula or report data is mutated here.
    /// </summary>
    [DefaultExecutionOrder(2200)]
    public sealed class PrototypeSpreadsheetRedesign : MonoBehaviour
    {
        private static readonly BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", PrivateInstance);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", PrivateInstance);
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", PrivateInstance);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", PrivateInstance);
        private static readonly FieldInfo CurrentIntentField = typeof(ExcelHellPrototype).GetField("currentIntent", PrivateInstance);

        // Corpo-terminal palette. Values are deliberately restrained so the later psychosis/glitch pass
        // still has somewhere to escalate.
        public static readonly Color ShellOuter = new Color32(10, 14, 20, 255);       // #0A0E14
        public static readonly Color Shell = new Color32(17, 23, 34, 255);            // #111722
        public static readonly Color ShellRaised = new Color32(24, 34, 48, 255);      // #182230
        public static readonly Color Metal = new Color32(52, 65, 81, 255);            // #344151
        public static readonly Color MetalHighlight = new Color32(75, 91, 111, 255);  // #4B5B6F
        public static readonly Color Cell = new Color32(32, 41, 54, 255);             // #202936
        public static readonly Color CellRaised = new Color32(38, 50, 65, 255);       // #263241
        public static readonly Color Header = new Color32(27, 38, 52, 255);           // #1B2634
        public static readonly Color Grid = new Color32(67, 80, 100, 255);            // #435064
        public static readonly Color Deleted = new Color32(11, 16, 23, 255);          // #0B1017
        public static readonly Color DeletedGrid = new Color32(35, 45, 58, 255);      // #232D3A
        public static readonly Color CellText = new Color32(214, 222, 231, 255);       // #D6DEE7
        public static readonly Color MutedText = new Color32(145, 160, 175, 255);      // #91A0AF
        public static readonly Color Selection = new Color32(110, 166, 184, 255);      // #6EA6B8
        public static readonly Color FormulaCell = new Color32(25, 49, 59, 255);       // #19313B
        public static readonly Color FormulaText = new Color32(139, 199, 212, 255);    // #8BC7D4
        public static readonly Color ReportCell = new Color32(52, 45, 32, 255);        // #342D20
        public static readonly Color ReportAccent = new Color32(208, 168, 92, 255);    // #D0A85C
        public static readonly Color IntentCell = new Color32(73, 51, 26, 255);        // #49331A
        public static readonly Color IntentAccent = new Color32(214, 145, 73, 255);    // #D69149
        public static readonly Color RefCell = new Color32(75, 24, 34, 255);           // #4B1822
        public static readonly Color RefAccent = new Color32(200, 70, 85, 255);        // #C84655
        public static readonly Color RefText = new Color32(244, 220, 224, 255);        // #F4DCE0

        private ExcelHellPrototype prototype;
        private RectTransform spreadsheet;
        private RectTransform formulaBar;
        private RectTransform background;
        private GameObject pixelFrame;
        private Font spreadsheetFont;
        private bool loggedFontFallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeSpreadsheetRedesign>() != null) return;
            var root = new GameObject("[PRESENTATION] Spreadsheet Redesign v1");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeSpreadsheetRedesign>();
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
            if (spreadsheet == null || formulaBar == null) return;

            EnsurePixelFrame();
            StyleShell();
            StyleFormulaBar();
            StyleHeaders();
            StyleCells();
            StyleShortControls();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            spreadsheet = null;
            formulaBar = null;
            background = null;
            spreadsheetFont = null;
            loggedFontFallback = false;
            DestroyFrame();
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

        private Font SpreadsheetFont
        {
            get
            {
                if (spreadsheetFont != null) return spreadsheetFont;
                spreadsheetFont = Resources.Load<Font>("Fonts/Tiny5-Regular");
                if (spreadsheetFont != null) return spreadsheetFont;

                spreadsheetFont = PrototypeVisualTheme.MonoFont;
                if (!loggedFontFallback)
                {
                    Debug.LogWarning("[SPREADSHEET/REDESIGN] Tiny5-Regular is not available yet; using MonoFont fallback.");
                    loggedFontFallback = true;
                }
                return spreadsheetFont;
            }
        }

        private void StyleShell()
        {
            SetImage("Spreadsheet App", Shell);
            SetImage("Topbar Surface", ShellRaised);
            SetImage("Formula Row Surface", ShellRaised);
            SetImage("Worksheet Surface", Shell);
            SetImage("Delete Reserved", CellRaised);
        }

        private void EnsurePixelFrame()
        {
            if (pixelFrame != null || background == null || spreadsheet == null || formulaBar == null) return;

            pixelFrame = new GameObject("Spreadsheet Pixel Frame", typeof(RectTransform), typeof(Image));
            pixelFrame.transform.SetParent(background, false);
            var frameRect = pixelFrame.GetComponent<RectTransform>();

            var left = Mathf.Min(spreadsheet.anchoredPosition.x, formulaBar.anchoredPosition.x);
            var right = Mathf.Max(
                spreadsheet.anchoredPosition.x + spreadsheet.rect.width,
                formulaBar.anchoredPosition.x + formulaBar.rect.width);
            var top = Mathf.Max(spreadsheet.anchoredPosition.y, formulaBar.anchoredPosition.y);
            var bottom = Mathf.Min(
                spreadsheet.anchoredPosition.y - spreadsheet.rect.height,
                formulaBar.anchoredPosition.y - formulaBar.rect.height);

            const float pad = 7f;
            SetTopLeft(frameRect, left - pad, top + pad, right - left + pad * 2f, top - bottom + pad * 2f);

            var frameImage = pixelFrame.GetComponent<Image>();
            frameImage.color = ShellOuter;
            frameImage.raycastTarget = false;

            var metal = CreateInsetPanel(pixelFrame.transform, "Frame Metal", 3f, Metal);
            CreateInsetPanel(metal.transform, "Frame Well", 3f, Shell);
            CreateBevelStrip(pixelFrame.transform, "Frame Highlight Top", true, true, 2f, MetalHighlight);
            CreateBevelStrip(pixelFrame.transform, "Frame Highlight Left", false, true, 2f, MetalHighlight);
            CreateBevelStrip(pixelFrame.transform, "Frame Shadow Bottom", true, false, 3f, ShellOuter);
            CreateBevelStrip(pixelFrame.transform, "Frame Shadow Right", false, false, 3f, ShellOuter);

            var sibling = Mathf.Min(spreadsheet.GetSiblingIndex(), formulaBar.GetSiblingIndex());
            pixelFrame.transform.SetSiblingIndex(Mathf.Max(0, sibling - 1));
            Debug.Log("[SPREADSHEET/REDESIGN] Pixel frame and dark worksheet skin ready.");
        }

        private void StyleFormulaBar()
        {
            var image = formulaBar.GetComponent<Image>();
            if (image != null) image.color = Deleted;

            var outline = EnsureOutline(formulaBar.gameObject);
            outline.effectColor = Metal;
            outline.effectDistance = new Vector2(2f, -2f);

            foreach (var text in formulaBar.GetComponentsInChildren<Text>(true))
            {
                text.font = SpreadsheetFont;
                text.fontStyle = FontStyle.Normal;
                text.fontSize = 18;
                text.color = string.Equals((text.text ?? string.Empty).Trim(), "fx", StringComparison.OrdinalIgnoreCase)
                    ? FormulaText
                    : CellText;
            }
        }

        private void StyleHeaders()
        {
            foreach (Transform child in spreadsheet)
            {
                if (child == null || child.gameObject.name != "Header") continue;
                var image = child.GetComponent<Image>();
                if (image != null) image.color = Header;

                var outline = EnsureOutline(child.gameObject);
                outline.effectColor = Grid;
                outline.effectDistance = new Vector2(2f, -2f);
                outline.useGraphicAlpha = false;

                foreach (var text in child.GetComponentsInChildren<Text>(true))
                {
                    text.font = SpreadsheetFont;
                    text.fontSize = 18;
                    text.fontStyle = FontStyle.Normal;
                    text.color = MutedText;
                }
            }
        }

        private void StyleCells()
        {
            var models = CellsField?.GetValue(prototype) as CellModel[,];
            var views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            if (models == null || views == null) return;

            var selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            var goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
            var intent = CurrentIntentField?.GetValue(prototype);
            var hasIntent = intent is AnomalyIntent;
            var currentIntent = hasIntent ? (AnomalyIntent)intent : default;

            var rowCount = models.GetLength(0);
            var columnCount = models.GetLength(1);
            for (var row = 0; row < rowCount; row++)
            for (var column = 0; column < columnCount; column++)
            {
                var cell = models[row, column];
                var view = views[row, column];
                if (cell == null || view == null) continue;

                var selected = selection != null && selection.Contains(cell);
                var reportTarget = goals != null && goals.Any(goal => goal.TargetRow == row && goal.TargetColumn == column);
                var intentTarget = hasIntent && currentIntent.TargetRow == row && currentIntent.TargetColumn == column;
                StyleCell(view, cell, selected, reportTarget, intentTarget);
            }
        }

        private void StyleCell(ExcelHellCellView view, CellModel cell, bool selected, bool reportTarget, bool intentTarget)
        {
            var image = view.GetComponent<Image>();
            var outline = EnsureOutline(view.gameObject);
            var hover = view.GetComponent<PrototypeSpreadsheetCellHover>();
            if (hover == null) hover = view.gameObject.AddComponent<PrototypeSpreadsheetCellHover>();

            Color backgroundColor;
            Color borderColor;
            Color foreground;
            var borderWidth = 2f;

            if (cell.State == CellState.Destroyed)
            {
                // A deleted cell is absence, not an empty live panel. Keep a faint coordinate seam so multiple
                // adjacent holes do not destroy the player's spatial reading of the worksheet.
                backgroundColor = Deleted;
                borderColor = DeletedGrid;
                foreground = MutedText;
                borderWidth = 1f;
            }
            else if (cell.State == CellState.Corrupted)
            {
                backgroundColor = RefCell;
                borderColor = RefAccent;
                foreground = RefText;
            }
            else if (intentTarget)
            {
                backgroundColor = IntentCell;
                borderColor = IntentAccent;
                foreground = CellText;
            }
            else if (reportTarget)
            {
                backgroundColor = ReportCell;
                borderColor = ReportAccent;
                foreground = new Color32(229, 211, 165, 255);
            }
            else if (cell.IsFormula)
            {
                backgroundColor = FormulaCell;
                borderColor = FormulaText;
                foreground = FormulaText;
            }
            else if (cell.Occupant != null &&
                     (cell.Occupant.Kind == ContentKind.RecordKey || cell.Occupant.Kind == ContentKind.FieldKey))
            {
                backgroundColor = CellRaised;
                borderColor = Grid;
                foreground = new Color32(188, 202, 215, 255);
            }
            else
            {
                backgroundColor = Cell;
                borderColor = Grid;
                foreground = CellText;
            }

            if (hover.IsHovered && cell.State != CellState.Destroyed && !selected)
                backgroundColor = Color.Lerp(backgroundColor, Color.white, 0.055f);

            if (selected)
            {
                borderColor = Selection;
                borderWidth = 3f;
            }

            if (image != null) image.color = backgroundColor;
            outline.effectColor = borderColor;
            outline.effectDistance = new Vector2(borderWidth, -borderWidth);
            outline.useGraphicAlpha = false;

            var directLabel = view.GetComponentsInChildren<Text>(true)
                .FirstOrDefault(text => text.transform.parent == view.transform);
            if (cell.State == CellState.Destroyed && directLabel != null)
                directLabel.text = string.Empty; // replaces the prototype's temporary × marker with a real hole.

            foreach (var text in view.GetComponentsInChildren<Text>(true))
            {
                if (HasAncestorStartingWith(text.transform, "Cell Message")) continue;

                text.font = SpreadsheetFont;
                text.color = foreground;
                text.fontStyle = FontStyle.Normal;

                var parentName = text.transform.parent != null ? text.transform.parent.name : string.Empty;
                var value = (text.text ?? string.Empty).Trim();
                if (parentName == "Report Goal Caption")
                {
                    text.fontSize = 12;
                    text.color = ReportAccent;
                }
                else if (parentName == "Formula 2.0 Interaction")
                {
                    text.fontSize = 18;
                    text.color = FormulaText;
                }
                else if (value == "#REF!")
                {
                    text.fontSize = 24;
                    text.color = RefText;
                }
                else if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out _) ||
                         double.TryParse(value, NumberStyles.Any, CultureInfo.CurrentCulture, out _))
                {
                    text.fontSize = 30;
                }
                else
                {
                    text.fontSize = 18;
                }
            }
        }

        private void StyleShortControls()
        {
            if (background == null) return;
            foreach (var text in background.GetComponentsInChildren<Text>(true))
            {
                if (text == null || text.transform.parent == null) continue;
                var parent = text.transform.parent.name;
                if (parent != "Tasks Reserved" && parent != "Help Reserved" &&
                    parent != "Chat Reserved" && parent != "Menu Reserved" &&
                    parent != "Delete Reserved") continue;

                text.font = SpreadsheetFont;
                text.fontSize = 18;
                text.fontStyle = FontStyle.Normal;
                text.color = CellText;
            }
        }

        private void SetImage(string objectName, Color color)
        {
            if (background == null) return;
            var rect = FindRect(background, objectName);
            if (rect == null) return;
            var image = rect.GetComponent<Image>();
            if (image != null) image.color = color;
        }

        private void DestroyFrame()
        {
            if (pixelFrame != null) Destroy(pixelFrame);
            pixelFrame = null;
        }

        private void OnDisable() => DestroyFrame();

        private static Outline EnsureOutline(GameObject target)
        {
            var outline = target.GetComponent<Outline>();
            if (outline == null) outline = target.AddComponent<Outline>();
            return outline;
        }

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

        private static Image CreateInsetPanel(Transform parent, string name, float inset, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void CreateBevelStrip(Transform parent, string name, bool horizontal, bool leading, float size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            if (horizontal)
            {
                rect.anchorMin = new Vector2(0f, leading ? 1f : 0f);
                rect.anchorMax = new Vector2(1f, leading ? 1f : 0f);
                rect.pivot = new Vector2(0.5f, leading ? 1f : 0f);
                rect.sizeDelta = new Vector2(0f, size);
                rect.anchoredPosition = Vector2.zero;
            }
            else
            {
                rect.anchorMin = new Vector2(leading ? 0f : 1f, 0f);
                rect.anchorMax = new Vector2(leading ? 0f : 1f, 1f);
                rect.pivot = new Vector2(leading ? 0f : 1f, 0.5f);
                rect.sizeDelta = new Vector2(size, 0f);
                rect.anchoredPosition = Vector2.zero;
            }

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }
    }

    /// <summary>Pointer-only visual state. It deliberately never forwards gameplay actions.</summary>
    public sealed class PrototypeSpreadsheetCellHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsHovered { get; private set; }
        public void OnPointerEnter(PointerEventData eventData) => IsHovered = true;
        public void OnPointerExit(PointerEventData eventData) => IsHovered = false;
        private void OnDisable() => IsHovered = false;
    }
}
