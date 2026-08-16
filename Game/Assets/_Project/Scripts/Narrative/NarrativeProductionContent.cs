using System;
using System.Collections.Generic;
using System.Linq;
using ExcelHell.Prototype;
using UnityEngine;

namespace ExcelHell.Narrative
{
    /// <summary>
    /// Canonical jam narrative schedule. This is deliberately data-only authoring on top of NarrativeLayer v1:
    /// no gameplay mutation, no bespoke level scripting, and no dependency on a particular UI renderer.
    /// </summary>
    public static class NarrativeProductionContent
    {
        public static List<NarrativeEventDefinition> Build(PrototypeLevelConfig level)
        {
            if (level == null) return new List<NarrativeEventDefinition>();
            var id = level.Id ?? string.Empty;
            if (id.StartsWith("01_", StringComparison.OrdinalIgnoreCase)) return BuildL1(level);
            if (id.StartsWith("02_", StringComparison.OrdinalIgnoreCase)) return BuildL2(level);
            if (id.StartsWith("03_", StringComparison.OrdinalIgnoreCase)) return BuildL3(level);
            if (id.StartsWith("04_", StringComparison.OrdinalIgnoreCase)) return BuildL4(level);
            return new List<NarrativeEventDefinition>();
        }

        private static List<NarrativeEventDefinition> BuildL1(PrototypeLevelConfig level)
        {
            return new List<NarrativeEventDefinition>
            {
                Event("L1_BOSS_START", level, NarrativeTriggerType.LevelStart,
                    Boss(StartBossMessage(level, "Доброе утро. Отчёт нужен сегодня. Дедлайн — 18:00. Следите за часами."))),
                Event("L1_DEPT_TEMPLATE", level, NarrativeTriggerType.ActionNumber,
                    Department("У кого-нибудь есть последняя версия шаблона?"), number: 1),
                Event("L1_DEPT_KETTLE", level, NarrativeTriggerType.ActionNumber,
                    Department("Я чайник включил, если кому надо."), number: 3),
                Event("L1_ALL_GOALS", level, NarrativeTriggerType.AllGoalsCompleted,
                    Boss("Когда закончите сверку, отправьте отчёт.")),
                Event("L1_HINT_START", level, NarrativeTriggerType.LevelStart,
                    Protagonist("Сначала сверю, какие показатели нужны в отчёте. До 18:00.", ProtagonistMood.Normal), delay: 1.2f)
            };
        }

        private static List<NarrativeEventDefinition> BuildL2(PrototypeLevelConfig level)
        {
            return new List<NarrativeEventDefinition>
            {
                Event("L2_BOSS_START", level, NarrativeTriggerType.LevelStart,
                    Boss(StartBossMessage(level, "Как продвигается отчёт? Дедлайн сегодня в 18:00."))),
                Event("L2_DEPT_MOROZOV", level, NarrativeTriggerType.ActionNumber,
                    Department("А Морозов сегодня будет?"), number: 1),
                Event("L2_DEPT_WROTE", level, NarrativeTriggerType.ActionNumber,
                    Department("Я ему написал, пока не отвечает."), number: 3),
                Event("L2_PRO_FIRST_REF", level, NarrativeTriggerType.FirstRefSpawn,
                    Protagonist("Стоп. Этого здесь точно не было.", ProtagonistMood.Alarmed), delay: 0.05f),
                Event("L2_CELL_SEEN", level, NarrativeTriggerType.FirstRefSpawn,
                    Cell("ТЫ ВИДЕЛ?"), delay: 0.8f),
                Event("L2_BOSS_FIRST_DESTROY", level, NarrativeTriggerType.CellDestroyed,
                    Boss("Проверьте, пожалуйста, что ничего не потерялось при сверке.")),
                Event("L2_DEPT_WHICH", level, NarrativeTriggerType.ActionNumber,
                    Department("Какого Морозова?"), number: 7),
                Event("L2_DEPT_STAPLER", level, NarrativeTriggerType.ActionNumber,
                    Department("Ладно, потом разберёмся. Кто забрал степлер?"), number: 9)
            };
        }

