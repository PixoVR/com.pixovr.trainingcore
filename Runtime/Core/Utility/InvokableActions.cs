using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PixoVR.TrainingCore.Utility
{
    /// <summary>UnityEvent-style invokable scene actions (enable/disable objects).</summary>
    public class InvokableActions : MonoBehaviour
    {
        /// <summary>Activate a GameObject.</summary>
        public void EnableObjectAction(GameObject target)
        {
            if (target != null)
                target.SetActive(true);
        }

        /// <summary>Deactivate a GameObject.</summary>
        public void DisableObjectAction(GameObject target)
        {
            if (target != null)
                target.SetActive(false);
        }
    }
}
