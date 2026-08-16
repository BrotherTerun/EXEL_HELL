using System.Collections.Generic;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Read-only bridge from existing FC2 status feedback into diegetic protagonist hints.
    /// It never changes gameplay state and shows each hint category at most once per worksheet.
    /// </summary>
    [DefaultExecutionOrder(1170)]
    public sealed class PrototypeContextHintProbe : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);

        private ExcelHellPrototype prototype;
        private Text statusText;
        private string lastStatus;
        private readonly HashSet<string> shown = new();

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

            var hintId = Classify(value);
            if (string.IsNullOrEmpty(hintId) || shown.Contains(hintId)) return;
            var presenter = FindFirstObjectByType<PrototypeProtagonistPresenter>();
            if (presenter == null) return;

            var text = HintText(hintId);
            if (string.IsNullOrEmpty(text)) return;
            shown.Add(hintId);

            var effect = new NarrativeEffectDefinition
            {
                type = NarrativeEffectType.ProtagonistLine,
                text = text,
                mood = HintMood(),
                lifetime = new NarrativeLifetime
                {
                    dismissMode = NarrativeDismissMode.TimedOrClick,
                    duration = 4.5f
                },
                priority = 20
            };
            var request = new NarrativeEffectRequest($"context.{hintId}", effect);
            presenter.Receive(new NarrativeEffectTicket(request));
            Debug.Log($"[NARRATIVE/HINT] {hintId}: \"{text}\"");
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            statusText = null;
            lastStatus = null;
            shown.Clear();
            if (prototype != null)
                statusText = StatusTextField?.GetValue(prototype) as Text;
        }

        private static string Classify(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var normalized = value.ToLowerInvariant();

            if (value.Contains("#SPILL!")) return "spill";
            if (normalized.Contains("формула занята") || normalized.Contains("formula is occupied")) return "formula_occupied";
            if (normalized.Contains("формульное поле нельзя удалить") || normalized.Contains("formula fields cannot be deleted")) return "formula_delete";
            if (normalized.Contains("формуле нужна пустая") || normalized.Contains("formula needs an empty")) return "formula_move";
            if (normalized.Contains("sum: нужен диапазон минимум") || normalized.Contains("sum: range needs at least")) return "sum_count";
            if (normalized.Contains("sum: диапазон может содержать только") || normalized.Contains("sum: range may contain only")) return "sum_numeric";
            if (normalized.Contains("sum: диапазон пересекает недоступную") || normalized.Contains("sum: range crosses an unavailable")) return "sum_unavailable";
            if (normalized.Contains("sort: нужен ключ") || normalized.Contains("sort: a field or employee key is required") ||
                normalized.Contains("sort: перетащите один ключ") || normalized.Contains("sort: drag one field or employee key")) return "sort_key";
            if (normalized.Contains("move: конечный диапазон занят") || normalized.Contains("move: destination range is occupied")) return "move_occupied";
            if (normalized.Contains("move: конечные клетки должны быть доступными") || normalized.Contains("move: destination cells must be available")) return "move_blocked";
            return null;
        }

        private static string HintText(string hintId) => hintId switch
        {
            "spill" => "Нет, сначала нужно освободить место.",
            "formula_occupied" => "Сначала нужно вынести результат из формулы.",
            "formula_delete" => "Формулу нельзя удалить. Её можно только перенести.",
            "formula_move" => "Формуле нужна пустая доступная ячейка.",
            "sum_count" => "Для SUM нужно хотя бы два числа.",
            "sum_numeric" => "В диапазоне для SUM должны остаться только числа.",
            "sum_unavailable" => "Диапазон пересекает недоступную ячейку. Нужно выбрать другой.",
            "sort_key" => "Для SORT нужен один ключ параметра или сотрудника.",
            "move_occupied" => "Сюда не поместится. Сначала нужно освободить место.",
            "move_blocked" => "Здесь путь заблокирован. Нужно переставить данные.",
            _ => null
        };

        private static ProtagonistMood HintMood()
        {
            var id = PrototypeLevelRuntime.Current?.Id ?? string.Empty;
            if (id.StartsWith("04_")) return ProtagonistMood.Psychotic;
            if (id.StartsWith("03_")) return ProtagonistMood.Alarmed;
            if (id.StartsWith("02_")) return ProtagonistMood.Tired;
            return ProtagonistMood.Normal;
        }
    }
}
