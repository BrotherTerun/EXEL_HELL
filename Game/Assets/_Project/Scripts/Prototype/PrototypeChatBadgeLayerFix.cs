using System.Linq;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Keeps the unread marker rendered above the custom chat icon.
    /// Presentation-only sibling-order fix.
    /// </summary>
    [DefaultExecutionOrder(2290)]
    public sealed class PrototypeChatBadgeLayerFix : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeChatBadgeLayerFix>() != null) return;
            var root = new GameObject("[PRESENTATION] Chat Badge Layer Fix");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeChatBadgeLayerFix>();
        }

        private void LateUpdate()
        {
            if (PrototypeAuthoringMode.Active) return;
            var hud = FindFirstObjectByType<PrototypeProductionHud>();
            if (hud == null) return;

            var badge = hud.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect != null && rect.gameObject.name == "Chat Badge");
            if (badge == null) return;

            badge.SetAsLastSibling();
        }
    }
}
