using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class Grenade_base : NetworkBehaviour {
    //for the grenade projectile

    public GameObject explosionParticle;

    public SphereCollider explosionCollider, body;
    public GameObject sphereCollider;

    public int damage = 100;
    public bool isFromServer = false;

    public Item_grenade owner;

    float distToGround = 0;

    int mask = 1 << 10;
    
    // Use this for initialization
	void Start () {
        //StartTimer();

        Debug.Log(GetComponent<Rigidbody>().velocity);
        Debug.Log(damage);
        Debug.Log(owner);

        distToGround = body.radius + 0.1f;

        mask = ~mask;
    }
	
	// Update is called once per frame
	void Update ()
    {
	    if(sphereCollider.layer == 0)
        {
            if(Physics.Raycast(transform.position, -Vector3.up, distToGround, mask))
            {
                IgnorePlayerCollision(true);
            }
        }
	}

    public void StartTimer(float detonationTimer)
    {
        Debug.Log("timer called");

        if (detonationTimer > 0)
        {
            Invoke("Detonate", detonationTimer);
        }
        else
        {
            Detonate();
        }

        if (isServer)
        {
            Debug.Log("starting timers for clients");
            RpcStartTimer(detonationTimer);
        }
        
    }

    [ClientRpc]
    void RpcStartTimer(float detonationTimer)
    {
        if (!isServer)
        {
            StartTimer(detonationTimer);
        }
        
    }

    public void SetRadius(float inRadius)
    {
        explosionCollider.radius = inRadius;
    }

    public void Detonate()
    {
        var explosion = (GameObject)Instantiate(explosionParticle, transform.position, transform.rotation);

        enabled = false;

        //body.enabled = false;
        //GetComponentInChildren<MeshRenderer>().enabled = false;
        sphereCollider.SetActive(false);

        if (isFromServer)
        {
            explosionCollider.enabled = true;
        }

        Destroy(explosion, 0.3f);
    }    

    void OnTriggerEnter(Collider other)
    {
       // Debug.Log("grenade exploded");
        if (other.gameObject.GetComponent<Health>() != null)
        {
           
            owner.OnGrenadeExploded(other.gameObject, this);
        }
    }

    public void IgnorePlayerCollision(bool ignore)
    {
        if (ignore)
        {
            sphereCollider.layer = 11;
        }
        else
        {
            sphereCollider.layer = 0;
        }
    }
}
