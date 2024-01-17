using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LobsterFramework.Utility
{
    /// <summary>
    /// Stats that can combined, meaning it can be affected by multiple effectors. <br/>
    /// i.e The player may be unable to act for some time due to multiple sources of negative effects, <br/>
    /// and the flag setting (CombinedStat) that governs this player state will remain unchanged if not <br/> 
    /// all of these effects (effectors) are removed.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CombinedValue<T> where T : IEquatable<T>
    {
        private IdDistributor distributor = new();
        protected Dictionary<int, T> stats = new();
        protected T baseValue;
        private T value;
        internal Action onEffectorCleared;
        internal Action<T> onValueChanged;

        /// <summary>
        ///
        /// </summary>
        /// <param name="baseValue"> The base value when no effectors are present </param>
        public CombinedValue(T baseValue) { this.baseValue = baseValue; }

        /// <summary>
        /// Return thec cached buffered value
        /// </summary>
        public T Value { 
            get { return stats.Count == 0 ? baseValue : value; }
        }

        /// <summary>
        /// Determines if the effector can be added
        /// </summary>
        /// <param name="obj">The value of the effector to be examined</param>
        /// <returns>true if can be added, otherwise false</returns>
        public virtual bool Compatible(T obj) { return true; }

        internal int AddEffector(T obj) {
            if (!Compatible(obj)) {
                return -1;
            }
            int id = distributor.GetID();
            stats.Add(id, obj);
            T prevValue = value;
            value = ComputeValue();
            if (!prevValue.Equals(value)) {
                onValueChanged?.Invoke(value);
            }
            return id;
        }
        internal bool RemoveEffector(int id) {
            if (stats.Remove(id)) {
                T prevValue = value;
                value = ComputeValue();
                if (!prevValue.Equals(value))
                {
                    onValueChanged?.Invoke(value);
                }
                
                distributor.RecycleID(id);
                return true;
            }
            return false;
        }

        internal bool SetEffector(int id, T obj)
        {
            if (!Compatible(obj)) {
                return false;
            }
            if (stats.ContainsKey(id)) {
                stats[id] = obj;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Remove all effectors
        /// </summary>
        public void ClearEffectors() {
            foreach (int id in stats.Keys) {
                distributor.RecycleID(id);
            }
            stats.Clear();
            onEffectorCleared?.Invoke();
        }

        /// <summary>
        /// Returns a BufferedValueAccessor that manages setting and removing the effector
        /// </summary>
        /// <returns> A BufferedValueAccessor that manages setting and removing the effector </returns>
        public CombinedValueEffector<T> MakeEffector() {
            return new(this);
        }

        /// <summary>
        /// The number of currently active effectors
        /// </summary>
        public int EffectorCount { get { return stats.Count; } }

        /// <summary>
        /// Compute the value taking all effectors into account
        /// </summary>
        /// <returns></returns>
        protected abstract T ComputeValue();
    }

    public class CombinedValueEffector<T> where T : IEquatable<T> {
        private CombinedValue<T> stat;
        private int effectorID = -1;

        internal CombinedValueEffector(CombinedValue<T> stat)
        {
            this.stat = stat;
            stat.onEffectorCleared += Release;
        }

        ~CombinedValueEffector() {
            if (stat != null) {
                stat.onEffectorCleared -= Release;
            }
        }

        /// <summary>
        /// Set a effector to the specified value and apply it
        /// </summary>
        /// <param name="value">The value of the effector to be added</param>
        public void Apply(T value) {
            if (effectorID == -1)
            {
                effectorID = stat.AddEffector(value);
            }
            else {
                Debug.LogWarning("Effector must be released before acquiring new ones.");
            }
        }

        /// <summary>
        /// Remove the effector from CombinedValue
        /// </summary>
        public void Release() {
            if (effectorID != -1)
            {
                stat.RemoveEffector(effectorID);
                effectorID = -1;
            }
        }

        /// <summary>
        /// Change the value of the effector added, effector value must be compatible. If the effector is not applied then nothing will happen
        /// </summary>
        /// <param name="value"> The value to change to </param>
        /// <returns> true if the effector value has been changed, otherwise false </returns>
        public bool SetValue(T value) {
            if (effectorID != -1)
            {
                return stat.SetEffector(effectorID, value);
            }
            return false;
        }
    }

    /// <summary>
    /// Value is the sum of all effectors
    /// </summary>
    public class IntSum : CombinedValue<int>
    {
        public IntSum(int value) : base(value)
        {
        }

        protected override int ComputeValue()
        {
            return stats.Sum(pair => pair.Value) ;
        }
    }

    /// <summary>
    /// Value is the sum of all effectors
    /// </summary>
    public class FloatSum : CombinedValue<float>
    {
        private bool addNegative;
        private bool nonNegative;
        public FloatSum(int value, bool nonNegative=false, bool addNegative=true) : base(value)
        {
            this.nonNegative = nonNegative;
            this.addNegative = addNegative;
        }

        public override bool Compatible(float obj)
        {
            if (!addNegative && obj < 0) {
                return false;
            }
            return true;
        }

        protected override float ComputeValue()
        {
            float total = 0;
            foreach (var pair in stats) {
                total += pair.Value;
            }
            if (nonNegative && total < 0)
            {
                total = 0;
            }
            return total;
        }
    }

    /// <summary>
    /// Value is the product of all effectors
    /// </summary>
    public class FloatProduct : CombinedValue<float>
    {
        private bool nonNegative;

        public FloatProduct(float value, bool nonNegative=false) : base(value)
        {
            this.nonNegative = nonNegative;
        }

        public override bool Compatible(float obj)
        {
            if (nonNegative && obj < 0) {
                return false;
            }
            return true;
        }

        protected override float ComputeValue()
        {
            float value = baseValue;
            foreach (var pair in stats) { 
                value *= pair.Value;
            }
            return value;  
        }
    }

    /// <summary>
    /// Value is true if one effector is true, otherwise return base value
    /// </summary>
    public class Or : CombinedValue<bool> {
        public Or(bool value) : base(value)
        {
        }

        protected override bool ComputeValue()
        {
            foreach (var pair in stats) {
                if (pair.Value) {
                    return true;
                }
            }
            return baseValue;
        }
    }

    /// <summary>
    /// Value is true if all effectors are true, otherwise return base value
    /// </summary>
    public class BaseAnd : CombinedValue<bool>
    {
        public BaseAnd(bool value) : base(value)
        {
        }

        protected override bool ComputeValue()
        {
            foreach (var pair in stats)
            {
                if (!pair.Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
