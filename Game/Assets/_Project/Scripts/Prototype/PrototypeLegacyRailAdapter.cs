using System.Linq;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Temporary bridge while the legacy report/control block still owns production callbacks.
    /// Scales the whole block rather than rewriting child geometry, so every existing Button keeps its hit target.
    /// This file is expected to disappear after the visual redesign replaces the legacy sidebar skin.
    /// </summary>
    [DefaultExecutionOrder(1850)]
    public sealed class PrototypeLegacyRailAdapter : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private RectTransform sidebar;
        private bool applied;

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
                applied = false;
            }

            if (prototype == null || applied) return;
            sidebar ??= prototype.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.gameObject.name == "Sidebar");
            if (sidebar == null) return;

            // Preserve the original 650x760 coordinate system used by all child controls.
            sidebar.anchorMin = sidebar.anchorMax = new Vector2(0f, 1f);
            sidebar.pivot = new Vector2(0f, 1f);
            sidebar.anchoredPosition = new Vector2(1044f, -278f);
            sidebar.sizeDelta = new Vector2(650f, 760f);
            sidebar.localScale = new Vector3(0.74f, 0.68f, 1f);

            applied = true;
            Debug.Log("[UI-SHELL] Legacy report/control rail fitted without rebinding gameplay callbacks.");
        }
    }
}
