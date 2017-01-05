using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;

public class Item_Gun_Shotgun_Base : GunController {

	[SerializeField]
    protected int pellet_Count = 10;

    List<Vector3> hitLocations = new List<Vector3>();
    List<bool> hitSomethings = new List<bool>();

    public int smoothSpreaditerations = 2;

    // Use this for initialization

    
    protected override void CalculateHit()
    {
        for( int i = 0; i < pellet_Count; i++)
        {
            base.CalculateHit();
        }
    }
   
    protected override Vector3 GetSpread()
    {
        /*float tanA = handlingProfiles[0].bloomcurrent / 50;
        Vector3 accAdjust  = UnityEngine.Random.insideUnitCircle * (tanA* tanA);*/

        Vector3 accAdjust = base.GetSpread();

        for ( int i = 0; i < smoothSpreaditerations; i++)
        {
            Vector3 toADD = base.GetSpread();

            accAdjust += toADD;
        }

        if( smoothSpreaditerations > 0)
        {
            accAdjust = accAdjust / smoothSpreaditerations;
        }

        return accAdjust;
    }

    protected override void Pre_PlayFireEffects(Vector3 hitLoc, bool hitSomething = false)
    {
        //Debug.Log("jive turkey");
        //Debug.Log(hitLoc);
        hitLocations.Add(hitLoc);
        hitSomethings.Add(hitSomething);

        if(hitLocations.Count == pellet_Count)
        {
            PlayShotGunFireEffects(hitLocations, hitSomethings);

            hitLocations.Clear();
            hitSomethings.Clear();
        }
    }

    void PlayShotGunFireEffects(List<Vector3> inVectors, List<bool> inBools)
    {
        Debug.Log("nookie");
        for (int i = 0; i < inVectors.Count; i++)
        {
           // 
            
            bulletSpawn.LookAt(inVectors[i]);
            PlayFireEffects(inVectors[i], inBools[i]);
        }

        if (isOnLocal)
        {
            Vector3[] vToSend = inVectors.ToArray();

            bool[] bToSend = inBools.ToArray();

            //vToSend = inVectors.ToArray();
            //bToSend = inBools.ToArray();

            Cmd_PlayShotGunFireEffects(vToSend, bToSend);
        }
        
    }

    [Command]
    void Cmd_PlayShotGunFireEffects(Vector3[] inList, bool[] inBools)
    {
        Debug.Log("recieving fire info on server");
        Rpc_PlayShotGunFireEffects(inList, inBools);
    }

    [ClientRpc]
    void Rpc_PlayShotGunFireEffects(Vector3[] inList, bool[] inBools)
    {
        Debug.Log("recieving fire info on clients");

        if (!isOnLocal)
        {
            Debug.Log("not player, will fire");

            List<Vector3> newHits = new List<Vector3>();
            List<bool> newBools = new List<bool>();
            
            for (int i = 0; i<inList.Length; i++)
            {
                newHits.Add(inList[i]);
                newBools.Add(inBools[i]);
            }

            PlayShotGunFireEffects(newHits, newBools);
        }
    }

}
