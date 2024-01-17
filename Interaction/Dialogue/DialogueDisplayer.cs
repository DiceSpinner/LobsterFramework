using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

namespace LobsterFramework.Utility
{
    public class DialogueDisplayer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool canStop;
        [SerializeField] private float displaySpeed;
        [SerializeField] private float secondsDelayBetweenLines;
        [SerializeField] private float secondsLingering;
        [SerializeField] private RectTransform dialogueCanva;

        [Header("Dialogue Area")]
        [SerializeField] private TMP_Text mainText;

        [Header("Response Area")]
        [SerializeField] private GameObject responseArea;
        [SerializeField] private RectTransform responseLayoutGroup;
        [SerializeField] private Button responseButton;

        [Header("Speaker Info")]
        [SerializeField] private TMP_Text speakerName;
        [SerializeField] private Image speakerIcon;

        private Coroutine coroutine;
        private CoroutineRunner updateRunner = new();

        private DialogueObject currentDialogue;

        private List<Button> responseButtons = new();

        public Action onStartDisplaying;
        public Action onStopDisplaying;

        // Speed up and confirm
        private bool signaled = false;
        private bool displayingText = false;
        private bool fastForward = false;
        private DialogueNode nodeToBeConfirmed;

        private void Update()
        {
            updateRunner.Run();
        }

        public void Confirm(InputAction.CallbackContext context)
        {
            if (context.started && nodeToBeConfirmed != null) {
                signaled = true;
            }
        }
        public void FastForward(InputAction.CallbackContext context) {
            if (context.started && displayingText) {
                fastForward = true;
            }
        }

        public void SetDialogue(DialogueObject dialogue) {
            StopDisplaying();
            currentDialogue = dialogue;
        }
        public void StopDisplaying() {
            if (coroutine != null)
            {
                coroutine.Stop();
                coroutine = null;
            }
            signaled = false;
            nodeToBeConfirmed = null;
            dialogueCanva.gameObject.SetActive(false);
        }

        public void DisplayDialogue()
        {
            if (coroutine == null)
            {
                coroutine = updateRunner.AddCoroutine(DisplayText());
                onStartDisplaying?.Invoke();
            }
            else if (canStop)
            {
                StopDisplaying();
                currentDialogue = null;
            }
        }

        private void OnDisable()
        {
            if (dialogueCanva != null)
            {
                dialogueCanva.gameObject.SetActive(false);
            }
        }

        public IEnumerator<CoroutineOption> DisplayText()
        {
            if (currentDialogue == null) {
                onStopDisplaying.Invoke();
                yield break;
            }
            dialogueCanva.gameObject.SetActive(true);
            responseArea.SetActive(false);
            
            currentDialogue.onDialogueStart?.Invoke();

            // Begin displaying text in each node
            bool lingerAfterDialogueFinish = true;

            foreach (DialogueNode node in currentDialogue.Nodes)
            {
                lingerAfterDialogueFinish = true;
                speakerName.text = node.Speaker;
                speakerIcon.sprite = node.Icon;
                foreach (string str in node.Texts)
                {
                    float index = 0;
                    displayingText = true;
                    fastForward = false;
                    while (index < str.Length && !fastForward)
                    {
                        mainText.text = str.Substring(0, Mathf.FloorToInt(index));
                        index += Time.unscaledDeltaTime * displaySpeed;
                        yield return CoroutineOption.Continue;
                    }
                    mainText.text = str;
                    displayingText = false;
                    yield return CoroutineOption.WaitForUnscaledSeconds(secondsDelayBetweenLines);
                }

                // Wait for confirmation
                if (node.RequiresConfirmation) {
                    nodeToBeConfirmed = node;
                    lingerAfterDialogueFinish = false;
                }
                while (node.RequiresConfirmation && !signaled) {
                    yield return CoroutineOption.Continue;
                }
                signaled = false;
                nodeToBeConfirmed = null;
            }

            // Instantiate response buttons
            if (currentDialogue.HasResponses)
            {
                responseArea.SetActive(true);
                DialogueResponse[] responses = currentDialogue.Responses;
                foreach (DialogueResponse response in responses)
                {
                    GameObject obj = Instantiate(responseButton.gameObject, responseLayoutGroup);
                    obj.SetActive(true);
                    Button btn = obj.GetComponent<Button>(); 
                    btn.onClick.AddListener(() => Respond(response));
                    btn.GetComponentInChildren<TMP_Text>().text = response.Text;
                    responseButtons.Add(btn);
                }
            }

            coroutine = null;

            // Hide the canvas if there's no response to display
            if (responseButtons.Count == 0)
            {
                if (lingerAfterDialogueFinish) {
                    Debug.Log("Lingering");
                    yield return CoroutineOption.WaitForUnscaledSeconds(secondsLingering);
                }
                dialogueCanva.gameObject.SetActive(false);
                onStopDisplaying.Invoke();
            }
        }

        public void Respond(DialogueResponse response)
        {
            currentDialogue.onDialogueFinished?.Invoke();
            response.Respond();
            responseArea.SetActive(false);
            if (response.Dialogue != null)
            {
                currentDialogue = response.Dialogue;
                coroutine = updateRunner.AddCoroutine(DisplayText());
            }
            else
            {
                currentDialogue = null;
                onStopDisplaying?.Invoke();
                dialogueCanva.gameObject.SetActive(false);
            }

            foreach (Button b in responseButtons)
            {
                Destroy(b.gameObject);
            }
            responseButtons.Clear();
        }
    }
}
