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

}
