using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace LobsterFramework.Interaction
{
    /// <summary>
    /// Allows interaction with InteractableObjects. Inherit from this class to implement custom interaction behaviors.
    ///  Use InteractionHandler and InteractabilityChecker attributes to mark methods responsible for implementations.
    ///  Details see <see cref="InteractionHandlerAttribute"/> and <see cref="InteractabilityCheckerAttribute"/>. <br/><br/>
    /// 
    /// Use <see cref="RegisterInteractorAttribute"/> to register marked methods as interaction handlers and interactability checkers for this interactor.
    /// Unregistered interactor will not be able to perform interactions even if handlers and checkers are defined. <br/><br/>
    /// 
    ///  Each interactor can only have 1 implementation of interation handling and interactability checker with 1 type of InteractableObject.
    ///  Interactors without any interaction handlers are allowed but will not be able do interactions.
    /// </summary>

    [RequireComponent(typeof(Collider2D))]
    public abstract class Interactor : MonoBehaviour
    {
        [SerializeField] private StringEventChannel responseChannel;
        private Dictionary<Collider2D, IInteractable[]> objectsInRange = new();
        private HashSet<IInteractable> interactables = new();

        /// <summary>
        /// Called when an interactable gets in range
        /// </summary>
        public Action<IInteractable> onInteractableAdded;
        /// <summary>
        /// Called when an interactable goes out of range
        /// </summary>
        public Action<IInteractable> onInteractableRemoved;

        /// <summary>
        /// Find any interactable objects on the collided object and add them to the interactables.
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            IInteractable[] objs = collision.GetComponents<IInteractable>();
            foreach (IInteractable obj in objs) {
                interactables.Add(obj);
                onInteractableAdded?.Invoke(obj);
            }
            if (objs.Length != 0) {
                objectsInRange[collision] = objs;
            }
        }

        /// <summary>
        /// Remove all interactable objects on this object from the interactables when it goes out of range
        /// </summary>
        /// <param name="collision"></param>
        private void OnTriggerExit2D(Collider2D collision)
        {
            if (!objectsInRange.ContainsKey(collision)) {
                return;
            }
            IInteractable[] objs = objectsInRange[collision];
            foreach (IInteractable obj in objs) {
                interactables.Remove(obj);
                onInteractableRemoved?.Invoke(obj);
            }
            objectsInRange.Remove(collision);
        }

        /// <summary>
        /// Get all of the interatables in range of this interactor. Note that this does not guarantee that they can be interacted with. 
        /// Use CheckInteractability to check for this info.
        /// </summary>
        /// <returns>A list contains all of the interactable objects in range of this interactor</returns>
        public void GetInteractablesInRange(List<IInteractable> objects){
            objects.Clear();
            objects.AddRange(interactables);
        }

        /// <summary>
        /// Check the interactablity between the interactor and the object using the corresponding checker, if no checker is defined, return default value where no interaction is available
        /// </summary>
        /// <param name="interactableObject">The object to be queried</param>
        /// <returns>The interactability of this object</returns>
        public InteractionPrompt CheckInteractability(IInteractable interactableObject)
        {
            return InteractabilityCheckerAttribute.GetInteractionPrompts(this, interactableObject);
        }

        /// <summary>
        ///  Attempts to interact with the interactable
        /// </summary>
        /// <param name="interactable">The object to interact with</param>
        /// <param name="interactionType">The type of the interaction</param>
        public void Interact(IInteractable interactable, InteractionType interactionType)
        {
            // Do nothing if object is out of range
            if (interactable == null || !interactables.Contains(interactable)) {
                return;
            }

            // Check if the interactor's self constraint is satisfied
            InteractionPrompt prompts = CheckInteractability(interactable);
            if (!prompts.Available(interactionType)) {
                return;
            }

            string result = InteractionHandlerAttribute.HandleInteraction(this, interactable, interactionType);

            if (result != default) {
                responseChannel?.RaiseEvent(result);
            }
        }
    }
}
