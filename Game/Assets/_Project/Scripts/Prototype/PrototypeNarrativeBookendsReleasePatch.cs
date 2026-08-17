using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Final release bookends around the frozen gameplay field.
    /// Presentation/state glue only: L1 diegetic hand-off, unread badge ordering,
    /// deadline feedback and the authored L4 post-submit calm.
    /// </summary>
    [DefaultExecutionOrder(1185)]
    public sealed class PrototypeNarrativeBookendsReleasePatch : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo ViewsField = typeof(ExcelHellPrototype).GetField("views", Flags);
        private static readonly FieldInfo FinishedField = typeof(ExcelHellPrototype).GetField("finished", Flags);
        private static readonly FieldInfo StatusTextField = typeof(ExcelHellPrototype).GetField("statusText", Flags);
        private static readonly MethodInfo RefreshAllMethod = typeof(ExcelHellPrototype).GetMethod("RefreshAll", Flags);
        private static readonly MethodInfo ResetPrototypeMethod = typeof(ExcelHellPrototype).GetMethod("ResetPrototype", Flags);

        private static readonly FieldInfo RunnerEventsField = typeof(NarrativeEventRunner).GetField("events", Flags);
        private static readonly FieldInfo ProbeLevelCompletedField = typeof(NarrativeGameplayProbe).GetField("levelCompletedPublished", Flags);

        private static readonly FieldInfo HudChatReservedField = typeof(PrototypeProductionHud).GetField("chatReserved", Flags);
        private static readonly FieldInfo HudChatBadgeField = typeof(PrototypeProductionHud).GetField("chatBadge", Flags);
        private static readonly FieldInfo HudCompletionModalField = typeof(PrototypeProductionHud).GetField("completionModal", Flags);
        private static readonly FieldInfo HudCompletionTitleField = typeof(PrototypeProductionHud).GetField("completionTitle", Flags);
        private static readonly FieldInfo HudCompletionBodyField = typeof(PrototypeProductionHud).GetField("completionBody", Flags);
        private static readonly FieldInfo HudCompletionButtonField = typeof(PrototypeProductionHud).GetField("completionButton", Flags);
        private static readonly FieldInfo HudCompletionButtonTextField = typeof(PrototypeProductionHud).GetField("completionButtonText", Flags);
        private static readonly FieldInfo HudTasksReservedField = typeof(PrototypeProductionHud).GetField("tasksReserved", Flags);

        private static readonly FieldInfo GuidedBoundAtField = typeof(PrototypeGuidedOnboarding).GetField("boundAt", Flags);
        private static readonly FieldInfo ProtagonistActiveTicketField = typeof(PrototypeProtagonistPresenter).GetField("activeTicket", Flags);
        private static readonly FieldInfo ProtagonistBubbleTextField = typeof(PrototypeProtagonistPresenter).GetField("bubbleText", Flags);
        private static readonly FieldInfo ProtagonistImageField = typeof(PrototypeProtagonistPresenter).GetField("protagonistImage", Flags);
        private static readonly FieldInfo ProtagonistTiredFramesField = typeof(PrototypeProtagonistPresenter).GetField("tiredFrames", Flags);

        private static readonly Color IntroMaskColor = new(0.075f, 0.095f, 0.11f, 1f);
        private static readonly Color FailureRed = new(0.95f, 0.07f, 0.10f, 1f);
        private static readonly Color FailureMagenta = new(0.97f, 0.11f, 0.95f, 1f);
        private static readonly Color Gold = new(1.0f, 0.69f, 0.18f, 1f);

        private ExcelHellPrototype prototype;
        private PrototypeProductionHud hud;
        private NarrativeEventRunner runner;
        private NarrativeGameplayProbe probe;
        private PrototypeGuidedOnboarding guided;
        private float boundAt;

        private bool l1EventsPruned;
        private bool welcomeSent;
        private bool predecessorSent;
        private bool tasksSent;
        private bool introComplete;
        private readonly List<GameObject> introMasks = new();

        private bool submitReady;
        private GameObject submitPulse;

        private bool failureActive;
        private float failureStartedAt;
        private Vector2 failureTitleBase;
        private bool failureButtonBound;

        private bool finalSequenceActive;
        private float finalSequenceStartedAt;
        private bool finalCellSent;
        private bool finalCalmApplied;
        private Coroutine finalTiredRoutine;
        private PrototypeProtagonistPresenter mutedProtagonist;
        private readonly List<MonoBehaviour> mutedBehaviours = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeNarrativeBookendsReleasePatch>() != null) return;
            var root = new GameObject("[RELEASE] Narrative Bookends");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeNarrativeBookendsReleasePatch>();
        }

        private void OnEnable() => NarrativeSignals.Triggered += OnNarrativeTrigger;
        private void OnDisable() => NarrativeSignals.Triggered -= OnNarrativeTrigger;

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null) return;

            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            runner ??= FindFirstObjectByType<NarrativeEventRunner>();
            probe ??= FindFirstObjectByType<NarrativeGameplayProbe>();

            PruneLegacyL1StartEvents();
            RunL1Intro();
            KeepGuidedIntroSuppressed();
            DetectDeadlineFailure();
        }

        private void LateUpdate()
        {
            if (prototype == null) return;
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();

            KeepIntroMaskOnTop();
            FixBadgeLayer();
            RewriteFirstGuidedLine();
            DrawSubmitReadyPulse();
            PresentFailure();
            PresentFinalSequence();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            RestoreMutedPresentation();
            ClearIntroMasks();
            DestroySubmitPulse();
            if (finalTiredRoutine != null)
            {
                StopCoroutine(finalTiredRoutine);
                finalTiredRoutine = null;
            }

            prototype = owner;
            hud = null;
            runner = null;
            probe = null;
            guided = null;
            boundAt = Time.unscaledTime;
            ResetLocalState();

            if (prototype != null && IsL1())
                BuildIntroMasks();
        }

        private void ResetLocalState()
        {
            l1EventsPruned = false;
            welcomeSent = false;
            predecessorSent = false;
            tasksSent = false;
            introComplete = false;
            submitReady = false;
            failureActive = false;
            failureButtonBound = false;
            finalSequenceActive = false;
            finalCellSent = false;
            finalCalmApplied = false;
        }

        private bool IsL1() => PrototypeLevelRuntime.CurrentIndex == 0;
        private bool IsL4Final() => PrototypeLevelRuntime.IsLast &&
            (PrototypeLevelRuntime.Current?.Id ?? string.Empty).StartsWith("04_", StringComparison.OrdinalIgnoreCase);

        private void PruneLegacyL1StartEvents()
        {
            if (!IsL1() || l1EventsPruned || runner == null) return;
            if (RunnerEventsField?.GetValue(runner) is not List<NarrativeEventDefinition> events || events.Count == 0) return;

            var removed = events.RemoveAll(definition => definition != null &&
                (string.Equals(definition.id, "L1_BOSS_START", StringComparison.Ordinal) ||
                 string.Equals(definition.id, "L1_HINT_START", StringComparison.Ordinal)));
            l1EventsPruned = true;
            if (removed > 0)
                Debug.Log($"[BOOKENDS/L1] Replaced {removed} legacy LevelStart event(s) with authored intro.");
        }

        private void RunL1Intro()
        {
            if (!IsL1() || introComplete) return;
            var elapsed = Time.unscaledTime - boundAt;
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            if (hud == null) return;

            if (!predecessorSent && introMasks.Count == 0)
                BuildIntroMasks();

            if (!welcomeSent && elapsed >= 0.20f)
            {
                welcomeSent = true;
                SendBoss("L1_INTRO_WELCOME",
                    "Добро пожаловать. С ребятами ты уже познакомился — это твоё рабочее место. Осваивайся.");
            }

            if (!predecessorSent && elapsed >= 2.10f)
            {
                predecessorSent = true;
                SendBoss("L1_INTRO_PREDECESSOR",
                    "Твой предшественник не закончил сверку. Я сейчас пришлю, на чём он остановился. Ничего сложного: приведи данные в порядок и сдай отчёт до 18:00.");
                ClearIntroMasks();
                Debug.Log("[BOOKENDS/L1] Worksheet revealed with predecessor hand-off.");
            }

            if (!tasksSent && elapsed >= 4.10f)
            {
                tasksSent = true;
                SendBoss("L1_INTRO_TASKS", BuildTaskMessage());
            }

            if (elapsed >= 4.85f)
            {
                introComplete = true;
                RestoreGuidedOnboarding(true);
                Debug.Log("[BOOKENDS/L1] Intro complete; guided onboarding released.");
            }
        }

        private void KeepGuidedIntroSuppressed()
        {
            if (!IsL1() || introComplete) return;
            guided ??= FindFirstObjectByType<PrototypeGuidedOnboarding>();
            if (guided != null && guided.enabled) guided.enabled = false;
        }

        private void RestoreGuidedOnboarding(bool announceImmediately)
        {
            guided ??= FindFirstObjectByType<PrototypeGuidedOnboarding>();
            if (guided == null) return;
            guided.enabled = true;
            if (announceImmediately && GuidedBoundAtField != null)
                GuidedBoundAtField.SetValue(guided, Time.unscaledTime - 1.70f);
        }

        private void BuildIntroMasks()
        {
            if (prototype == null || predecessorSent || introMasks.Count > 0) return;
            if (ViewsField?.GetValue(prototype) is not ExcelHellCellView[,] views) return;

            foreach (var view in views)
            {
                if (view == null) continue;
                var mask = new GameObject("L1 Empty Cell Mask", typeof(RectTransform), typeof(Image), typeof(IntroInputBlocker));
                mask.transform.SetParent(view.transform, false);
                var rect = mask.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(1.5f, 1.5f);
                rect.offsetMax = new Vector2(-1.5f, -1.5f);
                var image = mask.GetComponent<Image>();
                image.color = IntroMaskColor;
                image.raycastTarget = true;
                mask.transform.SetAsLastSibling();
                introMasks.Add(mask);
            }
        }

        private void KeepIntroMaskOnTop()
        {
            if (!IsL1() || predecessorSent) return;
            if (introMasks.Count == 0) BuildIntroMasks();
            foreach (var mask in introMasks)
                if (mask != null) mask.transform.SetAsLastSibling();
        }

        private void ClearIntroMasks()
        {
            foreach (var mask in introMasks)
                if (mask != null) Destroy(mask);
            introMasks.Clear();
        }

        private string BuildTaskMessage()
        {
            var level = PrototypeLevelRuntime.Current;
            var goals = level?.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>();
            if (goals.Length == 0) return "Вот на чём он остановился. Проверь файл и закончи сверку.";
            var lines = goals.Select(goal =>
                $"— {ExcelHellPrototype.ColumnName(goal.Column)}{goal.Row + 1}: {NarrativeProductionContent.GoalLabel(goal.Goal)}");
            return $"Вот на чём он остановился.\nНа сегодня заполните:\n{string.Join("\n", lines)}";
        }

        private void SendBoss(string eventId, string text)
        {
            if (hud == null || string.IsNullOrWhiteSpace(text)) return;
            var effect = new NarrativeEffectDefinition { type = NarrativeEffectType.BossChatMessage, text = text };
            hud.Receive(new NarrativeEffectTicket(new NarrativeEffectRequest(eventId, effect)));
        }

        private void RewriteFirstGuidedLine()
        {
            if (!IsL1() || !introComplete) return;
            var presenter = FindFirstObjectByType<PrototypeProtagonistPresenter>();
            if (presenter == null) return;
            var ticket = ProtagonistActiveTicketField?.GetValue(presenter) as NarrativeEffectTicket;
            if (ticket == null || !string.Equals(ticket.Request.EventId, "guided.l1.0", StringComparison.Ordinal)) return;

            const string text = "Так. Сначала сверю, что именно ему нужно. Задачи должны быть в чате.";
            if (ticket.Request.Effect != null) ticket.Request.Effect.text = text;
            if (ProtagonistBubbleTextField?.GetValue(presenter) is Text bubbleText && bubbleText.text != text)
                bubbleText.text = text;
        }

        private void DetectDeadlineFailure()
        {
            if (failureActive || finalSequenceActive || !IsFinished() || IsReportAccepted()) return;
            // Redundant guard for old probe instances during hot reload; the probe itself now also requires
            // accepted status before publishing LevelCompleted.
            if (probe != null && ProbeLevelCompletedField != null)
                ProbeLevelCompletedField.SetValue(probe, true);

            failureActive = true;
            failureStartedAt = Time.unscaledTime;
            submitReady = false;
            DestroySubmitPulse();
            Debug.Log($"[BOOKENDS/FAIL] Deadline failure detected on day {PrototypeLevelRuntime.CurrentIndex + 1}.");
        }

        private bool IsFinished() => FinishedField?.GetValue(prototype) is bool value && value;

        private bool IsReportAccepted()
        {
            if (StatusTextField?.GetValue(prototype) is not Text status) return false;
            var text = status.text ?? string.Empty;
            return text.IndexOf("ОТЧЁТ ПРИНЯТ", StringComparison.OrdinalIgnoreCase) >= 0 ||
                   text.IndexOf("REPORT ACCEPTED", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void OnNarrativeTrigger(NarrativeTrigger trigger)
        {
            if (prototype == null) return;
            if (trigger.Type == NarrativeTriggerType.AllGoalsCompleted)
                submitReady = true;

            if (trigger.Type == NarrativeTriggerType.LevelCompleted && IsL4Final() && !finalSequenceActive)
            {
                finalSequenceActive = true;
                finalSequenceStartedAt = Time.unscaledTime;
                submitReady = false;
                DestroySubmitPulse();
                Debug.Log("[BOOKENDS/FINAL] Accepted final report; table gets the last word.");
            }
        }

        private void FixBadgeLayer()
        {
            if (hud == null) return;
            var reserved = HudChatReservedField?.GetValue(hud) as RectTransform;
            var badge = HudChatBadgeField?.GetValue(hud) as GameObject;
            if (reserved == null || badge == null || badge.transform.parent != reserved) return;

            badge.transform.SetAsLastSibling();
            var rect = badge.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = new Vector2(37f, 2f);
            if (badge.GetComponent<Image>() is { } image) image.raycastTarget = false;
        }

        private void DrawSubmitReadyPulse()
        {
            if (!submitReady || IsFinished())
            {
                DestroySubmitPulse();
                return;
            }
            if (hud == null) return;
            var reserved = HudTasksReservedField?.GetValue(hud) as RectTransform;
            if (reserved == null) return;

            if (submitPulse == null)
            {
                submitPulse = new GameObject("Submit Ready Pulse", typeof(RectTransform), typeof(Image), typeof(Outline));
                submitPulse.transform.SetParent(reserved, false);
                var rect = submitPulse.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = new Vector2(-3f, -3f);
                rect.offsetMax = new Vector2(3f, 3f);
                var image = submitPulse.GetComponent<Image>();
                image.raycastTarget = false;
                image.color = new Color(Gold.r, Gold.g, Gold.b, 0.035f);
                var outline = submitPulse.GetComponent<Outline>();
                outline.effectDistance = new Vector2(2f, -2f);
                outline.useGraphicAlpha = false;
            }

            submitPulse.transform.SetAsLastSibling();
            var pulse = 0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 5.5f);
            if (submitPulse.GetComponent<Outline>() is { } o)
                o.effectColor = new Color(Gold.r, Gold.g, Gold.b, Mathf.Lerp(0.35f, 0.95f, pulse));
        }

        private void DestroySubmitPulse()
        {
            if (submitPulse != null) Destroy(submitPulse);
            submitPulse = null;
        }

        private void PresentFailure()
        {
            if (!failureActive || hud == null) return;
            if (!TryCompletionUi(out var modal, out var title, out var body, out var button, out var buttonText)) return;

            modal.SetActive(true);
            modal.transform.SetAsLastSibling();
            if (!failureButtonBound)
            {
                failureButtonBound = true;
                button.onClick.AddListener(RestartCurrentLevel);
                failureTitleBase = title.rectTransform.anchoredPosition;
            }

            var day = Mathf.Clamp(PrototypeLevelRuntime.CurrentIndex + 1, 1, 4);
            var copy = FailureCopy(day);
            title.text = copy.title;
            var elapsed = Time.unscaledTime - failureStartedAt;
            var charCount = Mathf.Clamp(Mathf.FloorToInt(elapsed / 0.055f), 0, copy.body.Length);
            body.text = copy.body.Substring(0, charCount);
            buttonText.text = "ЕЩЁ РАЗ";
            button.gameObject.SetActive(charCount >= copy.body.Length);

            var glitchPhase = Mathf.FloorToInt(Time.unscaledTime * 11f);
            title.color = (glitchPhase & 1) == 0 ? FailureRed : FailureMagenta;
            title.rectTransform.anchoredPosition = failureTitleBase +
                                                   new Vector2((glitchPhase % 3) - 1, ((glitchPhase / 2) % 3) - 1);
        }

        private static (string title, string body) FailureCopy(int day) => day switch
        {
            1 => ("ДЕДЛАЙН ПРОПУЩЕН", "Отчёт не принят.\nРабочий день завершён.\nПопробуйте ещё раз."),
            2 => ("ДЕДЛАЙН ПРОПУЩЕН", "Ты опоздал.\nФайл уже закрыт.\nПопробуй ещё раз."),
            3 => ("ТЫ ОПОЗДАЛ", "Он уже был здесь.\n\nПопробуй ещё раз."),
            _ => ("ТЫ ОПОЗДАЛ", "Он придёт за тобой тоже.\n\nПопробуй ещё раз.")
        };

        private void RestartCurrentLevel()
        {
            if (hud != null && HudCompletionButtonField?.GetValue(hud) is Button button)
                button.onClick.RemoveListener(RestartCurrentLevel);

            failureActive = false;
            failureButtonBound = false;
            RestoreMutedPresentation();
            ResetPrototypeMethod?.Invoke(prototype, null);
            boundAt = Time.unscaledTime;
            ResetLocalState();
            hud = null;
            runner = null;
            probe = null;
            guided = null;
            if (IsL1()) BuildIntroMasks();
        }

        private void PresentFinalSequence()
        {
            if (!finalSequenceActive || hud == null) return;
            if (!TryCompletionUi(out var modal, out var title, out var body, out var button, out var buttonText)) return;

            var elapsed = Time.unscaledTime - finalSequenceStartedAt;
            if (!finalCalmApplied) modal.SetActive(false);

            if (!finalCellSent && elapsed >= 1.15f)
            {
                finalCellSent = true;
                SendFinalCellMessage();
            }

            // Boss acknowledgement arrives at +0.5s. The final cell appears at +1.15s,
            // types for roughly four seconds, then remains fully readable for another three.
            if (!finalCalmApplied && elapsed >= 8.20f)
            {
                finalCalmApplied = true;
                CalmAllPresentationNoise();
                modal.SetActive(true);
                modal.transform.SetAsLastSibling();
                Debug.Log("[BOOKENDS/FINAL] Interface went quiet; protagonist returned to Tired.");
            }

            if (!finalCalmApplied) return;
            modal.SetActive(true);
            modal.transform.SetAsLastSibling();
            title.text = "СМЕНА ЗАВЕРШЕНА";
            title.color = Color.white;
            body.text = "Отчёт принят.\nСмена закрыта. До завтра.";
            buttonText.text = "В МЕНЮ";
            button.gameObject.SetActive(true);
        }

        private void SendFinalCellMessage()
        {
            var presenter = FindFirstObjectByType<PrototypeNarrativePresentation>();
            if (presenter == null) return;
            var effect = new NarrativeEffectDefinition
            {
                type = NarrativeEffectType.CellMessage,
                text = "ТЫ ОБ ЭТОМ ПОЖАЛЕЕШЬ",
                lifetime = new NarrativeLifetime { dismissMode = NarrativeDismissMode.OnClick, duration = 0f }
            };
            presenter.Receive(new NarrativeEffectTicket(new NarrativeEffectRequest("L4_CELL_LAST_WORD", effect)));
        }

        private bool TryCompletionUi(out GameObject modal, out Text title, out Text body, out Button button, out Text buttonText)
        {
            modal = HudCompletionModalField?.GetValue(hud) as GameObject;
            title = HudCompletionTitleField?.GetValue(hud) as Text;
            body = HudCompletionBodyField?.GetValue(hud) as Text;
            button = HudCompletionButtonField?.GetValue(hud) as Button;
            buttonText = HudCompletionButtonTextField?.GetValue(hud) as Text;
            return modal != null && title != null && body != null && button != null && buttonText != null;
        }

        private void CalmAllPresentationNoise()
        {
            mutedBehaviours.Clear();
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (behaviour == null || behaviour == this || !behaviour.enabled) continue;
                var typeName = behaviour.GetType().Name;
                var noisy = typeName.IndexOf("Psychosis", StringComparison.Ordinal) >= 0 ||
                            typeName.IndexOf("Glitch", StringComparison.Ordinal) >= 0 ||
                            string.Equals(typeName, "PrototypeRefTelegraphLayer", StringComparison.Ordinal) ||
                            string.Equals(typeName, "PrototypeChatFinalPolish", StringComparison.Ordinal);
                if (!noisy) continue;
                behaviour.StopAllCoroutines();
                behaviour.enabled = false;
                mutedBehaviours.Add(behaviour);
            }

            foreach (var rect in FindObjectsByType<RectTransform>(FindObjectsSortMode.None))
            {
                if (rect == null) continue;
                var name = rect.gameObject.name ?? string.Empty;
                if (name.StartsWith("Psychosis ", StringComparison.Ordinal) ||
                    name.StartsWith("Chat Glitch", StringComparison.Ordinal) ||
                    name.StartsWith("REF Telegraph", StringComparison.Ordinal) ||
                    name.StartsWith("Cell Message ", StringComparison.Ordinal))
                    Destroy(rect.gameObject);
            }

            RefreshAllMethod?.Invoke(prototype, null);

            var protagonist = FindFirstObjectByType<PrototypeProtagonistPresenter>();
            if (protagonist == null) return;
            protagonist.SetMood(ProtagonistMood.Tired);
            var image = ProtagonistImageField?.GetValue(protagonist) as Image;
            var frames = ProtagonistTiredFramesField?.GetValue(protagonist) as Sprite[];
            protagonist.enabled = false;
            mutedProtagonist = protagonist;
            if (image != null && frames != null && frames.Length > 0)
                finalTiredRoutine = StartCoroutine(AnimateFinalTired(image, frames));
        }

        private void RestoreMutedPresentation()
        {
            foreach (var behaviour in mutedBehaviours)
                if (behaviour != null) behaviour.enabled = true;
            mutedBehaviours.Clear();

            if (mutedProtagonist != null)
                mutedProtagonist.enabled = true;
            mutedProtagonist = null;
        }

        private IEnumerator AnimateFinalTired(Image image, Sprite[] frames)
        {
            var sequence = new[] { 0, 0, 1, 0, 2, 2, 0, 3 };
            var cursor = 0;
            while (finalSequenceActive && image != null && frames != null && frames.Length > 0)
            {
                var index = Mathf.Clamp(sequence[cursor % sequence.Length], 0, frames.Length - 1);
                image.sprite = frames[index];
                image.enabled = image.sprite != null;
                cursor++;
                yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.75f, 1.45f));
            }
        }

        private sealed class IntroInputBlocker : MonoBehaviour,
            IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
            IBeginDragHandler, IDragHandler, IEndDragHandler
        {
            public void OnPointerDown(PointerEventData eventData) => eventData.Use();
            public void OnPointerUp(PointerEventData eventData) => eventData.Use();
            public void OnPointerClick(PointerEventData eventData) => eventData.Use();
            public void OnBeginDrag(PointerEventData eventData) => eventData.Use();
            public void OnDrag(PointerEventData eventData) => eventData.Use();
            public void OnEndDrag(PointerEventData eventData) => eventData.Use();
        }
    }
}
