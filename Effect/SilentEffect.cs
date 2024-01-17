using UnityEngine;
using LobsterFramework.AbilitySystem;
using LobsterFramework.Utility;

namespace LobsterFramework.Effects
{
    [CreateAssetMenu(menuName = "Effect/Silent Effect")]
    public class SilentEffect : Effect
    {
        private AbilityRunner ar;
        private CombinedValueEffector<bool> valueAccessor;

        protected override void OnApply()
        {
            ar = processor.GetComponentInBoth<AbilityRunner>();
            
            if (ar != null) {
                valueAccessor = ar.actionLock.MakeEffector();
                valueAccessor.Apply(true);
            }
        }

        protected override void OnEffectOver()
        {
            if (valueAccessor != null) {
                valueAccessor.Release();
                valueAccessor = null;
            }
        }
    }
}
