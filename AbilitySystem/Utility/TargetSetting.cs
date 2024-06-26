using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability/TargetSetting")]
    public class TargetSetting : ScriptableObject
    {
        public List<EntityGroup> targetGroups;
        public List<EntityGroup> ignoreGroups;
        private HashSet<Entity> targets;
        private HashSet<Entity> ignores;

        public bool IsTarget(Entity entity) {
            return targets.Contains(entity) && !ignores.Contains(entity); 
        }

        private void OnEnable()
        {
            targets = new();
            ignores = new();
            if(targetGroups == null)
            {
                targetGroups = new();
            }
            if (ignoreGroups == null)
            {
                ignoreGroups = new();
            }
            foreach (EntityGroup group in targetGroups)
            {
                targets.UnionWith(group);
                group.OnEntityAdded += Add;
                group.OnEntityRemoved += Remove;
            }
            foreach (EntityGroup group in ignoreGroups)
            {
                ignores.UnionWith(group);
                group.OnEntityAdded += AddIgnore;
                group.OnEntityRemoved += RemoveIgnore;
            }
        }

        private void Add(Entity entity)
        {
            targets.Add(entity);
        }

        private void Remove(Entity entity) {
            targets.Remove(entity);
        }

        private void AddIgnore(Entity entity) {
            ignores.Add(entity);
        }
        private void RemoveIgnore(Entity entity) {
            ignores.Remove(entity);
        }


        private void OnDisable()
        {
            foreach (EntityGroup group in targetGroups)
            {
                group.OnEntityAdded += (Entity entity) => { targets.Add(entity); };
                group.OnEntityRemoved += (Entity entity) => { targets.Remove(entity); };
            }
            foreach (EntityGroup group in ignoreGroups)
            {
                group.OnEntityAdded += (Entity entity) => { ignores.Add(entity); };
                group.OnEntityRemoved += (Entity entity) => { ignores.Remove(entity); };
            }
            targets.Clear();
            ignores.Clear();
        }
    }
}
