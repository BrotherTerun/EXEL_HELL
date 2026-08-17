using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Narrow release patch for the already-working guided onboarding.
    /// Keeps the main state machine intact while smoothing the authored L1 route:
    /// explain the intentional first SORT spill, split the dense mid-tutorial line,
    /// shorten the final submit prompt, keep protagonist copy above tutorial frames,
    /// and add three recovery actions to L1.
    /// </summary>
    [DefaultExecutionOrder(2490)]
    public sealed class PrototypeGuidedOnboardingReleasePatch : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo GuidedStepField = typeof(PrototypeGuidedOnboarding).GetField("step", Flags);
        private static readonly MethodInfo GuidedHideFramesMethod = typeof(PrototypeGuidedOnboarding).GetMethod("HideFrames", Flags);
        private static readonly MethodInfo GuidedHighlightMethod = typeof(PrototypeGuidedOnboarding).GetMethod("Highlight", Flags);

        private static readonly FieldInfo CellsField = typeof(ExcelHellPrototype).GetField("cells", Flags);
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);

        private static readonly FieldInfo ActiveTicketField = typeof(PrototypeProtagonistPresenter).GetField("activeTicket", Flags);
        private static readonly FieldInfo BubbleField = typeof(PrototypeProtagonistPresenter).GetField("bubble", Flags);
        private static readonly FieldInfo BubbleTextField = typeof(PrototypeProtagonistPresenter).GetField("bubbleText", Flags);

        private static readonly Color Gold = new(1.0f, 0.69f, 0.18f, 1f);

        private const string Step4FirstText =
            "Первая сумма готова. SORT умеет и фамилии: ключ сотрудника собирает его данные в строку. Если формула занята, содержимое сначала нужно вынести MOVE'ом.";

        private const string Step4SecondText =
            "Пустую формулу можно переносить, а DELETE её не уничтожает. Теперь соберу «Премию»: перетащу её ключ во вторую подсвеченную =SORT().";

        private const string FinalSubmitText =
            "Обе клетки отчёта готовы. Осталось нажать «ОТПРАВИТЬ ОТЧЁТ». Если что-то забуду — справка под «?».";

        private ExcelHellPrototype prototype;
        private PrototypeGuidedOnboarding guided;
        private PrototypeProtagonistPresenter protagonist;
        private CellModel[,] cells;
        private ExcelHellCellView[,] views;

        private bool turnBufferApplied;
        private bool salarySpillExplained;
        private bool salaryRetryAnnounced;
        private bool step4FirstSeen;
        private bool step4ContinuationShown;
        private string lastRewrittenEvent;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeGuidedOnboardingReleasePatch>() != null) return;
            var root = new GameObject("[PRESENTATION] Guided Onboarding Release Patch");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeGuidedOnboardingReleasePatch>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;

            var currentPrototype = FindFirstObjectByType<ExcelHellPrototype>();
            if (currentPrototype != prototype) Bind(currentPrototype);
            if (!IsL1()) return;

            ApplyTurnBuffer();

            guided ??= FindFirstObjectByType<PrototypeGuidedOnboarding>();
            protagonist ??= FindFirstObjectByType<PrototypeProtagonistPresenter>();
            if (guided == null || protagonist == null || cells == null || views == null) return;

            var step = CurrentStep();
            RewriteDenseGuidedLines();
            HandleSalarySortSpill(step);
            HandleStepFourContinuation(step);
            OverrideSpillHighlights(step);
            RaiseProtagonistBubble();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            guided = null;
            protagonist = null;
            cells = prototype == null ? null : CellsField?.GetValue(prototype) as CellModel[,];
            views = prototype == null ? null : ViewsField?.GetValue(prototype) as ExcelHellCellView[,];

            turnBufferApplied = false;
            salarySpillExplained = false;
            salaryRetryAnnounced = false;
            step4FirstSeen = false;
            step4ContinuationShown = false;
            lastRewrittenEvent = null;
        }

        private bool IsL1()
        {
            var level = PrototypeLevelRuntime.Current;
            return prototype != null && PrototypeLevelRuntime.CurrentIndex == 0 && level != null;
        }

        private void ApplyTurnBuffer()
        {
            if (turnBufferApplied) return;
            var level = PrototypeLevelRuntime.Current;
            if (level == null) return;

            if (level.MaxTurns < 11)
            {
                level.MaxTurns = 11;
                RefreshAllMethod?.Invoke(prototype, null);
                Debug.Log("[TUTORIAL/PATCH] L1 action budget raised to 11 (+3 recovery actions).");
            }
            turnBufferApplied = true;
        }

        private int CurrentStep() => GuidedStepField?.GetValue(guided) is int value ? value : -1;

        private NarrativeEffectTicket ActiveTicket() =>
            protagonist == null ? null : ActiveTicketField?.GetValue(protagonist) as NarrativeEffectTicket;

        private string ActiveEventId() => ActiveTicket()?.Request.EventId ?? string.Empty;

        private void RewriteDenseGuidedLines()
        {
            var ticket = ActiveTicket();
            if (ticket == null) return;

            var eventId = ticket.Request.EventId ?? string.Empty;
            if (eventId == "guided.l1.4")
            {
                step4FirstSeen = true;
                SetActiveText(ticket, Step4FirstText);
            }
            else if (eventId == "guided.l1.8")
            {
                SetActiveText(ticket, FinalSubmitText);
            }
        }

        private void SetActiveText(NarrativeEffectTicket ticket, string text)
        {
            if (ticket == null || string.IsNullOrWhiteSpace(text)) return;
            if (ticket.Request.Effect != null)
                ticket.Request.Effect.text = text;

            if (BubbleTextField?.GetValue(protagonist) is Text bubbleText && bubbleText.text != text)
                bubbleText.text = text;

            var eventId = ticket.Request.EventId ?? string.Empty;
            if (lastRewrittenEvent != eventId)
            {
                lastRewrittenEvent = eventId;
                Debug.Log($"[TUTORIAL/PATCH] Rewritten {eventId}: \"{text}\"");
            }
        }

        private void HandleSalarySortSpill(int step)
        {
            if (step != 1) return;

            if (!salarySpillExplained && CurrentStatus() == "#SPILL!")
            {
                salarySpillExplained = true;
                ShowLine(
                    "guided.l1.salary.spill",
                    "Не вышло: SORT не может развернуть столбец, пока под формулой стоят два чужих числа. Перетащу мешающие клетки по одной в свободные места, затем повторю SORT.");
                Debug.Log("[TUTORIAL/PATCH] Explained intentional first SORT #SPILL! and highlighted blockers.");
                return;
            }

            if (!salarySpillExplained || salaryRetryAnnounced || SalaryFieldIsAssembled()) return;
            if (SalarySortBlockers().Any()) return;
            if (ActiveTicket() != null) return;

            salaryRetryAnnounced = true;
            ShowLine(
                "guided.l1.salary.retry",
                "Теперь столбец свободен. Ещё раз: перетаскиваю «Зарплату» в =SORT().");
        }

        private string CurrentStatus() =>
            StatusTextField?.GetValue(prototype) is Text status ? status.text : string.Empty;

        private IEnumerable<CellModel> SalarySortBlockers()
        {
            if (cells == null || cells.GetLength(0) < 7 || cells.GetLength(1) < 4) yield break;

            // L1 authored route: D2 SORT expands vertically into D3:D7.
            for (var row = 2; row <= 6; row++)
            {
                var cell = cells[row, 3];
                if (cell == null) continue;
                if (cell.State != CellState.Normal || cell.IsFormula)
                {
                    yield return cell;
                    continue;
                }

                var occupant = cell.Occupant;
                if (occupant != null && !string.Equals(occupant.FieldId, "salary", StringComparison.Ordinal))
                    yield return cell;
            }
        }

        private bool SalaryFieldIsAssembled()
        {
            if (cells == null || cells.GetLength(0) < 7 || cells.GetLength(1) < 4) return false;
            var formula = cells[1, 3];
            if (formula?.Occupant?.Kind != ContentKind.FieldKey || formula.Occupant.FieldId != "salary") return false;

            for (var row = 2; row <= 6; row++)
                if (cells[row, 3]?.Occupant?.FieldId != "salary") return false;
            return true;
        }

        private void OverrideSpillHighlights(int step)
        {
            if (step != 1 || !salarySpillExplained || guided == null) return;
            var blockers = SalarySortBlockers().ToList();
            if (blockers.Count == 0) return;

            GuidedHideFramesMethod?.Invoke(guided, null);
            foreach (var blocker in blockers)
            {
                var view = View(blocker);
                var rect = view == null ? null : view.GetComponent<RectTransform>();
                if (rect != null)
                    GuidedHighlightMethod?.Invoke(guided, new object[] { rect, Gold, 5f });
            }
        }

        private ExcelHellCellView View(CellModel cell) =>
            cell == null || views == null ? null : views[cell.Row, cell.Column];

        private void HandleStepFourContinuation(int step)
        {
            if (step != 4 || !step4FirstSeen || step4ContinuationShown) return;
            if (ActiveTicket() != null) return;

            step4ContinuationShown = true;
            ShowLine("guided.l1.4b", Step4SecondText);
            Debug.Log("[TUTORIAL/PATCH] Step 4 continuation shown after first line dismissal.");
        }

        private void ShowLine(string eventId, string text)
        {
            if (protagonist == null) return;

            var effect = new NarrativeEffectDefinition
            {
                type = NarrativeEffectType.ProtagonistLine,
                text = text,
                mood = ProtagonistMood.Normal,
                priority = 55,
                lifetime = new NarrativeLifetime
                {
                    dismissMode = NarrativeDismissMode.TimedOrClick,
                    duration = 22f
                }
            };
            protagonist.Receive(new NarrativeEffectTicket(new NarrativeEffectRequest(eventId, effect)));
            lastRewrittenEvent = null;
        }

        private void RaiseProtagonistBubble()
        {
            if (protagonist == null) return;
            if (BubbleField?.GetValue(protagonist) is not GameObject bubble || !bubble.activeSelf) return;
            bubble.transform.SetAsLastSibling();
        }
    }
}
