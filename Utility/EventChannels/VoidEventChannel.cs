using System;
using UnityEngine;
using UnityEngine.Events;

namespace LobsterFramework
{
	[CreateAssetMenu(menuName = "EventChannel/VoidEventChannel")]

	public class VoidEventChannel : DescriptionBaseSO
	{
		public event Action OnEventRaised;
		public void RaiseEvent()
		{
            OnEventRaised?.Invoke();
        }
	}
}
