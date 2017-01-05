using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;
//using UnityEngine.UI;


public class GunController : ItemBase
{

    //public GameObject bulletPrefab;
    [NonSerialized]
    public Transform bulletSpawn, cameraTransform, muzzleFlashLoc, bulletFlashLoc;
    
    public GameObject bulletParticlePrefab, muzzleFlashPrefab;

    public GameObject _impactPrefab;
    ParticleSystem _impactEffect;
    //public Text ammoFeed;
    

    [SerializeField]
    protected int maxAmmo = 30;
    int currentAmmo;
    [SerializeField]
    bool hasMag = true;
    [SerializeField]
    protected float reloadTime = 0.5f;
    float currentReloadTime = 0f;
    [SerializeField]
    protected int damage = 10;
    [SerializeField]
    protected float fireRate = 0.1f;
    [SerializeField]
    protected float triggerTimer = 0.2f;
    bool canPullTrigger = true;
    [SerializeField]
    protected bool hasAutomatic = true;
    [SerializeField]
    protected bool hasBurst = true;
    [SerializeField]
    protected int numBurstMax = 3;
    [SyncVar]
    protected int fireSelector = 2;//0 singleshot, 1 = burst, 2 is automatic
    int numBurst = 0;

    bool canFire = true;
    bool replicatedIsFiring = false;
    bool isReloading;

    [SerializeField]
    protected int defaultFireState;
    [NonSerialized]
    public bool isAiming;

    /*protected struct HandlingProfile
    {
        float baseSpread;//initial accuracy
        float maxSpread;//max inaccuracy spread will bloom too
        float bloomFactor;// how fast it will bloom per bullet on a exponential scale
        float bloomDegredation;//linear reduction in spread when not fireing
        Vector2 weaponKick;//physical motion of gun when shooting
    }*/

    [SerializeField]
    protected HandlingProfile[] handlingProfiles;
    bool degradeBloom = true;

    public void SetGunToDefaults()
    {
        currentAmmo = maxAmmo;
        
        handlingProfiles[0].bloomcurrent = handlingProfiles[0].baseSpread;


        fireSelector = defaultFireState;
        canFire = true;
        isReloading = false;
        replicatedIsFiring = false;

        currentReloadTime = 0.0f;
        numBurst = 0;

        SetAmmoFeed();
    }

    void Update()
    {
        
            if (isReloading)
            {
                //Debug.Log("is reloading");

                currentReloadTime += Time.deltaTime;
            
                if (!hasMag && currentReloadTime >= (reloadTime / maxAmmo) * (currentAmmo + 1))
                {
                    IndividualReload();
                
                }

                if(currentReloadTime >= reloadTime)
                {
                    currentReloadTime = 0;
                    FinishReload();
                }
            
        }

        if(handlingProfiles[0].bloomcurrent > handlingProfiles[0].baseSpread * 0.5 + 20 && degradeBloom)
        {
            handlingProfiles[0].bloomcurrent -= handlingProfiles[0].bloomDegredation * Time.deltaTime;
        }

        if (handlingProfiles[0].bloomcurrent < handlingProfiles[0].baseSpread * 0.5f + 20 )
        {
            handlingProfiles[0].bloomcurrent = handlingProfiles[0].baseSpread * 0.5f + 20 ;

            degradeBloom = false;
        }

        if (isActive)
        {
            UI_Manager.SetCrossHairBloom(handlingProfiles[0].bloomcurrent);
        }
        
    }

    public override void OnStartFire()
    {
        if (canFire && canPullTrigger)
        {
            canPullTrigger = false;
            Invoke("CanRepullTrigger", triggerTimer);
            
            StartFire();
        }
    }

    public override void OnEndFire()
    {
        //degradeBloom = true;
        CmdFire(false);
    }

    public override void OnStartAltFire()
    {
        //Debug.Log("call to addzoom");
        AddZoom(true);
    }

    public override void OnEndAltFire()
    {
        //Debug.Log("call to stop zoom");
        AddZoom(false);
    }

    public override void OnChangeMode()
    {
        if (hasAutomatic || hasBurst)
        {
            SelectFireMode();
        }
    }

    void CanRepullTrigger()
    {
        canPullTrigger = true;
    }

    public override void OnReload()
    {
        //Debug.Log("reload called");

        StartReload();

        if (!hasMag)
        {
            currentReloadTime = (reloadTime / maxAmmo) * currentAmmo;
        }
    }

    public override void SetUiElements()
    {
        base.SetUiElements();

        SetAmmoFeed();
    }

    void SelectFireMode()
    {
        if (hasAutomatic && hasBurst)
        {
            fireSelector++;

            if (fireSelector > 2)
            {
                fireSelector = 0;
            }
        }
        else if (hasBurst)
        {
            fireSelector++;

            if(fireSelector > 1)
            {
                fireSelector = 0;
            }
        }
        else if(hasAutomatic)
        {
            if(fireSelector == 0)
            {
                fireSelector = 2;
            }
            else if(fireSelector == 2)
            {
                fireSelector = 0;
            }
        }

        CmdSetFireMode(fireSelector);               
    }