        private static List<NarrativeEventDefinition> BuildL3(PrototypeLevelConfig level)
        {
            return new List<NarrativeEventDefinition>
            {
                Event("L3_BOSS_START_TASKS", level, NarrativeTriggerType.LevelStart,
                    Boss(StartBossMessage(level, "На сегодня нужен ещё один отчёт."))),
                Event("L3_DEPT_WHERE", level, NarrativeTriggerType.ActionNumber,
                    Department("Я всё-таки не понимаю, где Морозов."), number: 1),
                Event("L3_CELL_HELP", level, NarrativeTriggerType.FirstRefSpawn,
                    Cell("ПОМОГИТЕ"), delay: 0.7f),
                Event("L3_DEPT_DESK", level, NarrativeTriggerType.ActionNumber,
                    Department("Он же сидит через два стола от меня."), number: 3),
                Event("L3_BOSS_PRESSURE", level, NarrativeTriggerType.ActionNumber,
                    Boss("Почему отчёт всё ещё не готов?"), number: 4),
                Event("L3_DEPT_NOBODY", level, NarrativeTriggerType.ActionNumber,
                    Department("Там никто не сидит."), number: 5),
                Event("L3_CELL_TYPED", level, NarrativeTriggerType.ActionNumber,
                    Cell("ТЫ ЭТО НЕ ПЕЧАТАЛ"), number: 6),
                Event("L3_DEPT_CHARGER", level, NarrativeTriggerType.ActionNumber,
                    Department("У кого есть зарядка Type-C?"), number: 7),
                Event("L3_BOSS_DESTROY", level, NarrativeTriggerType.CellDestroyed,
                    Boss("Не надо переделывать весь файл. Просто доведите отчёт до конца.")),
                Event("L3_DEPT_NO_MOROZOV", level, NarrativeTriggerType.ActionNumber,
                    Department("У нас нет Морозова."), number: 9),
                Event("L3_DEPT_EXISTS", level, NarrativeTriggerType.ActionNumber,
                    Department("Есть. Он в прошлом месяце мне смену закрывал."), number: 10),
                Event("L3_CELL_BLIND", level, NarrativeTriggerType.ActionNumber,
                    Cell("ОНИ НЕ ВИДЯТ"), number: 11),
                Event("L3_DEPT_NOT_FOUND", level, NarrativeTriggerType.ActionNumber,
                    Department("Не нашёл."), number: 12)
            };
        }

