using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.InteropServices;

namespace LobsterFramework.Interaction
{
    public enum InteractionType
    {
        Primary,
        Secondary,
        Tertiary,
        Quaternary
    }

    public struct InteractionPrompt
    {
        public static InteractionPrompt none = new() { };
        public static implicit operator bool(InteractionPrompt prompt) { return prompt.Primary != default || prompt.Secondary != default || prompt.Tertiary != default || prompt.Quaternary != default; }

        public string Primary { get; set; }
        public string Secondary { get; set; }
        public string Tertiary { get; set; }
        public string Quaternary { get; set; }

        public bool Available(InteractionType interactionType) {
            switch (interactionType)
            {
                case InteractionType.Primary:
                    return Primary != default;
                case InteractionType.Secondary:
                    return Secondary != default;
                case InteractionType.Tertiary:
                    return Tertiary != default;
                default:
                    return Quaternary != default;
            }
        }
    }

    public abstract class InteractableObject : MonoBehaviour
    {
    }
}
