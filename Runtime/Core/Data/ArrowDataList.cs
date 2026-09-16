using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Settings;
using UnityEngine;
using UnityEngine.Video;

namespace PixoVR.TrainingCore.Data
{
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
