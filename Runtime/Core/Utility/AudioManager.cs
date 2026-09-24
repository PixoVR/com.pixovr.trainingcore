using PixoVR.TrainingCore.Settings;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>Singleton audio service playing clips and <see cref="AudioClipSettings"/> playlists.</summary>
    public class AudioManager : SingletonBehaviour<AudioManager>
    {
        private AudioSource oneShotSource;

        /// <summary>Play a single clip on an internal 2D source.</summary>
        public void Play(AudioClip clip)
        {
            if (clip == null)
                return;
            EnsureSource();
            oneShotSource.PlayOneShot(clip);
        }

        /// <summary>Play all clips of a settings blob in sequence, on the given source or the internal one.</summary>
        public void Play(AudioClipSettings settings, AudioSource source = null)
        {
            if (settings?.ClipsToPlay == null || settings.ClipsToPlay.Count == 0)
                return;

            AudioSource src = source;
            if (src == null)
            {
                EnsureSource();
                src = oneShotSource;
                if (settings.PlayFromSpecificLocation)
                    src.transform.position = settings.LocationToPlayFrom;
            }
            src.volume = settings.Volume;
            src.spatialBlend = settings.SpatialBlend;
            if (running.TryGetValue(src, out var existing))
                StopCoroutine(existing);
            running[src] = StartCoroutine(PlaySequence(settings, src));
        }

        private readonly Dictionary<AudioSource, Coroutine> running = new Dictionary<AudioSource, Coroutine>();

        private IEnumerator PlaySequence(AudioClipSettings settings, AudioSource source)
        {
            foreach (var clip in settings.ClipsToPlay)
            {
                if (clip == null)
                    continue;
                source.clip = clip;
                source.Play();
                while (source.isPlaying)
                    yield return null;
            }
            running.Remove(source);
        }

        /// <summary>Stop playback on the given source, or the internal one when null.</summary>
        public void StopPlaying(AudioSource source = null)
        {
            var src = source != null ? source : oneShotSource;
            if (src == null)
                return;
            if (running.TryGetValue(src, out var coroutine))
            {
                StopCoroutine(coroutine);
                running.Remove(src);
            }
            src.Stop();
        }

        /// <summary>Preview a clip (editor tooling hook).</summary>
        public void PreviewClip(AudioClip clip) => Play(clip);

        /// <summary>Stop preview.</summary>
        public void StopPreview() => StopPlaying();

        private void EnsureSource()
        {
            if (oneShotSource == null)
                oneShotSource = gameObject.AddComponent<AudioSource>();
        }
    }

}
