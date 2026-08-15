using System;

namespace ExcelHell.Prototype
{
    public enum PrototypeGameplayEventType
    {
        SortCompleted,
        SumStarted,
        SumCommitted,
        CutCompleted,
        PasteCompleted,
        DeleteCompleted,
        SubmitCompleted
    }

    /// <summary>
    /// Small read-only gameplay signal used by tutorial, audio, VFX and narrative layers.
    /// It deliberately describes completed player-facing actions without owning gameplay state.
    /// </summary>
    public readonly struct PrototypeGameplayEvent
    {
        public readonly PrototypeGameplayEventType Type;
        public readonly string SubjectId;
        public readonly string GoalStringId;
        public readonly string[] TokenIds;
        public readonly int Row;
        public readonly int Column;
        public readonly bool ReportTarget;
        public readonly bool Success;

        public PrototypeGameplayEvent(
            PrototypeGameplayEventType type,
            string subjectId = null,
            string goalStringId = null,
            string[] tokenIds = null,
            int row = -1,
            int column = -1,
            bool reportTarget = false,
            bool success = true)
        {
            Type = type;
            SubjectId = subjectId;
            GoalStringId = goalStringId;
            TokenIds = tokenIds ?? Array.Empty<string>();
            Row = row;
            Column = column;
            ReportTarget = reportTarget;
            Success = success;
        }
    }
}