        private static List<NarrativeEventDefinition> BuildL4(PrototypeLevelConfig level)
        {
            return new List<NarrativeEventDefinition>
            {
                Event("L4_BOSS_START", level, NarrativeTriggerType.LevelStart,
                    Boss(StartBossMessage(level, "Статус?"))),
                Event("L4_DEPT_MAIL", level, NarrativeTriggerType.ActionNumber,
                    Department("Нашёл письмо от Морозова."), number: 2),
                Event("L4_CELL_REPEAT", level, NarrativeTriggerType.ActionNumber,
                    Cell("ТЫ УЖЕ ДЕЛАЛ ЭТО"), number: 3),
                Event("L4_DEPT_WHAT_MAIL", level, NarrativeTriggerType.ActionNumber,
                    Department("Какое ещё письмо?"), number: 4),
                Event("L4_SYSTEM_ONE_A", level, NarrativeTriggerType.ActionNumber,
                    SystemStatus("1 ПОЛЬЗОВАТЕЛЬ В ФАЙЛЕ", 2.8f), number: 5),
                Event("L4_DEPT_SENT", level, NarrativeTriggerType.ActionNumber,
                    Department("Обычное. Он мне таблицу присылал."), number: 6),
                Event("L4_SYSTEM_TWO", level, NarrativeTriggerType.ActionNumber,
                    SystemStatus("2 ПОЛЬЗОВАТЕЛЯ В ФАЙЛЕ", 2.8f), number: 7),
                Event("L4_BOSS_MID", level, NarrativeTriggerType.ActionNumber,
                    Boss("Мы не можем задерживать закрытие дня из-за одной таблицы."), number: 8),
                Event("L4_CELL_FORGET", level, NarrativeTriggerType.ActionNumber,
                    Cell("ОНИ НЕ ПОМНЯТ"), number: 9),
                Event("L4_SYSTEM_ONE_B", level, NarrativeTriggerType.ActionNumber,
                    SystemStatus("1 ПОЛЬЗОВАТЕЛЬ В ФАЙЛЕ", 2.8f), number: 10),
                Event("L4_DEPT_CANT_OPEN", level, NarrativeTriggerType.ActionNumber,
                    Department("Не могу открыть."), number: 10, delay: 0.7f),
                Event("L4_CELL_CLOCK", level, NarrativeTriggerType.ActionNumber,
                    Cell("ЧАСЫ ВРУТ"), number: 11),
                Event("L4_SYSTEM_PREVIOUS", level, NarrativeTriggerType.ActionNumber,
                    SystemStatus("НАЙДЕНА ПРЕДЫДУЩАЯ ВЕРСИЯ", 3.5f), number: 12),
                Event("L4_BOSS_CORRECT", level, NarrativeTriggerType.ActionNumber,
                    Boss("Не усложняйте. Нужны только правильные цифры."), number: 13),
                Event("L4_DEPT_SENDER", level, NarrativeTriggerType.ActionNumber,
                    Department("А отправитель кто?"), number: 14),
                Event("L4_DEPT_ELLIPSIS", level, NarrativeTriggerType.ActionNumber,
                    Department("..."), number: 15),
                Event("L4_PRO_MOROZOV", level, NarrativeTriggerType.ActionNumber,
                    Protagonist("Кто такой Морозов?..", ProtagonistMood.Psychotic), number: 15, delay: 1.0f),
                Event("L4_DEPT_HOME", level, NarrativeTriggerType.ActionNumber,
                    Department("Я ухожу. Всем до завтра."), number: 16),
                Event("L4_CELL_DONT_SEND", level, NarrativeTriggerType.AllGoalsCompleted,
                    Cell("НЕ ОТПРАВЛЯЙ")),
                Event("L4_PRO_DONT_SEND", level, NarrativeTriggerType.AllGoalsCompleted,
                    Protagonist("Просто отправить. И всё закончится... Да?", ProtagonistMood.Psychotic), delay: 0.4f),
                Event("L4_BOSS_FINAL", level, NarrativeTriggerType.LevelCompleted,
                    Boss("Спасибо. Получил."), delay: 0.5f)
            };
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

        private static NarrativeEffectDefinition Boss(string text) => new()
        {
            type = NarrativeEffectType.BossChatMessage,
            text = text
        };

        private static NarrativeEffectDefinition Department(string text) => new()
        {
            type = NarrativeEffectType.DepartmentChatMessage,
            text = text
        };

        private static NarrativeEffectDefinition Cell(string text) => new()
        {
            type = NarrativeEffectType.CellMessage,
            text = text,
            row = -1,
            column = -1,
            lifetime = new NarrativeLifetime
            {
                dismissMode = NarrativeDismissMode.OnClick,
                duration = 0f
            }
        };

        private static NarrativeEffectDefinition Protagonist(string text, ProtagonistMood mood) => new()
        {
            type = NarrativeEffectType.ProtagonistLine,
            text = text,
            mood = mood,
            lifetime = new NarrativeLifetime
            {
                dismissMode = NarrativeDismissMode.TimedOrClick,
                duration = 4.5f
            }
        };

        private static NarrativeEffectDefinition SystemStatus(string text, float seconds) => new()
        {
            type = NarrativeEffectType.SystemStatus,
            text = text,
            lifetime = NarrativeLifetime.Timed(seconds)
        };

        private static string StartBossMessage(PrototypeLevelConfig level, string opening)
        {
            var goals = level.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>();
            if (goals.Length == 0) return opening;
            var lines = goals.Select(goal => $"— {Address(goal.Row, goal.Column)}: {GoalLabel(goal.Goal)}");
            return $"{opening}\nНа сегодня заполните:\n{string.Join("\n", lines)}";
        }

        public static string GoalLabel(PrototypeReportGoals goal) => goal switch
        {
            PrototypeReportGoals.SalaryTotal => "общая зарплата",
            PrototypeReportGoals.OvertimeTotal => "сумма переработок",
            PrototypeReportGoals.BonusTotal => "сумма премий",
            PrototypeReportGoals.BonusAtLeastFour => "сумма премий не ниже 5",
            PrototypeReportGoals.SalaryOfMaxOvertime => "зарплата сотрудника с максимальной переработкой",
            PrototypeReportGoals.SalaryForHoursBelowForty => "зарплата сотрудников с часами ниже 40",
            _ => goal.ToString()
        };

        public static string ShortGoalLabel(PrototypeReportGoals goal) => goal switch
        {
            PrototypeReportGoals.SalaryTotal => "ЗАРПЛАТА",
            PrototypeReportGoals.OvertimeTotal => "ПЕРЕРАБОТКИ",
            PrototypeReportGoals.BonusTotal => "ПРЕМИИ",
            PrototypeReportGoals.BonusAtLeastFour => "ПРЕМИИ ≥ 5",
            PrototypeReportGoals.SalaryOfMaxOvertime => "ЗП / MAX ПЕРЕРАБ.",
            PrototypeReportGoals.SalaryForHoursBelowForty => "ЗП / ЧАСЫ < 40",
            _ => "ОТЧЁТ"
        };

        private static string Address(int row, int column) => $"{ExcelHellPrototype.ColumnName(column)}{row + 1}";
    }

    [DefaultExecutionOrder(1180)]
    public sealed class NarrativeProductionInstaller : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private string installedLevelId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<NarrativeProductionInstaller>() != null) return;
            var root = new GameObject("[NARRATIVE] Production Content");
            DontDestroyOnLoad(root);
            root.AddComponent<NarrativeProductionInstaller>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;
            var currentPrototype = FindFirstObjectByType<ExcelHellPrototype>();
            var currentRunner = FindFirstObjectByType<NarrativeEventRunner>();
            if (currentPrototype == null || currentRunner == null) return;

            var level = PrototypeLevelRuntime.Current;
            var levelId = level?.Id ?? "runtime";
            if (currentPrototype == prototype && currentRunner == runner && installedLevelId == levelId) return;

            prototype = currentPrototype;
            runner = currentRunner;
            installedLevelId = levelId;
            runner.LevelId = levelId;
            var events = NarrativeProductionContent.Build(level);
            runner.ReplaceEvents(events);
            Debug.Log($"[NARRATIVE/CONTENT] Installed {events.Count} authored event(s) for {levelId}.");
        }
    }
}
