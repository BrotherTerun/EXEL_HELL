using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExcelHell.Narrative
{
    public sealed class NarrativeEffectTicket
    {
        public NarrativeEffectRequest Request { get; }
        public bool IsCompleted { get; private set; }

        public NarrativeEffectTicket(NarrativeEffectRequest request)
        {
            Request = request;
        }

        public void Complete() => IsCompleted = true;
    }

    public interface INarrativeEffectReceiver
    {
        bool CanReceive(NarrativeEffectType type);
        void Receive(NarrativeEffectTicket ticket);
    }

    public static class NarrativeSignals
    {
        public static event Action<NarrativeTrigger> Triggered;

        public static void Publish(NarrativeTrigger trigger)
        {
            Debug.Log($"[NARRATIVE/TRIGGER] {trigger}");
            Triggered?.Invoke(trigger);
        }
    }

    [DefaultExecutionOrder(1200)]
    public sealed class NarrativeEventRunner : MonoBehaviour
    {
        [SerializeField] private string levelId = "runtime";
        [SerializeField] private bool verboseLogging = true;
        [SerializeField] private List<NarrativeEventDefinition> events = new();

        private readonly HashSet<string> consumed = new();
        private readonly Queue<NarrativeEffectRequest> queue = new();
        private readonly List<INarrativeEffectReceiver> receivers = new();
        private bool draining;

        public string LevelId
        {
            get => levelId;
            set => levelId = value ?? string.Empty;
        }

        public IReadOnlyCollection<string> ConsumedEventIds => consumed;
        public int EventCount => events?.Count ?? 0;

        private void OnEnable()
        {
            NarrativeSignals.Triggered += OnTrigger;
            DiscoverReceivers();
        }

        private void OnDisable()
        {
            NarrativeSignals.Triggered -= OnTrigger;
            StopAllCoroutines();
            draining = false;
            queue.Clear();
        }

        public void ReplaceEvents(IEnumerable<NarrativeEventDefinition> definitions)
        {
            events = definitions?.Where(definition => definition != null).ToList()
                     ?? new List<NarrativeEventDefinition>();
            consumed.Clear();
            if (verboseLogging)
                Debug.Log($"[NARRATIVE] Loaded {events.Count} event(s) for level '{levelId}'.");
        }

        public void RegisterReceiver(INarrativeEffectReceiver receiver)
        {
            if (receiver != null && !receivers.Contains(receiver)) receivers.Add(receiver);
        }

        public void UnregisterReceiver(INarrativeEffectReceiver receiver)
        {
            if (receiver != null) receivers.Remove(receiver);
        }

        public void FireDebug(NarrativeTriggerType type, int number = 0, string subjectId = null)
        {
            NarrativeSignals.Publish(new NarrativeTrigger(type, number, subjectId));
        }

        private void DiscoverReceivers()
        {
            receivers.Clear();
            foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (behaviour is INarrativeEffectReceiver receiver && !ReferenceEquals(receiver, this))
                    receivers.Add(receiver);
        }

        private void OnTrigger(NarrativeTrigger trigger)
        {
            foreach (var definition in events)
            {
                if (!Matches(definition, trigger)) continue;
                if (definition.once && !string.IsNullOrWhiteSpace(definition.id) && consumed.Contains(definition.id))
                {
                    if (verboseLogging)
                        Debug.Log($"[NARRATIVE/SKIP] {definition.id} — once event already consumed.");
                    continue;
                }

                if (definition.once && !string.IsNullOrWhiteSpace(definition.id)) consumed.Add(definition.id);
                if (verboseLogging)
                    Debug.Log($"[NARRATIVE/MATCH] {definition.id ?? "<unnamed>"} <- {trigger.Type}");
                StartCoroutine(DispatchEvent(definition));
            }
        }

        private bool Matches(NarrativeEventDefinition definition, NarrativeTrigger trigger)
        {
            if (definition == null || definition.trigger != trigger.Type) return false;
            if (!string.IsNullOrWhiteSpace(definition.levelId) &&
                !string.Equals(definition.levelId, levelId, StringComparison.OrdinalIgnoreCase)) return false;
            if (definition.trigger == NarrativeTriggerType.ActionNumber && definition.triggerNumber != trigger.Number) return false;
            return true;
        }

        private IEnumerator DispatchEvent(NarrativeEventDefinition definition)
        {
            if (definition.delay > 0f) yield return new WaitForSeconds(definition.delay);

            foreach (var effect in definition.effects)
            {
                if (effect == null) continue;
                queue.Enqueue(new NarrativeEffectRequest(definition.id, effect));
            }

            if (!draining) StartCoroutine(DrainQueue());
        }

        private IEnumerator DrainQueue()
        {
            draining = true;
            while (queue.Count > 0)
            {
                var request = queue.Dequeue();
                var effect = request.Effect;
                if (verboseLogging)
                {
                    Debug.Log(
                        $"[NARRATIVE/EFFECT] event={request.EventId ?? "<unnamed>"} type={effect.type} " +
                        $"text=\"{effect.text}\" mood={effect.mood} dismiss={effect.lifetime.dismissMode} " +
                        $"duration={effect.lifetime.duration:0.##} cell=({effect.row},{effect.column}) value={effect.intValue}");
                }

                INarrativeEffectReceiver receiver = null;
                for (var i = receivers.Count - 1; i >= 0; i--)
                {
                    if (receivers[i] == null)
                    {
                        receivers.RemoveAt(i);
                        continue;
                    }
                    if (receiver == null && receivers[i].CanReceive(effect.type)) receiver = receivers[i];
                }

                if (receiver == null)
                {
                    if (verboseLogging)
                        Debug.Log($"[NARRATIVE/SKIP] No receiver for {effect.type}; request completed without presentation.");
                    yield return null;
                    continue;
                }

                var ticket = new NarrativeEffectTicket(request);
                receiver.Receive(ticket);
                yield return new WaitUntil(() => ticket.IsCompleted || receiver == null);
            }
            draining = false;
        }
    }

    public sealed class DebugNarrativeReceiver : MonoBehaviour, INarrativeEffectReceiver
    {
        public bool CanReceive(NarrativeEffectType type) => true;

        public void Receive(NarrativeEffectTicket ticket)
        {
            var request = ticket.Request;
            var effect = request.Effect;
            Debug.Log($"[NARRATIVE/RECEIVER] {effect.type} accepted from {request.EventId ?? "<unnamed>"}.");
            ticket.Complete();
        }
    }
}
