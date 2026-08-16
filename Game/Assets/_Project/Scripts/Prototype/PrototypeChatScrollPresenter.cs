using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Keeps long chat history navigable even though the legacy HUD refreshes its text every frame.
    /// Owns only presentation state: mouse-wheel position and a visible vertical scrollbar.
    /// </summary>
    [DefaultExecutionOrder(2180)]
    public sealed class PrototypeChatScrollPresenter : MonoBehaviour
    {
        private GameObject chatWindow;
        private ScrollRect scroll;
        private RectTransform viewport;
        private RectTransform content;
        private Scrollbar bar;
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

            var overflow = content.rect.height > viewport.rect.height + 1f;
            if (bar != null) bar.gameObject.SetActive(overflow);
            if (!overflow)
            {
                desiredPosition = 1f;
                scroll.verticalNormalizedPosition = 1f;
                return;
            }

            if (RectTransformUtility.RectangleContainsScreenPoint(viewport, Input.mousePosition, null))
            {
                var wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > 0.001f)
                {
                    var page = Mathf.Clamp01(viewport.rect.height / Mathf.Max(viewport.rect.height, content.rect.height));
                    var step = Mathf.Lerp(0.045f, 0.13f, page);
                    desiredPosition = Mathf.Clamp01(desiredPosition + wheel * step);
                }
            }

            scroll.verticalNormalizedPosition = desiredPosition;
            if (bar != null)
            {
                bar.size = Mathf.Clamp01(viewport.rect.height / Mathf.Max(viewport.rect.height, content.rect.height));
                bar.SetValueWithoutNotify(desiredPosition);
            }
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
                if (chatWindow == null || viewport == null || content == null)
                {
                    scroll = null;
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

            var handleGo = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGo.transform.SetParent(root.transform, false);
            var handleRect = handleGo.GetComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(1f, 1f);
            handleRect.offsetMax = new Vector2(-1f, -1f);
            var handleImage = handleGo.GetComponent<Image>();
            handleImage.color = new Color(0.52f, 0.58f, 0.67f, 0.92f);

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
