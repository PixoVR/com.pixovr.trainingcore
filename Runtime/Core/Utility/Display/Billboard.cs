using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Settings;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility.Display
{
    /// <summary>Rotates an object to always face a target (billboard).</summary>
    public class Billboard : MonoBehaviour
    {
        /// <summary>Local axis that tracks the target.</summary>
        public Vector3 pivotAxis = Vector3.up;

        /// <summary>The object to rotate (defaults to self).</summary>
        public Transform ObjectToRotate;

        /// <summary>Rotation lerp speed.</summary>
        public float LerpSpeed = 10f;

        /// <summary>Target transform (defaults to main camera).</summary>
        public Transform targetTransform;

        /// <summary>Axis to pivot around (alias for <see cref="pivotAxis"/>).</summary>
        public Vector3 PivotAxis => pivotAxis;

        private void Update()
        {
            var body = ObjectToRotate != null ? ObjectToRotate : transform;
            var target = targetTransform != null
                ? targetTransform
                : (Camera.main != null ? Camera.main.transform : null);
            if (target == null)
                return;
            var look = Quaternion.LookRotation(body.position - target.position, pivotAxis);
            body.rotation = Quaternion.Slerp(body.rotation, look, Time.deltaTime * LerpSpeed);
        }
    }
}
