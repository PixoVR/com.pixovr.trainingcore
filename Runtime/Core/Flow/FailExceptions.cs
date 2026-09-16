using System;
using System.Collections.Generic;
using PixoVR.TrainingCore.Identity;
using UnityEngine;

namespace PixoVR.TrainingCore.Flow.Exceptions
{
    /// <summary>Base for a typed fail-exception parameter.</summary>
    [Serializable]
    public class FailExceptionParameterBase
    {
        /// <summary>Parameter is ignored during matching.</summary>
        public bool Ignore;

        /// <summary>Display name.</summary>
        public string Name = "Name missing";
    }

    /// <summary>Typed fail-exception parameter.</summary>
    [Serializable]
    public class FailExceptionParameter<T> : FailExceptionParameterBase, IEquatable<FailExceptionParameter<T>>
    {
        /// <summary>Expected value.</summary>
        public T Value;

        /// <summary>Empty parameter.</summary>
        public FailExceptionParameter() { }

        /// <summary>Parameter with expected value.</summary>
        public FailExceptionParameter(T parameterValue) => Value = parameterValue;

        /// <summary>Ignored parameter.</summary>
        public FailExceptionParameter(bool ignore) => Ignore = ignore;

        /// <summary>See the interface/base contract.</summary>
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;

        /// <summary>See the interface/base contract.</summary>
        public override bool Equals(object obj) => obj is FailExceptionParameter<T> other && Equals(other);

        /// <summary>See the interface/base contract.</summary>
        public bool Equals(FailExceptionParameter<T> other) =>
            other != null && EqualityComparer<T>.Default.Equals(Value, other.Value);

        /// <summary>See the interface/base contract.</summary>
        public override string ToString() => Ignore ? "(ignored)" : Value?.ToString();
    }

    /// <summary>Fail parameter that references a scene object via <see cref="GuidReference"/>.</summary>
    [Serializable]
    public class ObjectReferenceFailParameter : FailExceptionParameter<GuidReference>
    {
        /// <summary>Assembly-qualified expected component type name.</summary>
        public string ObjectType;

        /// <summary>Reference parameter for a component type.</summary>
        public ObjectReferenceFailParameter(GuidReference objectReference, Type underlyingType)
        {
            Value = objectReference;
            ObjectType = underlyingType?.AssemblyQualifiedName;
        }

        /// <summary>Ignored parameter for a component type.</summary>
        public ObjectReferenceFailParameter(Type objectType, bool ignore = true)
        {
            ObjectType = objectType?.AssemblyQualifiedName;
            Ignore = ignore;
        }
    }

    /// <summary>Base class for fail exceptions attached to steps / global exception nodes.</summary>
    [Serializable]
    public class FailExceptionBase
    {
        /// <summary>Fail parameters.</summary>
        public List<FailExceptionParameterBase> Parameters = new List<FailExceptionParameterBase>();

        /// <summary>Type-level (class-wide) exception flag.</summary>
        public bool TypeException;

        /// <summary>Persistent guid for editor bookkeeping.</summary>
        public string PersistGuid;

        [SerializeField]
        private List<GameModes.GameModeData> gameModesData = new List<GameModes.GameModeData>();

        /// <summary>Display name of this exception.</summary>
        public virtual string Name => "Exception";

        /// <summary>Whether this exception applies in the given game mode.</summary>
        public bool IsIncludedInMode(GameModes.GameMode mode)
        {
            var data = GetGameModeDataFor(mode);
            return data == null || data.Include;
        }

        private GameModes.GameModeData GetGameModeDataFor(GameModes.GameMode mode)
        {
            foreach (var d in gameModesData)
                if (d.Mode == mode)
                    return d;
            return null;
        }

        /// <summary>Set the include flag for a game mode.</summary>
        public void SetIncludeValueFor(GameModes.GameMode mode, bool includeValue)
        {
            var data = GetGameModeDataFor(mode);
            if (data != null)
                data.Include = includeValue;
            else
                gameModesData.Add(new GameModes.GameModeData(mode, includeValue));
        }

        /// <summary>Structural equality including parameters.</summary>
        public virtual bool ExactEquals(object obj) => Equals(obj);
    }

    /// <summary>Base for exceptions that target a single interactable.</summary>
    [Serializable]
    public abstract class SingleObjectFailException : FailExceptionBase
    {
        /// <summary>The interactable this exception watches.</summary>
        public ObjectReferenceFailParameter InteractedObjectParameter;

        /// <summary>Type-scoped exception.</summary>
        protected SingleObjectFailException(Type type)
        {
            InteractedObjectParameter = new ObjectReferenceFailParameter(type);
        }

        /// <summary>Object-scoped exception.</summary>
        protected SingleObjectFailException(GuidReference objectReference, Type type)
        {
            InteractedObjectParameter = new ObjectReferenceFailParameter(objectReference, type);
        }
    }

    /// <summary>Exception: wrong/mistimed grab.</summary>
    [Serializable]
    public class GrabFailException : SingleObjectFailException
    {
        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Grab Object";

