using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Formula Cells 2.0 interaction layer.
    /// Drag = MOVE, Shift+Drag = SELECT. Filled formulas expose their occupant first;
    /// empty Formula properties are themselves movable. Formula activation happens only by DROP.
    /// </summary>
    [DefaultExecutionOrder(1100)]
    public sealed class PrototypeFormulaCells : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float DragThresholdPixels = 7f;
        private static readonly Color FormulaTextColor = new(0.03f, 0.30f, 0.86f, 1f);
        private static readonly Color FormulaBackgroundColor = new(0.88f, 0.92f, 0.97f, 1f);

        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo SelectionField = typeof(ExcelHellPrototype).GetField("selection", Flags);
        private static readonly FieldInfo SchemaField = typeof(ExcelHellPrototype).GetField("schema", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly FieldInfo AggregateCounterField = typeof(ExcelHellPrototype).GetField("aggregateCounter", Flags);
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);
        private static readonly FieldInfo LocalizationField = typeof(ExcelHellPrototype).GetField("loc", Flags);
        private static readonly FieldInfo ClipboardTextField = typeof(ExcelHellPrototype).GetField("clipboardTextUi", Flags);
        private static readonly MethodInfo CanActMethod = typeof(ExcelHellPrototype).GetMethod("CanAct", Flags);
        private static readonly MethodInfo CompletePlayerActionMethod = typeof(ExcelHellPrototype).GetMethod("CompletePlayerAction", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);
        private static readonly MethodInfo LegacyDeleteMethod = typeof(ExcelHellPrototype).GetMethod("OnDelete", Flags);

        private readonly Dictionary<CellModel, FormulaCellOverlay> overlays = new();
        private readonly List<TokenPayload> dragTokens = new();
        private readonly List<CellModel> dragSelectionCells = new();

        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private List<CellModel> selection;
        private WorksheetSchema schema;
        private List<ReportGoal> goals;

        private Text formulaBarText;
        private string lastExpression = string.Empty;
        private Button deleteButton;

        private bool pointerDown;
        private bool selectingRange;
        private bool movePrepared;
        private bool draggingMove;
        private bool draggingFormula;
        private CellModel pressCell;
        private CellModel hoverCell;
        private FormulaKind draggedFormula;
        private Vector2 pressScreenPosition;

        private GameObject dragGhost;
        private RectTransform dragGhostRect;
        private Image dragGhostImage;
        private Text dragGhostText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeFormulaCells>() != null) return;
            var helper = new GameObject("EXEL HELL Formula Cells 2.0").AddComponent<PrototypeFormulaCells>();
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
            if (prototype == null || cells == null || !FormulaModeEnabled()) return;

            EnsureInteractionBindings();
            RefreshFormulaPresentation();

            if (pointerDown && movePrepared && !selectingRange && !draggingMove)
            {
                var pointer = CurrentPointerPosition();
                if (Vector2.Distance(pointer, pressScreenPosition) >= DragThresholdPixels)
                    BeginMoveDrag();
            }

            if (draggingMove)
                UpdateGhostPosition();
        }

        private bool FormulaModeEnabled() => PrototypeLevelRuntime.Current.FormulaCellsEnabled;

        private void Bind(ExcelHellPrototype owner)
        {
            DestroyGhost();
            overlays.Clear();
            ResetPointerState();

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

            if (!FormulaModeEnabled()) return;

            BuildFormulaBar();
            HideLegacyControls();
            RebindDeleteProtection();
            EnsureInteractionBindings();
            BuildGhost();
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
            fx.raycastTarget = false;

            formulaBarText = CreateText(root.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            SetRect(formulaBarText.rectTransform, 48f, 0f, 844f, 28f);
            formulaBarText.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            formulaBarText.raycastTarget = false;
        }

        private void HideLegacyControls()
        {
            var hidden = new HashSet<string> { "ui.sum", "ui.sort", "ui.cut", "ui.paste" };
            foreach (var button in prototype.GetComponentsInChildren<Button>(true))
                if (hidden.Contains(button.gameObject.name)) button.gameObject.SetActive(false);

            var clipboardText = ClipboardTextField?.GetValue(prototype) as Text;
            if (clipboardText != null) clipboardText.gameObject.SetActive(false);

            deleteButton = prototype.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "ui.delete");
            if (deleteButton != null)
            {
                var rect = deleteButton.GetComponent<RectTransform>();
                if (rect != null)
                {
                    var position = rect.anchoredPosition;
                    position.x = 20f;
                    rect.anchoredPosition = position;
                }
            }
        }

        private void RebindDeleteProtection()
        {
            if (deleteButton == null)
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

        private void EnsureInteractionBindings()
        {
            if (views == null || cells == null) return;

            foreach (var cell in cells)
            {
                if (overlays.ContainsKey(cell)) continue;
                var view = views[cell.Row, cell.Column];
                if (view == null) continue;

                var overlayGo = new GameObject("Formula 2.0 Interaction", typeof(RectTransform), typeof(Image), typeof(FormulaCellOverlay));
                overlayGo.transform.SetParent(view.transform, false);
                Stretch(overlayGo.GetComponent<RectTransform>());

                var hitbox = overlayGo.GetComponent<Image>();
                hitbox.color = new Color(1f, 1f, 1f, 0.001f);
                hitbox.raycastTarget = true;

                var text = CreateText(overlayGo.transform, string.Empty, 13, FontStyle.Bold, TextAnchor.MiddleCenter);
                Stretch(text.rectTransform, 4f);
                text.color = FormulaTextColor;
                text.raycastTarget = false;

                var overlay = overlayGo.GetComponent<FormulaCellOverlay>();
                overlay.Initialize(this, cell, text, view.GetComponent<Image>());
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

                if (!cell.IsFormula || cell.State != CellState.Normal)
                {
                    overlay.SetFormulaText(string.Empty);
                    continue;
                }

                overlay.SetFormulaText(FormulaDisplay(cell));
                overlay.SetTextColor(FormulaTextColor);
                if (!IsReportTarget(cell)) overlay.SetBackground(FormulaBackgroundColor);
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

        internal void HandlePointerDown(CellModel cell, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || prototype == null || cell == null) return;

            ResetPointerState();
            pointerDown = true;
            pressCell = cell;
            hoverCell = cell;
            pressScreenPosition = eventData.position;

            if (ShiftHeld())
            {
                selectingRange = true;
                prototype.BeginSelection(cell.Row, cell.Column);
                return;
            }

            PrepareMove(cell);
        }

        internal void HandlePointerEnter(CellModel cell, PointerEventData eventData)
        {
            if (cell == null) return;
            hoverCell = cell;
            if (!pointerDown) return;

            if (selectingRange)
            {
                prototype.HoverSelection(cell.Row, cell.Column);
                return;
            }

            if (movePrepared && !draggingMove && pressCell != cell)
                BeginMoveDrag();
        }

        internal void HandlePointerExit(CellModel cell, PointerEventData eventData)
        {
            if (hoverCell == cell) hoverCell = null;
        }

        internal void HandlePointerUp(CellModel cell, PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left || !pointerDown) return;

            if (selectingRange)
            {
                prototype.EndSelection();
                pointerDown = false;
                selectingRange = false;
                return;
            }

            if (!draggingMove)
            {
                SelectSingle(pressCell);
                ResetPointerState();
                return;
            }

            TryDrop(hoverCell);
            ResetPointerState();
        }

        private void PrepareMove(CellModel source)
        {
            dragTokens.Clear();
            dragSelectionCells.Clear();
            draggingFormula = false;
            draggedFormula = FormulaKind.None;
            movePrepared = false;

            if (source == null || source.State != CellState.Normal) return;

            if (source.Occupant != null && IsMovableToken(source.Occupant))
            {
                var movingSelection = selection != null && selection.Count > 1 && selection.Contains(source);
                if (movingSelection)
                {
                    dragSelectionCells.AddRange(selection);
                    foreach (var selectedCell in selection)
                        if (selectedCell.State == CellState.Normal && selectedCell.Occupant != null && IsMovableToken(selectedCell.Occupant))
                            dragTokens.Add(new TokenPayload(selectedCell, selectedCell.Occupant));
                }
                else
                {
                    dragSelectionCells.Add(source);
                    dragTokens.Add(new TokenPayload(source, source.Occupant));
                    selection?.Clear();
                    RefreshAllMethod?.Invoke(prototype, null);
                }

                movePrepared = dragTokens.Count > 0;
                return;
            }

            if (source.Occupant == null && source.IsFormula)
            {
                selection?.Clear();
                RefreshAllMethod?.Invoke(prototype, null);
                draggingFormula = true;
                draggedFormula = source.Formula;
                dragSelectionCells.Add(source);
                movePrepared = true;
            }
        }

        private void BeginMoveDrag()
        {
            if (!movePrepared || pressCell == null) return;

            draggingMove = true;
            EnsureGhost();

            if (draggingFormula)
            {
                dragGhostText.text = draggedFormula == FormulaKind.Sum ? "=SUM()" : "=SORT()";
                dragGhostImage.color = new Color(0.78f, 0.87f, 1f, 0.94f);
            }
            else if (dragTokens.Count == 1)
            {
                dragGhostText.text = DisplayToken(dragTokens[0].Token);
                dragGhostImage.color = new Color(0.95f, 0.96f, 0.98f, 0.94f);
            }
            else
            {
                var preview = string.Join(" · ", dragTokens.Take(4).Select(payload => DisplayToken(payload.Token)));
                if (dragTokens.Count > 4) preview += " · …";
                dragGhostText.text = $"{dragTokens.Count} ×  {preview}";
                dragGhostImage.color = new Color(0.95f, 0.96f, 0.98f, 0.94f);
            }

            dragGhost.SetActive(true);
            UpdateGhostPosition();
        }

        private void TryDrop(CellModel target)
        {
            if (target == null)
            {
                SetStatus(Ru("MOVE: отпустите объект над клеткой таблицы.", "MOVE: drop the object over a worksheet cell."));
                return;
            }

            if (CanActMethod != null && !(bool)CanActMethod.Invoke(prototype, null)) return;

            if (draggingFormula)
            {
                TryMoveFormula(target);
                return;
            }

            if (target.IsFormula)
            {
                if (target.Occupant != null || target.State != CellState.Normal)
                {
                    SetStatus(Ru("Формула занята. Сначала вынесите её содержимое.", "Formula is occupied. Move its occupant out first."));
                    return;
                }

                if (target.Formula == FormulaKind.Sum) TrySumDrop(target);
                else if (target.Formula == FormulaKind.Sort) TrySortDrop(target);
                return;
            }

            TryMoveTokens(target);
        }

        private void TryMoveFormula(CellModel target)
        {
            if (pressCell == null || !pressCell.IsFormula || pressCell.Occupant != null || pressCell.State != CellState.Normal || draggedFormula == FormulaKind.None)
            {
                SetStatus(Ru("MOVE: эта формула сейчас не переносится.", "MOVE: this formula cannot be moved right now."));
                return;
            }

            if (target == pressCell) return;
            if (target.State != CellState.Normal || target.Occupant != null || target.IsFormula)
            {
                SetStatus(Ru("MOVE: формуле нужна пустая доступная клетка.", "MOVE: formula needs an empty available cell."));
                return;
            }

            pressCell.Formula = FormulaKind.None;
            target.Formula = draggedFormula;
            selection?.Clear();
            lastExpression = string.Empty;
            CompleteAction(Ru($"Формула перенесена в {target.Address}.", $"Formula moved to {target.Address}."));
        }

        private void TryMoveTokens(CellModel target)
        {
            if (dragTokens.Count == 0 || pressCell == null) return;

            var dr = target.Row - pressCell.Row;
            var dc = target.Column - pressCell.Column;
            if (dr == 0 && dc == 0) return;

            var movingIds = dragTokens.Select(payload => payload.Token.Id).ToHashSet();
            var destinations = new List<CellModel>(dragTokens.Count);

            foreach (var payload in dragTokens)
            {
                if (payload.Source.State != CellState.Normal || payload.Source.Occupant != payload.Token)
                {
                    SetStatus(Ru("MOVE: исходные данные изменились.", "MOVE: source data changed."));
                    return;
                }

                var row = payload.Source.Row + dr;
                var column = payload.Source.Column + dc;
                if (!InsideBoard(row, column))
                {
                    SetStatus(Ru("MOVE: диапазон выходит за границы таблицы.", "MOVE: range would leave the worksheet."));
                    return;
                }

                var destination = cells[row, column];
                if (destination.State != CellState.Normal || destination.IsFormula)
                {
                    SetStatus(Ru("MOVE: конечные клетки должны быть доступными и не содержать формул.", "MOVE: destination cells must be available and formula-free."));
                    return;
                }

                if (destination.Occupant != null && !movingIds.Contains(destination.Occupant.Id))
                {
                    SetStatus(Ru("MOVE: конечный диапазон занят.", "MOVE: destination range is occupied."));
                    return;
                }
                destinations.Add(destination);
            }

            foreach (var payload in dragTokens) payload.Source.Occupant = null;
            for (var i = 0; i < dragTokens.Count; i++) destinations[i].Occupant = dragTokens[i].Token;

            var movedCount = dragTokens.Count;
            selection?.Clear();
            lastExpression = string.Empty;
            CompleteAction(Ru(
                movedCount == 1 ? $"MOVE → {target.Address}." : $"MOVE перенёс {movedCount} значений.",
                movedCount == 1 ? $"MOVE → {target.Address}." : $"MOVE relocated {movedCount} values."));
        }

        private void TrySumDrop(CellModel target)
        {
            if (!target.CanActivateFormula || target.Formula != FormulaKind.Sum) return;
            if (dragSelectionCells.Contains(target))
            {
                SetStatus(Ru("SUM: целевая формула не должна входить в исходный диапазон.", "SUM: target formula cannot be part of the source range."));
                return;
            }

            var numericSources = new List<TokenPayload>();
            foreach (var sourceCell in dragSelectionCells)
            {
                if (sourceCell.State != CellState.Normal)
                {
                    SetStatus(Ru("SUM: диапазон пересекает недоступную клетку.", "SUM: range crosses an unavailable cell."));
                    return;
                }
                if (sourceCell.Occupant == null) continue;

                var token = sourceCell.Occupant;
                if (!token.IsNumeric || (token.Kind != ContentKind.Data && token.Kind != ContentKind.Aggregate))
                {
                    SetStatus(Ru("SUM: диапазон может содержать только числа и обычные пустые клетки.", "SUM: range may contain only numbers and normal blanks."));
                    return;
                }
                numericSources.Add(new TokenPayload(sourceCell, token));
            }

            if (numericSources.Count < 2)
            {
                SetStatus(Ru("SUM: нужен диапазон минимум с двумя числами.", "SUM: range needs at least two numbers."));
                return;
            }

            var sum = numericSources.Sum(source => source.Token.Number.Value);
            var provenance = numericSources.SelectMany(source => source.Token.SourceTokenIds ?? new List<string>()).Distinct().ToArray();
            var required = numericSources.Any(source => source.Token.IsRequiredSource);

            // ReportCell > FormulaCell: report occupants are readable but persistent operands.
            foreach (var source in numericSources)
                if (!IsReportTarget(source.Source)) source.Source.Occupant = null;

            var counter = AggregateCounterField != null ? (int)AggregateCounterField.GetValue(prototype) : 0;
            counter++;
            AggregateCounterField?.SetValue(prototype, counter);
            target.Occupant = ContentToken.Aggregate($"aggregate.{counter}", sum, provenance, required);

            lastExpression = $"=SUM({AddressExpression(dragSelectionCells)})";
            selection?.Clear();
            CompleteAction(Ru(
                $"SUM схлопнул {numericSources.Count} значений в {target.Address}.",
                $"SUM collapsed {numericSources.Count} values into {target.Address}."));
        }

        private void TrySortDrop(CellModel target)
        {
            if (!target.CanActivateFormula || target.Formula != FormulaKind.Sort) return;
            if (dragTokens.Count != 1)
            {
                SetStatus(Ru("SORT: перетащите один ключ параметра или сотрудника.", "SORT: drag one field or employee key."));
                return;
            }

            var payload = dragTokens[0];
            var key = payload.Token;
            if (key.Kind != ContentKind.RecordKey && key.Kind != ContentKind.FieldKey)
            {
                SetStatus(Ru("SORT: нужен ключ параметра или сотрудника.", "SORT: a field or employee key is required."));
                return;
            }

            if (!TryBuildFormulaSortPlan(target, key, out var tokens, out var destinations))
            {
                SetStatus("#SPILL!");
                return;
            }

            var movingIds = tokens.Where(token => token != null).Select(token => token.Id).ToHashSet();
            foreach (var cell in cells)
                if (cell.Occupant != null && movingIds.Contains(cell.Occupant.Id)) cell.Occupant = null;
            for (var i = 0; i < tokens.Count; i++)
                if (tokens[i] != null) destinations[i].Occupant = tokens[i];

            payload.Source.Occupant = null;
            target.Occupant = key;
            var moved = tokens.Count(token => token != null);
            lastExpression = $"=SORT({payload.Source.Address})";
            selection?.Clear();
            CompleteAction(Ru(
                $"SORT переместил {moved} значений к {target.Address}.",
                $"SORT moved {moved} values to {target.Address}."));
        }

        private bool TryBuildFormulaSortPlan(CellModel formulaCell, ContentToken key, out List<ContentToken> tokens, out List<CellModel> destinations)
        {
            tokens = new List<ContentToken>();
            destinations = new List<CellModel>();
            if (schema == null) return false;

            var data = cells.Cast<CellModel>()
                .Where(cell => cell.State == CellState.Normal && cell.Occupant?.Kind == ContentKind.Data)
                .Select(cell => cell.Occupant)
                .ToList();

            int dr;
            int dc;
            if (key.Kind == ContentKind.FieldKey)
            {
                tokens = schema.Records.Select(recordId => data.FirstOrDefault(token => token.FieldId == key.FieldId && token.RecordId == recordId)).ToList();
                dr = 1;
                dc = 0;
            }
            else
            {
                tokens = schema.Fields.Select(fieldId => data.FirstOrDefault(token => token.RecordId == key.RecordId && token.FieldId == fieldId)).ToList();
                dr = 0;
                dc = 1;
            }

            if (tokens.All(token => token == null)) return false;
            var movingIds = tokens.Where(token => token != null).Select(token => token.Id).ToHashSet();
            for (var i = 1; i <= tokens.Count; i++)
            {
                var row = formulaCell.Row + dr * i;
                var column = formulaCell.Column + dc * i;
                if (!InsideBoard(row, column)) return false;

                var destination = cells[row, column];
                if (destination.State != CellState.Normal || destination.IsFormula) return false;
                if (destination.Occupant != null && !movingIds.Contains(destination.Occupant.Id)) return false;
                destinations.Add(destination);
            }
            return true;
        }

        private bool InsideBoard(int row, int column) => row >= 0 && column >= 0 && row < cells.GetLength(0) && column < cells.GetLength(1);

        private bool IsReportTarget(CellModel cell) =>
            cell != null && goals != null && goals.Any(goal => goal.TargetRow == cell.Row && goal.TargetColumn == cell.Column);

        private static bool IsMovableToken(ContentToken token) => token != null && token.Kind != ContentKind.Label;

        private void SelectSingle(CellModel cell)
        {
            if (cell == null) return;
            prototype.BeginSelection(cell.Row, cell.Column);
            prototype.EndSelection();
            lastExpression = string.Empty;
        }

        private bool ShiftHeld()
        {
            var keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        }

        private Vector2 CurrentPointerPosition() => Mouse.current != null ? Mouse.current.position.ReadValue() : pressScreenPosition;

        private string AddressExpression(IReadOnlyList<CellModel> sourceCells)
        {
            if (sourceCells == null || sourceCells.Count == 0) return string.Empty;
            var minRow = sourceCells.Min(cell => cell.Row);
            var maxRow = sourceCells.Max(cell => cell.Row);
            var minColumn = sourceCells.Min(cell => cell.Column);
            var maxColumn = sourceCells.Max(cell => cell.Column);
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
            RefreshFormulaPresentation();
        }

        private void ResetPointerState()
        {
            pointerDown = false;
            selectingRange = false;
            movePrepared = false;
            draggingMove = false;
            draggingFormula = false;
            pressCell = null;
            hoverCell = null;
            draggedFormula = FormulaKind.None;
            dragTokens.Clear();
            dragSelectionCells.Clear();
            if (dragGhost != null) dragGhost.SetActive(false);
        }

        private void BuildGhost()
        {
            if (dragGhost != null || prototype == null) return;
            var canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
            if (canvas == null) return;

            dragGhost = new GameObject("MOVE Ghost", typeof(RectTransform), typeof(Image), typeof(Outline));
            dragGhost.transform.SetParent(canvas.transform, false);
            dragGhostRect = dragGhost.GetComponent<RectTransform>();
            dragGhostRect.anchorMin = dragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);
            dragGhostRect.pivot = new Vector2(0f, 1f);
            dragGhostRect.sizeDelta = new Vector2(170f, 48f);

            dragGhostImage = dragGhost.GetComponent<Image>();
            dragGhostImage.color = new Color(0.95f, 0.96f, 0.98f, 0.94f);
            dragGhostImage.raycastTarget = false;

            var outline = dragGhost.GetComponent<Outline>();
            outline.effectColor = new Color(0.14f, 0.24f, 0.34f, 0.65f);
            outline.effectDistance = new Vector2(1f, -1f);

            dragGhostText = CreateText(dragGhost.transform, string.Empty, 14, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(dragGhostText.rectTransform, 5f);
            dragGhostText.color = new Color(0.10f, 0.13f, 0.18f, 1f);
            dragGhostText.raycastTarget = false;
            dragGhost.SetActive(false);
        }

        private void EnsureGhost()
        {
            if (dragGhost == null) BuildGhost();
        }

        private void UpdateGhostPosition()
        {
            if (dragGhostRect == null) return;
            dragGhostRect.position = CurrentPointerPosition() + new Vector2(16f, 18f);
            dragGhost.transform.SetAsLastSibling();
        }

        private void DestroyGhost()
        {
            if (dragGhost != null) Destroy(dragGhost);
            dragGhost = null;
            dragGhostRect = null;
            dragGhostImage = null;
            dragGhostText = null;
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

        private readonly struct TokenPayload
        {
            public readonly CellModel Source;
            public readonly ContentToken Token;

            public TokenPayload(CellModel source, ContentToken token)
            {
                Source = source;
                Token = token;
            }
        }
    }

    /// <summary>
    /// Universal pointer layer installed over every worksheet cell on Formula Cells 2.0 levels.
    /// </summary>
    public sealed class FormulaCellOverlay : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler
    {
        private PrototypeFormulaCells runtime;
        private CellModel cell;
        private Text text;
        private Image cellBackground;

        public void Initialize(PrototypeFormulaCells formulaRuntime, CellModel model, Text formulaText, Image background)
        {
            runtime = formulaRuntime;
            cell = model;
            text = formulaText;
            cellBackground = background;
        }

        public void SetFormulaText(string value) { if (text != null) text.text = value; }
        public void SetTextColor(Color value) { if (text != null) text.color = value; }
        public void SetBackground(Color value) { if (cellBackground != null) cellBackground.color = value; }
        public void OnPointerDown(PointerEventData eventData) => runtime?.HandlePointerDown(cell, eventData);
        public void OnPointerEnter(PointerEventData eventData) => runtime?.HandlePointerEnter(cell, eventData);
        public void OnPointerExit(PointerEventData eventData) => runtime?.HandlePointerExit(cell, eventData);
        public void OnPointerUp(PointerEventData eventData) => runtime?.HandlePointerUp(cell, eventData);
    }
}
