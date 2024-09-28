using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using LobsterFramework.Init;

namespace LobsterFramework.AbilitySystem{
    /// <summary>
    /// Carries out ability instance execution according to the priorities of the abilities.
    /// </summary>
    [PlayerLoopEventGroup(typeof(PreLateUpdate.ScriptRunBehaviourLateUpdate), Priority = 0, InjectAfter = false)]
    public sealed class AbilityInstanceManagement : IPlayerLoopEventGroup
    {
        /// <summary>
        /// The list of currently active ability instances, sorted by their priorities. Abilities with higher priority will be executed first.
        /// </summary>
        private static readonly List<AbilityInstance> abilityQueue = new();
        private static readonly List<AbilityInstance> suspendedInstances = new();

        internal static void EnqueueAction(AbilityInstance pair)
        {
            abilityQueue.Add(pair);
        }

        internal static void SuspendInstance(AbilityInstance instance) { 
            suspendedInstances.Add(instance);
        }

        private class ExecuteAbilityInstance { }

        [PlayerLoopEvent(typeof(ExecuteAbilityInstance))]
        private static void Execute()
        {
            abilityQueue.Sort((AbilityInstance a1, AbilityInstance a2) => {
                return a1.ability.ExecutionPriority - a2.ability.ExecutionPriority;
            });

            for (int i = abilityQueue.Count - 1; i >= 0; i--) {
                AbilityInstance instance = abilityQueue[i];
                if (instance.ability.channels[instance.name].IsSuspended)
                { 
                    abilityQueue.RemoveAt(i);
                    continue;
                }
            }

            for (int i = abilityQueue.Count - 1; i >= 0; i--)
            {
                AbilityInstance instance = abilityQueue[i];
                
                if (!instance.ability.Execute(instance.name))
                {
                    abilityQueue.RemoveAt(i);
                }
            }
        }

        private class TerminateAbilityInstance { }

        [PlayerLoopEvent(typeof(TerminateAbilityInstance))]
        private static void Terminate() {
            suspendedInstances.Sort((AbilityInstance a1, AbilityInstance a2) => {
                return a1.ability.ExecutionPriority - a2.ability.ExecutionPriority;
            });

            for (int i = suspendedInstances.Count - 1; i >= 0; i--)
            {
                AbilityInstance instance = suspendedInstances[i];
                instance.ability.Suspend(instance.name);
                suspendedInstances.RemoveAt(i);
            }
        }
    }
}

