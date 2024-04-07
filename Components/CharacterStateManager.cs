using LobsterFramework.AbilitySystem;
using LobsterFramework.Utility;
using UnityEngine;
using Animancer;
using System;

namespace LobsterFramework
{
    [RequireComponent(typeof(Entity))]
    [RequireComponent(typeof(Poise))]
    [RequireComponent(typeof(MovementController))]
    public class CharacterStateManager : MonoBehaviour
    {
        [Header("Component Reference")]
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private WeaponManager weaponWielder;
        [SerializeField] private AbilityManager abilityManager;

        [Header("Animations")]
        [SerializeField] private AnimationClip onPostureBroken;

        [Header("Status")]
        [ReadOnly][SerializeField] private CharacterState characterState;
        private readonly static Array enumStates = Enum.GetValues(typeof(CharacterState));
        private bool[] stateMap;

        // Requried Components
        private Entity entity;
        private Poise poise;
        private MovementController moveControl;

        // Suppression
        public readonly OrValue suppression = new(false);

        // Movement block keys
        private CombinedValueEffector<bool> postureMoveLock;
        private CombinedValueEffector<bool> poiseMoveLock;
        private CombinedValueEffector<bool> poiseAbilityLock;
        private CombinedValueEffector<bool> suppressMoveLock;
        private CombinedValueEffector<bool> suppressAbilityLock;

        void Start()
        {
            poise = GetComponent<Poise>();
            entity = GetComponent<Entity>();
            moveControl = GetComponent<MovementController>();

            postureMoveLock = moveControl.movementLock.MakeEffector();
            poiseMoveLock = moveControl.movementLock.MakeEffector();
            suppressMoveLock = moveControl.movementLock.MakeEffector();

            suppressAbilityLock = abilityManager.actionLock.MakeEffector(); 
            poiseAbilityLock = abilityManager.actionLock.MakeEffector();

            characterState = CharacterState.Normal;
            poise.onPoiseStatusChange += OnPoiseStatusChanged;
            entity.onPostureStatusChange += OnPostureStatusChanged;
            abilityManager.onAnimationEnd += OnAbilityAnimationEnd;
            abilityManager.onAnimationBegin += OnAbilityAnimationBegin;

            PlayAnimation(CharacterState.Normal);
            stateMap = new bool[enumStates.Length];
        }

        private void OnEnable()
        {
            suppression.onValueChanged += OnSuppressionStatusChanged;
        }

        private void OnDisable()
        {
            suppression.onValueChanged -= OnSuppressionStatusChanged;
        }

        #region StatusListeners
        private void OnSuppressionStatusChanged(bool suppressed) {
            if (suppressed)
            {
                suppressMoveLock.Apply(true);
                suppressAbilityLock.Apply(true);
                moveControl.SetVelocityImmediate(Vector2.zero);
            }
            else {
                stateMap[(int)CharacterState.Suppressed] = false;
                suppressMoveLock.Release();
                suppressAbilityLock.Release();
                ComputeStateAndPlayAnimation();
            }
        }
        

        private void OnPoiseStatusChanged(bool poiseBroken)
        {
            stateMap[(int)CharacterState.PoiseBroken] = poiseBroken;
            if (poiseBroken)
            {
                poiseMoveLock.Apply(true);
                poiseAbilityLock.Apply(true);
            }
            else
            {
                poiseMoveLock.Release();
                poiseAbilityLock.Release();
            }
            ComputeStateAndPlayAnimation();
        }

        private void OnPostureStatusChanged(bool postureBroken)
        {
            stateMap[(int)CharacterState.PostureBroken] = postureBroken;
            if (postureBroken)
            {
                postureMoveLock.Apply(true);
            }
            else
            {
                postureMoveLock.Release();
            }
            ComputeStateAndPlayAnimation();
        }

        private void OnAbilityAnimationEnd(Type ability)
        {
            stateMap[(int)CharacterState.AbilityCasting] = false;
            ComputeStateAndPlayAnimation();
        }

        private void OnAbilityAnimationBegin(Type ability)
        {
            stateMap[(int)CharacterState.AbilityCasting] = true;
            ComputeStateAndPlayAnimation();
        }
        #endregion

        private void ComputeStateAndPlayAnimation()
        {
            CharacterState prev = characterState;
            ComputeState();
            if (prev != characterState)
            {
                PlayAnimation(prev);
            }
        }

        private void ComputeState()
        {
            foreach (CharacterState state in enumStates)
            {
                if (stateMap[(int)state])
                {
                    characterState = state;
                    return;
                }
            }
            characterState = CharacterState.Normal;
        }

        private void PlayAnimation(CharacterState prevState)
        {
            switch (characterState)
            {
                case CharacterState.Normal:
                    if (prevState == CharacterState.AbilityCasting)
                    {
                        weaponWielder.PlayWeaponAnimation();
                    }
                    else
                    {
                        weaponWielder.PlayTransitionAnimation();
                    }
                    break;
                case CharacterState.PostureBroken:
                    PlayPostureBrokenClip();
                    break;
                case CharacterState.Dashing:
                    break;
                case CharacterState.PoiseBroken:
                    PlayPostureBrokenClip();
                    break;
                case CharacterState.Suppressed:
                    PlayPostureBrokenClip();
                    break;
                default: break;
            }
        }
        private void PlayPostureBrokenClip(float fadeTime = 0.15f)
        {
            foreach (AnimancerState state in animancer.States)
            {
                state.IsPlaying = false;
            }
            animancer.Play(onPostureBroken, fadeTime, FadeMode.FromStart);
        }

        /// <summary>
        /// The state of the character, ordered by their priorities. If the conditions for multiple character states are met, only the one with the highest
        /// priority will take place.
        /// </summary>
        public enum CharacterState
        {
            PostureBroken,
            PoiseBroken,
            Suppressed,
            Dashing,
            AbilityCasting,
            Normal,
        }
    }
}
