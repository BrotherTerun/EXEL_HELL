using System;
using System.Collections.Generic;
using UnityEngine;

namespace ExcelHell.Narrative
{
    public enum NarrativeTriggerType
    {
        LevelStart,
        ActionNumber,
        FirstRefSpawn,
        RefSpread,
        RefDestroyed,
        CellDestroyed,
        GoalCompleted,
        AllGoalsCompleted,
        ReportSubmitted,
        LevelCompleted,
        ManualDebug
    }

    public enum NarrativeEffectType
    {
        CellMessage,
        ProtagonistLine,
        BossChatMessage,
        DepartmentChatMessage,
        Toast,
        SystemStatus,
        VisualGlitch,
        PsychosisDelta,
        Sound
    }

    public enum NarrativeDismissMode
    {
        Timed,
        OnClick,
        TimedOrClick
    }

    public enum ProtagonistMood
    {
        Normal,
        Tired,
        Alarmed,
        Psychotic
    }

    [Serializable]
    public struct NarrativeLifetime
    {
        public NarrativeDismissMode dismissMode;
        [Min(0f)] public float duration;

        public static NarrativeLifetime Timed(float seconds) => new()
        {
            dismissMode = NarrativeDismissMode.Timed,
            duration = Mathf.Max(0f, seconds)
        };
    }

    [Serializable]
    public sealed class NarrativeEffectDefinition
    {
        public NarrativeEffectType type;
        [TextArea] public string text;
        // CellMessage convention: (-1,-1) asks the production presenter to pick a stable empty cell.
        public int row = -1;
        public int column = -1;
        public ProtagonistMood mood = ProtagonistMood.Normal;
        public NarrativeLifetime lifetime = new()
        {
            dismissMode = NarrativeDismissMode.Timed,
            duration = 3f
        };
        public int intValue;
        public string id;
        public int priority;
    }

    [Serializable]
    public sealed class NarrativeEventDefinition
    {
        public string id;
        public string levelId;
        public NarrativeTriggerType trigger;
        public int triggerNumber;
        public string triggerSubjectId;
        public bool once = true;
        [Min(0f)] public float delay;
        public List<NarrativeEffectDefinition> effects = new();
    }

    public readonly struct NarrativeTrigger
    {
        public readonly NarrativeTriggerType Type;
        public readonly int Number;
        public readonly string SubjectId;
        public readonly int Row;
        public readonly int Column;

        public NarrativeTrigger(
            NarrativeTriggerType type,
            int number = 0,
            string subjectId = null,
            int row = -1,
            int column = -1)
        {
            Type = type;
            Number = number;
            SubjectId = subjectId;
            Row = row;
            Column = column;
        }

        public override string ToString() =>
            $"{Type} number={Number} subject={SubjectId ?? "-"} cell=({Row},{Column})";
    }

    public readonly struct NarrativeEffectRequest
    {
        public readonly string EventId;
        public readonly NarrativeEffectDefinition Effect;

        public NarrativeEffectRequest(string eventId, NarrativeEffectDefinition effect)
        {
            EventId = eventId;
            Effect = effect;
        }
    }
}
