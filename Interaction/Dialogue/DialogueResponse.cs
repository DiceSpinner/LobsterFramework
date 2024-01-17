using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace LobsterFramework.Utility
{
    [System.Serializable]
    public class DialogueResponse
    {
        [TextArea]
        [SerializeField] private string text;
        [SerializeField] private DialogueObject dialogue;
        public Action onResponseSelected;

        public void Respond()
        {
            onResponseSelected?.Invoke();
        }

        public DialogueObject Dialogue
        {
            get { return dialogue; }
        }

        public string Text
        {
            get { return text; }
        }
    }
}
