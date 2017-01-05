using UnityEngine;
using System.Collections;
using UnityEngine.Networking;

public class NetworkedPlayer : NetworkBehaviour {

    [SerializeField] [Range(1, 60)] private int sendRate = 20;//updates per second
    private float tickRate = 0;
    [SerializeField]
    private Transform playerPosition;
    [SerializeField]
    private Transform playerHeadPosition;

    public GameObject positionPrefab;
    private GameObject actualPosition;


    [SyncVar]
    Vector3 realPosition = Vector3.zero;
    [SyncVar]
    Quaternion realRotation;
    [SyncVar]
    Quaternion realHeadRotation;


    [SerializeField]
    private GameObject body;
    [SerializeField]
    private GameObject capsuleBody;
    [SerializeField]
    private GameObject head;

    private float updateInterval;

    public UnityStandardAssets.Characters.FirstPerson.FirstPersonController fpsController;
    public Camera fpsCamera, gunCamera;
    public AudioListener audioListener;
    public GunController shootingScript;
    public GameObject gun;
    public MeshRenderer eyeBand;
    public Canvas floatingHealthBar;

    private bool canUpdate = true;

    private float oldTime = 0;//??
    private CharacterController characterController;
    
    private Vector3 oldPosition, oldRealPosition, n_velocity;
    [SyncVar]
    private Vector3 s_velocity = Vector3.zero;
    private bool n_Crouching, n_doProne;
    public int leanPos = 1;//0 left, 1 no lean, 2 right
                       //private Health

