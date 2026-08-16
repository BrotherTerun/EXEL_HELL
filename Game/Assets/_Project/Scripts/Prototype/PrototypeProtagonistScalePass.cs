using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Late visual-only sizing pass. The protagonist presenter owns mood/lifetime;
    /// this component only makes the approved sprite occupy the authored office workstation region.
    /// </summary>
    [DefaultExecutionOrder(1980)]
    public sealed class PrototypeProtagonistScalePass : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private RectTransform avatarSlot;
        private RectTransform protagonistRect;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeProtagonistScalePass>() != null) return;
            var root = new GameObject("[PRESENTATION] Protagonist Scale Pass");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeProtagonistScalePass>();
        }

        private void LateUpdate()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype)
            {
                prototype = current;
                avatarSlot = null;
                protagonistRect = null;
            }
            if (prototype == null) return;

            if (avatarSlot == null)
            {
                avatarSlot = prototype.GetComponentsInChildren<RectTransform>(true)
                    .FirstOrDefault(rect => rect.gameObject.name == "Avatar Reserved");
            }
            if (avatarSlot == null) return;

            if (protagonistRect == null)
            {
                protagonistRect = avatarSlot.GetComponentsInChildren<RectTransform>(true)
                    .FirstOrDefault(rect => rect.gameObject.name == "Protagonist Sprite");
            }
            if (protagonistRect == null) return;

            protagonistRect.anchorMin = Vector2.zero;
            protagonistRect.anchorMax = Vector2.one;
            protagonistRect.pivot = new Vector2(0.5f, 0f);
            protagonistRect.offsetMin = new Vector2(-18f, -4f);
            protagonistRect.offsetMax = new Vector2(18f, 10f);
            protagonistRect.localScale = Vector3.one;

            var image = protagonistRect.GetComponent<Image>();
            if (image != null)
            {
                image.preserveAspect = true;
                image.raycastTarget = false;
            }
        }
    }
}
