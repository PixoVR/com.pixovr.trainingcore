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

    /// <summary>A single answer option on a quiz/question step.</summary>
    [Serializable]
    public class Answer
    {
        /// <summary>Answer text.</summary>
        public string Text = string.Empty;

        /// <summary>Display index.</summary>
        public int Index = -1;

        /// <summary>Whether this answer is correct.</summary>
        public bool Correct = false;
    }

    /// <summary>Answer payload carried by question steps and display commands.</summary>
    [Serializable]
    public class AnswerData
    {
        /// <summary>Answer option prefab.</summary>
        [NonSerialized]
        public GameObject Prefab;

        /// <summary>Show answers in a random order.</summary>
        public bool RandomOrder;

        /// <summary>Number of answers shown.</summary>
        public int DisplayCount;

        /// <summary>Candidate answers.</summary>
        public List<Answer> Answers = new List<Answer>();
    }

    /// <summary>Scriptable answer option used by <see cref="Interactions.QuizAnswer"/> components.</summary>
    [CreateAssetMenu(fileName = "QuizAnswerData", menuName = "TrainingCore/Quiz Answer Data")]
    public class QuizAnswerData : ScriptableObject
    {
        /// <summary>The text of the answer.</summary>
        public string AnswerText;

        /// <summary>Unique identifier for this answer asset.</summary>
        public Guid AnswerId { get; private set; }

        /// <summary>Create a new answer asset with a fresh identifier.</summary>
        public QuizAnswerData()
        {
            if (AnswerId == default)
                AnswerId = Guid.NewGuid();
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
