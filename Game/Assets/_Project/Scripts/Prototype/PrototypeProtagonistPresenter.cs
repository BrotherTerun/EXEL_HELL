using System.Collections;
using System.Linq;
using ExcelHell.Narrative;
using UnityEngine;
using UnityEngine.UI;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Art-agnostic protagonist presentation endpoint. The reserved lower-right office slot will later receive
    /// the real pixel character; this component already owns mood and speech lifetime semantics.
    /// </summary>
    [DefaultExecutionOrder(1950)]
    public sealed class PrototypeProtagonistPresenter : MonoBehaviour, INarrativeEffectReceiver
    {
        private ExcelHellPrototype prototype;
        private NarrativeEventRunner runner;
        private Canvas canvas;
        private RectTransform avatarSlot;
        private Text avatarState;
        private GameObject bubble;
        private Text bubbleText;
        private Button bubbleButton;
        private NarrativeEffectTicket activeTicket;
        private Coroutine timeoutRoutine;
        private bool activeShown;
        private bool pendingLogged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeProtagonistPresenter>() != null) return;
            var root = new GameObject("[PRESENTATION] Protagonist Presenter");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeProtagonistPresenter>();
        }

        private void Update()
        {
            var current = FindFirstObjectByType<ExcelHellPrototype>();
            if (current != prototype) Bind(current);
            BindRunner();
            if (prototype != null && bubble == null) TryBuild();
            if (activeTicket != null && !activeShown) TryShowActive();
        }

        private void Bind(ExcelHellPrototype owner)
        {
            CompleteActive("rebind");
            prototype = owner;
            canvas = null;
            avatarSlot = null;
            avatarState = null;
            DestroyBubble();
            if (prototype != null)
                canvas = prototype.GetComponentsInChildren<Canvas>(true).FirstOrDefault();
        }

        private void BindRunner()
        {
            var current = FindFirstObjectByType<NarrativeEventRunner>();
            if (current == runner) return;
            if (runner != null) runner.UnregisterReceiver(this);
            runner = current;
            if (runner != null) runner.RegisterReceiver(this);
        }

        private void TryBuild()
        {
            if (canvas == null) return;
            avatarSlot = canvas.GetComponentsInChildren<RectTransform>(true)
                .FirstOrDefault(rect => rect.gameObject.name == "Avatar Reserved");
            if (avatarSlot == null) return;

            avatarState = avatarSlot.GetComponentsInChildren<Text>(true).FirstOrDefault();
            if (avatarState != null)
            {
                avatarState.text = "NORMAL\n[PROTAGONIST]";
                avatarState.fontSize = 13;
            }

            bubble = new GameObject("Protagonist Line", typeof(RectTransform), typeof(Image), typeof(Button));
            bubble.transform.SetParent(canvas.transform, false);
            SetTopLeft(bubble.GetComponent<RectTransform>(), 1050f, -390f, 510f, 110f);

            var image = bubble.GetComponent<Image>();
            image.color = new Color(0.075f, 0.085f, 0.105f, 0.985f);
            image.raycastTarget = true;

            bubbleButton = bubble.GetComponent<Button>();
            bubbleButton.targetGraphic = image;
            bubbleButton.onClick.AddListener(OnBubbleClicked);

            bubbleText = CreateText(bubble.transform, string.Empty, 16, FontStyle.Normal, TextAnchor.MiddleLeft);
            Stretch(bubbleText.rectTransform, 16f);
            bubbleText.color = Color.white;
            bubbleText.raycastTarget = false;
            bubble.SetActive(false);
        }

        public bool CanReceive(NarrativeEffectType type) => type == NarrativeEffectType.ProtagonistLine;

        public void Receive(NarrativeEffectTicket ticket)
        {
            if (ticket == null) return;

            CompleteActive("replaced");
            activeTicket = ticket;
            activeShown = false;
            pendingLogged = false;

            if (!TryShowActive())
            {
                pendingLogged = true;
                Debug.Log($"[PROTAGONIST/UI] Pending event={ticket.Request.EventId}; waiting for Avatar Reserved.");
            }
        }

        private bool TryShowActive()
        {
            if (activeTicket == null) return false;
            if (bubble == null) TryBuild();
            if (bubble == null || bubbleText == null) return false;

            var effect = activeTicket.Request.Effect;
            bubbleText.text = effect.text ?? string.Empty;
            SetMood(effect.mood);
            bubble.SetActive(true);
            bubble.transform.SetAsLastSibling();
            activeShown = true;

            Debug.Log($"[PROTAGONIST/UI] Show event={activeTicket.Request.EventId} mood={effect.mood} " +
                      $"dismiss={effect.lifetime.dismissMode} duration={effect.lifetime.duration:0.##} text=\"{effect.text}\"");

            if (effect.lifetime.dismissMode == NarrativeDismissMode.Timed ||
                effect.lifetime.dismissMode == NarrativeDismissMode.TimedOrClick)
            {
                timeoutRoutine = StartCoroutine(DismissAfter(Mathf.Max(0.05f, effect.lifetime.duration)));
            }
            return true;
        }

        private void SetMood(ProtagonistMood mood)
        {
            if (avatarState == null) return;
            avatarState.text = mood switch
            {
                ProtagonistMood.Tired => "TIRED\n[PROTAGONIST]",
                ProtagonistMood.Alarmed => "ALARMED\n[PROTAGONIST]",
                ProtagonistMood.Psychotic => "PSYCHOTIC\n[PROTAGONIST]",
                _ => "NORMAL\n[PROTAGONIST]"
            };
        }

        private void OnBubbleClicked()
        {
            if (activeTicket == null) return;
            var mode = activeTicket.Request.Effect.lifetime.dismissMode;
            if (mode == NarrativeDismissMode.OnClick || mode == NarrativeDismissMode.TimedOrClick)
                CompleteActive("click");
        }

        private IEnumerator DismissAfter(float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            CompleteActive("timeout");
        }

        private void CompleteActive(string reason)
        {
            if (timeoutRoutine != null)
            {
                StopCoroutine(timeoutRoutine);
                timeoutRoutine = null;
            }

            if (bubble != null) bubble.SetActive(false);
            if (activeTicket != null)
            {
                var eventId = activeTicket.Request.EventId;
                activeTicket.Complete();
                activeTicket = null;
                Debug.Log($"[PROTAGONIST/UI] Hide event={eventId} reason={reason}.");
            }
            activeShown = false;
            pendingLogged = false;
        }

        private void DestroyBubble()
        {
            if (bubble != null) Destroy(bubble);
            bubble = null;
            bubbleText = null;
            bubbleButton = null;
            activeShown = false;
        }

        private void OnDisable()
        {
            CompleteActive("disabled");
            if (runner != null) runner.UnregisterReceiver(this);
        }

        private static Text CreateText(Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
