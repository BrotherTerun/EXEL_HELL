using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelHell.Prototype;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Narrative
{
    /// <summary>
    /// Read-only adapter over the current prototype. It observes gameplay state and publishes narrative triggers
    /// without mutating turns, cells, goals or rendering.
    /// </summary>
    [DefaultExecutionOrder(1190)]
    public sealed class NarrativeGameplayProbe : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo GoalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
        private static readonly FieldInfo FinishedField = typeof(ExcelHellPrototype).GetField("finished", Flags);

        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private List<ReportGoal> goals;
        private Button submitButton;
        private int lastTurn = -1;
        private bool firstRefPublished;
        private bool levelCompletedPublished;
        private readonly HashSet<string> corruptedCells = new();
        private readonly HashSet<string> destroyedCells = new();
        private readonly HashSet<string> completedGoals = new();

        private void OnDestroy() => UnbindSubmitButton();

        private void Update()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || cells == null) return;

            ObserveTurn();
            ObserveCells();
            ObserveGoals();
            ObserveLevelCompletion();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            UnbindSubmitButton();
            prototype = owner;
            cells = null;
            goals = null;
            lastTurn = -1;
            firstRefPublished = false;
            levelCompletedPublished = false;
            corruptedCells.Clear();
            destroyedCells.Clear();
            completedGoals.Clear();

            if (prototype == null) return;

            cells = CellsField?.GetValue(prototype) as CellModel[,];
            goals = GoalsField?.GetValue(prototype) as List<ReportGoal>;
            BindSubmitButton();

            var levelId = PrototypeLevelRuntime.Current?.Id ?? "runtime";
            var runner = FindFirstObjectByType<NarrativeEventRunner>();
            if (runner != null) runner.LevelId = levelId;

            Debug.Log($"[NARRATIVE/PROBE] Bound to gameplay. level={levelId}");
            NarrativeSignals.Publish(new NarrativeTrigger(NarrativeTriggerType.LevelStart, subjectId: levelId));
        }

        private void BindSubmitButton()
        {
            if (prototype == null) return;
            submitButton = prototype.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "ui.submit");
            if (submitButton != null) submitButton.onClick.AddListener(OnReportSubmitted);
        }

        private void UnbindSubmitButton()
        {
            if (submitButton != null) submitButton.onClick.RemoveListener(OnReportSubmitted);
            submitButton = null;
        }

        private void OnReportSubmitted()
        {
            NarrativeSignals.Publish(new NarrativeTrigger(
                NarrativeTriggerType.ReportSubmitted,
                subjectId: PrototypeLevelRuntime.Current?.Id));
        }

        private void ObserveTurn()
        {
            if (TurnField?.GetValue(prototype) is not int turn || turn == lastTurn) return;
            lastTurn = turn;
            if (turn <= 0) return;
            NarrativeSignals.Publish(new NarrativeTrigger(NarrativeTriggerType.ActionNumber, turn));
        }

        private void ObserveCells()
        {
            foreach (var cell in cells)
            {
                var address = cell.Address;
                if (cell.State == CellState.Corrupted && corruptedCells.Add(address))
                {
                    if (!firstRefPublished)
                    {
                        firstRefPublished = true;
                        NarrativeSignals.Publish(new NarrativeTrigger(
                            NarrativeTriggerType.FirstRefSpawn,
                            subjectId: address,
                            row: cell.Row,
                            column: cell.Column));
                    }
                    else
                    {
                        NarrativeSignals.Publish(new NarrativeTrigger(
                            NarrativeTriggerType.RefSpread,
                            subjectId: address,
                            row: cell.Row,
                            column: cell.Column));
                    }
                }

                if (cell.State == CellState.Destroyed && destroyedCells.Add(address))
                {
                    NarrativeSignals.Publish(new NarrativeTrigger(
                        NarrativeTriggerType.CellDestroyed,
                        subjectId: address,
                        row: cell.Row,
                        column: cell.Column));
                }
            }
        }

        private void ObserveGoals()
        {
            if (goals == null) return;
            foreach (var goal in goals)
            {
                var key = goal.NameStringId;
                if (completedGoals.Contains(key)) continue;
                var target = cells[goal.TargetRow, goal.TargetColumn];
                if (target.State != CellState.Normal || !goal.IsSatisfiedBy(target.Occupant)) continue;

                completedGoals.Add(key);
                NarrativeSignals.Publish(new NarrativeTrigger(
                    NarrativeTriggerType.GoalCompleted,
                    subjectId: key,
                    row: goal.TargetRow,
                    column: goal.TargetColumn));
            }
        }

        private void ObserveLevelCompletion()
        {
            if (levelCompletedPublished) return;
            if (FinishedField?.GetValue(prototype) is not bool finished || !finished) return;
            levelCompletedPublished = true;
            NarrativeSignals.Publish(new NarrativeTrigger(
                NarrativeTriggerType.LevelCompleted,
                subjectId: PrototypeLevelRuntime.Current?.Id));
        }
    }
}
