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

    [Serializable]
    public sealed class PrototypeSpawnPoint
    {
        [Min(1)] public int row = 1;
        [Min(1)] public int column = 1;

        public PrototypeSpawnPoint() { }

        public PrototypeSpawnPoint(int row, int column)
        {
            this.row = row;
            this.column = column;
        }
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
        [Min(0)] public int anomalyActivationTurn = 3;
        [Min(1)] public int maxOutbreaks = 3;
        [Min(1)] public int respawnDelayTurns = 2;
        [Min(1)] public int corruptionTurnsBeforeDestroy = 2;
        [Tooltip("Deterministic 1-based spawn points. Invalid/destroyed points fall through to the next point.")]
        public PrototypeSpawnPoint[] anomalySpawnPoints =
        {
            new PrototypeSpawnPoint(8, 1),
            new PrototypeSpawnPoint(8, 8),
            new PrototypeSpawnPoint(4, 4)
        };

        [Header("Prototype debug")]
        public bool showExpectedAnswers = true;

        public int SafeRows => Mathf.Max(8, rows);
        public int SafeColumns => Mathf.Max(8, columns);
        public int SafeMaxTurns => Mathf.Max(1, maxTurns);
        public int SafeActivationTurn => Mathf.Max(0, anomalyActivationTurn);
        public int SafeMaxOutbreaks => Mathf.Max(1, maxOutbreaks);
        public int SafeRespawnDelay => Mathf.Max(1, respawnDelayTurns);
        public int SafeCorruptionLifetime => Mathf.Max(1, corruptionTurnsBeforeDestroy);

        public bool HasGoal(PrototypeReportGoals goal) => (reportGoals & goal) != 0;
    }
}
