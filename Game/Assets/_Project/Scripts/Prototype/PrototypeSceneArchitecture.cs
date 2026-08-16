using ExcelHell.Application;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ExcelHell.Prototype
{
    public enum PrototypeSceneRole
    {
        Menu = 0,
        Gameplay = 1,
        Constructor = 2
    }

    public static class PrototypeAuthoringMode
    {
        public static bool Active { get; internal set; }
    }

    /// <summary>
    /// Scene-local entry point. It keeps direct scene launches useful in the editor and
    /// creates named runtime service objects instead of relying on one monolithic bootstrap.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class PrototypeSceneEntry : MonoBehaviour
    {
        [SerializeField] private PrototypeSceneRole role = PrototypeSceneRole.Gameplay;
        [SerializeField, Min(0)] private int startLevelIndex;

        private bool sceneTransitionRequested;

        public PrototypeSceneRole Role => role;
        public int StartLevelIndex => startLevelIndex;

        private void Awake()
        {
            sceneTransitionRequested = false;

            if (role == PrototypeSceneRole.Menu)
            {
                PrototypeAuthoringMode.Active = false;
                return;
            }

            // Direct scene launch in Unity should not be blocked by the application menu shell.
            if (ExcelHellApplication.ShellAvailable && !ExcelHellApplication.GameplayActive)
            {
                var app = FindFirstObjectByType<ExcelHellApplication>();
                if (app != null)
                {
                    app.gameObject.SetActive(false);
                    Destroy(app.gameObject);
                }

                var shellGuard = FindFirstObjectByType<PrototypeShellGuard>();
                if (shellGuard != null) shellGuard.enabled = false;
            }

            if (!ExcelHellApplication.ShellAvailable || !ExcelHellApplication.GameplayActive)
                PrototypeLevelRuntime.SetCurrentIndex(Mathf.Clamp(startLevelIndex, 0, PrototypeLevelCatalog.Count - 1));

            PrototypeAuthoringMode.Active = role == PrototypeSceneRole.Constructor;
            CreateGameplayRuntime();
        }

        private void Start()
        {
            if (role == PrototypeSceneRole.Menu)
            {
                DestroyIfPresent<ExcelHellPrototype>();
                DestroyIfPresent<PrototypeLevelDatasetAdapter>();
                DestroyIfPresent<PrototypeFormulaCells>();
                DestroyIfPresent<PrototypeFormulaLevelCompatibility>();
                DestroyIfPresent<PrototypeLevelFlow>();
                DestroyIfPresent<PrototypeRefTelegraphLayer>();
                DestroyIfPresent<PrototypeLevelConstructor>();
                DestroyIfPresent<PrototypeAuthoringGuard>();
                return;
            }

            if (role == PrototypeSceneRole.Constructor)
            {
                // Legacy runtime bootstraps may have spawned these before the scene entry ran.
                DestroyIfPresent<PrototypeLevelFlow>();
                DestroyIfPresent<PrototypeRefTelegraphLayer>();
                CreateService<PrototypeAuthoringGuard>("[AUTHORING] Gameplay Freeze");
                CreateService<PrototypeLevelConstructor>("[AUTHORING] Level Constructor");
            }
        }

        private void Update()
        {
#if UNITY_EDITOR
            if (role == PrototypeSceneRole.Gameplay && Keyboard.current != null && Keyboard.current.f2Key.wasPressedThisFrame)
            {
                sceneTransitionRequested = true;
                SceneManager.LoadScene("LevelConstructor");
            }
#endif
        }

        private void LateUpdate()
        {
            if (sceneTransitionRequested) return;

            if (role == PrototypeSceneRole.Menu && ExcelHellApplication.ShellAvailable && ExcelHellApplication.GameplayActive)
            {
                sceneTransitionRequested = true;
                SceneManager.LoadScene("Gameplay");
                return;
            }

            if (role == PrototypeSceneRole.Gameplay && ExcelHellApplication.ShellAvailable && !ExcelHellApplication.GameplayActive)
            {
                sceneTransitionRequested = true;
                SceneManager.LoadScene("Menu");
            }
        }

        private void OnDestroy()
        {
            if (role == PrototypeSceneRole.Constructor)
                PrototypeAuthoringMode.Active = false;
        }

        private void CreateGameplayRuntime()
        {
            CreateService<ExcelHellPrototype>("[GAMEPLAY] Worksheet Core");
            CreateService<PrototypeLevelDatasetAdapter>("[GAMEPLAY] Level Dataset");
            CreateService<PrototypeFormulaCells>("[GAMEPLAY] Formula Cells 2.0");
            CreateService<PrototypeFormulaLevelCompatibility>("[GAMEPLAY] Formula Compatibility");

            if (role == PrototypeSceneRole.Gameplay)
            {
                CreateService<PrototypeRefTelegraphLayer>("[GAMEPLAY] REF Telegraph");
                CreateService<PrototypeLevelFlow>("[GAMEPLAY] Level Flow");
            }
            else
            {
                CreateService<PrototypeAuthoringGuard>("[AUTHORING] Gameplay Freeze");
                CreateService<PrototypeLevelConstructor>("[AUTHORING] Level Constructor");
            }
        }

        private T CreateService<T>(string objectName) where T : MonoBehaviour
        {
            var existing = FindFirstObjectByType<T>();
            if (existing != null) return existing;

            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }

        private static void DestroyIfPresent<T>() where T : MonoBehaviour
        {
            var item = FindFirstObjectByType<T>();
            if (item != null) Destroy(item.gameObject);
        }
    }
}
