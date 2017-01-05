using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public abstract class ItemBase : NetworkBehaviour {

    protected bool fireHeldDown = false;
    [NonSerialized]
    public UI_TextManagement UI_Manager;
    [NonSerialized]
    public bool isOnLocal = false;
    [NonSerialized]
    public bool isActive = false;

    public float itemSlowDown = 1f;
    [NonSerialized]
    public InventoryManager inventoryManager;

    [NonSerialized,SyncVar]
    public GameObject owner;

    [NonSerialized, SyncVar]
    public int inventroySlot = -1;

    public string displayName = "none";

    [NonSerialized]
    public bool hasBeenSet;

    // Use this for initialization
    void Start ()
    {
        OnItemStart();
    }
	
	protected virtual void OnItemStart()
    {
        //Debug.Log("===== new log object start");

        
        if (transform.parent == null)
        {
           
            transform.SetParent(owner.transform);
        }
       
        if (!hasBeenSet)
        {
           // Debug.Log("fuck my pants are down");
            inventoryManager = owner.GetComponent<InventoryManager>();

            inventoryManager.AssignToInventory(this);

        }

        //Debug.Log("inventoryslot = " + inventroySlot);

    }

   public abstract void OnStartFire();

    public abstract void OnEndFire();

    public abstract void OnStartAltFire();

    public abstract void OnEndAltFire();


    public abstract void OnChangeMode();

    public abstract void OnReload();

    public virtual void OnFireHeld(bool heldDown)
    {
        fireHeldDown = heldDown;
    }

    public virtual void SetUiElements()
    {
        UI_Manager.Change_ItemName(displayName);
    }

    public virtual void SetItemSlowDown( bool doSlowDown)
    {
        inventoryManager.networkedPlayer.fpsController.SetSlowDown(doSlowDown, itemSlowDown);
    }

    public virtual void DeactivateItem()
    {
        OnEndFire();
        OnEndAltFire();
        Debug.Log("deactivate called");
    }
}