    public override void OnStartLocalPlayer()
    {
        fpsController.enabled = true;
        fpsCamera.enabled = true;
        gunCamera.enabled = true;
        audioListener.enabled = true;
        //shootingScript.enabled = true;
        floatingHealthBar.enabled = false;
        
        gun.layer = 8;

        gameObject.name = "LOCAL Player";

        tickRate = 1f / (float)sendRate;
        //canUpdate = true;

        characterController = GetComponent<CharacterController>();

        base.OnStartLocalPlayer();

        realPosition = playerPosition.position;
        
    }

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        if (!isLocalPlayer)
        {
           actualPosition = (GameObject)Instantiate(positionPrefab, realPosition, realRotation);
        }
        
    }

    void Update ()
    {
        if (isLocalPlayer)
        {
           // Debug.Log("+++++++++++++++++++++====================+++++++++++++++++++++++++++++");
            //Debug.Log("i am local");
            updateInterval += Time.deltaTime;

           // Debug.Log("update interval = " + updateInterval);
            //Debug.Log("tickrate = " + tickRate);

            if (updateInterval >= tickRate)
            {
                //Debug.Log("banan fondue");
                updateInterval = 0.0f;

                
               CmdSyncLocation(playerPosition.position, playerPosition.rotation, playerHeadPosition.rotation);
                CmdSetVelocity(fpsController.GetMovementSpeed());
            }

           // Debug.Log("time = " + Time.deltaTime + " = " + (1 / Time.deltaTime)  + "frames per sec");
            
        }
        else//sync various transforms with server
        {
            //Debug.Log(s_velocity);

            playerPosition.rotation = Quaternion.Lerp(playerPosition.rotation, realRotation, 0.1f);
           playerHeadPosition.rotation = Quaternion.Lerp(playerHeadPosition.rotation, realHeadRotation, 0.1f);

            //playerPosition.position = Vector3.Lerp(playerPosition.position, realPosition, 0.1f);

            characterController.SimpleMove(s_velocity);

            // Vector3 newLocation = SmoothApproach(oldPosition, oldRealPosition, realPosition, 20f);

            //oldPosition = playerPosition.position;
            // playerPosition.position = newLocation;

            //playerPosition.position = Vector3.SmoothDamp(playerPosition.position, realPosition, ref n_velocity, 0.1f);
        }
    }

    private void FixedUpdate()
    {

        if (!isLocalPlayer)
        {
            //characterController.Move(s_velocity * Time.fixedDeltaTime);
        }
        
    }
    

        Vector3 SmoothApproach(Vector3 pastPosition, Vector3 pastTargetPosition, Vector3 targetPosition, float speed)
    {
        float t = Time.deltaTime * speed;
        Vector3 v = (targetPosition - pastTargetPosition) / t;
        Vector3 f = pastPosition - pastTargetPosition + v;
        return targetPosition - v + f * Mathf.Exp(-t);
    }

    [Command]
    void CmdSyncLocation(Vector3 inLocation, Quaternion inRotation, Quaternion inHeadRotation)
    {
       //Debug.Log("cmdSync called");

        if (canUpdate)
        {
            oldRealPosition = realPosition;

            realPosition = inLocation;
            realRotation = inRotation;
            realHeadRotation = inHeadRotation;

            if(actualPosition != null)
            {
                Vector3 tempV;

                tempV = realPosition;
                tempV.y += 3f;

                actualPosition.transform.position = tempV;
                //actualPosition.transform.position.y += 50f;
            }
            
            
        }
    }

    [Command]
    public void CmdSetVelocity(Vector3 inSpeed)
    {
        s_velocity = inSpeed;
    }

    public void ShouldRespawn(Vector3 newLocation)
    {
        playerPosition.position = newLocation;
        realPosition = newLocation;

        RpcSetLocation(newLocation);
    }

    [ClientRpc]
    void RpcSetLocation(Vector3 newLocation)
    {
        playerPosition.position = newLocation;
    }

    public void SetCrouch(bool crouching)
    {
        Debug.Log("crouching called = " + crouching);

        if(crouching == n_Crouching)
        {
            return;
        }

        if (crouching)
        {
            capsuleBody.transform.localScale += new Vector3(0, -0.4f, 0);//t(1, 0.6f, 1);
            capsuleBody.transform.localPosition += new Vector3(0, 0.4f, 0);//.Set(0, 0, 0);
            body.transform.localPosition += new Vector3(0, -0.6f, 0);
            n_Crouching = true;
               if(fpsController != null)
            {
                fpsController.n_crouching = true;
            }

            SetProne(false);


        }
        else
        {
            capsuleBody.transform.localScale += new Vector3(0, 0.4f, 0);//Set(1, 01, 1);
            capsuleBody.transform.localPosition += new Vector3(0, -0.4f, 0);//.Set(0, 0, 0);
            body.transform.localPosition += new Vector3(0, 0.6f, 0);
            n_Crouching = false;

            if (fpsController != null)
            {
                fpsController.n_crouching = false;
            }
        }

        if( isLocalPlayer)
        {
            CmdSetCrouch(crouching);
        }
    }

    [Command]
    void CmdSetCrouch(bool crouching)
    {
        RpcSetCrouch(crouching);
    }

    [ClientRpc]
    void RpcSetCrouch(bool crouching)
    {
        if (!isLocalPlayer)
        {
            SetCrouch(crouching);
        }

    }

    public void SetProne(bool doProne)
    {
        if(doProne == n_doProne)
        {
            return;
        }

        if (doProne)
        {
            body.transform.Rotate(Vector3.right * 90);
            head.transform.Rotate(Vector3.right * -90);

            body.transform.localPosition += new Vector3(0, -0.5f, 0);
            n_doProne = true;

            if (fpsController != null)
            {
                fpsController.n_doProne = true;
            }

            SetCrouch(false);
        }
        else
        {
            body.transform.Rotate(Vector3.right * -90);
            head.transform.Rotate(Vector3.right * 90);
            body.transform.localPosition += new Vector3(0, 0.5f, 0);

            n_doProne = false;

            if (fpsController != null)
            {
                fpsController.n_doProne = false;
            }
        }

        if (isLocalPlayer)
        {
            CmdSetProne(doProne);
        }
    }

    [Command]
    void CmdSetProne(bool doProne)
    {
        RpcSetProne(doProne);
    }

    [ClientRpc]
    void RpcSetProne(bool doProne)
    {
        if (!isLocalPlayer)
        {
            SetProne(doProne);
        }

    }

    public void SetLean(int newLeanPos)
    {
        //zrot 20%. xloc -.2

        //0 left, 1 no lean, 2 right

        if(newLeanPos == leanPos)
        {
            return;
        }

        if((leanPos == 0 && newLeanPos == 1) || (leanPos == 1 && newLeanPos == 2))
        {
            body.transform.localPosition += new Vector3(0.2f, 0, 0);
            body.transform.Rotate(Vector3.forward * -5);
        }
        else if ((leanPos == 2 && newLeanPos == 1) || (leanPos == 1 && newLeanPos == 0))
        {
            body.transform.localPosition += new Vector3(-0.2f, 0, 0);
            body.transform.Rotate(Vector3.forward * 5);
        }
        else if(leanPos == 0 && newLeanPos == 2)
        {
            body.transform.localPosition += new Vector3(0.4f, 0, 0);
            body.transform.Rotate(Vector3.forward * -10);
        }
        else if(leanPos == 2 && newLeanPos == 0)
        {
            body.transform.localPosition += new Vector3(-0.4f, 0, 0);
            body.transform.Rotate(Vector3.forward * 10);
        }

        if (isLocalPlayer)
        {
            CmdSetLean(newLeanPos);
        }

        leanPos = newLeanPos;
    }

    [Command]
    void CmdSetLean( int newLeanPos)
    {
        RpcSetLean(newLeanPos);
    }

    [ClientRpc]
    void RpcSetLean(int newLeanPos)
    {
        if (!isLocalPlayer)
        {
            SetLean(newLeanPos);
        }
    }

}
