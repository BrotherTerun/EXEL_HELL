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

        [Header("Realtime experiment")]
        [Tooltip("Real seconds available for the whole level. 300 seconds = 5 minutes.")]
        [Min(30f)] public float levelDurationSeconds = 300f;
        [Tooltip("Real seconds between telegraphed #REF! movement resolutions. Old 15-turn pacing scaled to a 5-minute level gives ~20 seconds per former turn.")]
        [Min(1f)] public float anomalyStepSeconds = 20f;
        [Tooltip("Real seconds before the first outbreak becomes active.")]
        [Min(1f)] public float firstOutbreakDelaySeconds = 60f;
        [Tooltip("Real seconds before a new outbreak after every live #REF! disappears.")]
        [Min(1f)] public float respawnDelaySeconds = 40f;
        [Tooltip("Real seconds before a parallel outbreak while another #REF! is active.")]
        [Min(1f)] public float outbreakDelayWhileActiveSeconds = 60f;

        [Header("Legacy turn values (ignored by realtime branch)")]
        [Min(1)] public int maxTurns = 15;
        [Min(0)] public int anomalyActivationTurn = 3;
        [Min(1)] public int respawnDelayTurns = 2;
        [Min(1)] public int outbreakDelayWhileActiveTurns = 3;

        [Header("Report goals")]
        [Tooltip("Select any combination before entering Play Mode.")]
        public PrototypeReportGoals reportGoals =
            PrototypeReportGoals.SalaryTotal |
            PrototypeReportGoals.BonusAtLeastFour |
            PrototypeReportGoals.SalaryOfMaxOvertime;

        [Header("#REF! lifecycle")]
        [Tooltip("How many realtime anomaly movement resolutions a Corrupted cell survives before becoming Destroyed.")]
        [Min(1)] public int corruptionTurnsBeforeDestroy = 2;

        [Header("Dynamic #REF! spawn")]
        [Tooltip("Desired Manhattan distance from report-critical data.")]
        [Min(1)] public int spawnPreferredDistance = 2;
        [Tooltip("Allowed distance deviation around the preferred distance.")]
        [Min(0)] public int spawnDistanceVariation = 1;
        [Tooltip("Number of equally strong spawn candidates considered before deterministic goal-aware selection.")]
        [Min(1)] public int spawnCandidatePoolSize = 4;

        [Header("Prototype debug")]
        public bool showExpectedAnswers = true;
        [Tooltip("Shows and logs the anchors/candidate pool used by the dynamic #REF! spawn selector.")]
        public bool showSpawnDebug = true;

        public int SafeRows => Mathf.Max(8, rows);
        public int SafeColumns => Mathf.Max(8, columns);
        public float SafeLevelDurationSeconds => levelDurationSeconds > 0f ? levelDurationSeconds : 300f;
        public float SafeAnomalyStepSeconds => anomalyStepSeconds > 0f ? anomalyStepSeconds : 20f;
        public float SafeFirstOutbreakDelaySeconds => firstOutbreakDelaySeconds > 0f ? firstOutbreakDelaySeconds : 60f;
        public float SafeRespawnDelaySeconds => respawnDelaySeconds > 0f ? respawnDelaySeconds : 40f;
        public float SafeActiveOutbreakDelaySeconds => outbreakDelayWhileActiveSeconds > 0f ? outbreakDelayWhileActiveSeconds : 60f;
        public int SafeCorruptionLifetime => Mathf.Max(1, corruptionTurnsBeforeDestroy);
        public int SafeSpawnPreferredDistance => Mathf.Max(1, spawnPreferredDistance);
        public int SafeSpawnDistanceVariation => Mathf.Max(0, spawnDistanceVariation);
        public int SafeSpawnCandidatePoolSize => Mathf.Max(1, spawnCandidatePoolSize);

        public bool HasGoal(PrototypeReportGoals goal) => (reportGoals & goal) != 0;
    }
}
