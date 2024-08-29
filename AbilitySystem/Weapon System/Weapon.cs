using System.Collections.Generic;
using UnityEngine;
using LobsterFramework.Utility;
using LobsterFramework.Pool;
using System;

namespace LobsterFramework.AbilitySystem.WeaponSystem
{
    /// <summary>
    /// 
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private string weaponName;
        [SerializeField] private WeaponType weaponType;
        [SerializeField] private float weight;
        [SerializeField] private float sharpness;
        [SerializeField] private float attackSpeed;
        [SerializeField] private float defenseSpeed;
        [SerializeField] private bool doubleHanded;

        [Header("VFX")]
        [SerializeField] private VarString clashSpark;

        [Header("Positions")]
        [SerializeField] private Transform head;
        [SerializeField] private Transform tail; 

        [Header("Guard")]
        [Range(0, 1)]
        [SerializeField] private float healthDamageReduction;
        [Range(0, 1)]
        [SerializeField] private float postureDamageReduction;
        [DisplayName("Rotation Speed Modifier"), Range(0, 1)]
        [SerializeField] private float gRotationSpeedModifier;
        [DisplayName("Move Speed Modifier"), Range(0, 1),]
        [SerializeField] private float gMoveSpeedModifier;

        [Header("Attack")]
        [DisplayName("Rotation Speed Modifier"), Range(0, 1)]
        [SerializeField] private float lRotationSpeedModifier;
        [DisplayName("Move Speed Modifier"), Range(0, 1),]
        [SerializeField] private float lMoveSpeedModifier;

        [Header("Charged Attack")]
        [DisplayName("Rotation Speed Modifier"), Range(0, 1)]
        [SerializeField] private float hRotationSpeedModifier;
        [DisplayName("Move Speed Modifier"), Range(0, 1),]
        [SerializeField] private float hMoveSpeedModifier;

        [Header("Special Move")]  
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private WeaponArtSelector abilitySelector;
        private WeaponStatDictionary weaponStats;

        #region Event
        public event Action<Entity> OnEntityHit;
        public event Action<Weapon> OnWeaponHit;
        public event Action OnWeaponDeflect;
        #endregion

        #region Cached References
        private Collider2D thisCollider;
        private Transform thisTransform;
        internal WeaponManager weaponManager;
        #endregion

        public string Name { get { return weaponName; } }
        public WeaponType WeaponType { get { return weaponType; } }

        public ValueTuple<Type, string> AbilitySetting { get { return ValueTuple.Create(abilitySelector.Type, abilitySelector.Instance); } }
        public float Weight { get { return weight; } }
        public float Sharpness { get { return sharpness; } }
        public float AttackSpeed { get {  return attackSpeed; } }

        public float DefenseSpeed { get { return defenseSpeed; } }

        public bool DoubleHanded { get { return doubleHanded; } }

        public string ClashSpark { get { return clashSpark; } }

        public Damage OnHitDamage { get; private set; }

        public Transform Head { get { return head; } }
        public Transform Tail { get { return tail; } }  

        #region Guard
        public float HealthDamageReduction { get { return healthDamageReduction; } }
        public float PostureDamageReduction { get { return postureDamageReduction; } }
        public float GMoveSpeedModifier { get { return gMoveSpeedModifier; } }
        public float GRotationSpeedModifier { get { return gRotationSpeedModifier; } }
        #endregion

        #region LightAttack
        public float LMoveSpeedModifier { get { return lMoveSpeedModifier; } }
        public float LRotationSpeedModifier { get { return lRotationSpeedModifier; } }
        #endregion

        #region HeavyAttack
        public float HMoveSpeedModifier { get { return hMoveSpeedModifier; } }
        public float HRotationSpeedModifier { get { return hRotationSpeedModifier; } }
        #endregion

        public Entity Entity { get; set; }

        private HashSet<Entity> hitted;
        private HashSet<Entity> newHit;

        [field:SerializeField]
        public WeaponState State { get; set; }
        // Start is called before the first frame update
        private void Awake()
        {
            thisCollider = GetComponent<Collider2D>();
            thisCollider.enabled = false;
            thisTransform = GetComponent<Transform>();
            State = WeaponState.Idle;
            newHit = new();
            hitted = new();
            if (weaponData != null)
            {
                weaponStats = weaponData.weaponStats;
            }
        }

        #region WeaponAction
        public void SetOnHitDamage(float health, float posture) {
            OnHitDamage = new Damage { health=health, posture=posture, source=Entity, type=DamageType.WeaponHit};
        }
        public void SetOnHitDamage(Damage damage)
        {
            if (damage == Damage.none)
            {
                OnHitDamage = Damage.none;
            }
            else {
                OnHitDamage = new Damage { health = damage.health, posture = damage.posture, source = Entity, type = DamageType.WeaponHit };
            }
        }

        /// <summary>
        /// Enable the collider of the weapon and set its weapon state. Momentum will start to accumulate after this. Can only be used after calling On().
        /// </summary>
        /// <param name="state">The weapon state to set the weapon to be</param>
        public void Enable(WeaponState state = WeaponState.Attacking) {
            thisCollider.enabled = true;
            this.State = state;
        }
        /// <summary>
        /// Disable the weapon collider and reset momentum
        /// </summary>
        public void Disable()
        {
            thisCollider.enabled = false;
            State = WeaponState.Idle;
            hitted.Clear();
        }

