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
        private readonly List<CellModel> selection = new();
        private readonly List<ReportGoal> goals = new();
        private readonly List<(Text Text, string StringId)> localizedLabels = new();
        private readonly PrototypeLocalization loc = new();
        private readonly HashSet<string> requiredForPlay = new();
        private readonly HashSet<(int Row, int Column)> reservedCells = new();

        private ExcelHellPrototypeConfig config;
        private WorksheetSchema schema;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;
        private int rows;
        private int columns;
        private int reportColumn;

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
        private SpawnIntent? pendingSpawnIntent;
        private int spawnSequence;

        private Text titleText;
        private Text headingText;
        private Text turnText;
        private Text clipboardTextUi;
        private Text intentText;
        private Text statusText;
        private Text goalsText;
        private Text helpText;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<ExcelHellPrototype>() != null)
                return;
            new GameObject("EXEL HELL Prototype").AddComponent<ExcelHellPrototype>();
        }

        private void Awake()
        {
            config = Resources.Load<ExcelHellPrototypeConfig>("ExcelHellPrototypeConfig");
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<ExcelHellPrototypeConfig>();
                Debug.LogWarning("EXEL HELL: Resources/ExcelHellPrototypeConfig.asset not found. Runtime defaults are used.");
            }

            rows = config.SafeRows;
            columns = config.SafeColumns;
            reportColumn = columns - 1;
            cells = new CellModel[rows, columns];
            views = new ExcelHellCellView[rows, columns];

            EnsureEventSystem();
            BuildModel();
            InitializeAnomaly();
            BuildUi();
            Canvas.ForceUpdateCanvases();
            RefreshAll();
            Canvas.ForceUpdateCanvases();
            SetStatus(goals.Count == 0 ? "ui.noGoals" : "ui.select");
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            go.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        private void BuildModel()
        {
            schema = new WorksheetSchema(
                new[] { "ivanov", "petrov", "sidorov", "volkova", "kim" },
                new[] { "hours", "salary", "overtime", "bonus" });

            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                cells[row, column] = new CellModel { Row = row, Column = column, State = CellState.Normal };

            Place(0, 1, ContentToken.FieldKey("hours"));
            Place(0, 2, ContentToken.FieldKey("salary"));
            Place(0, 3, ContentToken.FieldKey("overtime"));
            Place(0, 4, ContentToken.FieldKey("bonus"));

            Place(1, 0, ContentToken.RecordKey("ivanov"));
            Place(2, 0, ContentToken.RecordKey("petrov"));
            Place(3, 0, ContentToken.RecordKey("sidorov"));
            Place(4, 0, ContentToken.RecordKey("volkova"));
            Place(5, 0, ContentToken.RecordKey("kim"));

            Place(0, reportColumn, ContentToken.Label("report.label", "label.report"));
            BuildReportGoals();

            // MVP 0.4: fresh values to remove memorized answers from repeated MVP 0.3 playtests.
            var values = new Dictionary<string, double[]>
            {
                ["hours"] = new[] { 38d, 42d, 35d, 47d, 39d },
                ["salary"] = new[] { 63d, 54d, 71d, 58d, 66d },
                ["overtime"] = new[] { 1d, 4d, 6d, 3d, 2d },
                ["bonus"] = new[] { 7d, 2d, 5d, 9d, 4d }
            };

            var tokens = new List<ContentToken>();
            foreach (var field in schema.Fields)
            for (var recordIndex = 0; recordIndex < schema.Records.Count; recordIndex++)
            {
                var record = schema.Records[recordIndex];
                var id = DataId(record, field);
                tokens.Add(ContentToken.Data(id, record, field, values[field][recordIndex], requiredForPlay.Contains(id)));
            }

            var scramble = new[] { 7, 13, 0, 18, 5, 16, 2, 11, 9, 1, 19, 4, 14, 6, 17, 3, 12, 8, 15, 10 };
            var free = new List<CellModel>();
            for (var row = rows - 1; row >= 0; row--)
            for (var column = columns - 1; column >= 0; column--)
            {
                var cell = cells[row, column];
                if (cell.Occupant == null && !reservedCells.Contains((row, column))) free.Add(cell);
            }

            if (free.Count < tokens.Count)
                throw new InvalidOperationException($"Board {rows}x{columns} has no room for the prototype dataset and selected report goals.");

            for (var i = 0; i < tokens.Count; i++) free[i].Occupant = tokens[scramble[i]];
        }

        private void BuildReportGoals()
        {
            var targetRow = 1;

            void AddGoal(PrototypeReportGoals flag, string stringId, double expected,
                IEnumerable<string> semanticSources, string directToken = null, IEnumerable<string> threatSources = null)
            {
                if (!config.HasGoal(flag)) return;
                if (targetRow >= rows) throw new InvalidOperationException("Too many report goals for the current board height.");

                var sources = semanticSources?.ToArray() ?? Array.Empty<string>();
                goals.Add(new ReportGoal(stringId, expected, targetRow, reportColumn, sources, directToken));
                reservedCells.Add((targetRow, reportColumn));
                targetRow++;

                foreach (var id in sources) requiredForPlay.Add(id);
                if (!string.IsNullOrEmpty(directToken)) requiredForPlay.Add(directToken);
                if (threatSources != null)
                    foreach (var id in threatSources) requiredForPlay.Add(id);
            }

            var salaryAll = IdsForField("salary");
            var overtimeAll = IdsForField("overtime");
            var bonusAll = IdsForField("bonus");
            var hoursAll = IdsForField("hours");

            AddGoal(PrototypeReportGoals.SalaryTotal, "goal.salary", 312d, salaryAll);
            AddGoal(PrototypeReportGoals.OvertimeTotal, "goal.overtime", 16d, overtimeAll);
            AddGoal(PrototypeReportGoals.BonusTotal, "goal.bonus", 27d, bonusAll);

            var bonusAtLeastFive = new[]
            {
                DataId("ivanov", "bonus"), DataId("sidorov", "bonus"), DataId("volkova", "bonus")
            };
            AddGoal(PrototypeReportGoals.BonusAtLeastFour, "goal.bonus5", 21d,
                bonusAtLeastFive, threatSources: bonusAll);

            AddGoal(PrototypeReportGoals.SalaryOfMaxOvertime, "goal.maxOvertimeSalary", 71d,
                new[] { DataId("sidorov", "salary") }, DataId("sidorov", "salary"),
                overtimeAll.Concat(new[] { DataId("sidorov", "salary") }));

            var lowHoursSalaries = new[]
            {
                DataId("ivanov", "salary"), DataId("sidorov", "salary"), DataId("kim", "salary")
            };
            AddGoal(PrototypeReportGoals.SalaryForHoursBelowForty, "goal.lowHoursSalary", 200d,
                lowHoursSalaries, threatSources: hoursAll.Concat(lowHoursSalaries));
        }

        private string[] IdsForField(string fieldId) => schema.Records.Select(record => DataId(record, fieldId)).ToArray();
        private static string DataId(string recordId, string fieldId) => $"data.{recordId}.{fieldId}";

        private void Place(int row, int column, ContentToken token)
        {
            if (row >= 0 && row < rows && column >= 0 && column < columns) cells[row, column].Occupant = token;
        }

        private void InitializeAnomaly()
        {
            currentIntent = null;
            pendingSpawnIntent = null;
            spawnSequence = 0;
            ScheduleNextOutbreak(config.SafeActivationTurn == 0 ? 1 : config.SafeActivationTurn);
        }

        private bool ScheduleNextOutbreak(int delay)
        {
            if (pendingSpawnIntent.HasValue) return false;
            if (!TryChooseDynamicSpawnCell(out var spawn)) return false;
            pendingSpawnIntent = new SpawnIntent(spawn.Row, spawn.Column, Mathf.Max(1, delay));
            return true;
        }

        private bool TryChooseDynamicSpawnCell(out CellModel result)
        {
            result = null;

            // Only live report-critical data are primary anchors. Empty report targets do not bias spawn.
            var anchors = cells.Cast<CellModel>()
                .Where(cell => cell.State != CellState.Destroyed && cell.Occupant?.IsRequiredSource == true)
                .ToList();

            if (anchors.Count == 0)
                anchors = cells.Cast<CellModel>()
                    .Where(cell => cell.State != CellState.Destroyed && cell.Occupant != null)
                    .ToList();
            if (anchors.Count == 0) return false;

            var preferred = config.SafeSpawnPreferredDistance;
            var variation = config.SafeSpawnDistanceVariation;
            var minDistance = Mathf.Max(1, preferred - variation);
            var maxDistance = preferred + variation;

            var scored = cells.Cast<CellModel>()
                .Where(cell => cell.State == CellState.Normal)
                .Where(cell => !IsReportTarget(cell.Row, cell.Column))
                .Where(cell => cell.Occupant?.IsRequiredSource != true)
                .Select(cell => new
                {
                    Cell = cell,
                    Distance = anchors.Min(anchor => Mathf.Abs(anchor.Row - cell.Row) + Mathf.Abs(anchor.Column - cell.Column))
                })
                .Where(x => x.Distance > 0)
                .OrderBy(x => x.Distance < minDistance ? minDistance - x.Distance : x.Distance > maxDistance ? x.Distance - maxDistance : 0)
                .ThenBy(x => Mathf.Abs(x.Distance - preferred))
                .ThenBy(x => x.Cell.Occupant == null ? 0 : 1)
                .ThenBy(x => SpawnTieBreak(x.Cell))
                .ToList();

            if (scored.Count == 0) return false;
            var poolSize = Mathf.Min(config.SafeSpawnCandidatePoolSize, scored.Count);
            var pool = scored.Take(poolSize).ToList();
            var chosen = pool[spawnSequence % poolSize];
            result = chosen.Cell;

            if (config.showSpawnDebug)
            {
                var anchorText = string.Join(", ", anchors.Select(a => $"{a.Address}:{a.Occupant?.Id}"));
                var poolText = string.Join(", ", pool.Select(p => $"{p.Cell.Address}:d{p.Distance}"));
                Debug.Log($"EXEL HELL #REF! SPAWN goals={(int)config.reportGoals} seq={spawnSequence} chosen={result.Address} d={chosen.Distance} anchors=[{anchorText}] pool=[{poolText}]");
            }

            spawnSequence++;
            return true;
        }

        private int SpawnTieBreak(CellModel cell)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + (int)config.reportGoals;
                hash = hash * 31 + spawnSequence;
                hash = hash * 31 + cell.Row;
                hash = hash * 31 + cell.Column;
                hash ^= hash << 13;
                hash ^= hash >> 17;
                hash ^= hash << 5;
                return hash & int.MaxValue;
            }
        }

        private bool TrySpawnPendingOutbreak()
        {
            if (!pendingSpawnIntent.HasValue) return false;
            var intent = pendingSpawnIntent.Value;
            var spawn = cells[intent.Row, intent.Column];

            if (spawn.State != CellState.Normal || spawn.Occupant?.IsRequiredSource == true || IsReportTarget(spawn.Row, spawn.Column))
            {
                pendingSpawnIntent = null;
                ScheduleNextOutbreak(1);
                return false;
            }

            pendingSpawnIntent = null;
            spawn.State = CellState.Corrupted;
            spawn.CorruptionAge = 0;
            return true;
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject("Prototype Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

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
            const float maxWidth = 836f;
            const float maxHeight = 650f;
            var cellWidth = Mathf.Min(86f, maxWidth / (columns + 1));
            var cellHeight = Mathf.Min(58f, maxHeight / (rows + 1));

            var gridRoot = new GameObject("Spreadsheet", typeof(RectTransform), typeof(GridLayoutGroup));
            gridRoot.transform.SetParent(parent, false);
            SetRect(gridRoot.GetComponent<RectTransform>(), 24, -82, cellWidth * (columns + 1), cellHeight * (rows + 1), new Vector2(0, 1));

            var layout = gridRoot.GetComponent<GridLayoutGroup>();
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = columns + 1;
            layout.cellSize = new Vector2(cellWidth, cellHeight);
            layout.spacing = Vector2.zero;

            CreateHeaderCell(gridRoot.transform, string.Empty);
            for (var column = 0; column < columns; column++) CreateHeaderCell(gridRoot.transform, ColumnName(column));
            for (var row = 0; row < rows; row++)
            {
                CreateHeaderCell(gridRoot.transform, (row + 1).ToString());
                for (var column = 0; column < columns; column++) views[row, column] = CreateDataCell(gridRoot.transform, row, column);
            }
        }

        private void BuildSidebar(Transform parent)
        {
            var side = CreatePanel(parent, "Sidebar", new Color(0.985f, 0.985f, 0.985f, 1f));
            SetRect(side.rectTransform, 900, -82, 660, 760, new Vector2(0, 1));

            headingText = CreateText(side.transform, string.Empty, 24, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(headingText.rectTransform, 20, -18, 500, 38, new Vector2(0, 1));
            CreateLocalizedButton(side.transform, "ui.language", 530, -12, ToggleLanguage, 100, out _);

            goalsText = CreateText(side.transform, string.Empty, 17, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(goalsText.rectTransform, 20, -62, 620, 180, new Vector2(0, 1));
            intentText = CreateText(side.transform, string.Empty, 19, FontStyle.Bold, TextAnchor.UpperLeft);
            SetRect(intentText.rectTransform, 20, -245, 620, 58, new Vector2(0, 1));
            clipboardTextUi = CreateText(side.transform, string.Empty, 18, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(clipboardTextUi.rectTransform, 20, -305, 620, 44, new Vector2(0, 1));
            statusText = CreateText(side.transform, string.Empty, 16, FontStyle.Italic, TextAnchor.UpperLeft);
            SetRect(statusText.rectTransform, 20, -350, 620, 68, new Vector2(0, 1));

            CreateLocalizedButton(side.transform, "ui.sum", 20, -430, OnSum, 112, out _);
            CreateLocalizedButton(side.transform, "ui.sort", 142, -430, OnSort, 112, out _);
            CreateLocalizedButton(side.transform, "ui.cut", 264, -430, OnCut, 112, out _);
            CreateLocalizedButton(side.transform, "ui.paste", 386, -430, OnPaste, 112, out _);
            CreateLocalizedButton(side.transform, "ui.delete", 508, -430, OnDelete, 112, out _);
            CreateLocalizedButton(side.transform, "ui.submit", 20, -502, OnSubmit, 300, out _);
            CreateLocalizedButton(side.transform, "ui.reset", 340, -502, ResetPrototype, 160, out _);

            helpText = CreateText(side.transform, string.Empty, 15, FontStyle.Normal, TextAnchor.UpperLeft);
            SetRect(helpText.rectTransform, 20, -575, 620, 170, new Vector2(0, 1));
        }

        private ExcelHellCellView CreateDataCell(Transform parent, int row, int column)
        {
            var go = new GameObject($"Cell {ColumnName(column)}{row + 1}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(ExcelHellCellView));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = Color.white;
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.77f, 0.79f, 0.82f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);

            var label = CreateText(go.transform, string.Empty, 14, FontStyle.Normal, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 3);
            label.raycastTarget = false;

            var view = go.GetComponent<ExcelHellCellView>();
            view.Initialize(this, row, column, image, label, outline);
            return view;
        }

        private void CreateHeaderCell(Transform parent, string text)
        {
            var go = new GameObject("Header", typeof(RectTransform), typeof(Image), typeof(Outline));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.88f, 0.89f, 0.91f, 1f);
            var outline = go.GetComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.74f, 0.78f, 1f);
            outline.effectDistance = new Vector2(1f, -1f);
            var label = CreateText(go.transform, text, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 3);
            label.raycastTarget = false;
        }

        private void CreateLocalizedButton(Transform parent, string stringId, float x, float y, Action callback, float width, out Text label)
        {
            var go = new GameObject(stringId, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), x, y, width, 52, new Vector2(0, 1));
            var image = go.GetComponent<Image>();
            image.color = new Color(0.84f, 0.87f, 0.91f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => callback());

            label = CreateText(go.transform, string.Empty, 15, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform, 3);
            label.raycastTarget = false;
            localizedLabels.Add((label, stringId));
        }

        public void BeginSelection(int row, int column)
        {
            if (finished) return;
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
            if (selecting) UpdateSelection(row, column);
        }

        public void EndSelection() => selecting = false;

        private void UpdateSelection(int endRow, int endColumn)
        {
            selection.Clear();
            var minRow = Mathf.Min(selectionStartRow, endRow);
            var maxRow = Mathf.Max(selectionStartRow, endRow);
            var minColumn = Mathf.Min(selectionStartColumn, endColumn);
            var maxColumn = Mathf.Max(selectionStartColumn, endColumn);
            for (var row = minRow; row <= maxRow; row++)
            for (var column = minColumn; column <= maxColumn; column++) selection.Add(cells[row, column]);
            RefreshAll();
        }

        private void OnSort()
        {
            if (!CanAct()) return;
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
            var keyName = DisplayToken(plan.KeyCell.Occupant, true);
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
                tokens = AllDataTokens().Where(t => t.RecordId == key.RecordId).OrderBy(t => schema.FieldOrder(t.FieldId)).ToList();
                primary = (0, 1);
                fallback = (0, -1);
            }
            else
            {
                tokens = AllDataTokens().Where(t => t.FieldId == key.FieldId).OrderBy(t => schema.RecordOrder(t.RecordId)).ToList();
                primary = (1, 0);
                fallback = (-1, 0);
            }

            if (tokens.Count == 0) return false;
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
                if (row < 0 || row >= rows || column < 0 || column >= columns) return false;
                var cell = cells[row, column];
                if (cell.State != CellState.Normal) return false;
                if (cell.Occupant != null && !movingIds.Contains(cell.Occupant.Id)) return false;
                destinations.Add(cell);
            }
            return true;
        }

        private void ExecuteSort(SortPlan plan)
        {
            var movingIds = plan.Tokens.Select(t => t.Id).ToHashSet();
            foreach (var cell in cells)
                if (cell.Occupant != null && movingIds.Contains(cell.Occupant.Id)) cell.Occupant = null;
            for (var i = 0; i < plan.Tokens.Count; i++) plan.Destinations[i].Occupant = plan.Tokens[i];
        }

        private IEnumerable<ContentToken> AllDataTokens()
        {
            foreach (var cell in cells)
                if (cell.State == CellState.Normal && cell.Occupant?.Kind == ContentKind.Data) yield return cell.Occupant;
        }

        private void OnSum()
        {
            if (!CanAct()) return;
            if (selection.Count < 2)
            {
                SetStatus("ui.sumNeedTwo");
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
            statusText.text = loc.Format("ui.sumTarget", FormatNumber(pendingSum));
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
            var provenance = pendingSumSources.SelectMany(source => source.Occupant.SourceTokenIds ?? new List<string>()).Distinct().ToArray();
            var required = pendingSumSources.Any(source => source.Occupant.IsRequiredSource);
            var reportTarget = IsReportTarget(row, column);

            // SUM is destructive in worksheet space, but writing a report answer is a non-consuming calculation.
            if (!reportTarget)
                foreach (var source in pendingSumSources) source.Occupant = null;

            target.Occupant = ContentToken.Aggregate($"aggregate.{++aggregateCounter}", pendingSum, provenance, required);
            awaitingSumTarget = false;
            pendingSumSources = null;
            selection.Clear();
            CompletePlayerAction(loc.Format(reportTarget ? "ui.sumReported" : "ui.sumDone", count, target.Address));
        }

        private void OnCut()
        {
            if (!CanAct()) return;
            if (selection.Count != 1 || selection[0].State != CellState.Normal || selection[0].Occupant == null)
            {
                SetStatus("ui.cutNeed");
                return;
            }
            var cell = selection[0];
            clipboard = cell.Occupant;
            cell.Occupant = null;
            selection.Clear();
            CompletePlayerAction(loc.Format("ui.cutDone", cell.Address));
        }

        private void OnPaste()
        {
            if (!CanAct()) return;
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
            if (!CanAct()) return;
            if (selection.Count != 1)
            {
                SetStatus("ui.deleteNeed");
                return;
            }
            var cell = selection[0];
            var quarantinedRef = cell.State == CellState.Corrupted;
            cell.Occupant = null;
            cell.State = CellState.Destroyed;
            cell.CorruptionAge = 0;
            selection.Clear();
            CompletePlayerAction(loc.Format(quarantinedRef ? "ui.quarantineDone" : "ui.deleteDone", cell.Address));
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
            if (!finished && turn >= config.SafeMaxTurns)
            {
                finished = true;
                statusText.text = loc.Get("ui.deadline");
            }
            else if (!finished) statusText.text = localizedMessage;
            RefreshAll();
        }

        private void ResolveAnomaly()
        {
            var hadActiveRefBeforeResolve = cells.Cast<CellModel>().Any(cell => cell.State == CellState.Corrupted);
            var spawnedThisResolve = false;

            if (pendingSpawnIntent.HasValue)
            {
                var pending = pendingSpawnIntent.Value;
                if (pending.TurnsRemaining <= 1) spawnedThisResolve = TrySpawnPendingOutbreak();
                else pendingSpawnIntent = new SpawnIntent(pending.Row, pending.Column, pending.TurnsRemaining - 1);
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
                if (cell.State != CellState.Corrupted) continue;
                if (spawnedThisResolve && cell.CorruptionAge == 0) continue;
                if (executedIntent.HasValue && cell.Row == executedIntent.Value.TargetRow && cell.Column == executedIntent.Value.TargetColumn) continue;
                cell.CorruptionAge++;
                if (cell.CorruptionAge >= config.SafeCorruptionLifetime)
                {
                    cell.State = CellState.Destroyed;
                    cell.Occupant = null;
                }
            }

            var hasActiveRef = cells.Cast<CellModel>().Any(cell => cell.State == CellState.Corrupted);
            if (hasActiveRef) GenerateIntent();
            else currentIntent = null;

            if (hadActiveRefBeforeResolve && !hasActiveRef)
            {
                pendingSpawnIntent = null;
                ScheduleNextOutbreak(config.SafeRespawnDelay);
            }
            else if (!pendingSpawnIntent.HasValue)
                ScheduleNextOutbreak(hasActiveRef ? config.SafeActiveOutbreakDelay : config.SafeRespawnDelay);
        }

        private bool IsIntentValid(AnomalyIntent intent) =>
            cells[intent.SourceRow, intent.SourceColumn].State == CellState.Corrupted &&
            cells[intent.TargetRow, intent.TargetColumn].State == CellState.Normal;

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
                if (candidates.Count == 0) continue;
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
                if (row >= 0 && row < rows && column >= 0 && column < columns) yield return cells[row, column];
            }
        }

        private int DistanceToNearestRequiredToken(CellModel candidate)
        {
            var required = cells.Cast<CellModel>()
                .Where(cell => cell.State != CellState.Destroyed && cell.Occupant?.IsRequiredSource == true)
                .ToList();
            if (required.Count == 0) return int.MaxValue;
            return required.Min(cell => Mathf.Abs(cell.Row - candidate.Row) + Mathf.Abs(cell.Column - candidate.Column));
        }

        private void OnSubmit()
        {
            if (finished) return;
            if (goals.Count == 0)
            {
                SetStatus("ui.noGoals");
                return;
            }

            var wrong = new List<string>();
            foreach (var goal in goals)
            {
                var target = cells[goal.TargetRow, goal.TargetColumn];
                if (target.State != CellState.Normal || !goal.IsSatisfiedBy(target.Occupant))
                    wrong.Add(loc.Get(goal.NameStringId));
            }

            if (wrong.Count == 0)
            {
                finished = true;
                statusText.text = loc.Format("ui.accepted", turn, config.SafeMaxTurns);
            }
            else statusText.text = loc.Format("ui.rejected", string.Join(", ", wrong));
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
            new GameObject("EXEL HELL Prototype").AddComponent<ExcelHellPrototype>();
        }

        private void RefreshAll()
        {
            for (var row = 0; row < rows; row++)
            for (var column = 0; column < columns; column++)
                views[row, column]?.Refresh(cells[row, column], selection.Contains(cells[row, column]), IsIntentTarget(row, column), IsReportTarget(row, column), DisplayToken);

            titleText.text = loc.Get("ui.title");
            headingText.text = loc.Get("ui.reportTask");
            helpText.text = loc.Get("ui.help");
            foreach (var (text, stringId) in localizedLabels) text.text = loc.Get(stringId);

            turnText.text = loc.Format("ui.turn", turn, config.SafeMaxTurns);
            var clipboardValue = clipboard == null ? loc.Get("ui.empty") : DisplayToken(clipboard, true);
            clipboardTextUi.text = loc.Format("ui.clipboard", clipboardValue);

            if (pendingSpawnIntent.HasValue)
            {
                var spawn = pendingSpawnIntent.Value;
                intentText.text = loc.Format("ui.refSpawn", cells[spawn.Row, spawn.Column].Address, spawn.TurnsRemaining);
            }
            else if (currentIntent.HasValue)
                intentText.text = loc.Format("ui.refNext", cells[currentIntent.Value.TargetRow, currentIntent.Value.TargetColumn].Address);
            else intentText.text = loc.Get("ui.refNoPath");

            if (goals.Count == 0) goalsText.text = loc.Get("ui.noGoals");
            else
            {
                goalsText.text = string.Join("\n", goals.Select(goal =>
                {
                    var target = cells[goal.TargetRow, goal.TargetColumn];
                    var current = target.State == CellState.Normal && target.Occupant?.Number.HasValue == true
                        ? FormatNumber(target.Occupant.Number.Value)
                        : loc.Get("ui.empty");
                    var expected = config.showExpectedAnswers ? FormatNumber(goal.Expected) : "?";
                    return loc.Format("ui.goal", loc.Get(goal.NameStringId), current, expected, target.Address);
                }));
            }
        }

        private bool IsReportTarget(int row, int column) => goals.Any(g => g.TargetRow == row && g.TargetColumn == column);

        private bool IsIntentTarget(int row, int column)
        {
            // While a future outbreak is telegraphed, display only that spawn warning.
            // The sidebar also prioritizes SpawnIntent, so this keeps the board and text in sync.
            if (pendingSpawnIntent.HasValue)
                return pendingSpawnIntent.Value.Row == row && pendingSpawnIntent.Value.Column == column;

            return currentIntent.HasValue &&
                   currentIntent.Value.TargetRow == row && currentIntent.Value.TargetColumn == column;
        }

        private string DisplayToken(ContentToken token, bool compact)
        {
            if (token == null) return string.Empty;
            if (token.Kind == ContentKind.RecordKey || token.Kind == ContentKind.FieldKey || token.Kind == ContentKind.Label)
                return loc.Get(token.StringId);
            return token.Number.HasValue ? FormatNumber(token.Number.Value) : string.Empty;
        }

        private void SetStatus(string stringId)
        {
            if (statusText != null) statusText.text = loc.Get(stringId);
        }

        public static string ColumnName(int zeroBasedColumn)
        {
            var value = zeroBasedColumn + 1;
            var result = string.Empty;
            while (value > 0)
            {
                value--;
                result = (char)('A' + value % 26) + result;
                value /= 26;
            }
            return result;
        }

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
        private Outline outline;

        public void Initialize(ExcelHellPrototype owner, int cellRow, int cellColumn, Image image, Text text, Outline cellOutline)
        {
            prototype = owner;
            row = cellRow;
            column = cellColumn;
            background = image;
            label = text;
            outline = cellOutline;
        }

        public void Refresh(CellModel model, bool selected, bool intentTarget, bool reportTarget, Func<ContentToken, bool, string> displayToken)
        {
            outline.effectColor = selected
                ? new Color(0.08f, 0.52f, 0.92f, 1f)
                : reportTarget
                    ? new Color(0.20f, 0.55f, 0.34f, 1f)
                    : new Color(0.77f, 0.79f, 0.82f, 1f);
            outline.effectDistance = selected ? new Vector2(3f, -3f) : reportTarget ? new Vector2(2f, -2f) : new Vector2(1f, -1f);

            if (model.State == CellState.Destroyed)
            {
                background.color = selected ? new Color(0.23f, 0.31f, 0.38f, 1f) : new Color(0.16f, 0.17f, 0.18f, 1f);
                label.color = new Color(0.74f, 0.76f, 0.78f, 1f);
                label.text = "×";
                return;
            }
            if (model.State == CellState.Corrupted)
            {
                background.color = selected ? new Color(0.67f, 0.29f, 0.34f, 1f) : new Color(0.76f, 0.22f, 0.22f, 1f);
                label.color = Color.white;
                label.text = "#REF!";
                return;
            }

            // Intent fill is stable. Selection is represented by the blue outline and must not hide/move the orange telegraph.
            background.color = intentTarget
                ? new Color(1f, 0.73f, 0.34f, 1f)
                : selected
                    ? new Color(0.65f, 0.84f, 1f, 1f)
                    : reportTarget
                        ? new Color(0.88f, 0.96f, 0.89f, 1f)
                        : model.Occupant?.Kind == ContentKind.RecordKey || model.Occupant?.Kind == ContentKind.FieldKey
                            ? new Color(0.91f, 0.94f, 0.98f, 1f)
                            : Color.white;

            label.color = new Color(0.12f, 0.13f, 0.15f, 1f);
            label.fontStyle = reportTarget || model.Occupant?.Kind == ContentKind.RecordKey || model.Occupant?.Kind == ContentKind.FieldKey
                ? FontStyle.Bold : FontStyle.Normal;
            label.text = displayToken(model.Occupant, false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) prototype.BeginSelection(row, column);
        }

        public void OnPointerEnter(PointerEventData eventData) => prototype.HoverSelection(row, column);

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) prototype.EndSelection();
        }
    }
}
