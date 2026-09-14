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

        /// <summary>Play the first clip of a settings blob, on the given source or a spawned one.</summary>
        public void Play(AudioClipSettings settings, AudioSource source = null)
        {
            if (settings?.ClipsToPlay == null || settings.ClipsToPlay.Count == 0)
                return;
            var clip = settings.ClipsToPlay[0];
            if (clip == null)
                return;

            if (source != null)
            {
                source.clip = clip;
                source.volume = settings.Volume;
                source.spatialBlend = settings.SpatialBlend;
                source.Play();
                return;
            }

            if (settings.PlayFromSpecificLocation)
            {
                EnsureSource();
                oneShotSource.transform.position = settings.LocationToPlayFrom;
                oneShotSource.spatialBlend = settings.SpatialBlend;
            }
            Play(clip);
        }

        /// <summary>Stop playback.</summary>
        public void StopPlaying()
        {
            if (oneShotSource != null)
                oneShotSource.Stop();
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

    /// <summary>Component that plays a queued <see cref="AudioClipSettings"/> on a bound source.</summary>
    public class AudioClipPlaybackManager : MonoBehaviour
    {
        /// <summary>Delegate for state changes.</summary>
        public delegate void AudioClipPlaybackStateChange();

        /// <summary>Delegate for clip playback.</summary>
        public delegate void AudioClipPlayback(AudioSource audioSource);

        /// <summary>Output source.</summary>
        public AudioSource Source;

        /// <summary>Currently playing settings.</summary>
        public AudioClipSettings CurrentSettings;

        private Coroutine playbackCoroutine;
        private readonly Queue<AudioClip> clipQueue = new Queue<AudioClip>();
        private readonly Queue<AudioClip> prevQueue = new Queue<AudioClip>();

        /// <summary>Queue all clips in the settings and start playback.</summary>
        public void Play(AudioClipSettings settings)
        {
            CurrentSettings = settings;
            clipQueue.Clear();
            if (settings?.ClipsToPlay != null)
                foreach (var clip in settings.ClipsToPlay)
                    if (clip != null)
                        clipQueue.Enqueue(clip);
            if (playbackCoroutine == null)
                playbackCoroutine = StartCoroutine(PlaybackRoutine());
        }

        /// <summary>Stop and clear the queue.</summary>
        public void Stop()
        {
            if (playbackCoroutine != null)
            {
                StopCoroutine(playbackCoroutine);
                playbackCoroutine = null;
            }
            if (Source != null)
                Source.Stop();
        }

        private IEnumerator PlaybackRoutine()
        {
            while (clipQueue.Count > 0)
            {
                var clip = clipQueue.Dequeue();
                prevQueue.Enqueue(clip);
                var src = Source != null ? Source : GetComponent<AudioSource>();
                if (src == null)
                    src = gameObject.AddComponent<AudioSource>();
                src.clip = clip;
                src.Play();
                while (src.isPlaying)
                    yield return null;
            }
            playbackCoroutine = null;
        }
    }
}