    [Command]
    void CmdSetFireMode(int fireMode)
    {
        fireSelector = fireMode;
    }

    void StartFire()//begin the shooting process
    {
        if (ConsumeAmmo())
        {
            degradeBloom = false;
            
            CalculateHit();
            AddWeaponKick();
            if (!isAiming)
            {
                CalculateBloom();
            }
            

            if (fireSelector == 1)
            {
                numBurst++;
            }

            canFire = false;


            Invoke("Refire", fireRate);
            //CancelInvoke("DegradeBloom");
            //Invoke("DegradeBloom", triggerTimer + 0.2f);

            if (!replicatedIsFiring)
            {
                CmdFire(true);
            }
        }
    }

    protected virtual void DegradeBloom()
    {
        degradeBloom = true;
    }

    protected virtual void AddWeaponKick()
    {
        //Debug.Log("adding weaponkick0");
        if (isAiming)
        {
            inventoryManager.networkedPlayer.fpsController.AddWeaponKick(handlingProfiles[0].weaponKick);
        }
        else
        {
            inventoryManager.networkedPlayer.fpsController.AddWeaponKick(handlingProfiles[0].weaponKick / 10);
        }
        
    }

    protected virtual void AddZoom(bool doZoom)
    {
        //Debug.Log("can we zoom?");

        if (doZoom)
        {
           // Debug.Log("yes");
            inventoryManager.networkedPlayer.fpsController.ActivateZoom(2.2f, 0.2f);
            
        }
        else
        {
            //Debug.Log("no");
            inventoryManager.networkedPlayer.fpsController.DeactivateZoom();
        }

        isAiming = doZoom;
        SetItemSlowDown(doZoom);
    }


    void Refire()//allow us to fire again and then check to see if we are still holding down the fire button
    {
        canFire = true;

        degradeBloom = true;

        if (isActive)
        {
            if (fireSelector == 1)
            {
                StartFire();
            }
            else if (fireHeldDown && fireSelector == 2)
            {
                StartFire();
            }
            else
            {
                CmdFire(false);
            }
        }
    }

    bool ConsumeAmmo()//checks to see if we have ammo and if we are doing burst fire
    {
        if(currentAmmo <= 0)
        {

            return false;
        }
        if (numBurst >= numBurstMax)
        {
            numBurst = 0;
            return false;
        }

        currentAmmo--;

        isReloading = false;

        SetAmmoFeed();

        return true;
    }

    protected virtual Vector3 GetSpread()
    {
        //Debug.Log("getting spread");
        float tanA = 0;

        if (isAiming)
        {
            tanA = handlingProfiles[0].baseSpread  / 50;
        }
        else
        {
            tanA = handlingProfiles[0].bloomcurrent / 50;
        }

        Vector3 accAdjust = UnityEngine.Random.insideUnitCircle * (tanA * tanA );

       // Debug.Log((tanA * tanA) * 100);

        return accAdjust;
    }

    protected virtual void CalculateBloom()
    {
       // Debug.Log(handlingProfiles[0].bloomcurrent);

        float moddedbloomfactor = handlingProfiles[0].bloomFactor / 10;
        handlingProfiles[0].bloomcurrent += moddedbloomfactor + (handlingProfiles[0].bloomcurrent * 0.1f);

        if (handlingProfiles[0].bloomcurrent > handlingProfiles[0].maxSpread)
        {
            handlingProfiles[0].bloomcurrent = handlingProfiles[0].maxSpread;
        }
        //Debug.Log(handlingProfiles[0].bloomcurrent);

    }

    protected virtual void  CalculateHit()//this will handle fireing the raycasts
    {

        //calculate random shot placement

        Vector3 accAdjust = GetSpread();
        accAdjust.z = 10;
        accAdjust = cameraTransform.TransformDirection(accAdjust.normalized);
        //Debug.Log(isLocalPlayer);
        //set shot paramaters
        RaycastHit hit;
        Vector3 rayPos = cameraTransform.position + (1f * cameraTransform.forward);

        //perform shot
        Vector3 toSend = Vector3.zero;

        bool hitSomething = false;

        if (Physics.Raycast(rayPos, accAdjust, out hit, 50f))
        {
            bulletSpawn.LookAt(hit.point);

            if (isOnLocal)
            {

                GameObject objectHit = hit.collider.gameObject;

                while (objectHit.transform.parent != null)//finds the top object in hierachy
                {
                    //Debug.Log("this object = " + objectHit.transform.parent);

                    objectHit = objectHit.transform.parent.gameObject;
                }

                //Debug.Log("top object = " + objectHit);

                toSend = hit.point;
                hitSomething = true;

                CmdProcessHit(objectHit, hit);
            }
            
        }
        else
        {
            bulletSpawn.transform.rotation = cameraTransform.transform.rotation;

            bulletSpawn.LookAt( rayPos + accAdjust *50);

            toSend = rayPos + accAdjust * 50;
        }

        Pre_PlayFireEffects(toSend, hitSomething);        
    }

