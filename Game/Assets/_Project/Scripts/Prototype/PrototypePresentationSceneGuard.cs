using ExcelHell.Narrative;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Runtime presentation services are intentionally persistent across Menu/Gameplay transitions,
    /// but they must never attach themselves to the LevelConstructor worksheet. Scene architecture
    /// sets PrototypeAuthoringMode.Active during Awake, before AfterSceneLoad runtime initializers run.
    /// </summary>
    public static class PrototypePresentationSceneGuard
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void GuardAuthoringScene()
        {
            if (!PrototypeAuthoringMode.Active) return;

            DestroyService<PrototypeFinalUiShell>();
            DestroyService<PrototypeLegacyRailAdapter>();
            DestroyService<PrototypeProductionHud>();
            DestroyService<PrototypeProtagonistPresenter>();
            DestroyService<NarrativeEventRunner>();

            Debug.Log("[UI-SHELL] Authoring mode detected; production presentation/narrative services removed from LevelConstructor.");
        }

        private static void DestroyService<T>() where T : MonoBehaviour
        {
            var service = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (service != null) Object.Destroy(service.gameObject);
        }
    }
}
