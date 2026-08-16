using System.Linq;
using ExcelHell.Application;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Small keyboard affordances for the production shell. It forwards to existing UI callbacks instead of
    /// duplicating gameplay logic, and never acts through an open presentation modal or authoring mode.
    /// </summary>
    [DefaultExecutionOrder(1925)]
    public sealed class PrototypeProductionHotkeys : MonoBehaviour
    {
        private ExcelHellPrototype prototype;
        private Button deleteButton;
        private Canvas canvas;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeProductionHotkeys>(FindObjectsInactive.Include) != null) return;
            var root = new GameObject("[PRESENTATION] Production Hotkeys");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeProductionHotkeys>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active || ExcelHellApplication.Paused) return;

            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            if (prototype == null || Keyboard.current == null) return;
            if (!Keyboard.current.deleteKey.wasPressedThisFrame) return;
            if (PresentationModalOpen()) return;

            deleteButton?.onClick.Invoke();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            prototype = owner;
            deleteButton = null;
            canvas = null;
            if (prototype == null) return;

            canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
            deleteButton = prototype.GetComponentsInChildren<Button>(true)
                .FirstOrDefault(button => button.gameObject.name == "ui.delete");
        }

        private bool PresentationModalOpen()
        {
            if (canvas == null) return false;
            var modalNames = new[] { "Tasks Window", "Help Window", "Chat Window", "Completion Modal" };
            foreach (var rect in canvas.GetComponentsInChildren<RectTransform>(true))
            {
                if (!modalNames.Contains(rect.gameObject.name)) continue;
                if (rect.gameObject.activeInHierarchy) return true;
            }
            return false;
        }
    }
}
