using UnityEngine;
using LobsterFramework.AbilitySystem;
using LobsterFramework.Utility;

namespace LobsterFramework.Effects
{
    [CreateAssetMenu(menuName = "Effect/Silent Effect")]
    public class SilentEffect : Effect
    {
        private AbilityManager ar;
        private CombinedValueEffector<bool> valueAccessor;

        protected override void OnApply()
        {
            ar = processor.GetComponentInBoth<AbilityManager>();
            
            if (ar != null) {
                valueAccessor = ar.ActionBlocked.MakeEffector();
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
