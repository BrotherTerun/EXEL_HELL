using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    public sealed class ExcelHellPrototype : MonoBehaviour
    {
        public const int Rows = 10;
        public const int Columns = 10;
        private const int MaxTurns = 15;
        private const int AnomalyActivationTurn = 3;

        private readonly CellModel[,] cells = new CellModel[Rows, Columns];
        private readonly ExcelHellCellView[,] views = new ExcelHellCellView[Rows, Columns];
        private readonly List<CellModel> selection = new();
        private readonly List<ReportGoal> goals = new();
        private readonly List<(Text Text, string StringId)> localizedLabels = new();
        private readonly PrototypeLocalization loc = new();

        private WorksheetSchema schema;
        private bool selecting;
        private int selectionStartRow;
        private int selectionStartColumn;
        private bool awaitingSumTarget;
        private List<CellModel> pendingSumSources;
        private double pendingSum;
        private ContentToken clipboard;
        private int turn;
        private bool finished;
        private int aggregateCounter;
        private AnomalyIntent? currentIntent;

        private Text titleText;
        private Text headingText;
        private Text turnText;
        private Text clipboardTextUi;
        private Text intentText;
        private Text statusText;
        private Text goalsText;
        private Text helpText;
        private Text languageButtonText;

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
            SetStatus("ui.select");
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
                return;

            var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystemObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void BuildModel()
        {
            schema = new WorksheetSchema(
                new[] { "ivanov", "petrov", "sidorov", "volkova", "kim" },
                new[] { "hours", "salary", "overtime", "bonus" });

            for (var row = 0; row < Rows; row++)
            for (var column = 0; column < Columns; column++)
            {
                cells[row, column] = new CellModel
                {
                    Row = row,
                    Column = column,
                    State = CellState.Normal
                };
            }

            // Field keys define vertical projections.
            Place(0, 1, ContentToken.FieldKey("hours"));
            Place(0, 2, ContentToken.FieldKey("salary"));
            Place(0, 3, ContentToken.FieldKey("overtime"));
            Place(0, 4, ContentToken.FieldKey("bonus"));

            // Record keys define horizontal projections. Record SORT spills right.
            Place(1, 0, ContentToken.RecordKey("ivanov"));
            Place(2, 0, ContentToken.RecordKey("petrov"));
            Place(3, 0, ContentToken.RecordKey("sidorov"));
            Place(4, 0, ContentToken.RecordKey("volkova"));
            Place(5, 0, ContentToken.RecordKey("kim"));

            // Report area.
            Place(0, 7, ContentToken.Label("report.label", "label.report"));
            goals.Add(new ReportGoal("goal.salary", 247d, 1, 7));
            goals.Add(new ReportGoal("goal.overtime", 14d, 2, 7));
            goals.Add(new ReportGoal("goal.bonus", 20d, 3, 7));

            // Semantic data is deliberately detached from its visual row/column.
            // The 20 tokens occupy F7:J10 and keep recordId/fieldId internally.
            var values = new Dictionary<string, double[]>
            {
                ["hours"] = new[] { 40d, 40d, 32d, 44d, 36d },
                ["salary"] = new[] { 50d, 52d, 41d, 49d, 55d },
                ["overtime"] = new[] { 3d, 0d, 5d, 2d, 4d },
                ["bonus"] = new[] { 5d, 3d, 2d, 4d, 6d }
            };

            var tokens = new List<ContentToken>();
            foreach (var field in schema.Fields)
            for (var recordIndex = 0; recordIndex < schema.Records.Count; recordIndex++)
            {
                var record = schema.Records[recordIndex];
                tokens.Add(ContentToken.Data(
                    $"data.{record}.{field}",
                    record,
                    field,
                    values[field][recordIndex],
                    field != "hours"));
            }

            // Fixed scramble: deterministic for tests, visually unstructured.
            var scramble = new[] { 7, 13, 0, 18, 5, 16, 2, 11, 9, 1, 19, 4, 14, 6, 17, 3, 12, 8, 15, 10 };
            var index = 0;
            for (var row = 6; row < 10; row++)
            for (var column = 5; column < 10; column++)
                Place(row, column, tokens[scramble[index++]]);
        }

        private void Place(int row, int column, ContentToken token)
        {
            cells[row, column].Occupant = token;
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

            titleText = CreateText(background.transform, string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleLeft);
            SetRect(titleText.rectTransform, 24, -16, 700, 48, new Vector2(0, 1));

            turnText = CreateText(background.transform, string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleRight);
            SetRect(turnText.rectTransform, 1180, -18, 380, 44, new Vector2(0, 1));

            BuildGrid(background.transform);
            BuildSidebar(background.transform);
        }

        private void BuildGrid(Transform parent)
        {
            var gridRoot = new GameObject("Spreadsheet", typeof(RectTransform), typeof(GridLayoutGroup));
            gridRoot.transform.SetParent(parent, false);
            SetRect(gridRoot.GetComponent<RectTransform>(), 24, -82, 836, 572, new Vector2(0, 1));

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
                    views[row, column] = CreateDataCell(gridRoot.transform, row, column);
            }
        }

        private void BuildSidebar(Transform parent)
        {
            var side = CreatePanel(parent, "Sidebar", new Color(0.985f, 0.985f, 0.985f, 1f));
            SetRect(side.rectTransform, 900, -82, 660, 760, new Vector2(0, 1));

            headingText = CreateText(side.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(headingText.rectTransform, 20, -18, 500, 38, new Vector2(0, 1));

            CreateLocalizedButton(side.transform, "ui.language", 530, -12, ToggleLanguage, 100, out languageButtonText);

            goalsText = CreateText(side.transform, string.Empty, 19, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(goalsText.rectTransform, 20, -62, 620, 130, new Vector2(0, 1));

            intentText = CreateText(side.transform, string.Empty, 19, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(intentText.rectTransform, 20, -196, 620, 58, new Vector2(0, 1));

            clipboardTextUi = CreateText(side.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(clipboardTextUi.rectTransform, 20, -260, 620, 44, new Vector2(0, 1));

            statusText = CreateText(side.transform, string.Empty, 17, FontStyle.Italic, TextAnchor.UpperLeft);
            SetRect(statusText.rectTransform, 20, -312, 620, 86, new Vector2(0, 1));

            CreateLocalizedButton(side.transform, "ui.sum", 20, -420, OnSum, 112, out _);
            CreateLocalizedButton(side.transform, "ui.sort", 142, -420, OnSort, 112, out _);
            CreateLocalizedButton(side.transform, "ui.cut", 264, -420, OnCut, 112, out _);
            CreateLocalizedButton(side.transform, "ui.paste", 386, -420, OnPaste, 112, out _);
            CreateLocalizedButton(side.transform, "ui.delete", 508, -420, OnDelete, 112, out _);
            CreateLocalizedButton(side.transform, "ui.submit", 20, -492, OnSubmit, 300, out _);
            CreateLocalizedButton(side.transform, "ui.reset", 340, -492, ResetPrototype, 160, out _);

            helpText = CreateText(side.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(helpText.rectTransform, 20, -570, 620, 175, new Vector2(0, 1));
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

            var label = CreateText(cellObject.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 3);
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

        private void CreateLocalizedButton(Transform parent, string stringId, float x, float y, Action callback, float width, out Text label)
        {
            var buttonObject = new GameObject(stringId, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            SetRect(buttonObject.GetComponent<RectTransform>(), x, y, width, 52, new Vector2(0, 1));

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.84f, 0.87f, 0.91f, 1f);
            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => callback());

            label = CreateText(buttonObject.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 3);
            label.raycastTarget = false;
            localizedLabels.Add((label, stringId));
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

        public void EndSelection()
        {
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

        private void OnSort()
        {
            if (!CanAct())
                return;

            if (selection.Count != 1 || selection[0].Occupant == null ||
                (selection[0].Occupant.Kind != ContentKind.RecordKey && selection[0].Occupant.Kind != ContentKind.FieldKey))
            {
                SetStatus("ui.sortNeedKey");
                return;
            }

            var keyCell = selection[0];
            if (!TryBuildSortPlan(keyCell, out var plan))
            {
                SetStatus("ui.spill");
                return;
            }

            ExecuteSort(plan);
            var keyName = DisplayToken(plan.KeyCell.Occupant, false);
            var fallback = plan.UsesFallbackDirection ? loc.Get("ui.sortFallback") : string.Empty;
            selection.Clear();
            CompletePlayerAction(loc.Format("ui.sortDone", plan.Tokens.Count, keyName, fallback));
        }

        private bool TryBuildSortPlan(CellModel keyCell, out SortPlan plan)
        {
            plan = null;
            var key = keyCell.Occupant;
            List<ContentToken> tokens;
            (int dr, int dc) primary;
            (int dr, int dc) fallback;

            if (key.Kind == ContentKind.RecordKey)
            {
                tokens = AllDataTokens()
                    .Where(t => t.RecordId == key.RecordId)
                    .OrderBy(t => schema.FieldOrder(t.FieldId))
                    .ToList();
                primary = (0, 1);   // right
                fallback = (0, -1); // left
            }
            else
            {
                tokens = AllDataTokens()
                    .Where(t => t.FieldId == key.FieldId)
                    .OrderBy(t => schema.RecordOrder(t.RecordId))
                    .ToList();
                primary = (1, 0);   // down
                fallback = (-1, 0); // up
            }

            if (tokens.Count == 0)
                return false;

            if (TryDestinations(keyCell, tokens, primary.dr, primary.dc, out var destinations))
            {
                plan = new SortPlan(keyCell, tokens, destinations, false);
                return true;
            }

            if (TryDestinations(keyCell, tokens, fallback.dr, fallback.dc, out destinations))
            {
                plan = new SortPlan(keyCell, tokens, destinations, true);
                return true;
            }

            return false;
        }

        private bool TryDestinations(CellModel keyCell, List<ContentToken> movingTokens, int dr, int dc, out List<CellModel> destinations)
        {
            destinations = new List<CellModel>();
            var movingIds = movingTokens.Select(t => t.Id).ToHashSet();

            for (var i = 1; i <= movingTokens.Count; i++)
            {
                var row = keyCell.Row + dr * i;
                var column = keyCell.Column + dc * i;
                if (row < 0 || row >= Rows || column < 0 || column >= Columns)
                    return false;

                var cell = cells[row, column];
                if (cell.State != CellState.Normal)
                    return false;
                if (cell.Occupant != null && !movingIds.Contains(cell.Occupant.Id))
                    return false;

                destinations.Add(cell);
            }

            return true;
        }

        private void ExecuteSort(SortPlan plan)
        {
            var movingIds = plan.Tokens.Select(t => t.Id).ToHashSet();
            foreach (var cell in cells)
            {
                if (cell.Occupant != null && movingIds.Contains(cell.Occupant.Id))
                    cell.Occupant = null;
            }

            for (var i = 0; i < plan.Tokens.Count; i++)
            {
                var token = plan.Tokens[i];
                if (plan.KeyCell.Occupant.Kind == ContentKind.RecordKey)
                {
                    token.ContextHint = ContextHintKind.Field;
                    token.ContextId = token.FieldId;
                }
                else
                {
                    token.ContextHint = ContextHintKind.Record;
                    token.ContextId = token.RecordId;
                }
                plan.Destinations[i].Occupant = token;
            }
        }

        private IEnumerable<ContentToken> AllDataTokens()
        {
            foreach (var cell in cells)
            {
                if (cell.Occupant != null && cell.Occupant.Kind == ContentKind.Data)
                    yield return cell.Occupant;
            }
            if (clipboard != null && clipboard.Kind == ContentKind.Data)
                yield return clipboard;
        }

        private void OnSum()
        {
            if (!CanAct())
                return;

            if (selection.Count == 0)
            {
                SetStatus("ui.sumNeedRange");
                return;
            }

            if (selection.Any(cell => cell.State != CellState.Normal || cell.Occupant == null || !cell.Occupant.IsNumeric ||
                                      (cell.Occupant.Kind != ContentKind.Data && cell.Occupant.Kind != ContentKind.Aggregate)))
            {
                SetStatus("ui.sumInvalid");
                return;
            }

            pendingSumSources = selection.ToList();
            pendingSum = pendingSumSources.Sum(cell => cell.Occupant.Number.Value);
            awaitingSumTarget = true;
            SetStatus(loc.Format("ui.sumTarget", FormatNumber(pendingSum)));
        }

        private void CommitSum(int row, int column)
        {
            var target = cells[row, column];
            if (target.State != CellState.Normal || target.Occupant != null || pendingSumSources.Contains(target))
            {
                SetStatus("ui.sumBadTarget");
                return;
            }

            var count = pendingSumSources.Count;
            foreach (var source in pendingSumSources)
                source.Occupant = null;

            target.Occupant = ContentToken.Aggregate($"aggregate.{++aggregateCounter}", pendingSum);
            awaitingSumTarget = false;
            pendingSumSources = null;
            selection.Clear();
            CompletePlayerAction(loc.Format("ui.sumDone", count, target.Address));
        }

        private void OnCut()
        {
            if (!CanAct() || selection.Count != 1)
            {
                if (CanAct()) SetStatus("ui.cutNeed");
                return;
            }

            var cell = selection[0];
            if (cell.State != CellState.Normal || cell.Occupant == null)
            {
                SetStatus("ui.cutNeed");
                return;
            }

            clipboard = cell.Occupant;
            cell.Occupant = null;
            selection.Clear();
            CompletePlayerAction(loc.Format("ui.cutDone", cell.Address));
        }

        private void OnPaste()
        {
            if (!CanAct())
                return;
            if (clipboard == null)
            {
                SetStatus("ui.pasteEmpty");
                return;
            }
            if (selection.Count != 1 || selection[0].State != CellState.Normal || selection[0].Occupant != null)
            {
                SetStatus("ui.pasteTarget");
                return;
            }

            var cell = selection[0];
            cell.Occupant = clipboard;
            clipboard = null;
            selection.Clear();
            CompletePlayerAction(loc.Format("ui.pasteDone", cell.Address));
        }

        private void OnDelete()
        {
            if (!CanAct())
                return;
            if (selection.Count != 1)
            {
                SetStatus("ui.deleteNeed");
                return;
            }

            var cell = selection[0];
            cell.Occupant = null;
            cell.State = CellState.Destroyed;
            cell.CorruptionAge = 0;
            selection.Clear();
            CompletePlayerAction(loc.Format("ui.deleteDone", cell.Address));
        }

        private bool CanAct()
        {
            if (finished)
            {
                SetStatus("ui.finished");
                return false;
            }
            if (awaitingSumTarget)
            {
                SetStatus("ui.finishSum");
                return false;
            }
            return true;
        }

        private void CompletePlayerAction(string localizedMessage)
        {
            turn++;
            ResolveAnomaly();

            if (!finished && turn >= MaxTurns)
            {
                finished = true;
                statusText.text = loc.Get("ui.deadline");
            }
            else if (!finished)
            {
                statusText.text = localizedMessage;
            }

            RefreshAll();
        }

        private void ResolveAnomaly()
        {
            if (turn < AnomalyActivationTurn)
                return;

            if (!cells.Cast<CellModel>().Any(cell => cell.State == CellState.Corrupted))
            {
                var spawn = cells[9, 0]; // A10: empty edge, then paths toward semantic data.
                if (spawn.State == CellState.Normal)
                {
                    spawn.State = CellState.Corrupted;
                    spawn.CorruptionAge = 0;
                }
                GenerateIntent();
                return;
            }

            AnomalyIntent? executedIntent = null;
            if (currentIntent.HasValue && IsIntentValid(currentIntent.Value))
            {
                executedIntent = currentIntent;
                var intent = currentIntent.Value;
                var target = cells[intent.TargetRow, intent.TargetColumn];
                target.State = CellState.Corrupted;
                target.CorruptionAge = 0;
            }

            foreach (var cell in cells)
            {
                if (cell.State != CellState.Corrupted)
                    continue;
                if (executedIntent.HasValue && cell.Row == executedIntent.Value.TargetRow && cell.Column == executedIntent.Value.TargetColumn)
                    continue;

                cell.CorruptionAge++;
                if (cell.CorruptionAge >= 2)
                {
                    cell.State = CellState.Destroyed;
                    cell.Occupant = null;
                }
            }

            GenerateIntent();
        }

        private bool IsIntentValid(AnomalyIntent intent)
        {
            return cells[intent.SourceRow, intent.SourceColumn].State == CellState.Corrupted &&
                   cells[intent.TargetRow, intent.TargetColumn].State == CellState.Normal;
        }

        private void GenerateIntent()
        {
            currentIntent = null;
            var corrupted = cells.Cast<CellModel>()
                .Where(cell => cell.State == CellState.Corrupted)
                .OrderByDescending(cell => cell.CorruptionAge)
                .ThenBy(cell => cell.Row)
                .ThenBy(cell => cell.Column);

            foreach (var source in corrupted)
            {
                var candidates = Neighbours(source)
                    .Where(cell => cell.State == CellState.Normal)
                    .OrderByDescending(cell => cell.Occupant?.IsRequiredSource == true)
                    .ThenBy(DistanceToNearestRequiredToken)
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
            foreach (var (dr, dc) in offsets)
            {
                var row = cell.Row + dr;
                var column = cell.Column + dc;
                if (row >= 0 && row < Rows && column >= 0 && column < Columns)
                    yield return cells[row, column];
            }
        }

        private int DistanceToNearestRequiredToken(CellModel candidate)
        {
            var required = cells.Cast<CellModel>()
                .Where(cell => cell.State != CellState.Destroyed && cell.Occupant?.IsRequiredSource == true)
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
                var token = cells[goal.TargetRow, goal.TargetColumn].Occupant;
                if (token?.Number == null || Math.Abs(token.Number.Value - goal.Expected) > 0.001)
                    wrong.Add(loc.Get(goal.NameStringId));
            }

            if (wrong.Count == 0)
            {
                finished = true;
                statusText.text = loc.Format("ui.accepted", turn, MaxTurns);
            }
            else
            {
                statusText.text = loc.Format("ui.rejected", string.Join(", ", wrong));
            }
            RefreshAll();
        }

        private void ToggleLanguage()
        {
            loc.ToggleLanguage();
            RefreshAll();
        }

        private void ResetPrototype()
        {
            Destroy(gameObject);
            var replacement = new GameObject("EXEL HELL Prototype");
            replacement.AddComponent<ExcelHellPrototype>();
        }

        private void RefreshAll()
        {
            for (var row = 0; row < Rows; row++)
            for (var column = 0; column < Columns; column++)
                views[row, column]?.Refresh(cells[row, column], selection.Contains(cells[row, column]), IsIntentTarget(row, column), DisplayToken);

            titleText.text = loc.Get("ui.title");
            headingText.text = loc.Get("ui.reportTask");
            helpText.text = loc.Get("ui.help");
            foreach (var (text, stringId) in localizedLabels)
                text.text = loc.Get(stringId);

            turnText.text = loc.Format("ui.turn", turn, MaxTurns);
            var clipboardValue = clipboard == null ? loc.Get("ui.empty") : DisplayToken(clipboard, true);
            clipboardTextUi.text = loc.Format("ui.clipboard", clipboardValue);

            if (turn < AnomalyActivationTurn)
                intentText.text = loc.Format("ui.refDormant", AnomalyActivationTurn);
            else if (currentIntent.HasValue)
                intentText.text = loc.Format("ui.refNext", cells[currentIntent.Value.TargetRow, currentIntent.Value.TargetColumn].Address);
            else
                intentText.text = loc.Get("ui.refNoPath");

            goalsText.text = string.Join("\n", goals.Select(goal =>
            {
                var target = cells[goal.TargetRow, goal.TargetColumn];
                var current = target.Occupant?.Number.HasValue == true ? FormatNumber(target.Occupant.Number.Value) : loc.Get("ui.empty");
                return loc.Format("ui.goal", loc.Get(goal.NameStringId), current, target.Address);
            }));
        }

        private bool IsIntentTarget(int row, int column)
        {
            return currentIntent.HasValue && currentIntent.Value.TargetRow == row && currentIntent.Value.TargetColumn == column;
        }

        private string DisplayToken(ContentToken token, bool compact)
        {
            if (token == null)
                return string.Empty;

            if (token.Kind == ContentKind.RecordKey || token.Kind == ContentKind.FieldKey || token.Kind == ContentKind.Label)
                return loc.Get(token.StringId);

            var value = token.Number.HasValue ? FormatNumber(token.Number.Value) : string.Empty;
            if (compact || token.ContextHint == ContextHintKind.None)
                return value;

            var context = token.ContextHint == ContextHintKind.Record
                ? loc.Get($"record.{token.ContextId}")
                : loc.Get($"field.{token.ContextId}");
            return value + "\n" + context;
        }

        private void SetStatus(string stringId)
        {
            if (statusText != null)
                statusText.text = loc.Get(stringId);
        }

        public static string ColumnName(int zeroBasedColumn) => ((char)('A' + zeroBasedColumn)).ToString();

        private static string FormatNumber(double value) => Math.Abs(value % 1) < 0.001 ? value.ToString("0") : value.ToString("0.##");

        private static Font BuiltinFont() => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

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

        public void Refresh(CellModel model, bool selected, bool intentTarget, Func<ContentToken, bool, string> displayToken)
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

            background.color = selected
                ? new Color(0.65f, 0.84f, 1f, 1f)
                : intentTarget
                    ? new Color(1f, 0.73f, 0.34f, 1f)
                    : model.Occupant?.Kind == ContentKind.RecordKey || model.Occupant?.Kind == ContentKind.FieldKey
                        ? new Color(0.91f, 0.94f, 0.98f, 1f)
                        : Color.white;

            label.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            label.fontStyle = model.Occupant?.Kind == ContentKind.RecordKey || model.Occupant?.Kind == ContentKind.FieldKey
                ? FontStyle.Bold
                : FontStyle.Normal;
            label.text = displayToken(model.Occupant, false);
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
                prototype.EndSelection();
        }
    }
}
