using System;
using System.Collections.Generic;
using System.Linq;
using GraphProcessor;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>A global parameter plus the value it had when created (for <see cref="GlobalParameterManager.Reset"/>).</summary>
    [Serializable]
    public class GlobalParameter
    {
        /// <summary>The exposed parameter.</summary>
        [SerializeField]
        [SerializeReference]
        public ExposedParameter Parameter;

        /// <summary>Value captured when the parameter was added.</summary>
        [NonSerialized]
        public object InitialValue;

        /// <summary>Create from a parameter (captures its current value).</summary>
        public GlobalParameter(ExposedParameter parameter)
        {
            Parameter = parameter;
            InitialValue = parameter?.value;
        }

        /// <summary>Parameterless ctor for serialization.</summary>
        public GlobalParameter() { }
    }

    /// <summary>Asset holding global exposed parameters shared across graphs.</summary>
    public class GlobalParameters : ScriptableObject
    {
        /// <summary>Stored parameters.</summary>
        [SerializeField]
        [SerializeReference]
        public List<GlobalParameter> Data = new List<GlobalParameter>();

        /// <summary>The parameters alone.</summary>
        public List<ExposedParameter> ExposedParameters =>
            Data.Where(p => p?.Parameter != null).Select(p => p.Parameter).ToList();

        /// <summary>Fired when the parameter list changes.</summary>
        public event Action OnExposedParameterListChanged;

        /// <summary>Add a parameter; returns its guid.</summary>
        public string AddExposedParameter(string name, Type type, object value = null)
        {
            ExposedParameter param;
            if (type.IsSubclassOf(typeof(ExposedParameter)))
            {
                param = Activator.CreateInstance(type) as ExposedParameter;
            }
            else
            {
                // generic NGP parameters: pick the ExposedParameter subclass matching the value type
                param = CreateParameterForValueType(type);
                if (param == null)
                {
                    Debug.LogError($"Can't add global parameter of type {type}: no ExposedParameter subclass for it.");
                    return null;
                }
            }

            if (param.GetValueType().IsValueType)
                value = Activator.CreateInstance(param.GetValueType());
            param.Initialize(name, value);
            Data.Add(new GlobalParameter(param));
            OnExposedParameterListChanged?.Invoke();
            return param.guid;
        }

        private static Dictionary<Type, Type> valueTypeToParameterType;

        private static ExposedParameter CreateParameterForValueType(Type valueType)
        {
            if (valueTypeToParameterType == null)
            {
                valueTypeToParameterType = new Dictionary<Type, Type>();
                foreach (var type in AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes()))
                {
                    if (!type.IsSubclassOf(typeof(ExposedParameter)) || type.IsAbstract)
                        continue;
                    try
                    {
                        var probe = Activator.CreateInstance(type) as ExposedParameter;
                        var vt = probe?.GetValueType();
                        if (vt != null && vt != typeof(object) && !valueTypeToParameterType.ContainsKey(vt))
                            valueTypeToParameterType[vt] = type;
                    }
                    catch (Exception) { }
                }
            }
            return valueTypeToParameterType.TryGetValue(valueType, out var t)
                ? Activator.CreateInstance(t) as ExposedParameter
                : null;
        }
    }
}
