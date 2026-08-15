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

    [CreateAssetMenu(fileName = "ExcelHellPrototypeConfig", menuName = "EXCEL HELL/Prototype Config")]
    public sealed class ExcelHellPrototypeConfig : ScriptableObject
    {
        [Header("Fallback / debug values")]
        [Min(8)] public int rows = 8;
        [Min(8)] public int columns = 8;
        [Min(1)] public int maxTurns = 15;
        public PrototypeReportGoals reportGoals =
            PrototypeReportGoals.SalaryTotal |
            PrototypeReportGoals.BonusAtLeastFour |
            PrototypeReportGoals.SalaryOfMaxOvertime;

        [Header("Fallback #REF! values")]
        [Min(0)] public int anomalyActivationTurn = 3;
        [Min(1)] public int respawnDelayTurns = 2;
        [Min(1)] public int outbreakDelayWhileActiveTurns = 3;
        [Min(1)] public int corruptionTurnsBeforeDestroy = 2;
        [Min(1)] public int spawnPreferredDistance = 2;
        [Min(0)] public int spawnDistanceVariation = 1;
        [Min(1)] public int spawnCandidatePoolSize = 4;

        [Header("Prototype debug")]
        public bool showExpectedAnswers = true;
        public bool showSpawnDebug = true;

        private PrototypeLevelConfig Level => PrototypeLevelRuntime.Current;

        public int SafeRows => Mathf.Max(8, Level?.Rows ?? rows);
        public int SafeColumns => Mathf.Max(8, Level?.Columns ?? columns);
        public int SafeMaxTurns => Mathf.Max(1, Level?.MaxTurns ?? maxTurns);
        public int SafeActivationTurn => Mathf.Max(0, Level?.FirstOutbreakTurn ?? anomalyActivationTurn);
        public int SafeRespawnDelay => Mathf.Max(1, Level?.RespawnDelayTurns ?? respawnDelayTurns);
        public int SafeActiveOutbreakDelay => Mathf.Max(1, Level?.ActiveOutbreakDelayTurns ?? outbreakDelayWhileActiveTurns);
        public int SafeCorruptionLifetime => Mathf.Max(1, Level?.CorruptionStepsBeforeDestroy ?? corruptionTurnsBeforeDestroy);
        public int SafeSpawnPreferredDistance => Mathf.Max(1, Level?.SpawnPreferredDistance ?? spawnPreferredDistance);
        public int SafeSpawnDistanceVariation => Mathf.Max(0, Level?.SpawnDistanceVariation ?? spawnDistanceVariation);
        public int SafeSpawnCandidatePoolSize => Mathf.Max(1, Level?.SpawnCandidatePoolSize ?? spawnCandidatePoolSize);
        public bool RefEnabled => Level?.RefEnabled ?? true;

        public bool HasGoal(PrototypeReportGoals goal)
        {
            var selected = Level?.ReportGoals ?? reportGoals;
            return (selected & goal) != 0;
        }
    }
}
