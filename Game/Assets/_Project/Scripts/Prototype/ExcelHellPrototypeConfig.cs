using System;
using UnityEngine;

namespace ExcelHell.Prototype
{
    [Flags]
    public enum PrototypeReportGoals
    {
        None = 0,
        SalaryTotal = 1 << 0,
        OvertimeTotal = 1 << 1,
        BonusTotal = 1 << 2,
        BonusAtLeastFour = 1 << 3,
        SalaryOfMaxOvertime = 1 << 4,
        SalaryForHoursBelowForty = 1 << 5
    }

    [CreateAssetMenu(fileName = "ExcelHellPrototypeConfig", menuName = "EXEL HELL/Prototype Config")]
    public sealed class ExcelHellPrototypeConfig : ScriptableObject
    {
        [Header("Field")]
        [Min(8)] public int rows = 8;
        [Min(8)] public int columns = 8;

        [Header("Turn model")]
        [Min(1)] public int maxTurns = 15;

        [Header("Report goals")]
        [Tooltip("Select any combination before entering Play Mode.")]
        public PrototypeReportGoals reportGoals =
            PrototypeReportGoals.SalaryTotal |
            PrototypeReportGoals.BonusAtLeastFour |
            PrototypeReportGoals.SalaryOfMaxOvertime;

        [Header("#REF! outbreaks")]
        [Tooltip("Turns before the first telegraphed outbreak becomes active.")]
        [Min(0)] public int anomalyActivationTurn = 3;
        [Tooltip("If every active #REF! is quarantined/dies, a new outbreak is scheduled this many turns later.")]
        [Min(1)] public int respawnDelayTurns = 2;
        [Tooltip("A fresh outbreak is also scheduled after this many turns while other #REF! cells are still active.")]
        [Min(1)] public int outbreakDelayWhileActiveTurns = 3;
        [Min(1)] public int corruptionTurnsBeforeDestroy = 2;

        [Header("Dynamic #REF! spawn")]
        [Tooltip("Desired Manhattan distance from report-critical data / report cells.")]
        [Min(1)] public int spawnPreferredDistance = 2;
        [Tooltip("Allowed distance deviation around the preferred distance.")]
        [Min(0)] public int spawnDistanceVariation = 1;
        [Tooltip("Number of best spawn candidates rotated through deterministically to avoid identical outbreaks.")]
        [Min(1)] public int spawnCandidatePoolSize = 4;

        [Header("Prototype debug")]
        public bool showExpectedAnswers = true;

        public int SafeRows => Mathf.Max(8, rows);
        public int SafeColumns => Mathf.Max(8, columns);
        public int SafeMaxTurns => Mathf.Max(1, maxTurns);
        public int SafeActivationTurn => Mathf.Max(0, anomalyActivationTurn);
        public int SafeRespawnDelay => Mathf.Max(1, respawnDelayTurns);
        public int SafeActiveOutbreakDelay => Mathf.Max(1, outbreakDelayWhileActiveTurns);
        public int SafeCorruptionLifetime => Mathf.Max(1, corruptionTurnsBeforeDestroy);
        public int SafeSpawnPreferredDistance => Mathf.Max(1, spawnPreferredDistance);
        public int SafeSpawnDistanceVariation => Mathf.Max(0, spawnDistanceVariation);
        public int SafeSpawnCandidatePoolSize => Mathf.Max(1, spawnCandidatePoolSize);

        public bool HasGoal(PrototypeReportGoals goal) => (reportGoals & goal) != 0;
    }
}
