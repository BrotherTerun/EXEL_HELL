using System.Collections.Generic;
using UnityEngine;

namespace ExcelHell.Narrative
{
    /// <summary>
    /// Minimal v1 smoke harness. It installs synthetic events and lets Play Mode prove matching,
    /// once-only behaviour, delayed dispatch and effect routing without any visual renderer.
    /// </summary>
    public sealed class NarrativeDebugHarness : MonoBehaviour
    {
        [SerializeField] private bool installSampleEvents = true;
        [SerializeField] private bool runAutomaticSmokeTest = true;

        private NarrativeEventRunner runner;

        private void Start()
        {
            runner = GetComponent<NarrativeEventRunner>();
            if (runner == null) return;

            if (installSampleEvents) runner.ReplaceEvents(BuildSampleEvents());
            if (runAutomaticSmokeTest) Invoke(nameof(RunSmokeTest), 0.25f);
        }

        [ContextMenu("Narrative/Run Smoke Test")]
        public void RunSmokeTest()
        {
            if (runner == null) runner = GetComponent<NarrativeEventRunner>();
            if (runner == null) return;

            Debug.Log("[NARRATIVE/TEST] BEGIN. Expect MATCH/EFFECT/RECEIVER logs and one SKIP for duplicate once-event.");
            runner.FireDebug(NarrativeTriggerType.ManualDebug, 1, "smoke.one");
            runner.FireDebug(NarrativeTriggerType.ManualDebug, 1, "smoke.duplicate");
            runner.FireDebug(NarrativeTriggerType.ActionNumber, 3, "smoke.action3");
            Debug.Log("[NARRATIVE/TEST] END dispatch requested. Delayed effects may follow.");
        }

        private static IEnumerable<NarrativeEventDefinition> BuildSampleEvents()
        {
            return new[]
            {
                new NarrativeEventDefinition
                {
                    id = "debug_once_protagonist",
                    trigger = NarrativeTriggerType.ManualDebug,
                    once = true,
                    effects = new List<NarrativeEffectDefinition>
                    {
                        new()
                        {
                            type = NarrativeEffectType.ProtagonistLine,
                            text = "NarrativeLayer v1 online.",
                            mood = ProtagonistMood.Normal,
                            lifetime = new NarrativeLifetime
                            {
                                dismissMode = NarrativeDismissMode.TimedOrClick,
                                duration = 2f
                            }
                        }
                    }
                },
                new NarrativeEventDefinition
                {
                    id = "debug_action_three",
                    trigger = NarrativeTriggerType.ActionNumber,
                    triggerNumber = 3,
                    once = true,
                    delay = 0.1f,
                    effects = new List<NarrativeEffectDefinition>
                    {
                        new()
                        {
                            type = NarrativeEffectType.CellMessage,
                            text = "ПОМОГИТЕ",
                            row = 2,
                            column = 2,
                            lifetime = new NarrativeLifetime
                            {
                                dismissMode = NarrativeDismissMode.OnClick,
                                duration = 0f
                            }
                        },
                        new()
                        {
                            type = NarrativeEffectType.PsychosisDelta,
                            intValue = 1
                        }
                    }
                }
            };
        }
    }

    public static class NarrativeRuntimeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Object.FindFirstObjectByType<NarrativeEventRunner>() != null) return;

            var root = new GameObject("EXEL HELL NarrativeLayer v1");
            var runner = root.AddComponent<NarrativeEventRunner>();
            var receiver = root.AddComponent<DebugNarrativeReceiver>();
            root.AddComponent<NarrativeGameplayProbe>();
            root.AddComponent<NarrativeDebugHarness>();
            runner.RegisterReceiver(receiver);
            Object.DontDestroyOnLoad(root);

            Debug.Log("[NARRATIVE] Runtime bootstrap complete. Debug receiver active; renderer not attached.");
        }
    }
}
