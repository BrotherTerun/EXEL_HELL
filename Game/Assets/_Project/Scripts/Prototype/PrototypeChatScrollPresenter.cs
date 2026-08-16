using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Keeps long chat history navigable even though the legacy HUD refreshes its text every frame.
    /// Owns only presentation state: mouse-wheel position and a visible vertical scrollbar.
    /// Uses the project's active Input System package; never touches legacy UnityEngine.Input.
    /// </summary>
    [DefaultExecutionOrder(2180)]
    public sealed class PrototypeChatScrollPresenter : MonoBehaviour
    {
        private GameObject chatWindow;
        private ScrollRect scroll;
        private RectTransform viewport;
        private RectTransform content;
        private Text contentText;
        private Scrollbar bar;
        private Canvas canvas;
        private float desiredPosition;
        private bool wasOpen;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeChatScrollPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] Chat Scroll");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeChatScrollPresenter>();
        }

        private void LateUpdate()
        {
            if (!TryBind()) return;
            var open = chatWindow.activeInHierarchy;
            if (!open)
            {
                wasOpen = false;
                return;
            }

            if (!wasOpen)
            {
                desiredPosition = 0f; // newest messages on open
                wasOpen = true;
            }

            var viewportHeight = Mathf.Max(1f, Mathf.Abs(viewport.rect.height));
            var contentHeight = Mathf.Max(viewportHeight, Mathf.Abs(content.rect.height));
            if (contentText != null)
                contentHeight = Mathf.Max(contentHeight, contentText.preferredHeight + 28f);

            var overflow = contentHeight > viewportHeight + 1f;
            if (bar != null) bar.gameObject.SetActive(overflow);
            if (!overflow)
            {
                desiredPosition = 1f;
                scroll.verticalNormalizedPosition = 1f;
                if (bar != null)
                {
                    bar.size = 1f;
                    bar.SetValueWithoutNotify(1f);
                }
                return;
            }

            ReadMouseWheel();

            // PrototypeProductionHud refreshes the chat text every Update and snaps its ScrollRect to the bottom.
            // This component executes later and restores the user's own scroll position.
            scroll.verticalNormalizedPosition = desiredPosition;

            if (bar != null)
            {
                bar.size = Mathf.Clamp01(viewportHeight / contentHeight);
                bar.SetValueWithoutNotify(desiredPosition);
            }
        }

        private void ReadMouseWheel()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            var pointer = mouse.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(viewport, pointer, camera)) return;

            var wheelY = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(wheelY) <= 0.001f) return;

            // Input System reports a conventional mouse-wheel notch as roughly 120 units on Windows.
            // Keep continuous/high-resolution wheels useful while preventing one event from jumping the whole history.
            var wheelSteps = Mathf.Clamp(wheelY / 120f, -4f, 4f);
            if (Mathf.Abs(wheelSteps) < 0.01f)
                wheelSteps = Mathf.Sign(wheelY) * 0.01f;

            var page = Mathf.Clamp01(Mathf.Abs(viewport.rect.height) /
                                     Mathf.Max(Mathf.Abs(viewport.rect.height), EffectiveContentHeight()));
            var step = Mathf.Lerp(0.045f, 0.13f, page);
            desiredPosition = Mathf.Clamp01(desiredPosition + wheelSteps * step);
        }

        private float EffectiveContentHeight()
        {
            var height = content != null ? Mathf.Abs(content.rect.height) : 1f;
            if (contentText != null) height = Mathf.Max(height, contentText.preferredHeight + 28f);
            return Mathf.Max(1f, height);
        }

        private bool TryBind()
        {
            if (chatWindow != null && scroll != null && viewport != null && content != null) return true;

            foreach (var candidate in FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name != "Chat Body") continue;
                scroll = candidate;
                chatWindow = candidate.transform.parent != null ? candidate.transform.parent.gameObject : null;
                viewport = candidate.viewport != null ? candidate.viewport : candidate.GetComponent<RectTransform>();
                content = candidate.content;
                contentText = content != null ? content.GetComponent<Text>() : null;
                canvas = candidate.GetComponentInParent<Canvas>();
                if (chatWindow == null || viewport == null || content == null)
                {
                    scroll = null;
                    contentText = null;
                    canvas = null;
                    continue;
                }

                BuildScrollbar();
                desiredPosition = scroll.verticalNormalizedPosition;
                return true;
            }

            return false;
        }

        private void BuildScrollbar()
        {
            if (bar != null) Destroy(bar.gameObject);

            var root = new GameObject("Chat Scrollbar", typeof(RectTransform), typeof(Image), typeof(Scrollbar));
            root.transform.SetParent(viewport, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-3f, 0f);
            rect.sizeDelta = new Vector2(10f, -8f);

            var track = root.GetComponent<Image>();
            track.color = new Color(0.08f, 0.095f, 0.12f, 0.82f);
            track.raycastTarget = true;

            var slidingArea = new GameObject("Sliding Area", typeof(RectTransform));
            slidingArea.transform.SetParent(root.transform, false);
            var slidingRect = slidingArea.GetComponent<RectTransform>();
            slidingRect.anchorMin = Vector2.zero;
            slidingRect.anchorMax = Vector2.one;
            slidingRect.offsetMin = new Vector2(1f, 1f);
            slidingRect.offsetMax = new Vector2(-1f, -1f);

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(slidingArea.transform, false);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = Vector2.zero;
            handleRect.offsetMax = Vector2.zero;
            var handleImage = handleGo.GetComponent<Image>();
            handleImage.color = new Color(0.52f, 0.58f, 0.67f, 0.92f);
            handleImage.raycastTarget = true;

            bar = root.GetComponent<Scrollbar>();
            bar.targetGraphic = handleImage;
            bar.handleRect = handleRect;
            bar.direction = Scrollbar.Direction.BottomToTop;
            bar.numberOfSteps = 0;
            bar.onValueChanged.AddListener(value => desiredPosition = value);
            root.SetActive(false);
        }
    }
}
