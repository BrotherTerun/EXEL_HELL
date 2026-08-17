using System.Reflection;
using UnityEngine;

namespace ExcelHell.Prototype
{
    [DefaultExecutionOrder(2385)]
    public sealed class PrototypeAudioDestroyedReferenceGuard : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo HelpField = typeof(PrototypeProductionHud).GetField("helpWindow", Flags);
        private static readonly FieldInfo ChatField = typeof(PrototypeProductionHud).GetField("chatWindow", Flags);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeAudioDestroyedReferenceGuard>(FindObjectsInactive.Include) != null) return;

            var root = new GameObject("[PRESENTATION] Audio Destroyed Reference Guard");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeAudioDestroyedReferenceGuard>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var hud = FindFirstObjectByType<PrototypeProductionHud>(FindObjectsInactive.Include);
            if (hud == null) return;

            ClearDestroyedReference(HelpField, hud);
            ClearDestroyedReference(ChatField, hud);
        }

        private static void ClearDestroyedReference(FieldInfo field, PrototypeProductionHud hud)
        {
            if (field == null || hud == null) return;

            var value = field.GetValue(hud);
            if (value is Object unityObject && unityObject == null)
                field.SetValue(hud, null);
        }
    }
}
