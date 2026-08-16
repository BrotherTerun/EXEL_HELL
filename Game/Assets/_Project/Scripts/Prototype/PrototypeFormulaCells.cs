using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(1100)]
    public sealed class PrototypeFormulaCells : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly Color FormulaTextColor = new(0.03f, 0.30f, 0.86f, 1f);

        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);
        private static readonly FieldInfo SchemaField = typeof(ExcelHellPrototype).GetField("schema", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly FieldInfo AggregateCounterField = typeof(ExcelHellPrototype).GetField("aggregateCounter", Flags);
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);
        private static readonly FieldInfo LocalizationField = typeof(ExcelHellPrototype).GetField("loc", Flags);
        private static readonly MethodInfo CanActMethod = typeof(ExcelHellPrototype).GetMethod("CanAct", Flags);
        private static readonly MethodInfo CompletePlayerActionMethod = typeof(ExcelHellPrototype).GetMethod("CompletePlayerAction", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);
        private static readonly MethodInfo LegacyDeleteMethod = typeof(ExcelHellPrototype).GetMethod("OnDelete", Flags);

        private readonly Dictionary<CellModel, FormulaCellOverlay> overlays = new();
        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<CellModel> selection;
        private WorksheetSchema schema;
        private List<ReportGoal> goals;
        private Text formulaBarText;
        private string lastExpression = string.Empty;
        private Button deleteButton;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeFormulaCells>() != null) return;
            var helper = new GameObject("EXEL HELL Formula Cells").AddComponent<PrototypeFormulaCells>();
            DontDestroyOnLoad(helper.gameObject);
        }

        public static void AssignFormula(CellModel cell, FormulaKind formula)
        {
            if (cell != null) cell.Formula = formula;
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || cells == null) return;
            EnsureFormulaBindings();
            RefreshFormulaPresentation();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            overlays.Clear();
            prototype = owner;
            cells = null;
            views = null;
            selection = null;
            schema = null;
            goals = null;
            formulaBarText = null;
            deleteButton = null;
            lastExpression = string.Empty;
            if (prototype == null) return;

            cells = CellsField?.GetValue(prototype) as CellModel[,];
            views = ViewsField?.GetValue(prototype) as ExcelHellCellView[,];
            selection = SelectionField?.GetValue(prototype) as List<CellModel>;
            schema = SchemaField?.GetValue(prototype) as WorksheetSchema;
            goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;

            BuildFormulaBar();
            RebindDeleteProtection();
            EnsureFormulaBindings();
        }

        private void BuildFormulaBar()
        {
            var canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
            if (canvas == null) return;

            var root = new GameObject("Formula Bar", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(canvas.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(24f, -62f);
            rect.sizeDelta = new Vector2(900f, 28f);
            root.GetComponent<Image>().color = new Color(0.96f, 0.97f, 0.98f, 1f);

            var fx = CreateText(root.transform, "fx", 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(fx.rectTransform, 0f, 0f, 42f, 28f);
            fx.color = new Color(0.22f, 0.28f, 0.32f, 1f);

            formulaBarText = CreateText(root.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(formulaBarText.rectTransform, 48f, 0f, 844f, 28f);
            formulaBarText.color = new Color(0.12f, 0.13f, 0.15f, 1f);
        }

        private void RebindDeleteProtection()
        {
            deleteButton = prototype.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "ui.delete");
            if (deleteButton == null) return;
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(DeleteProxy);
        }

        private void DeleteProxy()
        {
            if (selection != null && selection.Count == 1 && selection[0].IsFormula)
            {
                SetStatus(Ru("Формульное поле нельзя удалить вручную.", "Formula fields cannot be deleted manually."));
                return;
            }
            LegacyDeleteMethod?.Invoke(prototype, null);
        }

        private void EnsureFormulaBindings()
        {
            if (views == null || cells == null) return;
            foreach (var cell in cells)
            {
                if (!cell.IsFormula || overlays.ContainsKey(cell)) continue;
                var view = views[cell.Row, cell.Column];
                if (view == null) continue;

                var overlayGo = new GameObject("Formula Activation", typeof(RectTransform), typeof(Image), typeof(FormulaCellOverlay));
                overlayGo.transform.SetParent(view.transform, false);
                Stretch(overlayGo.GetComponent<RectTransform>());
                overlayGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.001f);

                var text = CreateText(overlayGo.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform, 4f);
                text.color = FormulaTextColor;
                text.raycastTarget = false;

                var overlay = overlayGo.GetComponent<FormulaCellOverlay>();
                overlay.Initialize(this, prototype, cell, text, view.GetComponent<Image>());
                overlays[cell] = overlay;
            }
        }

        private void RefreshFormulaPresentation()
        {
            foreach (var pair in overlays)
            {
                var cell = pair.Key;
                var overlay = pair.Value;
                if (overlay == null) continue;
                if (cell.State != CellState.Normal)
                {
                    overlay.SetFormulaText(string.Empty);
                    continue;
                }
                overlay.SetFormulaText(FormulaDisplay(cell));
                overlay.SetTextColor(FormulaTextColor);
                if (!IsReportTarget(cell)) overlay.SetBackground(new Color(0.88f, 0.92f, 0.97f, 1f));
            }

            if (formulaBarText == null) return;
            if (!string.IsNullOrEmpty(lastExpression)) formulaBarText.text = lastExpression;
            else if (selection != null && selection.Count == 1 && selection[0].IsFormula) formulaBarText.text = FormulaDisplay(selection[0]);
            else formulaBarText.text = string.Empty;
        }

        private string FormulaDisplay(CellModel cell)
        {
            if (cell == null || !cell.IsFormula) return string.Empty;
            var function = cell.Formula == FormulaKind.Sum ? "SUM" : "SORT";
            return cell.Occupant == null ? $"={function}()" : $"={function}({DisplayToken(cell.Occupant)})";
        }

        internal bool TryActivate(CellModel formulaCell)
        {
            if (prototype == null || formulaCell == null || !formulaCell.CanActivateFormula) return false;
            if (CanActMethod != null && !(bool)CanActMethod.Invoke(prototype, null)) return true;
            return formulaCell.Formula switch
            {
                FormulaKind.Sum => TrySum(formulaCell),
                FormulaKind.Sort => TrySort(formulaCell),
                _ => false
            };
        }

        internal bool IsValidArgumentFor(CellModel formulaCell)
        {
            if (formulaCell == null || !formulaCell.CanActivateFormula || selection == null) return false;
            return formulaCell.Formula switch
            {
                FormulaKind.Sum => TryGetNumericSources(formulaCell, out _),
                FormulaKind.Sort => TryGetSortKey(out _, out _),
                _ => false
            };
        }

        private bool TrySum(CellModel target)
        {
            if (!TryGetNumericSources(target, out var sources))
            {
                SetStatus(Ru("SUM: выделите прямоугольный диапазон минимум с двумя числами.",
                    "SUM: select a rectangular range containing at least two numbers."));
                return true;
            }

            var sum = sources.Sum(cell => cell.Occupant.Number.Value);
            var provenance = sources.SelectMany(source => source.Occupant.SourceTokenIds ?? new List<string>()).Distinct().ToArray();
            var required = sources.Any(source => source.Occupant.IsRequiredSource);
            var count = sources.Count;

            // ReportCell > FormulaCell: filled report values are persistent operands.
            // Ordinary worksheet sources remain destructively consumed by SUM.
            foreach (var source in sources)
                if (!IsReportTarget(source)) source.Occupant = null;

            var counter = AggregateCounterField != null ? (int)AggregateCounterField.GetValue(prototype) : 0;
            counter++;
            AggregateCounterField?.SetValue(prototype, counter);
            target.Occupant = ContentToken.Aggregate($"aggregate.{counter}", sum, provenance, required);

            lastExpression = $"=SUM({SelectionAddressExpression()})";
            selection.Clear();
            CompleteAction(Ru($"SUM схлопнул {count} значений в {target.Address}.", $"SUM collapsed {count} values into {target.Address}."));
            return true;
        }

        private bool TryGetNumericSources(CellModel target, out List<CellModel> numeric)
        {
            numeric = new List<CellModel>();
            if (selection == null || selection.Count == 0 || selection.Contains(target)) return false;
            foreach (var cell in selection)
            {
                if (cell.State != CellState.Normal) return false;
                if (cell.Occupant == null) continue;
                if (!cell.Occupant.IsNumeric) return false;
                if (cell.Occupant.Kind != ContentKind.Data && cell.Occupant.Kind != ContentKind.Aggregate) return false;
                numeric.Add(cell);
            }
            return numeric.Count >= 2;
        }

        private bool TrySort(CellModel target)
        {
            if (!TryGetSortKey(out var keyCell, out var key))
            {
                SetStatus(Ru("SORT: выделите ровно один ключ параметра или сотрудника.", "SORT: select exactly one field or employee key."));
                return true;
            }
            if (!TryBuildFormulaSortPlan(target, key, out var tokens, out var destinations))
            {
                SetStatus("#SPILL!");
                return true;
            }

            var movingIds = tokens.Where(token => token != null).Select(token => token.Id).ToHashSet();
            foreach (var cell in cells)
                if (cell.Occupant != null && movingIds.Contains(cell.Occupant.Id)) cell.Occupant = null;
            for (var i = 0; i < tokens.Count; i++)
                if (tokens[i] != null) destinations[i].Occupant = tokens[i];

            keyCell.Occupant = null;
            target.Occupant = key;
            lastExpression = $"=SORT({keyCell.Address})";
            var moved = tokens.Count(token => token != null);
            selection.Clear();
            CompleteAction(Ru($"SORT переместил {moved} значений к {target.Address}.", $"SORT moved {moved} values to {target.Address}."));
            return true;
        }

        private bool TryGetSortKey(out CellModel keyCell, out ContentToken key)
        {
            keyCell = null;
            key = null;
            if (selection == null || selection.Count != 1) return false;
            var candidate = selection[0];
            if (candidate.State != CellState.Normal || candidate.Occupant == null) return false;
            if (candidate.Occupant.Kind != ContentKind.RecordKey && candidate.Occupant.Kind != ContentKind.FieldKey) return false;
            keyCell = candidate;
            key = candidate.Occupant;
            return true;
        }

        private bool TryBuildFormulaSortPlan(CellModel formulaCell, ContentToken key, out List<ContentToken> tokens, out List<CellModel> destinations)
        {
            tokens = new List<ContentToken>();
            destinations = new List<CellModel>();
            if (schema == null) return false;

            var data = cells.Cast<CellModel>()
                .Where(cell => cell.State == CellState.Normal && cell.Occupant?.Kind == ContentKind.Data)
                .Select(cell => cell.Occupant).ToList();

            int dr;
            int dc;
            if (key.Kind == ContentKind.FieldKey)
            {
                tokens = schema.Records.Select(recordId => data.FirstOrDefault(token => token.FieldId == key.FieldId && token.RecordId == recordId)).ToList();
                dr = 1; dc = 0;
            }
            else
            {
                tokens = schema.Fields.Select(fieldId => data.FirstOrDefault(token => token.RecordId == key.RecordId && token.FieldId == fieldId)).ToList();
                dr = 0; dc = 1;
            }

            if (tokens.All(token => token == null)) return false;
            var movingIds = tokens.Where(token => token != null).Select(token => token.Id).ToHashSet();
            for (var i = 1; i <= tokens.Count; i++)
            {
                var row = formulaCell.Row + dr * i;
                var column = formulaCell.Column + dc * i;
                if (row < 0 || row >= cells.GetLength(0) || column < 0 || column >= cells.GetLength(1)) return false;
                var destination = cells[row, column];
                if (destination.State != CellState.Normal || destination.IsFormula) return false;
                if (destination.Occupant != null && !movingIds.Contains(destination.Occupant.Id)) return false;
                destinations.Add(destination);
            }
            return true;
        }

        private bool IsReportTarget(CellModel cell) =>
            cell != null && goals != null && goals.Any(goal => goal.TargetRow == cell.Row && goal.TargetColumn == cell.Column);

        private string SelectionAddressExpression()
        {
            if (selection == null || selection.Count == 0) return string.Empty;
            var minRow = selection.Min(cell => cell.Row);
            var maxRow = selection.Max(cell => cell.Row);
            var minColumn = selection.Min(cell => cell.Column);
            var maxColumn = selection.Max(cell => cell.Column);
            var first = cells[minRow, minColumn].Address;
            var last = cells[maxRow, maxColumn].Address;
            return first == last ? first : $"{first}:{last}";
        }

        private string DisplayToken(ContentToken token)
        {
            if (token == null) return string.Empty;
            if (token.Kind == ContentKind.RecordKey || token.Kind == ContentKind.FieldKey || token.Kind == ContentKind.Label)
            {
                var localization = LocalizationField?.GetValue(prototype) as PrototypeLocalization;
                return localization?.Get(token.StringId) ?? token.StringId ?? token.Id;
            }
            if (token.Number.HasValue)
            {
                var value = token.Number.Value;
                return Math.Abs(value % 1d) < 0.001d ? value.ToString("0") : value.ToString("0.##");
            }
            return token.Id;
        }

        private string Ru(string ru, string en)
        {
            var localization = LocalizationField?.GetValue(prototype) as PrototypeLocalization;
            return localization?.Language == PrototypeLanguage.English ? en : ru;
        }

        private void SetStatus(string value)
        {
            var status = StatusTextField?.GetValue(prototype) as Text;
            if (status != null) status.text = value;
        }

        private void CompleteAction(string message)
        {
            CompletePlayerActionMethod?.Invoke(prototype, new object[] { message });
            RefreshAllMethod?.Invoke(prototype, null);
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }

    public sealed class FormulaCellOverlay : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        private PrototypeFormulaCells runtime;
        private ExcelHellPrototype prototype;
        private CellModel cell;
        private Text text;
        private Image cellBackground;
        private bool forwardingSelection;

        public void Initialize(PrototypeFormulaCells formulaRuntime, ExcelHellPrototype owner, CellModel model, Text formulaText, Image background)
        {
            runtime = formulaRuntime;
            prototype = owner;
            cell = model;
            text = formulaText;
            cellBackground = background;
        }

        public void SetFormulaText(string value) { if (text != null) text.text = value; }
        public void SetTextColor(Color value) { if (text != null) text.color = value; }
        public void SetBackground(Color value) { if (cellBackground != null) cellBackground.color = value; }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (runtime != null && runtime.IsValidArgumentFor(cell))
            {
                runtime.TryActivate(cell);
                forwardingSelection = false;
                return;
            }
            forwardingSelection = true;
            prototype.BeginSelection(cell.Row, cell.Column);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (forwardingSelection) prototype.HoverSelection(cell.Row, cell.Column);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            if (forwardingSelection) prototype.EndSelection();
            forwardingSelection = false;
        }
    }
}
