using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Settings;
using UnityEngine;
using UnityEngine.Video;

namespace PixoVR.TrainingCore.Data
{
    /// <summary>End-user platform.</summary>
    public enum PlayerPlatform
    {
        /// <summary>Headset.</summary>
        VR,
        /// <summary>Desktop.</summary>
        PC
    }

    /// <summary>Content payload for <see cref="Utility.Display.Displayer"/>/display steps.</summary>
    [Serializable]
    public class DisplayData
    {
        /// <summary>Heading.</summary>
        public string Title = "";

        /// <summary>Subheading.</summary>
        public string Subtitle = "";

        /// <summary>Body text.</summary>
        public string Body = "";

        /// <summary>Connection-point object reference.</summary>
        [SerializeField]
        [HideInInspector]
        private Graph.NodeSavedProperty guidProperty;

        /// <summary>Images to show.</summary>
        public List<Sprite> Sprites = new List<Sprite>();

        /// <summary>Audio to play.</summary>
        public AudioClipSettings AudioSettings;

        /// <summary>Editor foldout.</summary>
        public bool AudioFoldout = false;

        /// <summary>Videos to show.</summary>
        public List<VideoClip> VideoClips = new List<VideoClip>();

        /// <summary>Resolved connection-point object.</summary>
        public GameObject ConnectionPoint
        {
            get => guidProperty?.ObjectReference?.GameObject;
            set
            {
                guidProperty ??= new Graph.NodeSavedProperty("connection");
                guidProperty.ObjectReference = value == null ? null : new Identity.GuidReference(value);
            }
        }
    }

    /// <summary>Scriptable catalog of arrow prefabs used by arrow actions.</summary>
    [CreateAssetMenu(fileName = "ArrowData", menuName = "TrainingCore/Arrow Data")]
    public class ArrowDataList : ScriptableObject
    {
        /// <summary>Available arrow prefabs.</summary>
        public List<GameObject> ArrowPrefabs;

        /// <summary>Find a prefab by name.</summary>
        public GameObject GetArrowByName(string name) =>
            ArrowPrefabs?.Find(a => a != null && a.name == name);
    }
}
