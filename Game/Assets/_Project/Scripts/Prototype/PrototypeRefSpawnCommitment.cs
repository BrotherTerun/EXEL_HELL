using System.Reflection;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Enforces the telegraph contract for future #REF! outbreaks.
    /// A spawn cell is selected only while scheduling; after it is announced, SORT/CUT/PASTE may move
    /// important data into that cell but the anomaly keeps both its coordinate and its original deadline.
    /// </summary>
    [DefaultExecutionOrder(700)]
    public sealed class PrototypeRefSpawnCommitment : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo PendingSpawnField = typeof(ExcelHellPrototype).GetField("pendingSpawnIntent", Flags);
        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo TurnField = typeof(ExcelHellPrototype).GetField("turn", Flags);
        private static readonly MethodInfo GenerateIntentMethod = typeof(ExcelHellPrototype).GetMethod("GenerateIntent", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);

        private ExcelHellPrototype prototype;
        private CellModel[,] cells;
        private bool committed;
        private int committedRow;
        private int committedColumn;
        private int dueTurn;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeRefSpawnCommitment>() != null) return;
            var helper = new GameObject("EXCEL HELL REF Spawn Commitment").AddComponent<PrototypeRefSpawnCommitment>();
            DontDestroyOnLoad(helper.gameObject);
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
                Bind(current);

            if (prototype == null || !PrototypeLevelRuntime.Current.RefEnabled)
            {
                committed = false;
                return;
            }

            var turn = TurnField?.GetValue(prototype) is int value ? value : 0;
            var pending = ReadPending();

            if (!committed)
            {
                if (pending.HasValue)
                    Commit(pending.Value, turn);
                return;
            }

            if (turn < dueTurn)
            {
                var remaining = Mathf.Max(1, dueTurn - turn);
                if (!pending.HasValue ||
                    pending.Value.Row != committedRow ||
                    pending.Value.Column != committedColumn ||
                    pending.Value.TurnsRemaining != remaining)
                {
                    PendingSpawnField?.SetValue(prototype,
                        (SpawnIntent?)new SpawnIntent(committedRow, committedColumn, remaining));
                    RefreshAllMethod?.Invoke(prototype, null);
                }
                return;
            }

            var target = CellAt(committedRow, committedColumn);
            if (target == null)
            {
                committed = false;
                return;
            }

            // Normal core path: the committed cell spawned successfully.
            if (target.State == CellState.Corrupted)
            {
                committed = false;
                return;
            }

            // The old core rejected the cell because SORT moved report-critical data into it and
            // scheduled a replacement one turn later. Cancel that replacement and honour the promise.
            if (target.State == CellState.Normal)
            {
                PendingSpawnField?.SetValue(prototype, null);
                target.State = CellState.Corrupted;
                target.CorruptionAge = 0;
                GenerateIntentMethod?.Invoke(prototype, null);
                RefreshAllMethod?.Invoke(prototype, null);
            }
            else
            {
                // If the player has already sacrificed the committed cell with DELETE, the coordinate
                // still does not retarget. The announced outbreak simply has no viable cell to occupy.
                PendingSpawnField?.SetValue(prototype, null);
                RefreshAllMethod?.Invoke(prototype, null);
            }

            committed = false;
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            cells = owner == null ? null : CellsField?.GetValue(owner) as CellModel[,];
            committed = false;
            dueTurn = 0;
        }

        private SpawnIntent? ReadPending()
        {
            var raw = PendingSpawnField?.GetValue(prototype);
            return raw is SpawnIntent intent ? intent : (SpawnIntent?)null;
        }

        private void Commit(SpawnIntent intent, int currentTurn)
        {
            committed = true;
            committedRow = intent.Row;
            committedColumn = intent.Column;
            dueTurn = currentTurn + Mathf.Max(1, intent.TurnsRemaining);
        }

        private CellModel CellAt(int row, int column)
        {
            if (cells == null || row < 0 || column < 0 || row >= cells.GetLength(0) || column >= cells.GetLength(1))
                return null;
            return cells[row, column];
        }
    }
}
