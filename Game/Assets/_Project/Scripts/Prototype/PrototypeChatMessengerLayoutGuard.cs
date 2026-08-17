using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Small companion guard for the runtime-built messenger list. It makes the VerticalLayoutGroup respect
    /// the preferred heights of message rows/day dividers, preventing long bubbles from overlapping.
    /// </summary>
    [DefaultExecutionOrder(2190)]
    public sealed class PrototypeChatMessengerLayoutGuard : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeChatMessengerLayoutGuard>() != null) return;
            var root = new GameObject("[PRESENTATION] Chat Messenger Layout Guard");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeChatMessengerLayoutGuard>();
        }

        private void LateUpdate()
        {
            var content = GameObject.Find("Messenger Content");
            if (content == null) return;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout == null || layout.childControlHeight) return;

            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            LayoutRebuilder.MarkLayoutForRebuild(content.GetComponent<RectTransform>());
            Debug.Log("[CHAT/UI] Messenger layout guard enabled preferred-height rows.");
        }
    }
}
