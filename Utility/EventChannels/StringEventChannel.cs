using System;
using UnityEngine;
using UnityEngine.Events;

namespace LobsterFramework
{
	[CreateAssetMenu(menuName = "EventChannel/String Channel")]
	public class StringEventChannel : ScriptableObject
	{
		public event Action<string> OnEventRaised;
		public void RaiseEvent(string arg)
		{
            OnEventRaised?.Invoke(arg);
        }
	}
}