        /// <summary>Type-scoped grab exception.</summary>
        public GrabFailException() : base(typeof(Interactions.Grabbable)) { }

        /// <summary>Object-scoped grab exception.</summary>
        public GrabFailException(GuidReference grabObjectReference)
            : base(grabObjectReference, typeof(Interactions.Grabbable)) { }
    }

    /// <summary>Exception: wrong/mistimed tap.</summary>
    [Serializable]
    public class TapFailException : SingleObjectFailException
    {
        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Tap Object";

        /// <summary>Type-scoped tap exception.</summary>
        public TapFailException() : base(typeof(Interactions.Tappable)) { }

        /// <summary>Object-scoped tap exception.</summary>
        public TapFailException(GuidReference tapObjectGuidReference)
            : base(tapObjectGuidReference, typeof(Interactions.Tappable)) { }
    }

    /// <summary>Exception: wrong/mistimed use.</summary>
    [Serializable]
    public class UseFailException : SingleObjectFailException
    {
        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Use Object";

        /// <summary>Type-scoped use exception.</summary>
        public UseFailException() : base(typeof(Interactions.Snappable)) { }

        /// <summary>Object-scoped use exception.</summary>
        public UseFailException(GuidReference useObjectReference)
            : base(useObjectReference, typeof(Interactions.Snappable)) { }
    }

    /// <summary>Exception: wrong/mistimed teleport.</summary>
    [Serializable]
    public class TeleportFailException : SingleObjectFailException
    {
        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Teleport";

        /// <summary>Type-scoped teleport exception.</summary>
        public TeleportFailException() : base(typeof(Interactions.Teleporter)) { }

        /// <summary>Object-scoped teleport exception.</summary>
        public TeleportFailException(GuidReference teleportReference)
            : base(teleportReference, typeof(Interactions.Teleporter)) { }
    }

    /// <summary>Exception: wrong valve turn.</summary>
    [Serializable]
    public class ValveTurnException : SingleObjectFailException
    {
        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Valve Turn";

        /// <summary>Type-scoped valve exception.</summary>
        public ValveTurnException() : base(typeof(Interactions.Valve)) { }

        /// <summary>Object-scoped valve exception.</summary>
        public ValveTurnException(GuidReference valveReference)
            : base(valveReference, typeof(Interactions.Valve)) { }
    }

    /// <summary>Exception: wrong snap object/zone pairing.</summary>
    [Serializable]
    public class SnapFailException : FailExceptionBase
    {
        /// <summary>Snappable parameter.</summary>
        public ObjectReferenceFailParameter SnappedObjectParameter =
            new ObjectReferenceFailParameter(typeof(Interactions.Snappable));

        /// <summary>Expected snappable id.</summary>
        public int SnappedObjectId;

        /// <summary>Snapzone parameter.</summary>
        public ObjectReferenceFailParameter SnapzoneObjectParameter =
            new ObjectReferenceFailParameter(typeof(Interactions.Snapzone));

        /// <summary>Expected snapzone id.</summary>
        public int SnapzoneId;

        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Snap Object or Zone";

        /// <summary>Empty snap exception.</summary>
        public SnapFailException() { }

        /// <summary>Snap exception bound to a snap step.</summary>
        public SnapFailException(StepBase snapStep, GuidReference objectReference, int idParameter)
        {
            SnappedObjectParameter = new ObjectReferenceFailParameter(objectReference, typeof(Interactions.Snappable));
            SnappedObjectId = idParameter;
        }
    }

    /// <summary>Exception: unexpected collision pair.</summary>
    [Serializable]
    public class CollideFailException : FailExceptionBase
    {
        /// <summary>First collider parameter.</summary>
        public ObjectReferenceFailParameter collideObjectA;

        /// <summary>Second collider parameter.</summary>
        public ObjectReferenceFailParameter collideObjectB;

        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Collision";

        /// <summary>Empty collide exception.</summary>
        public CollideFailException() { }

        /// <summary>Exception for a specific pair.</summary>
        public CollideFailException(GuidReference objectAReference, GuidReference objectBReference = null)
        {
            collideObjectA = new ObjectReferenceFailParameter(objectAReference, typeof(Collider));
            collideObjectB = new ObjectReferenceFailParameter(objectBReference, typeof(Collider));
        }
    }

    /// <summary>Exception: hand-menu state change at the wrong time.</summary>
    [Serializable]
    public class HandMenuFailException : FailExceptionBase
    {
        /// <summary>Expected menu-open state.</summary>
        public FailExceptionParameter<bool> OpenMenuParameter = new FailExceptionParameter<bool>();

        /// <summary>See the interface/base contract.</summary>
        public override string Name => "Hand Menu";

        /// <summary>Empty hand-menu exception.</summary>
        public HandMenuFailException() { }

        /// <summary>Exception for a specific open state.</summary>
        public HandMenuFailException(bool open)
        {
            OpenMenuParameter = new FailExceptionParameter<bool> { Value = open };
        }
    }
}
