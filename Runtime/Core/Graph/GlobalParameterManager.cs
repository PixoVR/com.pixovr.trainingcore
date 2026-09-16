using System.Linq;
using GraphProcessor;
using UnityEngine;

namespace PixoVR.TrainingCore.Graph
{
    /// <summary>Access point for the shared <see cref="GlobalParameters"/> asset.</summary>
    public class GlobalParameterManager
    {
        private GlobalParameters parameters;
        private static GlobalParameterManager instance;
        private readonly string GlobalParameterFileName = "GlobalParameters";

        /// <summary>Singleton accessor; loads the Resources asset on first use.</summary>
        public static GlobalParameterManager Instance
        {
            get => instance ??= new GlobalParameterManager();
            set => instance = value;
        }

        /// <summary>Create the manager (loads `Resources/GlobalParameters`).</summary>
        public GlobalParameterManager()
        {
            parameters = Resources.Load<GlobalParameters>(GlobalParameterFileName);
        }

        /// <summary>Inject a parameters asset (tests / non-Resources setups).</summary>
        public void SetParameters(GlobalParameters globalParameters) => parameters = globalParameters;

        /// <summary>The loaded asset (may be null).</summary>
        public GlobalParameters Parameters => parameters;

        /// <summary>Restore every parameter's value to its captured initial value.</summary>
        public void Reset()
        {
            if (parameters?.Data == null)
                return;
            foreach (var entry in parameters.Data)
                if (entry?.Parameter != null)
                    entry.Parameter.value = entry.InitialValue;
        }

        /// <summary>Find a global parameter by name.</summary>
        public ExposedParameter GetParameter(string name) =>
            parameters?.ExposedParameters?.FirstOrDefault(p => p.name == name);
    }
}
