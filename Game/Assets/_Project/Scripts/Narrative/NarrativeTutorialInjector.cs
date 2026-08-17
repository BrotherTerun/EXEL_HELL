using System;
using System.Linq;
using ExcelHell.Prototype;
using UnityEngine;

namespace ExcelHell.Narrative
{
    /// <summary>
    /// Final L1 narrative filter. Guided onboarding now owns the protagonist's opening instruction, so the
    /// older authored L1_HINT_START must be removed before LevelStart dispatch to avoid two bubbles replacing
    /// each other on the same beat.
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
            var root = new GameObject("[NARRATIVE] Tutorial Filter");
            DontDestroyOnLoad(root);
            root.AddComponent<NarrativeTutorialInjector>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var currentPrototype = FindFirstObjectByType<ExcelHellPrototype>();
            var currentRunner = FindFirstObjectByType<NarrativeEventRunner>();
            var level = PrototypeLevelRuntime.Current;
            var levelId = level?.Id ?? string.Empty;
            if (currentPrototype == null || currentRunner == null || level == null) return;

            if (currentPrototype == prototype && currentRunner == runner &&
                string.Equals(installedLevelId, levelId, StringComparison.OrdinalIgnoreCase)) return;

            prototype = currentPrototype;
            runner = currentRunner;
            installedLevelId = levelId;

            if (!levelId.StartsWith("01_", StringComparison.OrdinalIgnoreCase)) return;

            var events = NarrativeProductionContent.Build(level)
                .Where(definition => definition != null && definition.id != "L1_HINT_START")
                .ToList();
            runner.LevelId = levelId;
            runner.ReplaceEvents(events);
            Debug.Log($"[TUTORIAL/NARRATIVE] Suppressed duplicate L1_HINT_START; events={events.Count}.");
        }
    }
}
