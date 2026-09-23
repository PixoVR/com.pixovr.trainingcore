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
            StartCoroutine(PlaySequence(settings, src));
        }

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

}
