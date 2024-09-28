using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;
using StopWatch = System.Diagnostics.Stopwatch;

#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

namespace LobsterFramework.Init
{
    /// <summary>
    /// Injects custom events into the player loop system. To create custom events, use <see cref="PlayerLoopEventGroupAttribute"/> on classes implementing <see cref="IPlayerLoopEventGroup"/>.
    /// </summary>
    public static class PlayerLoopEventInjection
    {
#if UNITY_EDITOR
        // [DidReloadScripts(Constants.PlayerLoopInjectionOrder)]
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#endif
        private static void InjectEvent()
        {
            var stopWatch = StopWatch.StartNew();
            stopWatch.Start();
            var playerloop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoopEventGroupPriorityComparer comparer = new();
            PlayerLoopEventGroupAttribute.EventGroups.Sort(comparer);

            foreach (var group in PlayerLoopEventGroupAttribute.EventGroups)
            {
                List<PlayerLoopSystem> lst = new();
                foreach (var updateDelagte in group.UpdateEvents)
                {
                    lst.Add(new PlayerLoopSystem
                    {
                        subSystemList = null,
                        updateDelegate = updateDelagte,
                        type = group.EventTypes[updateDelagte]
                    });
                }

                var injection = new PlayerLoopSystem
                {
                    subSystemList = lst.ToArray(),
                    updateDelegate = null,
                    type = group.Type
                };

                if (group.InjectAfter)
                {
                    playerloop = InjectAfter(group.NeighbourEvent, playerloop, injection, out bool isAdded);
                }
                else
                {
                    playerloop = InjectBefore(group.NeighbourEvent, playerloop, injection, out bool isAdded);
                }
            }

            StringBuilder sb = new();
            ShowPlayerLoop(playerloop, sb, 0);
            PlayerLoop.SetPlayerLoop(playerloop);
            Debug.Log(sb);
            stopWatch.Stop();
            Debug.Log($"PlayerLoop event inject took {stopWatch.Elapsed.TotalSeconds} seconds!");
        }

        internal static PlayerLoopSystem InjectAfter(Type neighbourEvent,  PlayerLoopSystem existingSystem, in PlayerLoopSystem systemToAdd, out bool isAdded) {
            isAdded = false;
            if (existingSystem.subSystemList == null) { 
                return existingSystem;
            }
            List<PlayerLoopSystem> subSystems = new();
            
            foreach (var subSystem in existingSystem.subSystemList) { 
                subSystems.Add(subSystem);
                if (!isAdded && subSystem.type == neighbourEvent) {
                    isAdded = true;
                    Debug.Log($"Injected {systemToAdd.type.Name} after {subSystem.type.Name}");
                    subSystems.Add(systemToAdd);
                }
            }

            existingSystem.subSystemList = subSystems.ToArray();
            if (!isAdded) {
                for(int i = 0; i < subSystems.Count;i++) {
                    var subSystem = subSystems[i]; 
                    subSystem = InjectAfter(neighbourEvent, subSystem, systemToAdd, out isAdded);
                    if (isAdded) {
                        existingSystem.subSystemList[i] = subSystem;
                        return existingSystem;
                    }
                }
            }
            return existingSystem;
        }

        internal static PlayerLoopSystem InjectBefore(Type neighbourEvent, PlayerLoopSystem existingSystem, in PlayerLoopSystem systemToAdd, out bool isAdded)
        {
            isAdded = false;
            if (existingSystem.subSystemList == null)
            {
                return existingSystem;
            }
            List<PlayerLoopSystem> subSystems = new();

            foreach (var subSystem in existingSystem.subSystemList)
            {
                if (!isAdded && subSystem.type == neighbourEvent)
                {
                    isAdded = true;
                    Debug.Log($"Injected {systemToAdd.type.Name} before {subSystem.type.Name}");
                    subSystems.Add(systemToAdd);
                }
                subSystems.Add(subSystem);
            }

            existingSystem.subSystemList = subSystems.ToArray();
            if (!isAdded)
            {
                for (int i = 0; i < subSystems.Count; i++)
                {
                    var subSystem = subSystems[i];
                    subSystem = InjectBefore(neighbourEvent, subSystem, systemToAdd, out isAdded);
                    if (isAdded)
                    {
                        existingSystem.subSystemList[i] = subSystem;
                        return existingSystem;
                    }
                }
            }
            return existingSystem;
        }

        private static void ShowPlayerLoop(PlayerLoopSystem playerLoopSystem, StringBuilder text, int inline)
        {
            if (playerLoopSystem.type != null)
            {
                for (var i = 0; i < inline; i++)
                {
                    text.Append("\t");
                }
                text.AppendLine(playerLoopSystem.type.Name);
            }

            if (playerLoopSystem.subSystemList != null)
            {
                inline++;
                foreach (var s in playerLoopSystem.subSystemList)
                {
                    ShowPlayerLoop(s, text, inline);
                }
            }
        }
    }
}
