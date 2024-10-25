using System;
using UnityEngine;
using UnityEngine.Events;

namespace LobsterFramework
{
	[CreateAssetMenu(menuName = "EventChannel/FloatEventChannel")]
	public class FloatEventChannel : DescriptionBaseSO
	{
		public event Action<float> OnEventRaised;
		public void RaiseEvent(float arg)
		{
            OnEventRaised?.Invoke(arg);
        }
	}
}
