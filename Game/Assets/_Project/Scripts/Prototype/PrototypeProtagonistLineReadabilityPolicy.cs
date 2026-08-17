using System.Collections;
using System.Reflection;
using ExcelHell.Narrative;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Release-time readability policy for protagonist lines.
    /// Keeps every line on-screen for up to 22 seconds, while allowing any subsequent mouse click to dismiss it.
    /// The short arming delay prevents the click that triggered a new line from instantly dismissing that same line.
    /// </summary>
    [DefaultExecutionOrder(1975)]
    public sealed class PrototypeProtagonistLineReadabilityPolicy : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static readonly FieldInfo ActiveTicketField = typeof(PrototypeProtagonistPresenter).GetField("activeTicket", Flags);
        private static readonly FieldInfo TimeoutRoutineField = typeof(PrototypeProtagonistPresenter).GetField("timeoutRoutine", Flags);
        private static readonly MethodInfo CompleteActiveMethod = typeof(PrototypeProtagonistPresenter).GetMethod("CompleteActive", Flags);

        private PrototypeProtagonistPresenter presenter;
        private NarrativeEffectTicket trackedTicket;
        private Coroutine fallbackTimeout;
        private float shownAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeProtagonistLineReadabilityPolicy>() != null) return;
            var root = new GameObject("[PRESENTATION] Protagonist Line Readability");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeProtagonistLineReadabilityPolicy>();
        }

        private void Update()
        {
            if (PrototypeAuthoringMode.Active) return;

            var current = FindFirstObjectByType<PrototypeProtagonistPresenter>();
            if (current != presenter)
                Bind(current);
            if (presenter == null) return;

            var active = ActiveTicketField?.GetValue(presenter) as NarrativeEffectTicket;
            if (!ReferenceEquals(active, trackedTicket))
                Track(active);

            if (trackedTicket == null || Time.unscaledTime - shownAt < 0.14f) return;

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                Dismiss("global-click");
        }

        private void Bind(PrototypeProtagonistPresenter owner)
        {
            if (fallbackTimeout != null)
            {
                StopCoroutine(fallbackTimeout);
                fallbackTimeout = null;
            }
            presenter = owner;
            trackedTicket = null;
            shownAt = 0f;
        }

        private void Track(NarrativeEffectTicket ticket)
        {
            if (fallbackTimeout != null)
            {
                StopCoroutine(fallbackTimeout);
                fallbackTimeout = null;
            }

            trackedTicket = ticket;
            shownAt = Time.unscaledTime;
            if (presenter == null || trackedTicket == null) return;

            // Replace the presenter's short authored timeout with the final readability timeout.
            var existingTimeout = TimeoutRoutineField?.GetValue(presenter) as Coroutine;
            if (existingTimeout != null)
            {
                presenter.StopCoroutine(existingTimeout);
                TimeoutRoutineField?.SetValue(presenter, null);
            }

            fallbackTimeout = StartCoroutine(DismissAfter(22f, trackedTicket));
        }

        private IEnumerator DismissAfter(float seconds, NarrativeEffectTicket expected)
        {
            yield return new WaitForSecondsRealtime(seconds);
            if (ReferenceEquals(trackedTicket, expected))
                Dismiss("readability-timeout");
        }

        private void Dismiss(string reason)
        {
            if (presenter == null || trackedTicket == null) return;
            CompleteActiveMethod?.Invoke(presenter, new object[] { reason });
            trackedTicket = null;
            if (fallbackTimeout != null)
            {
                StopCoroutine(fallbackTimeout);
                fallbackTimeout = null;
            }
        }

        private void OnDisable()
        {
            if (fallbackTimeout != null)
            {
                StopCoroutine(fallbackTimeout);
                fallbackTimeout = null;
            }
            trackedTicket = null;
        }
    }
}
