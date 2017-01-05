using UnityEngine;
using System.Collections;

public class Item_Melee_Base : ItemBase
{

    public BoxCollider meleeHitBox;

    public int damage = 50;

    public float refireTime = 0.4f;

    public float activeTime = 0.1f;

    public bool onCoolDown = false;

    
    

    public override void OnStartFire()
    {
        if (!onCoolDown)
        {
            onCoolDown = true;

            Invoke("CanReFire", refireTime);

            meleeHitBox.enabled = true;
            Invoke("EndMeleeStrike", activeTime);
        }
    }

    public override void OnEndFire()
    {

    }

    public override void OnStartAltFire()
    {

    }

    public override void OnEndAltFire()
    {

    }

    public override void OnChangeMode()
    {
        //do nothing
    }

    public override void OnReload()
    {
        //do nothing
    }

    public override void SetUiElements()
    {
        base.SetUiElements();

        if (UI_Manager != null)
        {
            UI_Manager.Change_AmmoCount("Melee");
        }
    }

    void CanReFire()
    {
        onCoolDown = false;
    }

    void EndMeleeStrike()
    {
        meleeHitBox.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log(other);

        if (other.gameObject.GetComponent<Health>() != null && other.name != "LOCAL Player")//don't hit yourself
        {
            Debug.Log(other);

            OnHitPlayer(other.gameObject);
        }
    }

    void OnHitPlayer( GameObject hitPlayer)
    {

        Debug.Log("starting los calc");
        RaycastHit hit;
        int layerMask = 1 << 10;
        layerMask = ~layerMask;

        if (!Physics.Linecast(transform.position, hitPlayer.transform.position, out hit, layerMask))
        {
            Health health = hitPlayer.GetComponent<Health>();

            if (health != null)
            {
                health.TakeDamage(damage);

                Debug.Log("he should be taking damage");
                
            }
        }
        else
        {
            Debug.Log("hit los");
            Debug.Log(hit.collider.gameObject);
        }
    }
}

