using System;
using PixoVR.TrainingCore.Data;
using PixoVR.TrainingCore.Identity;
using PixoVR.TrainingCore.Settings;
using PixoVR.TrainingCore.Utility.Display;
using UnityEngine;

namespace PixoVR.TrainingCore.Commands
{
    /// <summary>Instantiates a prefab; unexecute destroys the spawned instance. Re-execute spawns again.</summary>
    [Serializable]
    public class SpawnCommand : CommandBase
    {
        /// <summary>Prefab to instantiate.</summary>
        public GameObject Prefab;

        /// <summary>Optional parent transform.</summary>
        public Transform Parent;

        /// <summary>Spawn position.</summary>
        public Vector3 Position;

        /// <summary>Spawn rotation.</summary>
        public Quaternion Rotation;

        /// <summary>The instantiated object (null until executed / after unexecute).</summary>
        public GameObject SpawnedObject { get; protected set; }

        public SpawnCommand(GameObject prefab, Transform parent = null, Vector3 position = default,
            Quaternion rotation = default) : base(prefab != null ? prefab.GetGuidString() : string.Empty)
        {
            Prefab = prefab;
            Parent = parent;
            Position = position;
            Rotation = rotation;
        }

        /// <inheritdoc/>
        public override void Execute() => SpawnedObject = Instantiate();

        /// <inheritdoc/>
        public override void Unexecute()
        {
            if (SpawnedObject != null)
                UnityEngine.Object.Destroy(SpawnedObject);
            SpawnedObject = null;
        }

        /// <summary>Create the spawned instance.</summary>
        protected virtual GameObject Instantiate() =>
            UnityEngine.Object.Instantiate(Prefab, Position,
                Rotation == default ? Quaternion.identity : Rotation, Parent);

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new DestroySpawnedObjectCommand(this);
    }

    /// <summary>Destroys the object a <see cref="SpawnCommand"/> spawned; unexecute respawns it.</summary>
    [Serializable]
    public sealed class DestroySpawnedObjectCommand : CommandBase
    {
        /// <summary>The spawn command whose object is destroyed.</summary>
        public SpawnCommand Spawn;

        /// <summary>Destroy the spawned object's parent instead.</summary>
        public bool DestroyParent;

        public DestroySpawnedObjectCommand(SpawnCommand spawn, bool destroyParent = false)
            : base(spawn?.SubjectId ?? string.Empty)
        {
            Spawn = spawn;
            DestroyParent = destroyParent;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            var spawned = Spawn?.SpawnedObject;
            if (spawned == null)
                return;
            UnityEngine.Object.Destroy(DestroyParent && spawned.transform.parent != null
                ? spawned.transform.parent.gameObject
                : spawned);
        }

        /// <inheritdoc/>
        public override void Unexecute() => Spawn?.Execute();
    }

    /// <summary>Spawns a display object and applies placer settings + display data.</summary>
    [Serializable]
    public sealed class DisplayObjectCommand : SpawnCommand
    {
        /// <summary>Guid of the owning subject.</summary>
        public string OwnerGuid;

        /// <summary>Placement settings applied to the spawned placer.</summary>
        public PlacerSettings PlacerSettings;

        /// <summary>Data shown on the spawned displayer.</summary>
        public DisplayData DisplayData;

        public DisplayObjectCommand(string ownerGuid, GameObject displayObject, PlacerSettings placer,
            DisplayData data) : base(displayObject)
        {
            OwnerGuid = ownerGuid;
            PlacerSettings = placer;
            DisplayData = data;
        }

        private DisplayObjectPlacer appliedPlacer;

