using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Settings;
using UnityEngine;

namespace PixoVR.TrainingCore.Utility.Display
{
    /// <summary>Axes a billboard can rotate around.</summary>
    public enum PivotAxis
    {
        /// <summary>Rotate freely around X and Y.</summary>
        XY,
        /// <summary>Rotate around Y only.</summary>
        Y,
        /// <summary>Rotate around X only.</summary>
        X,
        /// <summary>Rotate around Z only.</summary>
        Z,
        /// <summary>Rotate around X and Z.</summary>
        XZ,
        /// <summary>Rotate around Y and Z.</summary>
        YZ,
        /// <summary>No pivot constraint.</summary>
        Free
    }

    /// <summary>Rotates an object to always face a target (billboard).</summary>
    public class Billboard : MonoBehaviour
    {
        /// <summary>Axes about which the object will rotate.</summary>
        [SerializeField]
        private PivotAxis pivotAxis = PivotAxis.XY;

        /// <summary>The object to rotate (defaults to self).</summary>
        public Transform ObjectToRotate;

        /// <summary>Rotation lerp speed.</summary>
        public float LerpSpeed = 10f;

        /// <summary>Target transform (defaults to main camera).</summary>
        public Transform targetTransform;

        /// <summary>Axis to pivot around.</summary>
        public PivotAxis PivotAxis => pivotAxis;

        private void Update()
        {
            var body = ObjectToRotate != null ? ObjectToRotate : transform;
            var target = targetTransform != null
                ? targetTransform
                : (Camera.main != null ? Camera.main.transform : null);
            if (target == null)
                return;
            var direction = target.position - body.position;
            bool useCameraUp = true;
            switch (pivotAxis)
            {
                case PivotAxis.X:
                    direction.x = 0f;
                    useCameraUp = false;
                    break;
                case PivotAxis.Y:
                    direction.y = 0f;
                    useCameraUp = false;
                    break;
                case PivotAxis.Z:
                    direction.x = 0f;
                    direction.y = 0f;
                    break;
                case PivotAxis.XY:
                    useCameraUp = false;
                    break;
                case PivotAxis.XZ:
                    direction.x = 0f;
                    break;
                case PivotAxis.YZ:
                    direction.y = 0f;
                    break;
            }
            if (direction.sqrMagnitude < 0.001f)
                return;
            var look = useCameraUp
                ? Quaternion.LookRotation(-direction, target.up)
                : Quaternion.LookRotation(-direction);
            body.rotation = Quaternion.Slerp(body.rotation, look, Time.deltaTime * LerpSpeed);
        }
    }
}
