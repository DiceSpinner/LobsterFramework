using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using LobsterFramework.Utility;
using LobsterFramework.AbilitySystem;
using Pathfinding;
using Pathfinding.Util;

namespace LobsterFramework.AI
{
    public class AIController : MonoBehaviour
    {
        [SerializeField] private List<ControllerData> controllerDatas;
        [SerializeField] private EntityGroup targetGroup;
        [SerializeField] private AbilityManager abilityManager;
        [SerializeField] private StateMachine stateMachine;
        [SerializeField] private AbilityManager playerAbilityManager;
        [SerializeField] private float visableDegree;
        [SerializeField] private List<MonoBehaviour> utilities;
        [SerializeField] private Entity entity;

        public Entity target;
        private Transform _transform;
        private Collider2D _collider;
        private AIPathFinder pathFinder;
        private GridGraph gridGraph;
        
        private MovementController moveControl;


        public AbilityManager AbilityManager { get { return abilityManager; }}
        public AbilityManager PlayerAbilityRunner { get { return playerAbilityManager; } }
        public Entity GetEntity { get { return entity; } }
        private void Awake()
        {
            gridGraph = AstarPath.active.data.gridGraph;
            moveControl = GetComponent<MovementController>();
            _transform = GetComponent<Transform>();
            _collider = GetComponent<Collider2D>();
            pathFinder = GetComponent<AIPathFinder>();
        }

        private void FixedUpdate()
        {
            if (isTargeting) {
                if (target != null)
                {
                    moveControl.RotateForwardDirection(target.transform.position - transform.position);
                }
                else {
                    isTargeting = false;
                }
            }
        }

        private void OnDrawGizmos()
        {
            if (_transform != null)
            {
                
                Draw.Gizmos.Line(_transform.position, _transform.position + (Quaternion.Euler(0, 0, 45) * transform.up).normalized * 5, Color.blue);
                Draw.Gizmos.Line(_transform.position, _transform.position + (Quaternion.Euler(0, 0, -45) * transform.up).normalized * 5, Color.blue);
                Draw.Gizmos.Line(_transform.position, pathFinder.Destination, Color.yellow);
            }
        }

        public T GetUtil<T>() where T : MonoBehaviour {
            foreach (MonoBehaviour item in utilities) {
                if (item.GetType() == typeof(T)) {
                    return (T)item;
                }
            }
            return default;
        }

        public T GetControllerData<T>() where T : ControllerData
        {
            Type t = typeof(T);
            foreach (ControllerData data in controllerDatas) {
                if (data.GetType() == t) {
                    return (T)data;
                }
            }
            return null;
        }

        public bool TargetInRange(float range)
        {
            return Vector3.Distance(_transform.position, target.transform.position) <= range;
        }

        public float GetTargetDistance()
        {
            return Vector3.Distance(_transform.position, target.transform.position);
        }

        public void ChaseTarget()
        {
            if (target != null)
            {
                pathFinder.SetTarget(target.transform);
            }
        }

        public void ResetTarget()
        {
            if(target != null)
            {
                pathFinder.Stop();
            }
            target = null;
        }
        public void StopChaseTarget()
        {
            if (target != null)
            {
                pathFinder.Stop();
            }
        }
        public bool SearchTarget(float sightRange)
        {
            Damage info = entity.LatestDamageInfo;
            if (info.source != null)
            {
                target = info.source;
                /*Debug.Log("Received attack from target");*/
                return true;
            }
            RaycastHit2D hit = AIUtility.Raycast2D(gameObject,  _transform.position, _transform.up, sightRange, AIUtility.VisibilityMask);
            if (hit.collider != null)
            {
                Entity t = GameUtility.FindEntity(hit.collider.gameObject);
                if (t != null && targetGroup.Contains(t))
                {
                    target = t;
                    /*Debug.Log("AI: Target in range " + hit.distance);*/
                    return true;
                }
            }
            return false;
        }
        public bool TargetVisibleAtPosition(Vector3 position,Vector3 direction, float range)
        {
            
            float angle = Vector3.Angle(direction, target.transform.position-position);
            Debug.Log(angle);
            if (angle > visableDegree || angle < -visableDegree)
            {
                return false;
            }
            RaycastHit2D hit = AIUtility.Raycast2D(gameObject, position, target.transform.position - position, range, AIUtility.VisibilityMask);
            if (hit.collider != null)
            {
                Entity t = hit.collider.GetComponent<Entity>();
                if (t != null && t == target)
                {
                    return true;
                }
            }
            return false;
        }
        public void PatrolLine(Vector3 postion)
        {
            pathFinder.MoveTo(postion);
        }

        /// <summary>
        /// Attemps to keep distance from the target
        /// </summary>
        /// <param name="distance">The amount of distance needed</param>
        /// <param name="rotateDegree"> The relative angle to rotate around the target </param>
        public void KeepDistanceFromTarget( float distance , float rotateDegree, float stopDistance = 1)
        {
            StopChaseTarget();
            Quaternion rotation = Quaternion.AngleAxis(rotateDegree, Vector3.forward);
            Vector3 viewDirection = transform.position - target.transform.position;
            viewDirection = (rotation * viewDirection).normalized * distance;

            Vector3 finalPosition = target.transform.position + viewDirection;
            Vector3 finalDirection = (finalPosition - transform.position);
            float targetDistance =finalDirection.magnitude;
            Debug.DrawLine(transform.position, finalPosition, Color.magenta);
            moveControl.MoveInDirection(finalDirection, targetDistance / stopDistance);
        }
        public void Wander(int wanderRadius)
        {
            GraphNode startNode = gridGraph.GetNearest(_transform.position, NNConstraint.Default).node;
            List<GraphNode> nodes = PathUtilities.BFS(startNode, wanderRadius, filter: (GraphNode node) => { return PathUtilities.IsPathPossible(startNode, node); });
            if (nodes.Count > 0)
            {
                Vector3 dest = PathUtilities.GetPointsOnNodes(nodes, 1)[0];
                pathFinder.MoveTo(dest);
            }
        }

        public void LookTowards()
        {
            moveControl.RotateForwardDirection(target.transform.position - _transform.position);
        }

        private bool isTargeting = false;
        public void SetFaceTarget(bool lookAt) {
            if (target != null) {
                isTargeting = lookAt;
            }
        }

        public void MoveInDirection(Vector3 direction, float distance)
        {
            if (moveControl.MovementBlocked)
            {
                return;
            }
            float offset = _collider.bounds.size.x / 2;
            RaycastHit2D hit = Physics2D.Raycast(_transform.position, direction, distance + offset);
            if (hit.collider == null)
            {
                pathFinder.MoveTo(_transform.position + direction.normalized * distance);
            }
            else
            {
                Vector2 dir = (Vector2)direction;
                pathFinder.MoveTo(hit.point - dir.normalized * offset);
            }
        }

        public bool ReachedDestination()
        {
            return pathFinder.ReachedDestination;
        }
    }
}