        /// <inheritdoc/>
        protected override GameObject Instantiate()
        {
            var spawned = base.Instantiate();
            if (spawned == null)
                return null;
            if (spawned.GetComponent<Events.ObservableSubject>() == null)
                spawned.AddComponent<Events.ObservableSubject>();
            var displayer = spawned.GetComponent<Displayer>();
            if (displayer != null)
                displayer.DisplayTextData(DisplayData);
            var placer = PlacerSettings?.ObjectPlacer ?? UserInterfaceManager.Instance?.MainDisplayer;
            if (placer != null)
            {
                placer.ApplySettings(PlacerSettings ?? placer.Settings);
                placer.PlacementObject = spawned;
                appliedPlacer = placer;
                Utility.Log.Info($"Display spawned: {spawned.name} subject={spawned.GetGuidString()}",
                    Utility.LogCategory.Flow);
            }
            return spawned;
        }

        /// <inheritdoc/>
        public override void Unexecute()
        {
            if (appliedPlacer != null && appliedPlacer.PlacementObject == SpawnedObject)
                appliedPlacer.PlacementObject = null;
            appliedPlacer = null;
            base.Unexecute();
        }
    }

    /// <summary>Spawns a hand coach and plays its animation after a delay.</summary>
    [Serializable]
    public sealed class SpawnHandCoachCommand : SpawnCommand
    {
        /// <summary>Animation to play.</summary>
        public string AnimationName;

        /// <summary>Delay before playback.</summary>
        public float StartDelay;

        /// <summary>Where to spawn.</summary>
        public Transform SpawnLocation;

        public SpawnHandCoachCommand(Interactions.HandCoachBase prefab, Transform spawnLocation,
            string animationName, float startDelay)
            : base(prefab != null ? prefab.gameObject : null, null,
                spawnLocation != null ? spawnLocation.position : default,
                spawnLocation != null ? spawnLocation.rotation : Quaternion.identity)
        {
            SpawnLocation = spawnLocation;
            AnimationName = animationName;
            StartDelay = startDelay;
        }

        /// <inheritdoc/>
        protected override GameObject Instantiate()
        {
            var spawned = base.Instantiate();
            var coach = spawned != null ? spawned.GetComponent<Interactions.HandCoachBase>() : null;
            coach?.Play(AnimationName, StartDelay);
            return spawned;
        }
    }

    /// <summary>Places an arrow pointing at a target via an <see cref="ArrowPlacer"/>.</summary>
    [Serializable]
    public sealed class PlaceArrowCommand : CommandBase
    {
        /// <summary>The arrow placer used.</summary>
        public ArrowPlacer Placer;

        /// <summary>Target the arrow points at.</summary>
        public Transform Target;

        /// <summary>Settings applied before placing.</summary>
        public ArrowPlacerSettings Settings;

        /// <summary>The placed arrow, if any.</summary>
        public GameObject PlacedArrow => Placer?.Arrow;

        public PlaceArrowCommand(ArrowPlacer placer, Transform target, ArrowPlacerSettings settings)
            : base(target != null ? target.gameObject.GetGuidString() : string.Empty)
        {
            Placer = placer;
            Target = target;
            Settings = settings;
        }

        /// <inheritdoc/>
        public override void Execute()
        {
            if (Placer == null || Target == null)
                return;
            if (Settings != null)
                Placer.Settings = Settings;
            Placer.PlaceArrow(Placer.gameObject, Target.gameObject);
        }

        /// <inheritdoc/>
        public override void Unexecute() => Clear();

        /// <summary>Remove the placed arrow.</summary>
        public void Clear() => Placer?.ClearArrow();

        /// <inheritdoc/>
        public override CommandBase GetInverse() => new DestroyPlacedArrowCommand(this);
    }

    /// <summary>Clears a placed arrow; unexecute re-places it.</summary>
    [Serializable]
    public sealed class DestroyPlacedArrowCommand : CommandBase
    {
        /// <summary>The place command whose arrow is destroyed.</summary>
        public PlaceArrowCommand Place;

        public DestroyPlacedArrowCommand(PlaceArrowCommand place)
            : base(place?.SubjectId ?? string.Empty)
        {
            Place = place;
        }

        /// <inheritdoc/>
        public override void Execute() => Place?.Clear();

        /// <inheritdoc/>
        public override void Unexecute() => Place?.Execute();
    }
}
