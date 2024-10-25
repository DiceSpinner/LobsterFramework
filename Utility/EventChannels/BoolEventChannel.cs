using System;
using UnityEngine;

namespace LobsterFramework
{
	[CreateAssetMenu(menuName = "EventChannel/BoolEventChannel")]
	public class BoolEventChannel : DescriptionBaseSO
	{
		public event Action<bool> OnEventRaised;
		public void RaiseEvent(bool arg)
		{
            OnEventRaised?.Invoke(arg);
        }
	}
}
