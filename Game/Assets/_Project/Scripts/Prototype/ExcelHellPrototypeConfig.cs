using UnityEngine;

namespace ExcelHell.Prototype
{
    [CreateAssetMenu(fileName = "ExcelHellPrototypeConfig", menuName = "EXEL HELL/Prototype Config")]
    public sealed class ExcelHellPrototypeConfig : ScriptableObject
    {
        [Header("Field")]
        [Min(8)] public int rows = 8;
        [Min(8)] public int columns = 8;

        [Header("Turn model")]
        [Min(1)] public int maxTurns = 15;
        [Min(0)] public int anomalyActivationTurn = 3;

        [Header("#REF!")]
        [Tooltip("0 = automatic bottom-left spawn. Otherwise 1-based worksheet row.")]
        [Min(0)] public int anomalySpawnRow = 0;
        [Tooltip("0 = automatic bottom-left spawn. Otherwise 1-based worksheet column.")]
        [Min(0)] public int anomalySpawnColumn = 0;
        [Min(1)] public int corruptionTurnsBeforeDestroy = 2;

        [Header("Prototype debug")]
        public bool showExpectedAnswers = true;

        public int SafeRows => Mathf.Max(8, rows);
        public int SafeColumns => Mathf.Max(8, columns);
        public int SafeMaxTurns => Mathf.Max(1, maxTurns);
        public int SafeActivationTurn => Mathf.Max(0, anomalyActivationTurn);
        public int SafeCorruptionLifetime => Mathf.Max(1, corruptionTurnsBeforeDestroy);
    }
}
