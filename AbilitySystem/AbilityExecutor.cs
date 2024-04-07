using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobsterFramework.AbilitySystem{
    /// <summary>
    /// Carries out ability instance execution according to the priorities of the abilities. Singleton class.
    /// </summary>
    public class AbilityExecutor : MonoBehaviour
    {
        private static AbilityExecutor _instance;

        /* Contains main queue for executing actions, actions are queueed in ascending order of their priorities.
         * Actions with low priorities will be executed first to allow further execution of higher priority actions to override/modify their 
         * effects.
         */
        private List<AbilityInstance> abilityQueue = new();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else { 
                Destroy( gameObject);
            }
        }

        internal static void EnqueueAction(AbilityInstance pair)
        {
            _instance.abilityQueue.Add(pair);
        }

        private void LateUpdate()
        {
            abilityQueue.Sort((AbilityInstance a1, AbilityInstance a2) => {
                return a2.ability.CompareByExecutionPriority(a1.ability);
            });

            for (int i = abilityQueue.Count - 1; i >= 0; i--)
            {
                AbilityInstance instance = abilityQueue[i];
                if (!instance.IsValid()) { // Is the ability is canceled by other ability routines, simply remove it from the queue and continue
                    abilityQueue.RemoveAt(i);
                    continue;
                }

                if (!instance.ability.Execute(instance.configName))
                {
                    abilityQueue.RemoveAt(i);
                    instance.StopAbility();
                }
            }
        }
    }
}

