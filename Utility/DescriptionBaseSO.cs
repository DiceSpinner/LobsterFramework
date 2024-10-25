using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework
{
	/// <summary>
	/// Scriptable objects with descriptions
	/// </summary>
	public class DescriptionBaseSO : ScriptableObject
	{
		[TextArea, SerializeField] private string description;
	}
}
