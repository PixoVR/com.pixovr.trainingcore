using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixoVR.TrainingCore.Platform
{
    /// <summary>Project-authored scenario/module catalog served by <see cref="PlatformSessionBase.GetUserScenarios"/>.</summary>
    [CreateAssetMenu(fileName = "Scenario Catalog", menuName = "PixoVR/TrainingCore/Scenario Catalog")]
    public class ScenarioCatalog : ScriptableObject
    {
        /// <summary>Scenarios offered to the user.</summary>
        public List<Scenario> Scenarios = new List<Scenario>();

        /// <summary>Deep copy: new Scenario/Module instances so runtime code can't mutate the asset.</summary>
        public UserScenarios ToUserScenarios()
        {
            var userScenarios = new UserScenarios();
            foreach (var scenario in Scenarios)
            {
                if (scenario == null)
                    continue;
                var scenarioCopy = new Scenario
                {
                    Id = scenario.Id,
                    ScenarioName = scenario.ScenarioName,
                    ScenarioId = scenario.ScenarioId,
                    AppToLoad = scenario.AppToLoad,
                    PackageName = scenario.PackageName,
                    Version = scenario.Version,
                    Description = scenario.Description,
                    DifficultyRating = scenario.DifficultyRating,
                    ImageAddress = scenario.ImageAddress,
                    EstimatedLength = scenario.EstimatedLength
                };
                if (scenario.Modules != null)
                {
                    foreach (var module in scenario.Modules)
                    {
                        if (module == null)
                            continue;
                        scenarioCopy.Modules.Add(new Module
                        {
                            Name = module.Name,
                            Description = module.Description,
                            SceneToLoad = module.SceneToLoad,
                            ImageAddress = module.ImageAddress,
                            EstimatedLength = module.EstimatedLength,
                            Difficulty = module.Difficulty
                        });
                    }
                }
                userScenarios.AvailableScenarios.Add(scenarioCopy);

                foreach (var module in scenarioCopy.Modules)
                {
                    userScenarios.ScheduledSessions.Add(SynthesizeSession(scenarioCopy, module, "Assessment"));
                    userScenarios.ScheduledSessions.Add(SynthesizeSession(scenarioCopy, module, "Guided Performance"));
                }
            }
            return userScenarios;
        }

        private static SessionDescription SynthesizeSession(Scenario scenario, Module module, string mode) =>
            new SessionDescription
            {
                SessionName = module.Name,
                RoomName = module.Name,
                ScenarioName = scenario.ScenarioName,
                ModuleName = module.Name,
                Mode = mode,
                ScheduledTime = DateTime.Now,
                Status = SessionStatus.Waiting,
                Modules = { module.Name }
            };
    }
}
