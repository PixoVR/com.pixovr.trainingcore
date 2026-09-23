using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace PixoVR.TrainingCore.Utility.Display
{
    /// <summary>Places a display object relative to the camera using <see cref="PlacerSettings"/>.</summary>
    public class DisplayObjectPlacer : MonoBehaviour
    {
        private class RayCast
        {
            public RaycastHit Hit;
            public Vector3 Origin;
            public Vector3 Direction;
        }

        /// <summary>Replace the placement settings.</summary>
        public void ApplySettings(Settings.PlacerSettings settings) => Settings = settings;

        /// <summary>Placement settings.</summary>
        public PlacerSettings Settings;

        /// <summary>Placement origin override.</summary>
        public Transform OriginTransform;

        /// <summary>Off-screen arrow (left).</summary>
        public ArrowPlacer OffScreenLeft;

        /// <summary>Off-screen arrow (right).</summary>
        public ArrowPlacer OffScreenRight;

        /// <summary>Show debug gizmos.</summary>
        public bool ShowGizmos;

        /// <summary>The object being placed.</summary>
        public GameObject PlacementObject
        {
            get => placementObject;
            set
            {
                placementObject = value;
                if (placementObject == null)
                    return;
                CalculateObjectSize();
                FindValidPlacementLocation(Settings != null && Settings.Mode == PlacementMode.Fixed);
                MoveToTargetPosition(instant: true);
            }
        }

        /// <summary>Live placement point.</summary>
        public Vector3 PlacementLocation { get; private set; }

        /// <summary>Live rotation.</summary>
        public Quaternion PlacementRotation { get; private set; }

        /// <summary>Seconds spent on placement.</summary>
        public float TotalPlacementTime => _totalPlacementTime;

        /// <summary>Whether the object is currently visible on screen.</summary>
        public bool ObjectOnScreen => _objectOnScreen;

        private GameObject placementObject;
        private Vector3 objectSize;
        private Vector3 targetLocation;
        private Vector3 previousLocation;
        private Vector3 currentDeadZonePosition;
        private readonly List<RayCast> completedRaycasts = new List<RayCast>();
        private bool locationBlocked;

        private float _totalPlacementTime;
        private bool _objectOnScreen = true;
        private bool _placementInitiated;
        private bool _placementFinished;

        private Transform Origin =>
            OriginTransform != null ? OriginTransform : (Camera.main != null ? Camera.main.transform : null);

        /// <summary>Show and place the object in front of the camera.</summary>
        public virtual void ShowObject()
        {
            if (PlacementObject == null)
                return;
            CalculateObjectSize();
            FindValidPlacementLocation(Settings != null && Settings.Mode == PlacementMode.Fixed);
            MoveToTargetPosition(instant: true);
            PlacementObject.SetActive(true);
            PlacementLocation = PlacementObject.transform.position;
            PlacementRotation = PlacementObject.transform.rotation;
            _placementFinished = true;
        }

        /// <summary>Hide the object.</summary>
        public virtual void HideObject()
        {
            if (PlacementObject != null)
                PlacementObject.SetActive(false);
            _placementFinished = false;
            _placementInitiated = false;
        }

        /// <summary>Start interactive placement.</summary>
        public void StartPlacementMode()
        {
            _placementInitiated = true;
            _placementFinished = false;
        }

        /// <summary>Stop placement without finishing.</summary>
        public void StopPlacementMode() => _placementInitiated = false;

        private void Update()
        {
            if (!_placementInitiated)
                return;
            _totalPlacementTime += Time.deltaTime;
        }

        private void FixedUpdate()
        {
            if (Settings == null || PlacementObject == null || Origin == null)
                return;
            switch (Settings.Mode)
            {
                case PlacementMode.Deadzone:
                    CheckPositionDeadzone();
                    CheckRotationDeadzone();
                    break;
                case PlacementMode.Active:
                case PlacementMode.SetLocation:
                    FindValidPlacementLocation();
                    break;
            }
        }

        private void LateUpdate()
        {
            if (Settings == null || PlacementObject == null)
                return;
            MoveToTargetPosition();
            HandleOffScreenArrows();
        }

        private void CheckPositionDeadzone()
        {
            var position = Origin.position;
            if (Settings.FixedHeight)
                position.y = FindFixedHeight();
            if (Vector3.Distance(position, currentDeadZonePosition) > Settings.PositionDeadzoneSize)
                FindValidPlacementLocation(snapToLast: false, position);
        }

        private void CheckRotationDeadzone()
        {
            if (completedRaycasts.Count == 0)
                return;
            var position = Origin.position;
            if (Settings.FixedHeight)
                position.y = FindFixedHeight();
            float distance = Vector3.Distance(position, targetLocation);
            var toTarget = targetLocation - position;
            float angle = Vector3.Angle(Origin.forward, completedRaycasts[completedRaycasts.Count - 1].Direction);
            float rayLength = Mathf.Abs(distance / Mathf.Cos(angle * Mathf.Deg2Rad));
            var rayVector = Origin.forward * rayLength;
            var difference = rayVector - toTarget;
            var deadzoneVector = difference.normalized * Settings.RotationDeadzoneSize;
            var adjusted = deadzoneVector - difference;
            if (Vector3.Distance(position + rayVector, targetLocation) < Settings.RotationDeadzoneSize)
                return;
            var newDirection = (toTarget - adjusted).normalized;
            FindValidPlacementLocation(snapToLast: false, position, newDirection, distance);
        }

        private void CalculateObjectSize()
        {
            if (placementObject == null)
                return;
            var rect = placementObject.GetComponent<RectTransform>();
            if (rect == null)
                rect = placementObject.GetComponentInChildren<RectTransform>();
            if (rect != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                objectSize = new Vector3(rect.rect.width, rect.rect.height,
                    Settings != null && Settings.SizeForRotation ? rect.rect.width : 0.1f);
                objectSize = Vector3.Scale(objectSize, rect.localScale);
                return;
            }
            var renderer = placementObject.GetComponent<Renderer>();
            if (renderer == null)
                renderer = placementObject.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                objectSize = renderer.bounds.size;
                objectSize = Vector3.Scale(objectSize, renderer.transform.localScale);
                objectSize.x = Settings != null && Settings.SizeForRotation && objectSize.x > objectSize.z
                    ? objectSize.x : objectSize.z;
            }
        }

        private void FindValidPlacementLocation(bool snapToLast = false,
            Vector3? overrideOrigin = null, Vector3? overrideDirection = null,
            float overrideDistance = -1f, List<float> setAngles = null)
        {
            if (Settings == null || placementObject == null)
                return;
            if (Settings.Mode == PlacementMode.SetLocation && Settings.SetLocation != null)
            {
                targetLocation = Settings.SetLocation.transform.position;
                placementObject.transform.rotation = Settings.SetLocation.transform.rotation;
                return;
            }
            if (!overrideOrigin.HasValue && Origin == null)
                return;
            completedRaycasts.Clear();
            if (Settings.DirectionAttempts == 0)
                return;
            var origin = overrideOrigin ?? Origin.position;
            var forward = overrideDirection ?? Origin.forward;
            float distance = overrideDistance == -1f ? Settings.TargetOriginDistance : overrideDistance;
            if (Settings.FixedHeight)
            {
                origin.y = FindFixedHeight();
                forward.y = 0f;
                forward.Normalize();
            }
            var positiveAngles = new List<float>();
            var fallbackAngles = new List<float>();
            if (setAngles == null)
            {
                float step = 360 / Settings.DirectionAttempts;
                for (int i = 0; i < Settings.DirectionAttempts; i++)
                {
                    float angle = step * i;
                    if (angle < 90f && angle > -270f)
                    {
                        positiveAngles.Add(angle);
                        if (i != 0)
                            positiveAngles.Add(-angle);
                    }
                    else
                    {
                        fallbackAngles.Add(angle);
                        if (i != 180)
                            fallbackAngles.Add(-angle);
                    }
                }
            }
            else
            {
                positiveAngles = setAngles;
            }
            bool found = false;
            for (int j = 0; j < positiveAngles.Count; j++)
            {
                var direction = Quaternion.Euler(0f, positiveAngles[j], 0f) * forward;
                bool blocked;
                RaycastHit hit;
                if (Settings.SizeForRotation)
                    blocked = Physics.SphereCast(origin, Mathf.Max(objectSize.x, objectSize.z) / 2f * (1f + Settings.Padding / 2f),
                        direction, out hit, distance, Settings.PlacementBlockingLayer);
                else
                    blocked = Physics.BoxCast(origin, objectSize / 2f * (1f + Settings.Padding / 2f),
                        direction, out hit, placementObject.transform.rotation, distance, Settings.PlacementBlockingLayer);
                locationBlocked = blocked;
                targetLocation = origin + direction * (blocked ? hit.distance : distance);
                completedRaycasts.Add(new RayCast { Hit = hit, Origin = origin, Direction = direction });
                if (hit.distance > Settings.MinimumDistanceFromCamera || hit.collider == null)
                {
                    found = true;
                    break;
                }
            }
            if (found && setAngles == null)
            {
                var last = completedRaycasts[completedRaycasts.Count - 1];
                if (CheckClipping(last.Origin, last.Direction, distance))
                {
                    FindValidPlacementLocation(snapToLast, origin, forward, -1f, fallbackAngles);
                    return;
                }
            }
            else if (setAngles == null)
            {
                FindValidPlacementLocation(snapToLast, origin, forward, distance, fallbackAngles);
            }
            if (Settings.FixedHeight)
                targetLocation.y = FindFixedHeight();
            else if (Settings.OriginHeight)
                targetLocation.y = Origin.position.y;
            if (Settings.Mode == PlacementMode.Fixed && snapToLast
                && Vector3.Distance(targetLocation, previousLocation) < Settings.SnapDistance)
                targetLocation = previousLocation;
            if (Settings.Mode == PlacementMode.Deadzone)
            {
                var flat = targetLocation;
                flat.y = 0f;
                currentDeadZonePosition = flat;
            }
            previousLocation = targetLocation;
        }

        private float FindFixedHeight()
        {
            if (Origin != null)
            {
                var ray = new Ray(Origin.position + new Vector3(0f, Settings.DisplayHeight / 2f, 0f), Vector3.down);
                var floorMask = LayerMask.GetMask("Floor");
                if (Physics.Raycast(ray, out var hit, 1000f, floorMask))
                    return hit.point.y + Settings.DisplayHeight;
            }
            return Settings.DisplayHeight;
        }

        private bool CheckClipping(Vector3 origin, Vector3 direction, float distance)
        {
            var points = new List<Vector3>
            {
                origin,
                origin + new Vector3(0f, objectSize.y / 2f, 0f),
                origin + new Vector3(0f, -objectSize.y / 2f, 0f)
            };
            foreach (var point in points)
                if (Physics.Raycast(point, direction, distance, Settings.PlacementBlockingLayer))
                    return true;
            return false;
        }

        private void MoveToTargetPosition(bool instant = false)
        {
            placementObject.transform.position = Vector3.Lerp(placementObject.transform.position,
                targetLocation, instant || Settings == null ? 1f : Settings.LerpSpeed);
            PlacementLocation = placementObject.transform.position;
            PlacementRotation = placementObject.transform.rotation;
        }

        private void HandleOffScreenArrows()
        {
            if (!Settings.OffScreenArrows || Camera.main == null)
            {
                SetArrows(false, false);
                return;
            }
            var viewport = Camera.main.WorldToViewportPoint(placementObject.transform.position);
            var margin = Mathf.Max(objectSize.x, objectSize.z) / 4f;
            bool offLeft = viewport.x < -margin;
            bool offRight = viewport.x > 1f + margin;
            _objectOnScreen = !offLeft && !offRight;
            SetArrows(offLeft, offRight);
            if (offLeft && OffScreenLeft != null)
                OffScreenLeft.PlaceArrow(Origin != null ? Origin.gameObject : gameObject, placementObject);
            if (offRight && OffScreenRight != null)
                OffScreenRight.PlaceArrow(Origin != null ? Origin.gameObject : gameObject, placementObject);
        }

        private void SetArrows(bool left, bool right)
        {
            if (OffScreenLeft != null && OffScreenLeft.gameObject.activeSelf != left)
                OffScreenLeft.gameObject.SetActive(left);
            if (OffScreenRight != null && OffScreenRight.gameObject.activeSelf != right)
                OffScreenRight.gameObject.SetActive(right);
        }
    }
}
