using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Keeps the old report/control sidebar available as a developer overlay without exposing it in production UI.
    /// F3 toggles it in Editor/Development Build; release builds keep it hidden permanently.
    /// </summary>
    [DefaultExecutionOrder(1850)]
    public sealed class PrototypeLegacyRailAdapter : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private RectTransform sidebar;
        private bool visible;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeLegacyRailAdapter>() != null) return;
            var root = new GameObject("[PRESENTATION] Legacy Rail Adapter");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeLegacyRailAdapter>();
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
            {
                prototype = current;
                sidebar = null;
                visible = false;
            }

            if (prototype == null) return;
            sidebar ??= prototype.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.gameObject.name == "Sidebar");
            if (sidebar == null) return;

            PrepareOverlayGeometry();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (Keyboard.current != null && Keyboard.current.f3Key.wasPressedThisFrame)
            {
                visible = !visible;
                ApplyVisibility();
                Debug.Log(visible
                    ? "[UI-DEBUG] Legacy report/control overlay shown (F3)."
                    : "[UI-DEBUG] Legacy report/control overlay hidden (F3).");
            }
#else
            visible = false;
#endif
            if (sidebar.gameObject.activeSelf != visible) ApplyVisibility();
        }

        private void PrepareOverlayGeometry()
        {
            sidebar.anchorMin = sidebar.anchorMax = new Vector2(0f, 1f);
            sidebar.pivot = new Vector2(0f, 1f);
            sidebar.anchoredPosition = new Vector2(830f, -84f);
            sidebar.sizeDelta = new Vector2(650f, 760f);
            sidebar.localScale = new Vector3(0.92f, 0.92f, 1f);
            sidebar.SetAsLastSibling();
        }

        private void ApplyVisibility()
        {
            if (sidebar != null) sidebar.gameObject.SetActive(visible);
        }

        private void OnDisable()
        {
            visible = false;
            if (sidebar != null) sidebar.gameObject.SetActive(false);
        }
    }
}
