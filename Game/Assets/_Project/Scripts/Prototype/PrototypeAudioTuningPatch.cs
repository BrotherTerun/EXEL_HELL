using System;
using System.Reflection;
using UnityEngine;

namespace ExcelHell.Prototype
{
    /// <summary>
    /// Fast release tuning layer for the first in-game audio review.
    /// Keeps the broad audio pass intact while making requested mix changes easy to revert.
    /// </summary>
    [DefaultExecutionOrder(2420)]
    public sealed class PrototypeAudioTuningPatch : MonoBehaviour
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        private const float AmbienceVolume = 0.087f; // ~1.5x quieter than 0.13
        private const float DropGain = 2f / 3f;      // ~1.5x quieter

        private static readonly FieldInfo NormalField = typeof(PrototypeAudioDirector).GetField("normal", Flags);
        private static readonly FieldInfo AmbienceField = typeof(PrototypeAudioDirector).GetField("ambience", Flags);
        private static readonly FieldInfo DropField = typeof(PrototypeAudioDirector).GetField("drop", Flags);

        private PrototypeAudioDirector director;
        private AudioClip tunedDrop;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (PrototypeAuthoringMode.Active) return;
            if (FindFirstObjectByType<PrototypeAudioTuningPatch>(FindObjectsInactive.Include) != null) return;

            var root = new GameObject("[PRESENTATION] Audio Tuning");
            DontDestroyOnLoad(root);
            root.AddComponent<PrototypeAudioTuningPatch>();
        }

        private void Update()
        {
            var current = PrototypeAudioDirector.Instance ??
                          FindFirstObjectByType<PrototypeAudioDirector>(FindObjectsInactive.Include);
            if (current != director) Bind(current);
            if (director == null) return;

            ApplyMix();
        }

        private void Bind(PrototypeAudioDirector current)
        {
            director = current;
            if (director == null) return;

            ApplyMix();

            var originalDrop = DropField?.GetValue(director) as AudioClip;
            if (originalDrop != null)
            {
                tunedDrop = CreateScaledCopy(originalDrop, DropGain);
                if (tunedDrop != null)
                    DropField?.SetValue(director, tunedDrop);
            }

            Debug.Log("[AUDIO/TUNE] ambience=0.087, cell_drop gain=0.67, normal music disabled.");
        }

        private void ApplyMix()
        {
            var normal = NormalField?.GetValue(director) as AudioSource;
            if (normal != null)
            {
                normal.volume = 0f;
                normal.mute = true;
                if (normal.isPlaying) normal.Stop();
            }

            var ambience = AmbienceField?.GetValue(director) as AudioSource;
            if (ambience != null)
                ambience.volume = AmbienceVolume;
        }

        private static AudioClip CreateScaledCopy(AudioClip source, float gain)
        {
            try
            {
                if (source.loadState != AudioDataLoadState.Loaded)
                    source.LoadAudioData();

                var data = new float[source.samples * source.channels];
                if (!source.GetData(data, 0))
                {
                    Debug.LogWarning("[AUDIO/TUNE] cell_drop data could not be read; keeping original level.");
                    return source;
                }

                for (var i = 0; i < data.Length; i++)
                    data[i] *= gain;

                var copy = AudioClip.Create(
                    source.name + "_tuned",
                    source.samples,
                    source.channels,
                    source.frequency,
                    false);
                copy.SetData(data, 0);
                return copy;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[AUDIO/TUNE] cell_drop attenuation failed; keeping original level: {exception.Message}");
                return source;
            }
        }
    }
}
