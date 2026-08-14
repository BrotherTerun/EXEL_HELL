using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    public enum CellState
    {
        Normal,
        Corrupted,
        Destroyed
    }

    [Serializable]
    public sealed class CellModel
    {
        public int Row;
        public int Column;
        public string Text;
        public double? Number;
        public CellState State;
        public int CorruptionAge;
        public bool IsRequiredSource;

        public string Address => $"{ExcelHellPrototype.ColumnName(Column)}{Row + 1}";
        public bool HasValue => Number.HasValue || !string.IsNullOrWhiteSpace(Text);

        public void ClearValue()
        {
            Number = null;
            Text = string.Empty;
        }
    }

    public readonly struct AnomalyIntent
    {
        public readonly int SourceRow;
        public readonly int SourceColumn;
        public readonly int TargetRow;
        public readonly int TargetColumn;

        public AnomalyIntent(int sourceRow, int sourceColumn, int targetRow, int targetColumn)
        {
            SourceRow = sourceRow;
            SourceColumn = sourceColumn;
            TargetRow = targetRow;
            TargetColumn = targetColumn;
        }
    }

    public sealed class ExcelHellPrototype : MonoBehaviour
    {
        public const int Rows = 10;
        public const int Columns = 10;
        private const int MaxTurns = 15;
        private const int AnomalyActivationTurn = 3;

        private readonly CellModel[,] cells = new CellModel[Rows, Columns];
        private readonly ExcelHellCellView[,] views = new ExcelHellCellView[Rows, Columns];
        private readonly List<CellModel> selection = new List<CellModel>();
        private readonly List<ReportGoal> goals = new List<ReportGoal>();

        private bool selecting;
        private int selectionStartRow;
        private int selectionStartColumn;
        private bool awaitingSumTarget;
        private double pendingSum;
        private string clipboardText;
        private double? clipboardNumber;
        private int turn;
        private bool finished;
        private AnomalyIntent? currentIntent;

        private Text turnText;
        private Text clipboardTextUi;
        private Text intentText;
        private Text statusText;
        private Text goalsText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<ExcelHellPrototype>() != null)
                return;

            var root = new GameObject("EXEL HELL Prototype");
            root.AddComponent<ExcelHellPrototype>();
        }

        private void Awake()
        {
            EnsureEventSystem();
            BuildModel();
            BuildUi();
            RefreshAll();
            SetStatus("Select cells. SUM calculates a range; CUT/PASTE can evacuate data. #REF! wakes after turn 3.");
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = eventSystemObject.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private void BuildModel()
        {
            for (var row = 0; row < Rows; row++)
            {
                for (var column = 0; column < Columns; column++)
                {
                    cells[row, column] = new CellModel
                    {
                        Row = row,
                        Column = column,
                        State = CellState.Normal,
                        Text = string.Empty
                    };
                }
            }

            SetText(0, 0, "Employee");
            SetText(0, 1, "Hours");
            SetText(0, 2, "Salary");
            SetText(0, 3, "Overtime");
            SetText(0, 4, "Bonus");
            SetText(0, 7, "REPORT");
            SetText(1, 6, "Salary total");
            SetText(2, 6, "Overtime total");
            SetText(3, 6, "Bonus total");

            var employees = new[] { "Ivanov", "Petrov", "Sidorov", "Volkova", "Kim" };
            var hours = new[] { 40d, 40d, 32d, 44d, 36d };
            var salary = new[] { 50d, 52d, 41d, 49d, 55d };
            var overtime = new[] { 3d, 0d, 5d, 2d, 4d };
            var bonus = new[] { 5d, 3d, 2d, 4d, 6d };

            for (var i = 0; i < employees.Length; i++)
            {
                var row = i + 1;
                SetText(row, 0, employees[i]);
                SetNumber(row, 1, hours[i]);
                SetNumber(row, 2, salary[i], true);
                SetNumber(row, 3, overtime[i], true);
                SetNumber(row, 4, bonus[i], true);
            }

            goals.Add(new ReportGoal("Salary total", 247d, 1, 7));
            goals.Add(new ReportGoal("Overtime total", 14d, 2, 7));
            goals.Add(new ReportGoal("Bonus total", 20d, 3, 7));
        }

        private void SetText(int row, int column, string value)
        {
            cells[row, column].Text = value;
            cells[row, column].Number = null;
        }

        private void SetNumber(int row, int column, double value, bool required = false)
        {
            cells[row, column].Number = value;
            cells[row, column].Text = string.Empty;
            cells[row, column].IsRequiredSource = required;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Prototype Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900);
            scaler.matchWidthOrHeight = 0.5f;

            var background = CreatePanel(canvasObject.transform, "Background", new Color(0.94f, 0.95f, 0.96f, 1f));
            Stretch(background.rectTransform);

            var title = CreateText(background.transform, "EXEL HELL // PLAYABLE CORE", 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(title.rectTransform, 24, -16, 700, 48, new Vector2(0, 1));

            turnText = CreateText(background.transform, string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(turnText.rectTransform, 1180, -18, 380, 44, new Vector2(0, 1));

            BuildGrid(background.transform);
            BuildSidebar(background.transform);
        }

        private void BuildGrid(Transform parent)
        {
            var gridRoot = new GameObject("Spreadsheet", typeof(RectTransform), typeof(GridLayoutGroup));
            gridRoot.transform.SetParent(parent, false);
            var gridRect = gridRoot.GetComponent<RectTransform>();
            SetRect(gridRect, 24, -82, 836, 572, new Vector2(0, 1));

            var layout = gridRoot.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = Columns + 1;
            layout.cellSize = new Vector2(76, 52);
            layout.spacing = Vector2.zero;

            CreateHeaderCell(gridRoot.transform, string.Empty);
            for (var column = 0; column < Columns; column++)
                CreateHeaderCell(gridRoot.transform, ColumnName(column));

            for (var row = 0; row < Rows; row++)
            {
                CreateHeaderCell(gridRoot.transform, (row + 1).ToString());
                for (var column = 0; column < Columns; column++)
                {
                    var view = CreateDataCell(gridRoot.transform, row, column);
                    views[row, column] = view;
                }
            }
        }

        private void BuildSidebar(Transform parent)
        {
            var side = CreatePanel(parent, "Sidebar", new Color(0.985f, 0.985f, 0.985f, 1f));
            SetRect(side.rectTransform, 900, -82, 660, 760, new Vector2(0, 1));

            var heading = CreateText(side.transform, "REPORT TASK", 24, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(heading.rectTransform, 20, -18, 620, 38, new Vector2(0, 1));

            goalsText = CreateText(side.transform, string.Empty, 19, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(goalsText.rectTransform, 20, -62, 620, 130, new Vector2(0, 1));

            intentText = CreateText(side.transform, string.Empty, 19, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(intentText.rectTransform, 20, -196, 620, 58, new Vector2(0, 1));

            clipboardTextUi = CreateText(side.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(clipboardTextUi.rectTransform, 20, -260, 620, 44, new Vector2(0, 1));

            statusText = CreateText(side.transform, string.Empty, 17, FontStyle.Italic, TextAnchor.UpperLeft);
            SetRect(statusText.rectTransform, 20, -312, 620, 86, new Vector2(0, 1));

            CreateActionButton(side.transform, "SUM", 20, -420, OnSum);
            CreateActionButton(side.transform, "CUT", 180, -420, OnCut);
            CreateActionButton(side.transform, "PASTE", 340, -420, OnPaste);
            CreateActionButton(side.transform, "DELETE", 500, -420, OnDelete);
            CreateActionButton(side.transform, "SUBMIT REPORT", 20, -492, OnSubmit, 300);
            CreateActionButton(side.transform, "RESET", 340, -492, ResetPrototype, 160);

            var help = CreateText(side.transform,
                "Prototype controls\n• Drag across cells to select a rectangular range.\n• SUM: select numbers → SUM → click target cell.\n• CUT/PASTE: select one cell and press the action.\n• Orange = next #REF! target. Red = corrupted. Black = destroyed.",
                16, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(help.rectTransform, 20, -570, 620, 170, new Vector2(0, 1));
        }

        private ExcelHellCellView CreateDataCell(Transform parent, int row, int column)
        {
            var cellObject = new GameObject($"Cell {ColumnName(column)}{row + 1}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(ExcelHellCellView));
            cellObject.transform.SetParent(parent, false);

            var image = cellObject.GetComponent<Image>();
            image.color = Color.white;

            var outline = cellObject.GetComponent<Outline>();
            outline.effectColor = new Color(0.77f, 0.79f, 0.82f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var label = CreateText(cellObject.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 4);
            label.raycastTarget = false;

            var view = cellObject.GetComponent<ExcelHellCellView>();
            view.Initialize(this, row, column, image, label);
            return view;
        }

        private void CreateHeaderCell(Transform parent, string text)
        {
            var header = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(Outline));
            header.transform.SetParent(parent, false);
            header.GetComponent<Image>().color = new Color(0.88f, 0.89f, 0.91f, 1f);
            var outline = header.GetComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.74f, 0.78f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var label = CreateText(header.transform, text, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 3);
            label.raycastTarget = false;
        }

        private void CreateActionButton(Transform parent, string caption, float x, float y, Action callback, float width = 140)
        {
            var buttonObject = new GameObject(caption, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            SetRect(buttonObject.GetComponent<RectTransform>(), x, y, width, 52, new Vector2(0, 1));

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.84f, 0.87f, 0.91f, 1f);

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => callback());

            var label = CreateText(buttonObject.transform, caption, 17, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 4);
            label.raycastTarget = false;
        }

        public void BeginSelection(int row, int column)
        {
            if (finished)
                return;

            if (awaitingSumTarget)
            {
                CommitSum(row, column);
                return;
            }

            selecting = true;
            selectionStartRow = row;
            selectionStartColumn = column;
            UpdateSelection(row, column);
        }

        public void HoverSelection(int row, int column)
        {
            if (selecting)
                UpdateSelection(row, column);
        }

        public void EndSelection(int row, int column)
        {
            if (!selecting)
                return;

            // PointerUp is delivered to the object where the press began, not necessarily
            // to the cell currently under the pointer. The drag range has already been
            // updated by OnPointerEnter, so only finish the drag here.
            selecting = false;
        }

        private void UpdateSelection(int endRow, int endColumn)
        {
            selection.Clear();
            var minRow = Mathf.Min(selectionStartRow, endRow);
            var maxRow = Mathf.Max(selectionStartRow, endRow);
            var minColumn = Mathf.Min(selectionStartColumn, endColumn);
            var maxColumn = Mathf.Max(selectionStartColumn, endColumn);

            for (var row = minRow; row <= maxRow; row++)
            for (var column = minColumn; column <= maxColumn; column++)
                selection.Add(cells[row, column]);

            RefreshAll();
        }

        private void OnSum()
        {
            if (!CanAct())
                return;

            if (selection.Count == 0)
            {
                SetStatus("SUM needs a selected range.");
                return;
            }

            if (selection.Any(cell => cell.State != CellState.Normal || !cell.Number.HasValue))
            {
                SetStatus("SUM accepts only Normal numeric cells in this prototype.");
                return;
            }

            pendingSum = selection.Sum(cell => cell.Number.Value);
            awaitingSumTarget = true;
            SetStatus($"SUM = {FormatNumber(pendingSum)}. Click a Normal target cell.");
        }

        private void CommitSum(int row, int column)
        {
            var target = cells[row, column];
            if (target.State != CellState.Normal)
            {
                SetStatus("SUM target must be a Normal cell.");
                return;
            }

            target.Number = pendingSum;
            target.Text = string.Empty;
            awaitingSumTarget = false;
            selection.Clear();
            CompletePlayerAction($"SUM written to {target.Address}.");
        }

        private void OnCut()
        {
            if (!CanAct() || !RequireSingleSelection(out var cell))
                return;

            if (cell.State != CellState.Normal || !cell.HasValue)
            {
                SetStatus("CUT needs one non-empty Normal cell.");
                return;
            }

            clipboardNumber = cell.Number;
            clipboardText = cell.Text;
            cell.ClearValue();
            selection.Clear();
            CompletePlayerAction($"CUT {cell.Address} → Clipboard.");
        }

        private void OnPaste()
        {
            if (!CanAct() || !RequireSingleSelection(out var cell))
                return;

            if (!clipboardNumber.HasValue && string.IsNullOrEmpty(clipboardText))
            {
                SetStatus("Clipboard is empty.");
                return;
            }

            if (cell.State != CellState.Normal)
            {
                SetStatus("PASTE target must be Normal.");
                return;
            }

            cell.Number = clipboardNumber;
            cell.Text = clipboardText ?? string.Empty;
            clipboardNumber = null;
            clipboardText = string.Empty;
            selection.Clear();
            CompletePlayerAction($"PASTE into {cell.Address}.");
        }

        private void OnDelete()
        {
            if (!CanAct() || !RequireSingleSelection(out var cell))
                return;

            cell.ClearValue();
            cell.State = CellState.Destroyed;
            cell.CorruptionAge = 0;
            selection.Clear();
            CompletePlayerAction($"DELETE {cell.Address}. Cell destroyed.");
        }

        private bool RequireSingleSelection(out CellModel cell)
        {
            cell = null;
            if (selection.Count != 1)
            {
                SetStatus("This action needs exactly one selected cell.");
                return false;
            }

            cell = selection[0];
            return true;
        }

        private bool CanAct()
        {
            if (finished)
            {
                SetStatus("Run finished. Press RESET.");
                return false;
            }

            if (awaitingSumTarget)
            {
                SetStatus("Finish SUM by clicking its target cell.");
                return false;
            }

            return true;
        }

        private void CompletePlayerAction(string message)
        {
            turn++;
            ResolveAnomaly();

            if (!finished && turn >= MaxTurns)
            {
                finished = true;
                SetStatus("DEADLINE MISSED. Press RESET.");
            }
            else if (!finished)
            {
                SetStatus(message);
            }

            RefreshAll();
        }

        private void ResolveAnomaly()
        {
            if (turn < AnomalyActivationTurn)
                return;

            if (!cells.Cast<CellModel>().Any(cell => cell.State == CellState.Corrupted))
            {
                var spawn = cells[6, 4]; // E7: just below the data block.
                if (spawn.State == CellState.Normal)
                {
                    spawn.State = CellState.Corrupted;
                    spawn.CorruptionAge = 0;
                }

                GenerateIntent();
                return;
            }

            if (currentIntent.HasValue && IsIntentValid(currentIntent.Value))
            {
                var intent = currentIntent.Value;
                var target = cells[intent.TargetRow, intent.TargetColumn];
                target.State = CellState.Corrupted;
                target.CorruptionAge = 0;
            }

            foreach (var cell in cells)
            {
                if (cell.State != CellState.Corrupted)
                    continue;

                // Cells newly infected this resolve remain visible for one full player action.
                if (currentIntent.HasValue && cell.Row == currentIntent.Value.TargetRow && cell.Column == currentIntent.Value.TargetColumn)
                    continue;

                cell.CorruptionAge++;
                if (cell.CorruptionAge >= 2)
                {
                    cell.State = CellState.Destroyed;
                    cell.ClearValue();
                }
            }

            GenerateIntent();
        }

        private bool IsIntentValid(AnomalyIntent intent)
        {
            var source = cells[intent.SourceRow, intent.SourceColumn];
            var target = cells[intent.TargetRow, intent.TargetColumn];
            return source.State == CellState.Corrupted && target.State == CellState.Normal;
        }

        private void GenerateIntent()
        {
            currentIntent = null;
            var corrupted = cells.Cast<CellModel>()
                .Where(cell => cell.State == CellState.Corrupted)
                .OrderByDescending(cell => cell.CorruptionAge)
                .ThenBy(cell => cell.Row)
                .ThenBy(cell => cell.Column)
                .ToList();

            foreach (var source in corrupted)
            {
                var candidates = Neighbours(source)
                    .Where(cell => cell.State == CellState.Normal)
                    .OrderByDescending(cell => cell.IsRequiredSource)
                    .ThenBy(DistanceToNearestRequiredSource)
                    .ThenBy(cell => cell.Row)
                    .ThenBy(cell => cell.Column)
                    .ToList();

                if (candidates.Count == 0)
                    continue;

                var target = candidates[0];
                currentIntent = new AnomalyIntent(source.Row, source.Column, target.Row, target.Column);
                return;
            }
        }

        private IEnumerable<CellModel> Neighbours(CellModel cell)
        {
            var offsets = new[] { (-1, 0), (0, -1), (0, 1), (1, 0) };
            foreach (var (rowOffset, columnOffset) in offsets)
            {
                var row = cell.Row + rowOffset;
                var column = cell.Column + columnOffset;
                if (row >= 0 && row < Rows && column >= 0 && column < Columns)
                    yield return cells[row, column];
            }
        }

        private int DistanceToNearestRequiredSource(CellModel candidate)
        {
            var required = cells.Cast<CellModel>()
                .Where(cell => cell.IsRequiredSource && cell.State != CellState.Destroyed)
                .ToList();

            if (required.Count == 0)
                return int.MaxValue;

            return required.Min(cell => Mathf.Abs(cell.Row - candidate.Row) + Mathf.Abs(cell.Column - candidate.Column));
        }

        private void OnSubmit()
        {
            if (finished)
                return;

            var wrong = new List<string>();
            foreach (var goal in goals)
            {
                var cell = cells[goal.TargetRow, goal.TargetColumn];
                if (!cell.Number.HasValue || Math.Abs(cell.Number.Value - goal.Expected) > 0.001)
                    wrong.Add(goal.Name);
            }

            if (wrong.Count == 0)
            {
                finished = true;
                SetStatus($"REPORT ACCEPTED on turn {turn}/{MaxTurns}. Core loop complete.");
            }
            else
            {
                SetStatus("REPORT REJECTED: " + string.Join(", ", wrong));
            }

            RefreshAll();
        }

        private void ResetPrototype()
        {
            var root = gameObject;
            Destroy(root);
            var replacement = new GameObject("EXEL HELL Prototype");
            replacement.AddComponent<ExcelHellPrototype>();
        }

        private void RefreshAll()
        {
            for (var row = 0; row < Rows; row++)
            for (var column = 0; column < Columns; column++)
                views[row, column]?.Refresh(cells[row, column], selection.Contains(cells[row, column]), IsIntentTarget(row, column));

            if (turnText != null)
                turnText.text = $"TURN {turn}/{MaxTurns}";

            if (clipboardTextUi != null)
            {
                var content = clipboardNumber.HasValue ? FormatNumber(clipboardNumber.Value) : clipboardText;
                clipboardTextUi.text = "CLIPBOARD: " + (string.IsNullOrEmpty(content) ? "—" : content);
            }

            if (intentText != null)
            {
                if (turn < AnomalyActivationTurn)
                    intentText.text = $"#REF! dormant — wakes after turn {AnomalyActivationTurn}";
                else if (currentIntent.HasValue)
                    intentText.text = $"NEXT #REF! → {cells[currentIntent.Value.TargetRow, currentIntent.Value.TargetColumn].Address}";
                else
                    intentText.text = "NEXT #REF! → no valid path";
            }

            if (goalsText != null)
            {
                goalsText.text = string.Join("\n", goals.Select(goal =>
                {
                    var target = cells[goal.TargetRow, goal.TargetColumn];
                    var current = target.Number.HasValue ? FormatNumber(target.Number.Value) : "—";
                    return $"{goal.Name}: {current} / ?   → {target.Address}";
                }));
            }
        }

        private bool IsIntentTarget(int row, int column)
        {
            return currentIntent.HasValue && currentIntent.Value.TargetRow == row && currentIntent.Value.TargetColumn == column;
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
                statusText.text = message;
        }

        public static string ColumnName(int zeroBasedColumn)
        {
            return ((char)('A' + zeroBasedColumn)).ToString();
        }

        private static string FormatNumber(double value)
        {
            return Math.Abs(value % 1) < 0.001 ? value.ToString("0") : value.ToString("0.##");
        }

        private static Font BuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static Image CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static Text CreateText(Transform parent, string content, int size, FontStyle style, TextAnchor alignment)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = BuiltinFont();
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height, Vector2 anchor)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float padding = 0)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }

        private sealed class ReportGoal
        {
            public readonly string Name;
            public readonly double Expected;
            public readonly int TargetRow;
            public readonly int TargetColumn;

            public ReportGoal(string name, double expected, int targetRow, int targetColumn)
            {
                Name = name;
                Expected = expected;
                TargetRow = targetRow;
                TargetColumn = targetColumn;
            }
        }
    }

    public sealed class ExcelHellCellView : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
    {
        private ExcelHellPrototype prototype;
        private int row;
        private int column;
        private Image background;
        private Text label;

        public void Initialize(ExcelHellPrototype owner, int cellRow, int cellColumn, Image image, Text text)
        {
            prototype = owner;
            row = cellRow;
            column = cellColumn;
            background = image;
            label = text;
        }

        public void Refresh(CellModel model, bool selected, bool intentTarget)
        {
            if (model.State == CellState.Destroyed)
            {
                background.color = new Color(0.16f, 0.17f, 0.18f, 1f);
                label.color = new Color(0.74f, 0.76f, 0.78f, 1f);
                label.text = "×";
                return;
            }

            if (model.State == CellState.Corrupted)
            {
                background.color = new Color(0.76f, 0.22f, 0.22f, 1f);
                label.color = Color.white;
                label.text = "#REF!";
                return;
            }

            if (selected)
                background.color = new Color(0.65f, 0.84f, 1f, 1f);
            else if (intentTarget)
                background.color = new Color(1f, 0.73f, 0.34f, 1f);
            else
                background.color = Color.white;

            label.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            label.text = model.Number.HasValue
                ? (Math.Abs(model.Number.Value % 1) < 0.001 ? model.Number.Value.ToString("0") : model.Number.Value.ToString("0.##"))
                : model.Text ?? string.Empty;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                prototype.BeginSelection(row, column);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            prototype.HoverSelection(row, column);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
                prototype.EndSelection(row, column);
        }
    }
}
