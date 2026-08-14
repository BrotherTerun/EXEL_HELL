using System;
using System.Collections.Generic;
using System.Linq;

namespace ExcelHell.Prototype
{
    public enum CellState
    {
        Normal,
        Corrupted,
        Destroyed
    }

    public enum ContentKind
    {
        Data,
        RecordKey,
        FieldKey,
        Aggregate,
        Label
    }

    public enum ContextHintKind
    {
        None,
        Record,
        Field
    }

    [Serializable]
    public sealed class ContentToken
    {
        public string Id;
        public ContentKind Kind;
        public string StringId;
        public string RecordId;
        public string FieldId;
        public double? Number;
        public bool IsRequiredSource;
        public ContextHintKind ContextHint;
        public string ContextId;

        public bool IsNumeric => Number.HasValue;

        public static ContentToken Data(string id, string recordId, string fieldId, double value, bool required)
        {
            return new ContentToken
            {
                Id = id,
                Kind = ContentKind.Data,
                RecordId = recordId,
                FieldId = fieldId,
                Number = value,
                IsRequiredSource = required
            };
        }

        public static ContentToken RecordKey(string recordId)
        {
            return new ContentToken
            {
                Id = $"record-key.{recordId}",
                Kind = ContentKind.RecordKey,
                RecordId = recordId,
                StringId = $"record.{recordId}"
            };
        }

        public static ContentToken FieldKey(string fieldId)
        {
            return new ContentToken
            {
                Id = $"field-key.{fieldId}",
                Kind = ContentKind.FieldKey,
                FieldId = fieldId,
                StringId = $"field.{fieldId}"
            };
        }

        public static ContentToken Label(string id, string stringId)
        {
            return new ContentToken
            {
                Id = id,
                Kind = ContentKind.Label,
                StringId = stringId
            };
        }

        public static ContentToken Aggregate(string id, double value)
        {
            return new ContentToken
            {
                Id = id,
                Kind = ContentKind.Aggregate,
                Number = value
            };
        }
    }

    [Serializable]
    public sealed class CellModel
    {
        public int Row;
        public int Column;
        public CellState State;
        public int CorruptionAge;
        public ContentToken Occupant;

        public string Address => $"{ExcelHellPrototype.ColumnName(Column)}{Row + 1}";
        public bool IsEmpty => Occupant == null;
    }

    public sealed class WorksheetSchema
    {
        public readonly IReadOnlyList<string> Records;
        public readonly IReadOnlyList<string> Fields;

        public WorksheetSchema(IEnumerable<string> records, IEnumerable<string> fields)
        {
            Records = records.ToArray();
            Fields = fields.ToArray();
        }

        public int RecordOrder(string recordId)
        {
            for (var i = 0; i < Records.Count; i++)
                if (Records[i] == recordId)
                    return i;
            return int.MaxValue;
        }

        public int FieldOrder(string fieldId)
        {
            for (var i = 0; i < Fields.Count; i++)
                if (Fields[i] == fieldId)
                    return i;
            return int.MaxValue;
        }
    }

    public sealed class ReportGoal
    {
        public readonly string NameStringId;
        public readonly double Expected;
        public readonly int TargetRow;
        public readonly int TargetColumn;

        public ReportGoal(string nameStringId, double expected, int targetRow, int targetColumn)
        {
            NameStringId = nameStringId;
            Expected = expected;
            TargetRow = targetRow;
            TargetColumn = targetColumn;
        }
    }

    public readonly struct AnomalyIntent
    {
        public readonly int SourceRow;
        public readonly int SourceColumn;
        public readonly int TargetRow;
        public readonly int TargetColumn;

        public AnomalyIntent(int sourceRow, int sourceColumn, int targetRow, int targetColumn)
        {
            SourceRow = sourceRow;
            SourceColumn = sourceColumn;
            TargetRow = targetRow;
            TargetColumn = targetColumn;
        }
    }

    public sealed class SortPlan
    {
        public readonly CellModel KeyCell;
        public readonly List<ContentToken> Tokens;
        public readonly List<CellModel> Destinations;
        public readonly bool UsesFallbackDirection;

        public SortPlan(CellModel keyCell, List<ContentToken> tokens, List<CellModel> destinations, bool usesFallbackDirection)
        {
            KeyCell = keyCell;
            Tokens = tokens;
            Destinations = destinations;
            UsesFallbackDirection = usesFallbackDirection;
        }
    }
}
