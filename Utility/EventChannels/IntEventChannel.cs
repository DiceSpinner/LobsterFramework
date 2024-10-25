using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace LobsterFramework
{
	[CreateAssetMenu(menuName = "EventChannel/IntEventChannel")]
	public class IntEventChannel : DescriptionBaseSO
	{
		public event Action<int> OnEventRaised;
		public void RaiseEvent(int arg)
		{
            OnEventRaised?.Invoke(arg);
        }
	}
}
