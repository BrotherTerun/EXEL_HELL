using System;
using System.Collections.Generic;
using System.Linq;
using ExcelHell.Prototype;
using UnityEngine;

namespace ExcelHell.Narrative
{
    /// <summary>
    /// Final onboarding authoring pass. It extends the production narrative schedule instead of owning gameplay:
    /// protagonist = primary instructions, boss = report/deadline truth, department = practical office tips.
    /// Runs immediately after NarrativeProductionInstaller and before the gameplay probe publishes LevelStart.
    /// </summary>
    [DefaultExecutionOrder(1185)]
    public sealed class NarrativeTutorialInjector : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private string installedLevelId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<NarrativeTutorialInjector>() != null) return;
            var root = new GameObject("[NARRATIVE] Contextual Tutorial Content");
            DontDestroyOnLoad(root);
            root.AddComponent<NarrativeTutorialInjector>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var currentPrototype = FindFirstObjectByType<ExcelHellPrototype>();
            var currentRunner = FindFirstObjectByType<NarrativeEventRunner>();
            var level = PrototypeLevelRuntime.Current;
            var levelId = level?.Id ?? "runtime";
            if (currentPrototype == null || currentRunner == null || level == null) return;

            if (currentPrototype == prototype && currentRunner == runner &&
                string.Equals(installedLevelId, levelId, StringComparison.OrdinalIgnoreCase)) return;

            prototype = currentPrototype;
            runner = currentRunner;
            installedLevelId = levelId;

            var events = NarrativeProductionContent.Build(level);
            PatchExistingTutorialLines(events, levelId);
            AddTutorialEvents(events, level);
            runner.LevelId = levelId;
            runner.ReplaceEvents(events);
            Debug.Log($"[TUTORIAL/NARRATIVE] Installed contextual onboarding; events={events.Count} level={levelId}.");
        }

        private static void PatchExistingTutorialLines(List<NarrativeEventDefinition> events, string levelId)
        {
            if (levelId.StartsWith("01_", StringComparison.OrdinalIgnoreCase))
            {
                SetFirstEffectText(events, "L1_HINT_START",
                    "Сначала сверю, какие показатели нужны в отчёте. Начальник прислал задачи в чат. До 18:00.");
            }

            if (levelId.StartsWith("02_", StringComparison.OrdinalIgnoreCase))
            {
                SetFirstEffectText(events, "L2_PRO_FIRST_REF",
                    "#REF!.. Красная клетка уже заражена. Выделить её и нажать DELETE — очаг будет уничтожен, пока он не расползся дальше.");
            }
        }

        private static void SetFirstEffectText(IEnumerable<NarrativeEventDefinition> events, string eventId, string text)
        {
            var definition = events.FirstOrDefault(item => item != null && item.id == eventId);
            var effect = definition?.effects?.FirstOrDefault();
            if (effect != null) effect.text = text;
        }

        private static void AddTutorialEvents(List<NarrativeEventDefinition> events, PrototypeLevelConfig level)
        {
            var id = level.Id ?? string.Empty;
            if (id.StartsWith("01_", StringComparison.OrdinalIgnoreCase)) AddL1(events, level);
            else if (id.StartsWith("02_", StringComparison.OrdinalIgnoreCase)) AddL2(events, level);
        }

        private static void AddL1(List<NarrativeEventDefinition> events, PrototypeLevelConfig level)
        {
            // Persistent chat tips carry secondary details; protagonist lines stay reserved for the rules
            // the player needs to internalize immediately.
            events.Add(Event("L1_TUTOR_MOVE_SELECT", level, NarrativeTriggerType.ActionNumber,
                Department("На всякий: обычный drag двигает данные. Shift+drag выделяет прямоугольный диапазон; если потянуть уже выделенный диапазон, он поедет целиком."), 1));

            events.Add(Event("L1_TUTOR_FORMULA_DROP", level, NarrativeTriggerType.ActionNumber,
                Protagonist("Формулы работают только через drop. Один ключ кидаю прямо в =SORT(), выделенный диапазон — прямо в =SUM()."), 2));

            events.Add(Event("L1_TUTOR_SORT", level, NarrativeTriggerType.ActionNumber,
                Department("SORT принимает один ключ. Ключ показателя собирает столбец, фамилия — строку. Под результат нужен свободный непрерывный участок, иначе будет #SPILL!."), 3));

            events.Add(Event("L1_TUTOR_SUM", level, NarrativeTriggerType.ActionNumber,
                Protagonist("SUM нужен диапазон хотя бы с двумя числами. Обычные пустые клетки он пропускает. Для «Премий ≥ 5» лишние числа лучше вынести MOVE'ом из диапазона, а не удалять."), 4));

            events.Add(Event("L1_TUTOR_REPORT", level, NarrativeTriggerType.ActionNumber,
                Boss("Результаты должны оказаться в зелёных полях отчёта по адресам из моего первого сообщения. Каждое успешное действие двигает рабочее время — дедлайн в 18:00."), 5));

            events.Add(Event("L1_TUTOR_FORMULA_REUSE", level, NarrativeTriggerType.ActionNumber,
                Department("Если формула занята ключом или результатом, сначала вынеси содержимое MOVE'ом. После этого пустую формулу можно перенести; DELETE формулы не уничтожает."), 6));

            events.Add(Event("L1_TUTOR_MOVE_DESTINATION", level, NarrativeTriggerType.ActionNumber,
                Protagonist("MOVE проверяет конечные клетки, а не путь: можно двигаться по диагонали и пересекать занятые места. Если что забуду — полные правила есть в справке «?»."), 7));
        }

        private static void AddL2(List<NarrativeEventDefinition> events, PrototypeLevelConfig level)
        {
            events.Add(Event("L2_TUTOR_REF_DELETE_DEPT", level, NarrativeTriggerType.FirstRefSpawn,
                Department("Красный #REF! лучше локализовать сразу: выдели заражённую клетку и жми DELETE. Потеряешь одну клетку, зато остановишь этот очаг."),
                delay: 2.2f));
        }

        private static NarrativeEventDefinition Event(
            string id,
            PrototypeLevelConfig level,
            NarrativeTriggerType trigger,
            NarrativeEffectDefinition effect,
            int number = 0,
            float delay = 0f)
        {
            return new NarrativeEventDefinition
            {
                id = id,
                levelId = level.Id,
                trigger = trigger,
                triggerNumber = number,
                once = true,
                delay = delay,
                effects = new List<NarrativeEffectDefinition> { effect }
            };
        }

        private static NarrativeEffectDefinition Department(string text) => new()
        {
            type = NarrativeEffectType.DepartmentChatMessage,
            text = text
        };

        private static NarrativeEffectDefinition Boss(string text) => new()
        {
            type = NarrativeEffectType.BossChatMessage,
            text = text
        };

        private static NarrativeEffectDefinition Protagonist(string text) => new()
        {
            type = NarrativeEffectType.ProtagonistLine,
            text = text,
            mood = ProtagonistMood.Normal,
            priority = 30,
            lifetime = new NarrativeLifetime
            {
                dismissMode = NarrativeDismissMode.TimedOrClick,
                duration = 3.8f
            }
        };
    }
}
