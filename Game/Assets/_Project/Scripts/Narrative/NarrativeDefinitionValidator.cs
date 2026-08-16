using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExcelHell.Narrative
{
    public enum NarrativeValidationSeverity
    {
        Warning,
        Error
    }

    public readonly struct NarrativeValidationIssue
    {
        public readonly NarrativeValidationSeverity Severity;
        public readonly string EventId;
        public readonly int EffectIndex;
        public readonly string Message;

        public NarrativeValidationIssue(
            NarrativeValidationSeverity severity,
            string eventId,
            string message,
            int effectIndex = -1)
        {
            Severity = severity;
            EventId = string.IsNullOrWhiteSpace(eventId) ? "<unnamed>" : eventId;
            EffectIndex = effectIndex;
            Message = message ?? string.Empty;
        }

        public override string ToString()
        {
            var effect = EffectIndex >= 0 ? $" effect[{EffectIndex}]" : string.Empty;
            return $"event={EventId}{effect}: {Message}";
        }
    }

    /// <summary>
    /// Lightweight authoring validation. Narrative mistakes must be visible in Console but must never stop gameplay.
    /// </summary>
    public static class NarrativeDefinitionValidator
    {
        public static IReadOnlyList<NarrativeValidationIssue> Validate(
            IEnumerable<NarrativeEventDefinition> definitions)
        {
            var result = new List<NarrativeValidationIssue>();
            var events = definitions?.Where(item => item != null).ToList()
                         ?? new List<NarrativeEventDefinition>();
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
            {
                var definition = events[eventIndex];
                var id = definition.id;

                if (!string.IsNullOrWhiteSpace(id) && !ids.Add(id))
                    Error(result, id, "duplicate event id; once-state would be ambiguous.");

                if (definition.once && string.IsNullOrWhiteSpace(id))
                    Error(result, id, "once=true requires a stable non-empty id.");

                if (definition.delay < 0f)
                    Error(result, id, "delay cannot be negative.");

                if (definition.trigger == NarrativeTriggerType.ActionNumber && definition.triggerNumber <= 0)
                    Error(result, id, "ActionNumber requires triggerNumber > 0.");

                if (definition.effects == null || definition.effects.Count == 0)
                {
                    Warn(result, id, "event has no effects.");
                    continue;
                }

                for (var effectIndex = 0; effectIndex < definition.effects.Count; effectIndex++)
                {
                    var effect = definition.effects[effectIndex];
                    if (effect == null)
                    {
                        Warn(result, id, "null effect will be skipped.", effectIndex);
                        continue;
                    }

                    ValidateEffect(result, id, effect, effectIndex);
                }
            }

            return result;
        }

        public static bool LogIssues(IEnumerable<NarrativeEventDefinition> definitions, UnityEngine.Object context = null)
        {
            var issues = Validate(definitions);
            var hasErrors = false;

            foreach (var issue in issues)
            {
                var message = $"[NARRATIVE/VALIDATION] {issue}";
                if (issue.Severity == NarrativeValidationSeverity.Error)
                {
                    hasErrors = true;
                    Debug.LogError(message, context);
                }
                else
                {
                    Debug.LogWarning(message, context);
                }
            }

            if (issues.Count == 0)
                Debug.Log("[NARRATIVE/VALIDATION] OK — no authoring issues found.", context);

            return !hasErrors;
        }

        private static void ValidateEffect(
            ICollection<NarrativeValidationIssue> result,
            string eventId,
            NarrativeEffectDefinition effect,
            int effectIndex)
        {
            switch (effect.type)
            {
                case NarrativeEffectType.CellMessage:
                    RequireText(result, eventId, effect, effectIndex);
                    if (effect.row < 0 || effect.column < 0)
                        Error(result, eventId, "CellMessage requires row and column >= 0.", effectIndex);
                    ValidateLifetime(result, eventId, effect, effectIndex);
                    break;

                case NarrativeEffectType.ProtagonistLine:
                    RequireText(result, eventId, effect, effectIndex);
                    ValidateLifetime(result, eventId, effect, effectIndex);
                    break;

                case NarrativeEffectType.BossChatMessage:
                case NarrativeEffectType.DepartmentChatMessage:
                    RequireText(result, eventId, effect, effectIndex);
                    break;

                case NarrativeEffectType.Toast:
                    RequireText(result, eventId, effect, effectIndex);
                    ValidateLifetime(result, eventId, effect, effectIndex);
                    break;

                case NarrativeEffectType.VisualGlitch:
                    ValidateLifetime(result, eventId, effect, effectIndex);
                    break;

                case NarrativeEffectType.PsychosisDelta:
                    if (effect.intValue == 0)
                        Warn(result, eventId, "PsychosisDelta is zero and has no effect.", effectIndex);
                    break;

                case NarrativeEffectType.Sound:
                    if (string.IsNullOrWhiteSpace(effect.id))
                        Error(result, eventId, "Sound requires a non-empty effect id/audio key.", effectIndex);
                    break;
            }
        }

        private static void RequireText(
            ICollection<NarrativeValidationIssue> result,
            string eventId,
            NarrativeEffectDefinition effect,
            int effectIndex)
        {
            if (string.IsNullOrWhiteSpace(effect.text))
                Error(result, eventId, $"{effect.type} requires non-empty text.", effectIndex);
        }

        private static void ValidateLifetime(
            ICollection<NarrativeValidationIssue> result,
            string eventId,
            NarrativeEffectDefinition effect,
            int effectIndex)
        {
            if ((effect.lifetime.dismissMode == NarrativeDismissMode.Timed ||
                 effect.lifetime.dismissMode == NarrativeDismissMode.TimedOrClick) &&
                effect.lifetime.duration <= 0f)
            {
                Error(result, eventId,
                    $"{effect.type} with {effect.lifetime.dismissMode} requires duration > 0.", effectIndex);
            }
        }

        private static void Warn(
            ICollection<NarrativeValidationIssue> result,
            string eventId,
            string message,
            int effectIndex = -1) =>
            result.Add(new NarrativeValidationIssue(
                NarrativeValidationSeverity.Warning, eventId, message, effectIndex));

        private static void Error(
            ICollection<NarrativeValidationIssue> result,
            string eventId,
            string message,
            int effectIndex = -1) =>
            result.Add(new NarrativeValidationIssue(
                NarrativeValidationSeverity.Error, eventId, message, effectIndex));
    }
}
