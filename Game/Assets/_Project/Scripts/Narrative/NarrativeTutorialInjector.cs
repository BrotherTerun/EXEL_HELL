using UnityEngine;

namespace ExcelHell.Narrative
{
    /// <summary>
    /// Compatibility shim. The old onboarding injected tutorial lines on ActionNumber, which meant the player
    /// had to discover an action before the game explained it. Guided onboarding is now owned by
    /// PrototypeGuidedOnboarding and advances from observed worksheet state instead.
    /// </summary>
    public sealed class NarrativeTutorialInjector : MonoBehaviour
    {
        // Intentionally no runtime bootstrap.
    }
}
