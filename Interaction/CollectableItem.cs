using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LobsterFramework.Interaction
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class CollectableItem : MonoBehaviour, IInteractable
    {
        [SerializeField] internal bool destroyWhenDeplete;
        [SerializeField] private InventoryItem item;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>(); 
            if (item != null) {
                spriteRenderer.sprite = item.itemData.Icon;
            }
        }

        public InventoryItem Item
        {
            get { return item; }
            set
            {
                item = value;
                spriteRenderer.sprite = item.itemData.Icon;
                if (destroyWhenDeplete)
                {
                    if (item.Quantity == 0)
                    {
                        Destroy(gameObject);
                    }
                }
            }
        }

        private void Update()
        {
            if (destroyWhenDeplete)
            {
                if (item.Quantity == 0)
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}
