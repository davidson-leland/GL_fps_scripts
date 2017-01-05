using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class InventoryManager :NetworkBehaviour {

    public Transform    bulletSpawn, 
                        cameraTransform, 
                        BulletParticalLocation, 
                        MuzzleFlashLocation, 
                        grenadeSpawnLocation;

    public NetworkedPlayer networkedPlayer;


    public GameObject[] assignedInventory = new GameObject[6];
    
    
    ItemBase[] inventory = new ItemBase[6];//do not assign in editor as of yet
    public ItemBase activeItem;

    public UI_TextManagement UI_Manager;
    public BoxCollider meleeHitBox;

    public Transform parentTransform;

    public GameObject inventoryObject;
    // Use this for initialization
	void Start () {

        if (isServer)
        {
            SetInventory();
            //Invoke("SetInventory", 2);
        }
        
        
        //
	}

    public int GetInventory(int i)
    {
        return inventory.Length;
    }

    private void SetInventory()//will have to redo this to be handles via server
    {
        int i = 0;
        while(i < assignedInventory.Length && assignedInventory[i] != null)
        {
            AssignToInventorySlot(i);
            i++;
        }

        activeItem = inventory[0];
        activeItem.isActive = true;
        activeItem.SetUiElements();
        
        RpcInventoryReady();

       // Debug.Log("+++++++++++++++++++============+++++++++++++++++++++++++++++");
    }

    public void AssignToInventory(ItemBase newItem)
    {
        //Debug.Log("attempting to assign item to inventory");

       // Debug.Log(newItem);
        //Debug.Log(assignedInventory[newItem.inventroySlot]);

        
            inventory[newItem.inventroySlot] = newItem;

            SetInventoryItemParameters(newItem.inventroySlot);

            if(newItem.inventroySlot == 0)
            {
                activeItem = inventory[0];
                inventory[0].isActive = true;
            }
        
    }

    [ClientRpc]
    void RpcInventoryReady()
    {
        
        activeItem = inventory[0];
        activeItem.isActive = true;
        activeItem.SetUiElements();
        //Debug.Log("all items set... hopefully");        
    }

    /*public void AssignToInventorySlot(ItemBase inItem)
    {

    }*/

    void AssignToInventorySlot( int inventorySlot)
    {

        var obj = (GameObject)Instantiate(assignedInventory[inventorySlot], parentTransform.position, parentTransform.rotation);
        obj.transform.parent = parentTransform.transform;

        inventory[inventorySlot] = obj.GetComponent<ItemBase>();
        inventory[inventorySlot].owner = gameObject;

        SetInventoryItemParameters(inventorySlot);
        
        //Debug.Log("is this the problem?");
        CmdSpawnObject(obj, gameObject);

        RpcSetInventory(obj, inventorySlot);

    }

    [Command]
    void CmdSpawnObject(GameObject _obj,GameObject player)
    {
        //Debug.Log("server attemptting to spawn object");
        NetworkServer.SpawnWithClientAuthority(_obj, player);
    }    

    [ClientRpc]
    void RpcSetInventory( GameObject _obj, int inventorySlot)
    {
        //Debug.Log("====setting clients properties==== for " + gameObject );

        inventory[inventorySlot] = _obj.GetComponent<ItemBase>();

       // Debug.Log(inventory[inventorySlot]);

        SetInventoryItemParameters(inventorySlot);
    }

    void SetInventoryItemParameters(int inventorySlot)
    {
        inventory[inventorySlot].UI_Manager = UI_Manager;
        inventory[inventorySlot].isOnLocal = isLocalPlayer;
        inventory[inventorySlot].inventoryManager = this;
        inventory[inventorySlot].hasBeenSet = true;
        inventory[inventorySlot].inventroySlot = inventorySlot;

        if (inventory[inventorySlot] as GunController != null)
        {
            //Debug.Log("as gun controller");

            UpdateBulletSpawnForGun(inventory[inventorySlot] as GunController);
        }
        else if (inventory[inventorySlot] as Item_grenade != null)
        {
           // Debug.Log("as grenade controller");
            UpdateGrenadeSpawn(inventory[inventorySlot] as Item_grenade);
        }
        else if (inventory[inventorySlot] as Item_Melee_Base != null)
        {
           // Debug.Log("as melee");
            UpdateMeleeBox(inventory[inventorySlot] as Item_Melee_Base);
        }
    }

    void UpdateBulletSpawnForGun( GunController inGun)
    {
        inGun.bulletSpawn = bulletSpawn;
        inGun.cameraTransform = cameraTransform;

        inGun.bulletFlashLoc = BulletParticalLocation;

        inGun.muzzleFlashLoc = MuzzleFlashLocation;

        inGun.UI_Manager = UI_Manager;

        
        inGun.SetGunToDefaults();
    }

    void UpdateGrenadeSpawn( Item_grenade inNade)
    {
        inNade.grenadeSpawn = grenadeSpawnLocation;
    }

    void UpdateMeleeBox( Item_Melee_Base inMelee)
    {
        //inMelee.meleeHitBox = meleeHitBox;
    }
    
	
	// Update is called once per frame
	void Update () {

        if (isLocalPlayer && !networkedPlayer.fpsController.GetPauseMenuOn() )
        {
            if (Input.GetButtonDown("Fire1"))
            {
                activeItem.OnStartFire();
            }

            if (Input.GetButtonUp("Fire1"))
            {
                activeItem.OnEndFire();
            }

            if (Input.GetButtonDown("Fire2"))
            {
                activeItem.OnStartAltFire();
            }

            if (Input.GetButtonUp("Fire2"))
            {
                activeItem.OnEndAltFire();
            }

            if (Input.GetButtonDown("FireSelector"))
            {
                activeItem.OnChangeMode();
            }

            if (Input.GetButtonDown("Reload"))
            {
                activeItem.OnReload();
            }
            
            activeItem.OnFireHeld(Input.GetButton("Fire1"));

            if (Input.GetKeyDown(KeyCode.Alpha1))//switch to primary
            {
                if(activeItem != inventory[0] && inventory[0] != null)
                {
                    SwitchToItem(0);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha2))//switch to secondary/handgun
            {
                if (activeItem != inventory[1] && inventory[1] != null)
                {
                    SwitchToItem(1);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha3))//switch to grenade
            {
                if (activeItem != inventory[2] && inventory[2] != null)
                {
                    SwitchToItem(2);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha4))//switch to shot
            {
                if (activeItem != inventory[3] && inventory[3] != null)
                {
                    SwitchToItem(3);
                }
            }

            if (Input.GetKeyDown(KeyCode.Alpha5))//switch to melee
            {
                if (activeItem != inventory[4] && inventory[4] != null)
                {
                    SwitchToItem(4);
                }
            }
        }


    }

    public void SwitchToItem(int itemIndex)
    {
        deactivateCurrentItem();

        ActivateItem(itemIndex);
    }

    void deactivateCurrentItem()
    {
        activeItem.DeactivateItem();
        activeItem.isActive = false;
    }

    void ActivateItem(int itemIndex)
    {
        //Debug.Log("how is this working?");
        activeItem = inventory[itemIndex];
        activeItem.isActive = true;
        inventory[itemIndex].SetUiElements();
        
    }
}
