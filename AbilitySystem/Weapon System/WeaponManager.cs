using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Animancer;
using LobsterFramework.Utility;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// Component that manages equipping and swaping weapons
    /// </summary>
    public class WeaponManager : MonoBehaviour
    {
        [Header("Weapon Prefabs")]
        [SerializeField] private GameObject mainhandWeapon1;
        [SerializeField] private GameObject mainhandWeapon2;
        [SerializeField] private GameObject offhandWeapon1;
        [SerializeField] private GameObject offhandWeapon2;
        [SerializeField] private GameObject emptyHand;

        [Header("Weapon Position")]
        [SerializeField] private Transform mainhandWeaponPosition;
        [SerializeField] private Transform offhandWeaponPosition;

        [Header("Animation Data")]
        [SerializeField] private CharacterWeaponAnimationData animationData;

        [Header("Component Reference")]
        [SerializeField] private Entity entity;
        [SerializeField] private AnimancerComponent animancer;
        [SerializeField] private AbilityManager abilityManager;

        private Weapon mainWeapon1;
        private Weapon mainWeapon2;
        private Weapon offWeapon1;
        private Weapon offWeapon2;
        private Weapon emptyMHandWeapon;
        private Weapon emptyOHandWeapon;

        private GameObject mainWeapon1Inst;
        private GameObject mainWeapon2Inst;
        private GameObject offWeapon1Inst;
        private GameObject offWeapon2Inst;
        private GameObject emptyMHandInst;
        private GameObject emptyOHandInst;

        private readonly Dictionary<Weapon, GameObject> objLookup = new();

        public Weapon Mainhand { get; private set; }
        public Weapon Mainhand2 { get; private set; }
        public Weapon Offhand { get; private set; }
        public Weapon Offhand2 { get; private set; }

        public Entity Wielder { get { return entity; } } 
        public CharacterWeaponAnimationData AnimationData { get { return animationData; } }

        private void Awake()
        {
            SetUpEmptyHand();
            SetUpMainhand();
            SetUpOffhand();
            
            if (Mainhand != null && Mainhand.DoubleHanded) {
                if (Offhand != null)
                {
                    objLookup[Offhand].SetActive(false);
                }
            }
        }

        private void SetUpEmptyHand() {
            if (emptyHand != null)
            {
                emptyMHandInst = Instantiate(emptyHand);
                emptyOHandInst = Instantiate(emptyHand);
                emptyMHandWeapon = emptyMHandInst.GetComponentInChildren<Weapon>();
                emptyOHandWeapon = emptyOHandInst.GetComponentInChildren<Weapon>();
                emptyOHandWeapon.Entity = entity;
                emptyMHandWeapon.Entity = entity;
                objLookup[emptyMHandWeapon] = emptyMHandInst;
                objLookup[emptyOHandWeapon] = emptyOHandInst;

                emptyMHandInst.transform.position = mainhandWeaponPosition.transform.position;
                emptyMHandInst.transform.up = mainhandWeaponPosition.transform.up;
                emptyMHandInst.transform.SetParent(mainhandWeaponPosition.transform);

                emptyOHandInst.transform.position = offhandWeaponPosition.transform.position;
                emptyOHandInst.transform.up = offhandWeaponPosition.transform.up;
                emptyOHandInst.transform.SetParent(offhandWeaponPosition.transform);

                emptyMHandInst.SetActive(false);
                emptyOHandInst.SetActive(false);
            }
        }

        private void SetUpMainhand() {
            if (mainhandWeapon1 != null)
            {
                mainWeapon1Inst = Instantiate(mainhandWeapon1);
                mainWeapon1 = mainWeapon1Inst.GetComponentInChildren<Weapon>();
                mainWeapon1.Entity = entity;
                mainWeapon1Inst.transform.SetPositionAndRotation(mainhandWeaponPosition.position, mainhandWeaponPosition.rotation);
                mainWeapon1Inst.transform.SetParent(mainhandWeaponPosition);
                Mainhand = mainWeapon1;
                mainWeapon1.weaponManager = this;
                objLookup[mainWeapon1] = mainWeapon1Inst;
            }
            if (mainhandWeapon2 != null)
            {
                mainWeapon2Inst = Instantiate(mainhandWeapon2);
                mainWeapon2 = mainWeapon2Inst.GetComponentInChildren<Weapon>();
                mainWeapon2.Entity = entity;
                mainWeapon2Inst.transform.SetPositionAndRotation(mainhandWeaponPosition.position, mainhandWeaponPosition.rotation);
                mainWeapon2Inst.transform.SetParent(mainhandWeaponPosition);
                if (mainhandWeapon1 == null)
                {
                    Mainhand = mainWeapon2;
                }
                else
                {
                    mainWeapon2Inst.SetActive(false);
                    Mainhand2 = mainWeapon2;
                }
                mainWeapon2.weaponManager = this;
                objLookup[mainWeapon2] = mainWeapon2Inst;
            }
            if (emptyHand != null) {
                if (Mainhand == null) {
                    Mainhand = emptyMHandWeapon;
                    emptyMHandInst.SetActive(true);
                }
                else if (Mainhand2 == null) {
                    Mainhand2 = emptyMHandWeapon;
                    emptyMHandInst.SetActive(true);
                }
            }
        }

        private void SetUpOffhand() {
            if (offhandWeapon1 != null)
            {
                offWeapon1Inst = Instantiate(offhandWeapon1);
                offWeapon1 = offWeapon1Inst.GetComponentInChildren<Weapon>();
                offWeapon1.Entity = entity;
                offWeapon1Inst.transform.SetPositionAndRotation(offhandWeaponPosition.position, offhandWeaponPosition.rotation);
                offWeapon1Inst.transform.SetParent(offhandWeaponPosition);
                Offhand = offWeapon1;
                objLookup[offWeapon1] = offWeapon1Inst;
            }
            if (offhandWeapon2 != null)
            {
                offWeapon2Inst = Instantiate(offhandWeapon2);
                offWeapon2 = offWeapon2Inst.GetComponentInChildren<Weapon>();
                offWeapon2.Entity = entity;
                offWeapon2Inst.transform.SetPositionAndRotation(offhandWeaponPosition.position, offhandWeaponPosition.rotation);
                offWeapon2Inst.transform.SetParent(offhandWeaponPosition);
                if (offhandWeapon1 == null)
                {
                    Offhand = offWeapon2;
                }
                else
                {
                    offWeapon2Inst.SetActive(false);
                    Offhand2 = offWeapon2;
                }
                objLookup[offWeapon2] = offWeapon2Inst;
            }
            if (emptyHand != null) {
                if (Offhand == null) {
                    Offhand = emptyOHandWeapon;
                    emptyOHandInst.SetActive(true);
                }
                else if (Offhand2 == null) {
                    Offhand2 = emptyOHandWeapon;
                }
            }
        }

        public void SwitchMainHand() {
            if (Mainhand != null && Mainhand.State == WeaponState.Idle)
            {
                if (Mainhand2 != null)
                {
                    Mainhand.Disable();
                    (Mainhand2, Mainhand) = (Mainhand, Mainhand2);
                    objLookup[Mainhand2].SetActive(false);
                    objLookup[Mainhand].SetActive(true);
                    if (abilityManager.IsAnimating)
                    {
                        abilityManager.InterruptAbilityAnimation();
                    }
                    PlayTransitionAnimation();
                }
            }
            if (Offhand != null)
            {
                if (Mainhand != null && Mainhand.DoubleHanded)
                {
                    objLookup[Offhand].SetActive(false);
                }
                else {
                    objLookup[Offhand].SetActive(true);
                }
            }
        }

        public void SwitchOffHand()
        {
            if (Offhand != null && Offhand.State == WeaponState.Idle)
            {
                if (Offhand2 != null)
                {
                    Offhand.Disable();
                    Weapon w = Offhand;
                    objLookup[Offhand].SetActive(false);
                    Offhand = Offhand2;
                    Offhand2 = w;
                    if (Mainhand != null && !Mainhand.DoubleHanded) {
                        objLookup[Offhand].SetActive(true);
                    }
                }
            }
        }

        public void PlayWeaponAnimation() {
            if (Mainhand != null && animancer != null)
            {
                AnimationClip clip = animationData.GetMoveClip(Mainhand.WeaponType);
                AnimancerState state = animancer.States.Current;
               
                if (state != null)
                {
                    state.IsPlaying = false;
                }
                animancer.Play(clip, 0.25f, FadeMode.FixedDuration).Speed = 1;
            }
        }

        public void PlayTransitionAnimation() {
            if(Mainhand != null && animancer != null)
            {
                if (abilityManager != null && !abilityManager.IsAnimating) {
                    AnimationClip clip = animationData.GetMoveClip(Mainhand.WeaponType);
                    animancer.Play(clip, 0.25f, FadeMode.FixedDuration).Speed = 1;
                }
            }
        }
    }
}
