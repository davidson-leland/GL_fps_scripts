using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;
using System.Collections;
//using UnityEngine.Networking;
//using UnityEngine.UI;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [RequireComponent(typeof (CharacterController))]
    [RequireComponent(typeof (AudioSource))]
    public class FirstPersonController : MonoBehaviour
    {
        [SerializeField] private bool m_IsWalking;
        [SerializeField]
        private float n_CreepSpeed;
        [SerializeField] private float m_WalkSpeed;
        [SerializeField] private float m_RunSpeed;
        [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
        [SerializeField] private float m_JumpSpeed;
        [SerializeField] private float m_StickToGroundForce;
        [SerializeField] private float m_GravityMultiplier;
        [SerializeField] private MouseLook m_MouseLook;
        [SerializeField] private bool m_UseFovKick;
        [SerializeField] private FOVKick m_FovKick = new FOVKick();
        [SerializeField] private bool m_UseHeadBob;
        [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
        [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
        [SerializeField] private float m_StepInterval;
        [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
        [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
        [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.
        

        [SerializeField]
        private GameObject body;
        [SerializeField]
        private GameObject capsuleBody;
        [SerializeField]
        private GameObject head;

        [SerializeField]
        private Camera m_Camera;
        [SerializeField]
        private Camera m_Camera2;
        private bool m_Jump;
        private float m_YRotation;
        private Vector2 m_Input;
        private Vector3 m_MoveDir = Vector3.zero;
        private CharacterController m_CharacterController;
        private CollisionFlags m_CollisionFlags;
        private bool m_PreviouslyGrounded;
        private Vector3 m_OriginalCameraPosition;
        private float m_StepCycle;
        private float m_NextStep;
        private bool m_Jumping;
        private AudioSource m_AudioSource;
        private NetworkedPlayer networkedPlayer;


        private bool n_isCreeping, n_overwriteCreeping;
        public  bool n_crouching = false;
        public  bool n_doProne = false;
        //public Text debugText;

        public UI_TextManagement n_UI_Manager;
        [SerializeField]
        GameObject ui_prefab;

        //stuff for double tap code
        private char lastCharHit;
        public float doubleTapTime = 0.2f;
        private float doubleTapTime_Actual = 0f;
        private bool countDoubleTap = false;


        //dodgeing stuffs
        private bool n_dodge;
        [SerializeField]
        private Vector2 dodgeVector = new Vector3(5,5);
        private Vector3 dodgeToUse = new Vector3();
        private bool allowMoveInput = true;

        //slowdown applied when using item
        private bool useItemSlowDown = false;
        private float slowDownFactor = 1;
        bool canMouseLook = true;
        bool pausedMenuOn = false;



        // Use this for initialization
        private void Start()
        {
            m_CharacterController = GetComponent<CharacterController>();
            //m_Camera = Camera.main;
            m_OriginalCameraPosition = m_Camera.transform.localPosition;
            m_FovKick.Setup(m_Camera, m_Camera2);
            m_HeadBob.Setup(m_Camera, m_StepInterval);
            m_StepCycle = 0f;
            m_NextStep = m_StepCycle/2f;
            m_Jumping = false;
            m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);
            networkedPlayer = GetComponent<NetworkedPlayer>();
            n_UI_Manager = GetComponent<UI_TextManagement>();

            n_crouching = false;

            if (networkedPlayer.isLocalPlayer)
            {
                ui_prefab.transform.SetParent(null);
                ui_prefab.SetActive(true);

                n_UI_Manager.LoadPreferences();
            }
            else
            {
                Destroy(ui_prefab);
                    
            }
        }


        // Update is called once per frame
        private void Update()
        {

            getInstantInput();

            if (canMouseLook && !pausedMenuOn)
            {
                RotateView();
            }
            
            // the jump state needs to read here to make sure it is not missed
            if (!m_Jump && !pausedMenuOn)
            {
                m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
            }

            if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
            {
                StartCoroutine(m_JumpBob.DoBobCycle());
                PlayLandingSound();
                m_MoveDir.y = 0f;
                m_Jumping = false;

                if (n_dodge)
                {
                    n_dodge = false;
                    AllowMoveInput();
                }
            }
            if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
            {
                m_MoveDir.y = 0f;
            }
            m_PreviouslyGrounded = m_CharacterController.isGrounded;

            testUpdate();
        }

        public Vector3 GetMovementSpeed()
        {
            return m_MoveDir;
        }


        private void PlayLandingSound()
        {
            m_AudioSource.clip = m_LandSound;
            m_AudioSource.Play();
            m_NextStep = m_StepCycle + .5f;
        }


        private void FixedUpdate()
        {
            /*
            float speed;
            GetInput(out speed);

           
                // always move along the camera forward as it is the direction that it being aimed at
                Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

                // get a normal for the surface that is being touched to move along it
                RaycastHit hitInfo;
                Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                                   m_CharacterController.height / 2f, ~0, QueryTriggerInteraction.Ignore);
                desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;
            
           

            if (allowMoveInput)
            {
                
                m_MoveDir.x = desiredMove.x * speed;
                m_MoveDir.z = desiredMove.z * speed;
            }

            if (n_dodge && m_Jumping)
            {
               // m_MoveDir.x = dodgeToUse.x;
                //m_MoveDir.z = dodgeToUse.z;
            }

            //Debug.Log(m_Input.x);
            

            if((m_Input.y != 0 || m_Input.x != 0 ) && speed > m_WalkSpeed)
            {
                networkedPlayer.SetLean(1);
            }

           // Debug.Log(desiredMove);

            //Debug.Log(m_Input.y);

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump && !n_dodge)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
                else if (n_dodge)
                {
                    
                    desiredMove = transform.forward * dodgeToUse.z + transform.right * dodgeToUse.x;

                    Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, ~0, QueryTriggerInteraction.Ignore);
                    desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

                    //Debug.Log(desiredMove);


                    if (!n_crouching && !n_doProne)
                    {
                        m_MoveDir.x = desiredMove.x * dodgeVector.x;
                        m_MoveDir.z = desiredMove.z * dodgeVector.x;

                        m_MoveDir.y = dodgeVector.y;

                        dodgeToUse = m_MoveDir;

                        m_Jumping = true;
                        allowMoveInput = false;
                    }
                    else if (n_doProne)
                    {
                        Debug.Log("do ground roll called");
                        n_dodge = false;
                        allowMoveInput = false;
                        canMouseLook = false;

                        m_MoveDir.x = desiredMove.x * dodgeVector.x * 0.5f;

                        Invoke("AllowMoveInput", 0.4f);
                        Invoke("AllowMouseLook", 0.4f);
                        
                    }
                }

            }
            else
            {
                m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);
            //networkedPlayer.StartReplicateMovement(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
           // UpdateCameraPosition(speed);

            //networkedPlayer.CmdSetVelocity(m_MoveDir);

            m_MouseLook.UpdateCursorLock();*/
        }

        void testUpdate() //this will break a great many things 
        {
            float speed;
            GetInput(out speed);


            // always move along the camera forward as it is the direction that it being aimed at
            Vector3 desiredMove = transform.forward * m_Input.y + transform.right * m_Input.x;

            // get a normal for the surface that is being touched to move along it
            RaycastHit hitInfo;
            Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, ~0, QueryTriggerInteraction.Ignore);
            desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;



            if (allowMoveInput)
            {

                m_MoveDir.x = desiredMove.x * speed;
                m_MoveDir.z = desiredMove.z * speed;
            }

            if (n_dodge && m_Jumping)
            {
                // m_MoveDir.x = dodgeToUse.x;
                //m_MoveDir.z = dodgeToUse.z;
            }

            //Debug.Log(m_Input.x);


            if ((m_Input.y != 0 || m_Input.x != 0) && speed > m_WalkSpeed)
            {
                networkedPlayer.SetLean(1);
            }

            // Debug.Log(desiredMove);

            //Debug.Log(m_Input.y);

            if (m_CharacterController.isGrounded)
            {
                m_MoveDir.y = -m_StickToGroundForce;

                if (m_Jump && !n_dodge)
                {
                    m_MoveDir.y = m_JumpSpeed;
                    PlayJumpSound();
                    m_Jump = false;
                    m_Jumping = true;
                }
                else if (n_dodge)
                {

                    desiredMove = transform.forward * dodgeToUse.z + transform.right * dodgeToUse.x;

                    Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
                               m_CharacterController.height / 2f, ~0, QueryTriggerInteraction.Ignore);
                    desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

                    //Debug.Log(desiredMove);


                    if (!n_crouching && !n_doProne)
                    {
                        m_MoveDir.x = desiredMove.x * dodgeVector.x;
                        m_MoveDir.z = desiredMove.z * dodgeVector.x;

                        m_MoveDir.y = dodgeVector.y;

                        dodgeToUse = m_MoveDir;

                        m_Jumping = true;
                        allowMoveInput = false;
                    }
                    else if (n_doProne)
                    {
                        Debug.Log("do ground roll called");
                        n_dodge = false;
                        allowMoveInput = false;
                        canMouseLook = false;

                        m_MoveDir.x = desiredMove.x * dodgeVector.x * 0.5f;

                        Invoke("AllowMoveInput", 0.4f);
                        Invoke("AllowMouseLook", 0.4f);

                    }
                }

            }
            else
            {
                m_MoveDir += Physics.gravity * m_GravityMultiplier * Time.fixedDeltaTime;
            }
            m_CollisionFlags = m_CharacterController.Move(m_MoveDir * Time.fixedDeltaTime);
            //networkedPlayer.StartReplicateMovement(m_MoveDir * Time.fixedDeltaTime);

            ProgressStepCycle(speed);
            // UpdateCameraPosition(speed);

            //networkedPlayer.CmdSetVelocity(m_MoveDir);

            m_MouseLook.UpdateCursorLock();
        }


        private void PlayJumpSound()
        {
            m_AudioSource.clip = m_JumpSound;
            m_AudioSource.Play();
        }

        
        private void ProgressStepCycle(float speed)
        {
            if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
            {
                m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                             Time.fixedDeltaTime;
            }

            if (!(m_StepCycle > m_NextStep))
            {
                return;
            }

            m_NextStep = m_StepCycle + m_StepInterval;

            PlayFootStepAudio();
        }


        private void PlayFootStepAudio()
        {
            if (!m_CharacterController.isGrounded)
            {
                return;
            }
            // pick & play a random footstep sound from the array,
            // excluding sound at index 0
            int n = Random.Range(1, m_FootstepSounds.Length);
            m_AudioSource.clip = m_FootstepSounds[n];
            m_AudioSource.PlayOneShot(m_AudioSource.clip);
            // move picked sound to index 0 so it's not picked next time
            m_FootstepSounds[n] = m_FootstepSounds[0];
            m_FootstepSounds[0] = m_AudioSource.clip;
        }


        private void UpdateCameraPosition(float speed)
        {
            Vector3 newCameraPosition;
            if (!m_UseHeadBob)
            {
                return;
            }
            if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
            {
                m_Camera.transform.localPosition =
                    m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                      (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
            }
            else
            {
                newCameraPosition = m_Camera.transform.localPosition;
                newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
            }
            m_Camera.transform.localPosition = newCameraPosition;
        }



        private void getInstantInput()
        {
            if (Input.GetButtonDown("Pause"))//turn mouse on off
            {
                pausedMenuOn = !pausedMenuOn;
                //SetCursorLock(!pausedMenuOn);
                m_MouseLook.SetCursorLock(!pausedMenuOn);
                n_UI_Manager.Set_Pause_Menu(pausedMenuOn);
            }

            if (pausedMenuOn)
            {
                return;
            }

            if (countDoubleTap)
            {
                doubleTapTime_Actual += Time.deltaTime;
                
                if(doubleTapTime_Actual >= doubleTapTime)
                {
                    doubleTapTime_Actual = 0f;
                    countDoubleTap = false;
                }
            }

            
                m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
            n_isCreeping = Input.GetKey(KeyCode.LeftAlt);

           
            //movement speed inputs
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                n_overwriteCreeping = false;
            }

            if (Input.GetKeyDown(KeyCode.LeftShift))
            {
                n_overwriteCreeping = true;
            }


            //stance inputs
            if (Input.GetKeyDown(KeyCode.LeftControl))
            {
                SetCrouch(!n_crouching);
            }

            if (Input.GetKeyDown(KeyCode.Z))
            {
                SetProne(!n_doProne);
            }

            //lean inputs
            if (Input.GetKeyDown(KeyCode.Q))
            {
                networkedPlayer.SetLean(0);
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                networkedPlayer.SetLean(2);
            }

            if (Input.GetKeyUp(KeyCode.E) || Input.GetKeyUp(KeyCode.Q))
            {

                if (Input.GetKey(KeyCode.Q))
                {
                    networkedPlayer.SetLean(0);
                }
                else if (Input.GetKey(KeyCode.E))
                {
                    networkedPlayer.SetLean(2);
                }
                else
                {
                    networkedPlayer.SetLean(1);
                }                
            }

            if (!m_IsWalking == n_isCreeping)//if sprinting And creeping being held down
            {
                if (n_overwriteCreeping)
                {
                    n_isCreeping = false;
                    //debugString = ("Debug movement = Sprinting");
                }
                else
                {
                    m_IsWalking = true;
                    //debugString = ("Debug movement = creeping");
                }
            }

            //double tap and movement inputs
            if (Input.GetKeyDown(KeyCode.D))
            {

                if(lastCharHit == 'd' && countDoubleTap)
                {
                    TryDodge(1,0);
                }

                if (lastCharHit != 'd' || !countDoubleTap)
                {
                    StartDoubleTap();
                    lastCharHit = 'd';
                }
            }

            if (Input.GetKeyDown(KeyCode.A))
            {

                if (lastCharHit == 'a' && countDoubleTap)
                {
                    TryDodge(-1,0);
                }

                if (lastCharHit != 'a' || !countDoubleTap)
                {
                    StartDoubleTap();
                    lastCharHit = 'a';
                }
            }

            if (Input.GetKeyDown(KeyCode.W))
            {

                if (lastCharHit == 'w' && countDoubleTap)
                {
                    TryDodge(0,1);
                }

                if (lastCharHit != 'w' || !countDoubleTap)
                {
                    StartDoubleTap();
                    lastCharHit = 'w';
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {

                if (lastCharHit == 's' && countDoubleTap)
                {
                    TryDodge(0,-1);
                }

                if (lastCharHit != 's' || !countDoubleTap)
                {
                    StartDoubleTap();
                    lastCharHit = 's';
                }
            }

            /*if (Input.GetKeyDown(KeyCode.T))
            {
                
                networkedPlayer.TryThrowGrenade();
            }*/

        }



        private void StartDoubleTap()
        {
            doubleTapTime_Actual = 0;
            countDoubleTap = true;
        }

        private void TryDodge(float inX, float inZ)
        {
            if(n_dodge || m_Jumping || !allowMoveInput)
            {
                return;
            }

            Debug.Log("double tap hit");
            dodgeToUse.x = inX;
            dodgeToUse.z = inZ;
            n_dodge = true;
            

        }

        public void AllowMoveInput()
        {
            //Debug.Log("allowInput");
            
            allowMoveInput = true;
        }

        public void AllowMouseLook()
        {
            canMouseLook = true;
        }

        private void GetInput(out float speed)
        {
            
            // Read input
            float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
            float vertical = CrossPlatformInputManager.GetAxis("Vertical");

            if (pausedMenuOn)
            {
                horizontal = 0;
                vertical = 0;
            }

            bool waswalking = m_IsWalking;
            bool wasSlowWalking = n_isCreeping;

            string debugString = ("Debug movement = Walking");

#if !MOBILE_INPUT
            // On standalone builds, walk/run speed is modified by a key press.
            // keep track of whether or not the character is walking or running
           

#endif
            

            if (n_isCreeping)
            {
                debugString = ("Debug movement = creeping");
            }

            if (!m_IsWalking)
            {
                debugString = ("Debug movement = Sprinting");
            }

            //debugText.text = debugString;

            n_UI_Manager.Change_Debug_MovementText(debugString);


            // set the desired speed to be walking or running
            //speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;

            speed = m_WalkSpeed;

            if (!m_IsWalking && !useItemSlowDown)
            {
                speed = m_RunSpeed;
            }
            else if (n_isCreeping)
            {
                speed = n_CreepSpeed;
            }
            else if(useItemSlowDown)
            {
               // Debug.Log("are aiming and slowing movement by " + slowDownFactor);
                speed *= slowDownFactor;
            }

            if (n_crouching)
            {
                speed *= 0.5f;
            }

            if (n_doProne)
            {
                speed *= 0.2f;
            }

                m_Input = new Vector2(horizontal, vertical);

            // normalize input if it exceeds 1 in combined length:
            if (m_Input.sqrMagnitude > 1)
            {
                m_Input.Normalize();
            }

            // handle speed change to give an fov kick
            // only if the player is going to a run, is running and the fovkick is to be used
            if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
            {
                StopAllCoroutines();
                StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
            }

        }

        private void RotateView()
        {
            m_MouseLook.LookRotation (transform, m_Camera.transform, head.transform);
        }

        public void AddWeaponKick(Vector2 inKick)
        {
            //Debug.Log("adding weaponkick1");
            m_MouseLook.AddWeaponKick(inKick);
        }


        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            Rigidbody body = hit.collider.attachedRigidbody;
            //dont move the rigidbody if the character is on top of it
            if (m_CollisionFlags == CollisionFlags.Below)
            {
                return;
            }

            if (body == null || body.isKinematic)
            {
                return;
            }
            body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
        }

        public void SetCrouch( bool crouching)
        {
            //Debug.Log("crouching called = " + crouching);
            networkedPlayer.SetCrouch(crouching);
            //original client side code for crouching. now handled in networkedplayer script
            /*if (crouching)
            {
                capsuleBody.transform.localScale += new Vector3(0, -0.4f, 0);//t(1, 0.6f, 1);
                capsuleBody.transform.localPosition += new Vector3(0, 0.4f, 0);//.Set(0, 0, 0);
                body.transform.localPosition += new Vector3(0, -0.6f, 0);
                n_crouching = true;
            }
            else
            {
                capsuleBody.transform.localScale += new Vector3(0, 0.4f, 0);//Set(1, 01, 1);
                capsuleBody.transform.localPosition += new Vector3(0, -0.4f, 0);//.Set(0, 0, 0);
                body.transform.localPosition += new Vector3(0, 0.6f, 0);
                n_crouching = false;
            }*/


        }

        public void SetProne( bool doProne)
        {
            networkedPlayer.SetLean(1);
            networkedPlayer.SetProne(doProne);

            if (Input.GetKey(KeyCode.Q))
            {
                networkedPlayer.SetLean(0);
            }
            else if (Input.GetKey(KeyCode.E))
            {
                networkedPlayer.SetLean(2);
            }
        }

        public void ActivateZoom( float zoomAmount, float zoomTime)
        {
           // Debug.Log("zooming weapon");
            m_FovKick.WeaponZoom(zoomAmount, zoomTime);

            StopAllCoroutines();
            StartCoroutine(m_FovKick.FOVKickUp());

        }

        public void DeactivateZoom()
        {
            //Debug.Log("un zooming weapon");
            StopAllCoroutines();
            StartCoroutine(m_FovKick.FOVKickDown());
        }

        public void SetSlowDown( bool doSlowDown, float amount)
        {
            useItemSlowDown = doSlowDown;
            slowDownFactor = amount;
        }

        public bool GetPauseMenuOn()
        {
            return pausedMenuOn;
        }

        public void SetFov(int inFov)
        {

            m_FovKick.originalFov = inFov;
            m_Camera.fieldOfView = inFov;
            //m_Camera2.fieldOfView = inFov;
        }

        public void SetSensitivity(float inSense)
        {
            m_MouseLook.sensitivity = inSense;
        }

        public bool IsNetworked()
        {
            return networkedPlayer.isLocalPlayer;
        }

        public void ExitGame()
        {
            Application.Quit();
        }

        void OnDestroy()
        {
            //Debug.Log("bye bye birdie");
            m_MouseLook.SetCursorLock(false);
            Destroy(ui_prefab);
        }
    }
}
