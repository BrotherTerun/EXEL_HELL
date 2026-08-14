using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// MVP 0.3 balancing shim for dynamic #REF! spawn selection.
    /// Keeps the existing anomaly movement AI untouched, but replaces the scheduled
    /// spawn cell with a deterministic, goal-aware candidate chosen around live
    /// report-critical data. Report cells themselves are not anchors.
    /// </summary>
    public sealed class PrototypeSpawnDirector : MonoBehaviour
    {
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private ExcelHellPrototype prototype;
        private ExcelHellPrototypeConfig config;
        private FieldInfo cellsField;
        private FieldInfo goalsField;
        private FieldInfo pendingSpawnField;

        private bool hadPending;
        private int lastRow;
        private int lastColumn;
        private int lastTurns;
        private int localSpawnSequence;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeSpawnDirector>() != null)
                return;

            new GameObject("EXEL HELL Spawn Director").AddComponent<PrototypeSpawnDirector>();
        }

        private void Update()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current == null)
                return;

            if (prototype != current)
                Bind(current);

            if (pendingSpawnField == null || cellsField == null || goalsField == null || config == null)
                return;

            var pending = (SpawnIntent?)pendingSpawnField.GetValue(prototype);
            if (!pending.HasValue)
            {
                hadPending = false;
                return;
            }

            var value = pending.Value;
            var looksNew = !hadPending ||
                           value.TurnsRemaining > lastTurns ||
                           (value.Row != lastRow || value.Column != lastColumn) && value.TurnsRemaining >= lastTurns;

            if (looksNew)
                ReplaceScheduledSpawn(value);
            else
                Remember(value);
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            config = Resources.Load<ExcelHellPrototypeConfig>("ExcelHellPrototypeConfig");
            cellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
            goalsField = typeof(ExcelHellPrototype).GetField("goals", Flags);
            pendingSpawnField = typeof(ExcelHellPrototype).GetField("pendingSpawnIntent", Flags);
            hadPending = false;
            localSpawnSequence = 0;
        }

        private void ReplaceScheduledSpawn(SpawnIntent original)
        {
            var cells = cellsField.GetValue(prototype) as CellModel[,];
            var goals = goalsField.GetValue(prototype) as List<ReportGoal>;
            if (cells == null || goals == null)
            {
                Remember(original);
                return;
            }

            var allCells = cells.Cast<CellModel>().ToList();
            var anchors = allCells
                .Where(cell => cell.State != CellState.Destroyed && cell.Occupant?.IsRequiredSource == true)
                .ToList();

            if (anchors.Count == 0)
            {
                anchors = allCells
                    .Where(cell => cell.State != CellState.Destroyed && cell.Occupant != null)
                    .ToList();
            }

            if (anchors.Count == 0)
            {
                Remember(original);
                return;
            }

            var preferred = config.SafeSpawnPreferredDistance;
            var variation = config.SafeSpawnDistanceVariation;
            var minDistance = Mathf.Max(1, preferred - variation);
            var maxDistance = preferred + variation;

            var ranked = allCells
                .Where(cell => cell.State == CellState.Normal)
                .Where(cell => cell.Occupant?.IsRequiredSource != true)
                .Where(cell => !goals.Any(goal => goal.TargetRow == cell.Row && goal.TargetColumn == cell.Column))
                .Select(cell => new SpawnCandidate(
                    cell,
                    anchors.Min(anchor => Manhattan(cell, anchor)),
                    DistanceToBand(anchors.Min(anchor => Manhattan(cell, anchor)), minDistance, maxDistance),
                    Mathf.Abs(anchors.Min(anchor => Manhattan(cell, anchor)) - preferred)))
                .Where(candidate => candidate.Distance > 0)
                .OrderBy(candidate => candidate.BandPenalty)
                .ThenBy(candidate => candidate.PreferredPenalty)
                .ThenBy(candidate => candidate.Cell.Occupant == null ? 0 : 1)
                .ThenBy(candidate => StableGoalTieBreak(candidate.Cell.Row, candidate.Cell.Column))
                .ToList();

            if (ranked.Count == 0)
            {
                Remember(original);
                return;
            }

            var poolSize = Mathf.Min(config.SafeSpawnCandidatePoolSize, ranked.Count);
            var pool = ranked.Take(poolSize).ToList();
            var poolIndex = StablePoolIndex(poolSize);
            var chosen = pool[poolIndex];
            var replacement = new SpawnIntent(chosen.Cell.Row, chosen.Cell.Column, original.TurnsRemaining);
            pendingSpawnField.SetValue(prototype, replacement);

            if (config.showSpawnDebug)
            {
                var poolText = string.Join(", ", pool.Select(c => $"{c.Cell.Address}:d{c.Distance}"));
                var anchorText = string.Join(", ", anchors.Select(a => a.Address).Distinct());
                Debug.Log($"EXEL HELL #REF! SPAWN | goals={(int)config.reportGoals} | chosen={chosen.Cell.Address} | d={chosen.Distance} | anchors=[{anchorText}] | pool=[{poolText}]");
            }

            localSpawnSequence++;
            Remember(replacement);
        }

        private int StablePoolIndex(int poolSize)
        {
            unchecked
            {
                uint x = (uint)((int)config.reportGoals * 2654435761u);
                x ^= (uint)((localSpawnSequence + 1) * 2246822519u);
                x ^= x >> 15;
                x *= 3266489917u;
                x ^= x >> 16;
                return (int)(x % (uint)poolSize);
            }
        }

        private int StableGoalTieBreak(int row, int column)
        {
            unchecked
            {
                uint x = (uint)((int)config.reportGoals * 73856093);
                x ^= (uint)((row + 1) * 19349663);
                x ^= (uint)((column + 1) * 83492791);
                x ^= (uint)((localSpawnSequence + 1) * 2654435761u);
                x ^= x >> 13;
                return (int)(x & 0x7fffffff);
            }
        }

        private static int Manhattan(CellModel a, CellModel b)
        {
            return Mathf.Abs(a.Row - b.Row) + Mathf.Abs(a.Column - b.Column);
        }

        private static int DistanceToBand(int distance, int minDistance, int maxDistance)
        {
            if (distance < minDistance) return minDistance - distance;
            if (distance > maxDistance) return distance - maxDistance;
            return 0;
        }

        private void Remember(SpawnIntent value)
        {
            hadPending = true;
            lastRow = value.Row;
            lastColumn = value.Column;
            lastTurns = value.TurnsRemaining;
        }

        private readonly struct SpawnCandidate
        {
            public readonly CellModel Cell;
            public readonly int Distance;
            public readonly int BandPenalty;
            public readonly int PreferredPenalty;

            public SpawnCandidate(CellModel cell, int distance, int bandPenalty, int preferredPenalty)
            {
                Cell = cell;
                Distance = distance;
                BandPenalty = bandPenalty;
                PreferredPenalty = preferredPenalty;
            }
        }
    }
}
