using ExcelHell.Prototype;
using UnityEngine;

namespace ExcelHell.Application
{
    /// <summary>
    /// Compatibility guard for the prototype's legacy AfterSceneLoad auto-bootstrap.
    /// The application shell is bootstrapped before the scene, so this guard removes the
    /// automatically spawned graybox while the player is in application menus.
    /// This lets the prototype branch retain its standalone bootstrap without invasive edits.
    /// </summary>
    public sealed class PrototypeShellGuard : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (FindFirstObjectByType<PrototypeShellGuard>() != null) return;
            var guard = new GameObject("EXEL HELL Prototype Shell Guard").AddComponent<PrototypeShellGuard>();
            DontDestroyOnLoad(guard.gameObject);
        }

        private void Update()
        {
            if (!ExcelHellApplication.ShellAvailable || ExcelHellApplication.GameplayActive) return;
            var prototype = FindFirstObjectByType<ExcelHellPrototype>();
            if (prototype != null) Destroy(prototype.gameObject);
        }
    }
}