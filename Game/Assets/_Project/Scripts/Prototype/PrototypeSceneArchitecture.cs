using System.Reflection;
using ExcelHell.Application;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Scene-local bootstrap for production scenes. The scene context owns level selection,
    /// worksheet creation and first-frame authored layout application.
    /// </summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class PrototypeSceneArchitecture : MonoBehaviour
    {
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;
        private static readonly MethodInfo ApplyLevelMethod = typeof(PrototypeLevelDatasetAdapter).GetMethod("Apply", StaticPrivate);

        [SerializeField] private PrototypeSceneRole role = PrototypeSceneRole.Gameplay;
        [SerializeField, Min(0)] private int startLevelIndex;

        private bool sceneTransitionRequested;

        public PrototypeSceneRole Role => role;
        public int StartLevelIndex => startLevelIndex;

        private void Awake()
        {
            sceneTransitionRequested = false;
            EnsureMainCamera();

            if (role == PrototypeSceneRole.Menu)
            {
                PrototypeAuthoringMode.Active = false;
                return;
            }

            // Direct scene launch in Unity should not be blocked by the persistent menu shell.
            if (ExcelHellApplication.ShellAvailable && !ExcelHellApplication.GameplayActive)
            {
                var app = FindFirstObjectByType<ExcelHellApplication>(FindObjectsInactive.Include);
                if (app != null)
                {
                    app.gameObject.SetActive(false);
                    Destroy(app.gameObject);
                }

                var shellGuard = FindFirstObjectByType<PrototypeShellGuard>(FindObjectsInactive.Include);
                if (shellGuard != null) shellGuard.enabled = false;
            }

            // Menu-driven NEW GAME / CONTINUE / LOAD has already selected the proper index.
            // A scene launched directly from the editor falls back to its serialized start index.
            if (!ExcelHellApplication.ShellAvailable || !ExcelHellApplication.GameplayActive)
                PrototypeLevelRuntime.SetCurrentIndex(Mathf.Clamp(startLevelIndex, 0, PrototypeLevelCatalog.Count - 1));

            PrototypeAuthoringMode.Active = role == PrototypeSceneRole.Constructor;
            InitializeGameplayRuntime();
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
                DestroyIfPresent<PrototypeContextualTutorial>();
                return;
            }

            // Legacy tutorial is never part of FC2 production scenes. Removing it in Start means
            // an old AfterSceneLoad bootstrap cannot draw even one OnGUI frame.
            DestroyIfPresent<PrototypeContextualTutorial>();

            if (role == PrototypeSceneRole.Constructor)
            {
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

            // Kept as a defensive fallback for old menu calls. Current NEW GAME / CONTINUE / LOAD
            // already load Gameplay directly from ExcelHellApplication.StartGameplay().
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

        private void InitializeGameplayRuntime()
        {
            // Scene-authored Worksheet Core exists before Awake ordering reaches ExcelHellPrototype.
            // Disable it while services are prepared, then activate and immediately overwrite its
            // legacy seed model with the authored level before Unity can render the first frame.
            var worksheet = FindFirstObjectByType<ExcelHellPrototype>(FindObjectsInactive.Include);
            if (worksheet != null && worksheet.gameObject.activeSelf)
                worksheet.gameObject.SetActive(false);

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

            if (worksheet == null)
            {
                var core = new GameObject("[GAMEPLAY] Worksheet Core");
                worksheet = core.AddComponent<ExcelHellPrototype>();
            }
            else
            {
                worksheet.gameObject.SetActive(true); // ExcelHellPrototype.Awake runs here.
            }

            // The adapter normally applies in LateUpdate. Production scenes apply it now as well,
            // before first render, eliminating the one-frame legacy worksheet/tutorial flash.
            ApplyLevelMethod?.Invoke(null, new object[] { worksheet, PrototypeLevelRuntime.Current });
        }

        private static void EnsureMainCamera()
        {
            if (FindFirstObjectByType<Camera>() != null) return;

            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.orthographic = true;
            camera.orthographicSize = 5f;
        }

        private T CreateService<T>(string objectName) where T : MonoBehaviour
        {
            var existing = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (existing != null) return existing;

            var child = new GameObject(objectName);
            child.transform.SetParent(transform, false);
            return child.AddComponent<T>();
        }

        private static void DestroyIfPresent<T>() where T : MonoBehaviour
        {
            var item = FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (item != null) Destroy(item.gameObject);
        }
    }

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
}
