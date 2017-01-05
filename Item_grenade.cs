using UnityEngine;
using System.Collections;
using UnityEngine.Networking;


public class Item_grenade : ItemBase {

    public int Max_Grenades = 1;
    public int current_Grenades = 1;
    public float timer = 5.0f;
    public int damage = 100;
    public float radius = 4;
    public float throwForce = 20;

    [SerializeField]
    private GameObject GrenadePrefab;
    public Transform grenadeSpawn;

    private bool isCooking, grenadeInHand;//grenadein hand acts as a safety so that if we have a grenade blow up on us we won't through another after we release the mouse button
    private float cookTimer = 0f;

    // Use this for initialization
    
	// Update is called once per frame
	void Update () {
        if (isCooking)
        {
            cookTimer += Time.deltaTime;

            if(cookTimer >= timer)
            {
               
                TryThrowGrenade(true);
                cookTimer = 0.0f;
                isCooking = false;
            }
        }
	}

    public override void OnStartFire()
    {

        if(current_Grenades > 0)
        {
            isCooking = true;
            cookTimer = 0.0f;
            grenadeInHand = true;
        }
        
    }

    public override void OnEndFire()
    {
        if (grenadeInHand)
        {
            TryThrowGrenade();
            isCooking = false;
        }
        
    }

    public override void OnStartAltFire()
    {
        OnStartFire();
    }

    public override void OnEndAltFire()
    {
        if (grenadeInHand)
        {
            TryThrowGrenade(true);
            isCooking = false;
        }
    }

    public override void SetUiElements()
    {
        base.SetUiElements();
        SetAmmoFeed();
    }

    void SetAmmoFeed()
    {
        if (UI_Manager != null)
        {
            UI_Manager.Change_AmmoCount(current_Grenades + " / " + Max_Grenades);
        }

    }

    public override void OnChangeMode()
    {
        //do nothing
    }

    public override void OnReload()
    {
        //do nothing
    }

    public void TryThrowGrenade(bool drop = false)
    {
        current_Grenades--;
        CmdThrowGrenade(drop);
        SetAmmoFeed();
    }

    [Command]
    void CmdThrowGrenade(bool drop)
    {
        var grenade = (GameObject)Instantiate(GrenadePrefab, grenadeSpawn.position, grenadeSpawn.rotation);

        var grenade_stats = grenade.GetComponent<Grenade_base>();

        if (!drop)
        {
            grenade.GetComponent<Rigidbody>().velocity = grenade.transform.forward * throwForce;
        }
        else
        {
            grenade_stats.IgnorePlayerCollision(true);
        }

        grenade_stats.owner = this;
        grenade_stats.damage = damage;
        grenade_stats.isFromServer = isServer;
        grenade_stats.SetRadius(radius);

        NetworkServer.Spawn(grenade);
        grenade_stats.StartTimer(timer - cookTimer);


        Destroy(grenade, 6);

        grenadeInHand = false;
        RpcRemoveGrenadeFromHand();

        //Debug.Log(gameObject.name);

    }

   
    [ClientRpc]
    void RpcRemoveGrenadeFromHand()
    {
        grenadeInHand = false;
    }

    public void OnGrenadeExploded( GameObject hitPlayer, Grenade_base grenade)
    {

        // Debug.Log(hitPlayer);
        RaycastHit hit;
        //Physics.Raycast(grenade.transform.position, , out hit, radius)

        int layerMask = 1 << 10;

        layerMask = ~layerMask;

        

        if(!Physics.Linecast(grenade.transform.position, hitPlayer.transform.position, out hit, layerMask))
        {
            Health health = hitPlayer.GetComponent<Health>();

            if (health != null)
            {
                health.TakeDamage(damage);

                //health = ObjectHit.GetComponentInParent<Health>();
            }
        }

        /*if(hit.collider != null)
        {
            Debug.Log(hit.collider.gameObject);
        }*/
        

        

    }
}