    protected virtual void Pre_PlayFireEffects(Vector3 hitLoc, bool hitSomething = false)
    {

        PlayFireEffects(hitLoc, hitSomething);

        if (isOnLocal)//this would be too slow on shotgun
        {
            //Debug.Log("replicate my fire");
            CmdReplicateFire(hitLoc, hitSomething);
        }
    }

    protected void PlayFireEffects(Vector3 hitLoc, bool hitSomething = false)
    {
        var bullet = (GameObject)Instantiate(bulletParticlePrefab, bulletFlashLoc.position, bulletFlashLoc.rotation);
        //Debug.Log(bulletSpawn.transform.rotation);

        Destroy(bullet, 0.2f);

        var muzzle = (GameObject)Instantiate(muzzleFlashPrefab, muzzleFlashLoc.position, muzzleFlashLoc.rotation);

        Destroy(muzzle, 0.2f);


        if(hitSomething)
        {
            var bulletHit = (GameObject)Instantiate(_impactPrefab, hitLoc, muzzleFlashLoc.rotation);
            Destroy(bulletHit, 0.2f);
        }
    }

    [Command]
    void CmdReplicateFire(Vector3 hitLoc, bool hitSomething)
    {
       // Debug.Log("server replicate the fire");
        RpcReplicateFire(hitLoc, hitSomething);
    }

    [ClientRpc]
    void RpcReplicateFire(Vector3 hitLoc, bool hitSomething)
    {
        //Debug.Log("client replicated the fire");
        //Debug.Log(hitLoc);

        if (!isOnLocal)
        {
            bulletSpawn.LookAt(hitLoc);

            PlayFireEffects(hitLoc, hitSomething);
        }
    }

    [Command]
    void CmdFire(bool replicatedCanFire)
    {
        //Debug.Log("cmdfire called");

        RpcStartReplicatedFire(replicatedCanFire);
        
        /*//old code to create bullut from prefab
        var bullet = (GameObject)Instantiate(bulletPrefab, bulletSpawn.position, bulletSpawn.rotation);

        //give bullet velocity
        bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * 6;

        //spawn bullet on other clients
        NetworkServer.Spawn(bullet);

        //destroy bullet after 2 seconds
        Destroy(bullet, 2.0f);*/
    }
    
    
    [Command]
    void CmdProcessHit(GameObject ObjectHit, RaycastHit hit)
    {
        Health health = ObjectHit.GetComponent<Health>();

        if (health != null)//wtf is this even for?
        {
            //health.TakeDamage(10);

            health = ObjectHit.GetComponentInParent<Health>();
        }

        if (health != null)
        {
            health.TakeDamage(damage);

            //health = ObjectHit.GetComponentInParent<Health>();
        }
    }

    [ClientRpc]
    void RpcStartReplicatedFire(bool replicatedCanFire)
    {
        /*replicatedIsFiring = replicatedCanFire;

        if (replicatedIsFiring)
        {
            ReplicatedFire();
        }*/
    }

    void ReplicatedFire()
    {
        if (!isOnLocal && replicatedIsFiring)
        {
            CalculateHit();

            if(fireSelector == 0)
            {
                return;
            }
            else if(fireSelector == 1)
            {
                numBurst++;
                //Debug.Log(numBurst + "     " + numBurstMax);

                if (numBurst >= numBurstMax)
                {

                    numBurst = 0;
                    return;
                }
            }
           Invoke("ReplicatedFire", fireRate);
        } 
    }

    void StartReload()
    {
        //Debug.Log("lets reload!");
        isReloading = true;
    }

    void IndividualReload()
    {
        currentAmmo++;
        SetAmmoFeed();
    }

    void FinishReload()
    {
        isReloading = false;
        currentAmmo = maxAmmo;
        SetAmmoFeed();
    }

    void SetAmmoFeed()
    {
        if( UI_Manager != null)
        {
            UI_Manager.Change_AmmoCount(currentAmmo + " / " + maxAmmo);
        }
        
    }

    public override void DeactivateItem()
    {
       // CancelInvoke("Refire");
       
        base.DeactivateItem();
    }

}

[System.Serializable]
public class HandlingProfile
{
    
    public float baseSpread = 0;//initial accuracy
    public float maxSpread = 100;//max inaccuracy spread will bloom too
    public float bloomFactor = 4;// how fast it will bloom per bullet on a exponential scale
    public float bloomDegredation = 1;//linear reduction in spread when not fireing
    public float bloomcurrent;
    public Vector2 weaponKick;//physical motion of gun when shooting
}

