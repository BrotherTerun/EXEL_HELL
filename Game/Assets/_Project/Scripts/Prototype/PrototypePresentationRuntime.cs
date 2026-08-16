using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Owns production presentation services across scene transitions. Services are kept alive so a direct
    /// LevelConstructor start can later enter Gameplay without relying on RuntimeInitialize being invoked again.
    /// Authoring mode disables presentation components before their Update/LateUpdate work can attach to the
    /// constructor worksheet.
    /// </summary>
    [DefaultExecutionOrder(1700)]
    public sealed class PrototypePresentationRuntime : MonoBehaviour
    {
        private PrototypeFinalUiShell shell;
        private PrototypeLegacyRailAdapter rail;
        private PrototypeProductionHud hud;
        private PrototypeProtagonistPresenter protagonist;
        private bool lastAuthoring;
        private bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var existing = FindFirstObjectByType<PrototypePresentationRuntime>(FindObjectsInactive.Include);
            if (existing != null) return;

            var root = new GameObject("[PRESENTATION] Runtime");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypePresentationRuntime>();
        }

        private void Awake()
        {
            shell = Ensure<PrototypeFinalUiShell>();
            rail = Ensure<PrototypeLegacyRailAdapter>();
            hud = Ensure<PrototypeProductionHud>();
            protagonist = Ensure<PrototypeProtagonistPresenter>();
            initialized = true;
            ApplyMode(force: true);
        }

        private void Update() => ApplyMode(force: false);

        private void ApplyMode(bool force)
        {
            if (!initialized) return;
            var authoring = PrototypeAuthoringMode.Active;
            if (!force && authoring == lastAuthoring) return;
            lastAuthoring = authoring;

            SetEnabled(shell, !authoring);
            SetEnabled(rail, !authoring);
            SetEnabled(hud, !authoring);
            SetEnabled(protagonist, !authoring);

            Debug.Log(authoring
                ? "[UI-SHELL] Presentation runtime paused for LevelConstructor."
                : "[UI-SHELL] Presentation runtime enabled for production scenes.");
        }

        private T Ensure<T>() where T : MonoBehaviour
        {
            var existing = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            return existing != null ? existing : gameObject.AddComponent<T>();
        }

        private static void SetEnabled(Behaviour behaviour, bool value)
        {
            if (behaviour != null && behaviour.enabled != value) behaviour.enabled = value;
        }
    }
}
