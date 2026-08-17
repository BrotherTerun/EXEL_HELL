using System;
using System.Linq;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Release-only pacing layer for the L1 cold open. It deliberately leaves the frozen gameplay
    /// and the existing bookends implementation intact, but owns the timing of the introduction:
    /// ~45 seconds unskipped, one click advances one beat, and the worksheet remains masked until
    /// the boss has had time to "send" the predecessor's file.
    /// </summary>
    [DefaultExecutionOrder(1175)]
    public sealed class PrototypeL1IntroPacingPatch : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;

        private static readonly FieldInfo BookendsIntroCompleteField =
            typeof(PrototypeNarrativeBookendsReleasePatch).GetField("introComplete", Flags);
        private static readonly FieldInfo BookendsPredecessorSentField =
            typeof(PrototypeNarrativeBookendsReleasePatch).GetField("predecessorSent", Flags);
        private static readonly FieldInfo BookendsBoundAtField =
            typeof(PrototypeNarrativeBookendsReleasePatch).GetField("boundAt", Flags);
        private static readonly MethodInfo BookendsClearIntroMasksMethod =
            typeof(PrototypeNarrativeBookendsReleasePatch).GetMethod("ClearIntroMasks", Flags);

        private static readonly FieldInfo GuidedBoundAtField =
            typeof(PrototypeGuidedOnboarding).GetField("boundAt", Flags);

        private static readonly FieldInfo HudToastRoutineField =
            typeof(PrototypeProductionHud).GetField("toastRoutine", Flags);
        private static readonly FieldInfo HudToastRootField =
            typeof(PrototypeProductionHud).GetField("toastRoot", Flags);
        private static readonly MethodInfo HudHideToastMethod =
            typeof(PrototypeProductionHud).GetMethod("HideToast", Flags);

        private ExcelHellPrototype prototype;
        private PrototypeNarrativeBookendsReleasePatch bookends;
        private PrototypeProductionHud hud;
        private PrototypeGuidedOnboarding guided;
        private GameObject clickCatcher;

        private int stage;
        private float stageStartedAt;
        private float observedBookendsBoundAt = float.NaN;
        private bool sequenceComplete;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeL1IntroPacingPatch>() != null) return;
            var root = new GameObject("[RELEASE] L1 Intro Pacing");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeL1IntroPacingPatch>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || PrototypeLevelRuntime.CurrentIndex != 0)
            {
                DestroyClickCatcher();
                return;
            }

            bookends ??= FindFirstObjectByType<PrototypeNarrativeBookendsReleasePatch>();
            hud ??= FindFirstObjectByType<PrototypeProductionHud>();
            guided ??= FindFirstObjectByType<PrototypeGuidedOnboarding>();

            DetectSameObjectRestart();
            SuppressLegacyFastIntro();

            if (sequenceComplete) return;

            if (guided != null && guided.enabled)
                guided.enabled = false;

            EnsureClickCatcher();

            // Wait until the production HUD has actually created its toast surface. This keeps the first
            // authored line from being stored in history without ever appearing on screen on a slow first frame.
            if (hud == null || HudToastRootField?.GetValue(hud) == null) return;

            if (Time.unscaledTime - stageStartedAt >= StageDuration(stage))
                AdvanceBeat(false);
        }

        private void LateUpdate()
        {
            if (prototype == null || PrototypeLevelRuntime.CurrentIndex != 0 || sequenceComplete) return;
            if (clickCatcher != null)
                clickCatcher.transform.SetAsLastSibling();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            DestroyClickCatcher();
            prototype = owner;
            bookends = null;
            hud = null;
            guided = null;
            observedBookendsBoundAt = float.NaN;
            ResetSequence();
        }

        private void ResetSequence()
        {
            stage = 0;
            stageStartedAt = Time.unscaledTime;
            sequenceComplete = false;
            if (prototype != null && PrototypeLevelRuntime.CurrentIndex == 0)
                EnsureClickCatcher();
            Debug.Log("[BOOKENDS/L1/PACING] Intro reset; target unskipped duration = 44.8s.");
        }

        private void DetectSameObjectRestart()
        {
            if (bookends == null || BookendsBoundAtField == null) return;
            if (BookendsBoundAtField.GetValue(bookends) is not float value) return;

            if (float.IsNaN(observedBookendsBoundAt))
            {
                observedBookendsBoundAt = value;
                return;
            }

            if (Mathf.Abs(value - observedBookendsBoundAt) < 0.01f) return;
            observedBookendsBoundAt = value;
            ResetSequence();
        }

        private void SuppressLegacyFastIntro()
        {
            if (bookends == null) return;
            BookendsIntroCompleteField?.SetValue(bookends, true);
        }

        private static float StageDuration(int value) => value switch
        {
            0 => 0.8f,  // establish the workspace before the first notification
            1 => 12.0f, // welcome remains readable before the next message
            2 => 14.0f, // predecessor message; table is still deliberately empty
            3 => 4.0f,  // visible hand-off pause after the table finally appears
            4 => 14.0f, // tasks remain readable before control is handed to onboarding
            _ => float.PositiveInfinity
        };

        private void AdvanceBeat(bool skippedByClick)
        {
            if (sequenceComplete || prototype == null || PrototypeLevelRuntime.CurrentIndex != 0) return;

            switch (stage)
            {
                case 0:
                    SendBoss("L1_INTRO_WELCOME",
                        "Добро пожаловать. С ребятами ты уже познакомился — это твоё рабочее место. Осваивайся.");
                    break;

                case 1:
                    SendBoss("L1_INTRO_PREDECESSOR",
                        "Твой предшественник не закончил сверку. Я сейчас пришлю, на чём он остановился. Ничего сложного: приведи данные в порядок и сдай отчёт до 18:00.");
                    break;

                case 2:
                    RevealWorksheet();
                    break;

                case 3:
                    SendBoss("L1_INTRO_TASKS", BuildTaskMessage());
                    break;

                case 4:
                    CompleteIntro();
                    return;
            }

            stage++;
            stageStartedAt = Time.unscaledTime;
            Debug.Log($"[BOOKENDS/L1/PACING] beat={stage}/5 source={(skippedByClick ? "click" : "timer")}");
        }

        private void RevealWorksheet()
        {
            if (bookends != null)
            {
                BookendsPredecessorSentField?.SetValue(bookends, true);
                BookendsClearIntroMasksMethod?.Invoke(bookends, null);
            }

            Debug.Log("[BOOKENDS/L1/PACING] Predecessor file received; worksheet revealed after the hand-off pause.");
        }

        private void CompleteIntro()
        {
            sequenceComplete = true;
            HidePinnedToast();
            DestroyClickCatcher();

            guided ??= FindFirstObjectByType<PrototypeGuidedOnboarding>();
            if (guided != null)
            {
                guided.enabled = true;
                if (GuidedBoundAtField != null)
                    GuidedBoundAtField.SetValue(guided, Time.unscaledTime - 1.70f);
            }

            Debug.Log("[BOOKENDS/L1/PACING] Intro complete; player control released to guided onboarding.");
        }

        private void EnsureClickCatcher()
        {
            if (clickCatcher != null || prototype == null) return;
            var canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
            if (canvas == null) return;

            clickCatcher = new GameObject("L1 Intro Click Catcher", typeof(RectTransform), typeof(Image), typeof(Button));
            clickCatcher.transform.SetParent(canvas.transform, false);

            var rect = clickCatcher.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;

            var image = clickCatcher.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0f);
            image.raycastTarget = true;

            var button = clickCatcher.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => AdvanceBeat(true));
            clickCatcher.transform.SetAsLastSibling();
        }

        private void DestroyClickCatcher()
        {
            if (clickCatcher != null) Destroy(clickCatcher);
            clickCatcher = null;
        }

        private void SendBoss(string eventId, string text)
        {
            if (hud == null || string.IsNullOrWhiteSpace(text)) return;
            var effect = new NarrativeEffectDefinition
            {
                type = NarrativeEffectType.BossChatMessage,
                text = text
            };
            hud.Receive(new NarrativeEffectTicket(new NarrativeEffectRequest(eventId, effect)));
            PinCurrentToast();
        }

        private void PinCurrentToast()
        {
            if (hud == null || HudToastRoutineField == null) return;
            if (HudToastRoutineField.GetValue(hud) is Coroutine routine)
            {
                hud.StopCoroutine(routine);
                HudToastRoutineField.SetValue(hud, null);
            }
        }

        private void HidePinnedToast()
        {
            if (hud != null)
                HudHideToastMethod?.Invoke(hud, null);
        }

        private static string BuildTaskMessage()
        {
            var level = PrototypeLevelRuntime.Current;
            var goals = level?.GoalLayout ?? Array.Empty<PrototypeReportGoalPlacement>();
            if (goals.Length == 0)
                return "Вот на чём он остановился. Проверь файл и закончи сверку.";

            var lines = goals.Select(goal =>
                $"— {ExcelHellPrototype.ColumnName(goal.Column)}{goal.Row + 1}: {NarrativeProductionContent.GoalLabel(goal.Goal)}");
            return $"Вот на чём он остановился.\nНа сегодня заполните:\n{string.Join("\n", lines)}";
        }
    }

    /// <summary>
    /// Guided onboarding intentionally keeps its highlight canvas top-most while it is active. A deadline or
    /// completion can occur during an unexpected tutorial step, so this very-late release guard clears those
    /// frames after every tutorial pass and then keeps the result modal on the top presentation layer.
    /// </summary>
    [DefaultExecutionOrder(2800)]
    public sealed class PrototypeCompletionTutorialLayerGuard : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo FinishedField =
            typeof(ExcelHellPrototype).GetField("finished", Flags);
        private static readonly MethodInfo GuidedHideFramesMethod =
            typeof(PrototypeGuidedOnboarding).GetMethod("HideFrames", Flags);
        private static readonly MethodInfo ContextualClearHighlightsMethod =
            typeof(PrototypeContextualTutorial).GetMethod("ClearHighlights", Flags);
        private static readonly FieldInfo HudCompletionModalField =
            typeof(PrototypeProductionHud).GetField("completionModal", Flags);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeCompletionTutorialLayerGuard>() != null) return;
            var root = new GameObject("[RELEASE] Completion Tutorial Layer Guard");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeCompletionTutorialLayerGuard>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;
            var prototype = FindFirstObjectByType<ExcelHellPrototype>();
            if (prototype == null || FinishedField?.GetValue(prototype) is not bool finished || !finished) return;

            var guided = FindFirstObjectByType<PrototypeGuidedOnboarding>();
            if (guided != null)
                GuidedHideFramesMethod?.Invoke(guided, null);

            var contextual = FindFirstObjectByType<PrototypeContextualTutorial>();
            if (contextual != null)
                ContextualClearHighlightsMethod?.Invoke(contextual, null);

            var hud = FindFirstObjectByType<PrototypeProductionHud>();
            if (hud == null) return;
            if (HudCompletionModalField?.GetValue(hud) is GameObject modal && modal.activeSelf)
                modal.transform.SetAsLastSibling();
        }
    }
}
