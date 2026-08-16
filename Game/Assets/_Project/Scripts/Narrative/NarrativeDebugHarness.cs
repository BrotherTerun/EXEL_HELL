using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ExcelHell.Narrative
{
    /// <summary>
    /// Minimal v1 smoke harness. It installs synthetic events temporarily and verifies matching,
    /// once-only behaviour, delayed dispatch, queue order and effect routing without requiring final art.
    /// Synthetic events are removed after the test so they cannot match real gameplay triggers.
    /// </summary>
    public sealed class NarrativeDebugHarness : MonoBehaviour
    {
        [SerializeField] private bool installSampleEvents = true;
        [SerializeField] private bool runAutomaticSmokeTest = true;

        private NarrativeEventRunner runner;
        private bool running;

        private void Start()
        {
            runner = GetComponent<NarrativeEventRunner>();
            if (runner == null) return;
            if (runAutomaticSmokeTest) StartCoroutine(RunSmokeTestAfterStartup());
        }

        private IEnumerator RunSmokeTestAfterStartup()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            yield return RunSmokeTestRoutine();
        }

        [ContextMenu("Narrative/Run Smoke Test")]
        public void RunSmokeTest()
        {
            if (!running) StartCoroutine(RunSmokeTestRoutine());
        }

        private IEnumerator RunSmokeTestRoutine()
        {
            if (running) yield break;
            running = true;

            if (runner == null) runner = GetComponent<NarrativeEventRunner>();
            if (runner == null)
            {
                Debug.LogError("[NARRATIVE/SELF-TEST] FAIL — NarrativeEventRunner missing.");
                running = false;
                yield break;
            }

            if (!installSampleEvents)
            {
                Debug.LogWarning("[NARRATIVE/SELF-TEST] SKIP — sample events disabled.");
                running = false;
                yield break;
            }

            runner.ReplaceEvents(BuildSampleEvents());
            runner.ResetDiagnostics();
            Debug.Log("[NARRATIVE/TEST] BEGIN synthetic smoke test.");

            runner.FireDebug(NarrativeTriggerType.ManualDebug, 1, "smoke.one");
            runner.FireDebug(NarrativeTriggerType.ManualDebug, 1, "smoke.duplicate");
            runner.FireDebug(NarrativeTriggerType.ActionNumber, 3, "smoke.action3");

            // UI branch routes ProtagonistLine through the real presenter (2s lifetime), so keep headroom.
            var timeout = Time.realtimeSinceStartup + 5f;
            while (!runner.IsIdle && Time.realtimeSinceStartup < timeout)
                yield return null;

            var pass = runner.MatchCount == 2 &&
                       runner.OnceSkipCount == 1 &&
                       runner.DispatchedEffectCount == 3 &&
                       runner.MissingReceiverCount == 0 &&
                       runner.IsIdle;

            var summary =
                $"matches={runner.MatchCount}/2, onceSkips={runner.OnceSkipCount}/1, " +
                $"effects={runner.DispatchedEffectCount}/3, missingReceivers={runner.MissingReceiverCount}/0, " +
                $"pendingDispatches={runner.PendingDispatchCount}, idle={runner.IsIdle}";

            if (pass)
                Debug.Log($"[NARRATIVE/SELF-TEST] PASS — {summary}.");
            else
                Debug.LogError($"[NARRATIVE/SELF-TEST] FAIL — {summary}.");

            runner.ReplaceEvents(System.Array.Empty<NarrativeEventDefinition>());
            Debug.Log("[NARRATIVE/TEST] Synthetic events cleared; real gameplay observation only.");
            running = false;
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
            root.AddComponent<NarrativeGameplayProbe>();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var receiver = root.AddComponent<DebugNarrativeReceiver>();
            root.AddComponent<NarrativeDebugHarness>();
            runner.RegisterReceiver(receiver);
            Debug.Log("[NARRATIVE] Runtime bootstrap complete. Debug receiver/harness active; visual renderer may override fallback routing.");
#else
            Debug.Log("[NARRATIVE] Runtime bootstrap complete. Production mode; debug harness disabled.");
#endif

            Object.DontDestroyOnLoad(root);
        }
    }
}