        /// <summary>
        /// Disable the weapon collider but keep the weapon occupied s.t other offensive abilities cannot override.
        /// </summary>
        public void Pause()
        {
            thisCollider.enabled = false;
            State = WeaponState.Occupied;
            hitted.Clear();
        }
        #endregion

        #region WeaponStat
        /// <summary>
        /// Get the weapon stat of specified type if exists
        /// </summary>
        /// <typeparam name="T">The type of the weapon stat to be fetched</typeparam>
        /// <returns>The target weapon stat if exists, otherwise null</returns>
        public T GetWeaponStat<T>() where T : WeaponStat
        {
            string key = typeof(T).AssemblyQualifiedName;
            if (weaponStats != null && weaponStats.TryGetValue(key, out WeaponStat stat))
            {
                return (T)stat;
            }
            return default;
        }

        /// <summary>
        /// Check if the WeaponStat is present on this weapon
        /// </summary>
        /// <typeparam name="T">The type of WeaponStat being queried</typeparam>
        /// <returns>True if exists, otherwise false</returns>
        public bool HasWeaponStat<T>() where T : WeaponStat {
            string key = typeof(T).AssemblyQualifiedName;
            return weaponStats != null && weaponStats.ContainsKey(key);
        }

        /// <summary>
        /// Check if the WeaponStat is present on this weapon
        /// </summary>
        /// <param name="type"> The type of the WeaponStat being queried </param>
        /// <returns>True if exists, otherwise false</returns>
        public bool HasWeaponStat(Type type)
        {
            string key = type.AssemblyQualifiedName;
            return weaponStats != null && weaponStats.ContainsKey(key);
        }
        #endregion

        private void OnTriggerEnter2D(Collider2D collider)
        {
            // Do nothing if weapon is not attacking
            if (State != WeaponState.Attacking) {
                return;
            }

            // Add entity to the list for processing in Update()
            if (collider.TryGetComponent(out Entity entity) && !hitted.Contains(entity)) {
                newHit.Add(entity);
                hitted.Add(entity);
                return;
            }

            // Attack guarded entity
            if (collider.TryGetComponent(out Weapon weapon) && (weapon.State == WeaponState.Guarding || weapon.State == WeaponState.Deflecting) && OnWeaponHit != null && (!hitted.Contains(weapon.Entity) || newHit.Contains(weapon.Entity)))
            {
                OnWeaponHit.Invoke(weapon);
                if (newHit.Contains(weapon.Entity))
                {
                    newHit.Remove(weapon.Entity);
                }
                hitted.Add(weapon.Entity);
                // OnHitDamage will be set to none if the entity is not a target
                if (OnHitDamage != Damage.none) {
                    Damage original = OnHitDamage;
                    Damage damage = WeaponUtility.ComputeGuardDamage(weapon, OnHitDamage);
                    if (weapon.State == WeaponState.Deflecting)
                    {
                        Damage deflectDamage = new Damage { health = 0, posture = original.posture * WeaponUtility.PostureDeflect, source = weapon.Entity, type=DamageType.WeaponDeflect };
                        Entity.Damage(deflectDamage);
                        weapon.OnWeaponDeflect?.Invoke();

                        damage.health = 0;
                        damage.posture *= WeaponUtility.PDamageOnDeflect;
                        weapon.Entity.Damage(damage);
                    }
                    else
                    {
                        weapon.Entity.Damage(damage);
                    }

                    // Apply knockback to entity
                    if (weapon.Entity.TryGetComponent<MovementController>(out var moveControl))
                    {
                        moveControl.ApplyForce(weapon.Entity.transform.position - Entity.transform.position, damage.posture * WeaponUtility.KnockbackAdjustment);
                    }

                    // Spawn firespark 
                    if (weapon.ClashSpark != null)
                    {
                        Vector2 point = Physics2D.ClosestPoint(thisTransform.position, collider);
                        ObjectPool.GetObject(weapon.ClashSpark, point, Quaternion.identity);
                    }
                }
            }
        }

        private void Update()
        {
            if(OnEntityHit != null)
            {
                foreach (Entity entity in newHit)
                {
                    OnEntityHit.Invoke(entity);
                    if (OnHitDamage != Damage.none) { 
                        entity.Damage(OnHitDamage);
                        MovementController moveControl = entity.GetComponent<MovementController>();
                        if (moveControl != null)
                        {
                            moveControl.ApplyForce(entity.transform.position - Entity.transform.position, OnHitDamage.posture * WeaponUtility.KnockbackAdjustment);
                        }
                    }
                }
            }
            
            newHit.Clear();
        }

        private void OnValidate()
        {
            if (attackSpeed <= 0) {
                Debug.LogWarning($"Attack Speed Can't be non-positive {attackSpeed}", gameObject);
                attackSpeed = 1;
            }
            if(defenseSpeed <= 0) {
                Debug.LogWarning($"Guard Speed Can't be non-positive {defenseSpeed}", gameObject);
                defenseSpeed = 1;
            }
            if (abilitySelector.weaponType != weaponType) {
                abilitySelector.weaponType = weaponType;
            } 
        }
    }

    #region Weapon Enums
    [Serializable]
    public enum WeaponType { 
        // Mainhand
        Sword,
        Hammer,
        Dagger,
        Stick,

        // Empty
        EmptyHand, 
        
        // Offhand
        Firearm 
    }

    public enum WeaponState { 
        Attacking,
        Guarding,
        Deflecting,
        Occupied,
        Idle
    }
    #endregion
}
