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

    public enum FormulaKind
    {
        None,
        Sum,
        Sort
    }

    public enum ContentKind
    {
        Data,
        RecordKey,
        FieldKey,
        Aggregate,
        Label
    }

    [Serializable]
    public sealed class ContentToken
    {
        private double? number;

        public string Id;
        public ContentKind Kind;
        public string StringId;
        public string RecordId;
        public string FieldId;
        public bool IsAccessible = true;
        public bool IsRequiredSource;
        public List<string> SourceTokenIds = new();

        public double? Number
        {
            get => IsAccessible ? number : null;
            set => number = value;
        }

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
                IsRequiredSource = required,
                SourceTokenIds = new List<string> { id }
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

        public static ContentToken Aggregate(string id, double value, IEnumerable<string> sourceTokenIds, bool required = false)
        {
            return new ContentToken
            {
                Id = id,
                Kind = ContentKind.Aggregate,
                Number = value,
                IsRequiredSource = required,
                SourceTokenIds = sourceTokenIds?.Distinct().OrderBy(x => x).ToList() ?? new List<string>()
            };
        }
    }

    [Serializable]
    public sealed class CellModel
    {
        private CellState state;

        public int Row;
        public int Column;
        public int CorruptionAge;
        public ContentToken Occupant;

        // MVP 0.5: formula is infrastructure owned by this coordinate, never by Occupant.
        public FormulaKind Formula;

        public CellState State
        {
            get => state;
            set
            {
                state = value;
                if (value != CellState.Normal && Occupant != null)
                    Occupant.IsAccessible = false;
            }
        }

        public string Address => $"{ExcelHellPrototype.ColumnName(Column)}{Row + 1}";
        public bool IsEmpty => Occupant == null;
        public bool IsFormula => Formula != FormulaKind.None;
        public bool CanActivateFormula => State == CellState.Normal && IsFormula && Occupant == null;
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

        // Kept as metadata for future optional quality/ending checks.
        public readonly HashSet<string> ExpectedSourceIds;
        public readonly string ExpectedDirectTokenId;

        public ReportGoal(
            string nameStringId,
            double expected,
            int targetRow,
            int targetColumn,
            IEnumerable<string> expectedSourceIds = null,
            string expectedDirectTokenId = null)
        {
            NameStringId = nameStringId;
            Expected = expected;
            TargetRow = targetRow;
            TargetColumn = targetColumn;
            ExpectedSourceIds = expectedSourceIds == null ? new HashSet<string>() : new HashSet<string>(expectedSourceIds);
            ExpectedDirectTokenId = expectedDirectTokenId;
        }

        public bool IsSatisfiedBy(ContentToken token)
        {
            return token?.Number != null && Math.Abs(token.Number.Value - Expected) <= 0.001;
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

    public readonly struct SpawnIntent
    {
        public readonly int Row;
        public readonly int Column;
        public readonly int TurnsRemaining;

        public SpawnIntent(int row, int column, int turnsRemaining)
        {
            Row = row;
            Column = column;
            TurnsRemaining = turnsRemaining;
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
