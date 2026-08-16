using System.Collections.Generic;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Read-only bridge from existing FC2 status feedback into diegetic protagonist hints.
    /// It never changes gameplay state and publishes each hint category at most once per worksheet.
    /// </summary>
    [DefaultExecutionOrder(1170)]
    public sealed class PrototypeContextHintProbe : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);

        private ExcelHellPrototype prototype;
        private Text statusText;
        private string lastStatus;
        private readonly HashSet<string> published = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeContextHintProbe>() != null) return;
            var root = new GameObject("[NARRATIVE] Context Hint Probe");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeContextHintProbe>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || statusText == null) return;

            var value = statusText.text ?? string.Empty;
            if (value == lastStatus) return;
            lastStatus = value;
            var hint = Classify(value);
            if (string.IsNullOrEmpty(hint) || !published.Add(hint)) return;

            NarrativeSignals.Publish(new NarrativeTrigger(
                NarrativeTriggerType.ManualDebug,
                subjectId: hint));
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            statusText = null;
            lastStatus = null;
            published.Clear();
            if (prototype != null)
                statusText = StatusTextField?.GetValue(prototype) as Text;
        }

        private static string Classify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var normalized = value.ToLowerInvariant();

            if (value.Contains("#SPILL!")) return "hint.spill";
            if (normalized.Contains("формула занята") || normalized.Contains("formula is occupied"))
                return "hint.formula_occupied";
            if (normalized.Contains("формульное поле нельзя удалить") || normalized.Contains("formula fields cannot be deleted"))
                return "hint.formula_delete";
            if (normalized.Contains("формуле нужна пустая") || normalized.Contains("formula needs an empty"))
                return "hint.formula_move";
            if (normalized.Contains("sum: нужен диапазон минимум") || normalized.Contains("sum: range needs at least"))
                return "hint.sum_count";
            if (normalized.Contains("sum: диапазон может содержать только") || normalized.Contains("sum: range may contain only"))
                return "hint.sum_numeric";
            if (normalized.Contains("sum: диапазон пересекает недоступную") || normalized.Contains("sum: range crosses an unavailable"))
                return "hint.sum_unavailable";
            if (normalized.Contains("sort: нужен ключ") || normalized.Contains("sort: a field or employee key is required") ||
                normalized.Contains("sort: перетащите один ключ") || normalized.Contains("sort: drag one field or employee key"))
                return "hint.sort_key";
            if (normalized.Contains("move: конечный диапазон занят") || normalized.Contains("move: destination range is occupied"))
                return "hint.move_occupied";
            if (normalized.Contains("move: конечные клетки должны быть доступными") || normalized.Contains("move: destination cells must be available"))
                return "hint.move_blocked";

            return null;
        }
    }
}
