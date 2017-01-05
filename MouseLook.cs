using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace UnityStandardAssets.Characters.FirstPerson
{
    [Serializable]
    public class MouseLook
    {
        public float XSensitivity = 2f;
        public float YSensitivity = 2f;

        public float sensitivity = 8;

        public bool clampVerticalRotation = true;
        public float MinimumX = -90F;
        public float MaximumX = 90F;
        public bool smooth;
        public float smoothTime = 5f;
        public bool lockCursor = true;


        private Quaternion m_CharacterTargetRot;
        private Quaternion m_CameraTargetRot;
        private bool m_cursorIsLocked = true;

        private bool addKick = false;
        private Vector2 weaponKick;
        private float kickTimer = 0;
        Vector2 kickToAdd;

        float testF = 10;

        bool recordrotation = true;
        Vector3 recordedRot, originalRot;
        

        public void Init(Transform character, Transform camera)
        {
            m_CharacterTargetRot = character.localRotation;
            m_CameraTargetRot = camera.localRotation;
        }

        public void LookRotation(Transform character, Transform camera)//default lookratation from example
        {
            float yRot = CrossPlatformInputManager.GetAxis("Mouse X") * XSensitivity;
            float xRot = CrossPlatformInputManager.GetAxis("Mouse Y") * YSensitivity;

            

            m_CharacterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
            m_CameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);


            if (clampVerticalRotation)
                m_CameraTargetRot = ClampRotationAroundXAxis(m_CameraTargetRot);

            if (smooth)
            {
                character.localRotation = Quaternion.Slerp(character.localRotation, m_CharacterTargetRot,
                    smoothTime * Time.deltaTime);
                camera.localRotation = Quaternion.Slerp(camera.localRotation, m_CameraTargetRot,
                    smoothTime * Time.deltaTime);
            }
            else
            {
                character.localRotation = m_CharacterTargetRot;
                camera.localRotation = m_CameraTargetRot;
            }

            UpdateCursorLock();
        }

        public void AddWeaponKick(Vector2 inKick)
        {
            //Debug.Log("adding weaponkick2");
            weaponKick = inKick;
            kickToAdd = weaponKick;
            addKick = true;
            kickTimer = 0;
        }

        private void stopKick()
        {

        }

        public void LookRotation(Transform character, Transform camera, Transform head)//
        {
            float appSens = sensitivity * 0.875f;// this adjustments should match to overwatch pretty closely

            float yRot = Input.GetAxisRaw("Mouse X") * (appSens / 2) * 0.01f;
            float xRot = Input.GetAxisRaw("Mouse Y") * (appSens / 2) * 0.01f;


            if (addKick)
            {
                //Debug.Log(yRot);
                //Debug.Log(yRot - weaponKick.y);

                if (recordrotation)
                {
                    recordrotation = false;
                    //originalRot = m_CameraTargetRot.eulerAngles;
                    recordedRot = Vector3.zero;
                }

                kickToAdd = kickToAdd - ((kickToAdd / 2) * (Time.deltaTime * 20));

                //Debug.Log(kickToAdd.x);

                yRot += kickToAdd.y * Time.deltaTime ;
                xRot += kickToAdd.x * Time.deltaTime;

                kickTimer += Time.deltaTime;

                recordedRot.x += xRot;

                if(recordedRot.x < -5)
                {
                    recordedRot.x = 0;
                    //Debug.Log(recordedRot.x);
                }

                if (kickToAdd.x < 1)
                {
                    addKick = false;
                    /*Debug.Log("ping");

                    Debug.Log("recordedRot = " + recordedRot);

                    Debug.Log(m_CameraTargetRot.eulerAngles + recordedRot);*/

                    //xRot = -recordedRot.x;

                    //recordedRot = Vector3.zero;
                    
                    recordrotation = true;
                }
                //addKick = false;
            }
            else
            {
                float retRot = 100 * Time.deltaTime;

                if(retRot < recordedRot.x)
                {
                    recordedRot.x -= retRot;
                    xRot -= retRot;
                }
                else
                {
                    xRot -= recordedRot.x;
                    //recordedRot.x = 0;
                    recordedRot = Vector3.zero;
                }
            }

            m_CharacterTargetRot *= Quaternion.Euler (0f, yRot, 0f);
            m_CameraTargetRot *= Quaternion.Euler (-xRot, 0f, 0f);

            //Debug.Log(addKick);

            if (clampVerticalRotation)
                m_CameraTargetRot = ClampRotationAroundXAxis (m_CameraTargetRot);

            if(smooth)
            {
                character.localRotation = Quaternion.Slerp (character.localRotation, m_CharacterTargetRot,
                    smoothTime * Time.deltaTime);
                camera.localRotation = Quaternion.Slerp (camera.localRotation, m_CameraTargetRot,
                    smoothTime * Time.deltaTime);
            }
            else
            {

                character.localRotation = m_CharacterTargetRot;
                //head.localRotation = m_CharacterTargetRot;
               camera.localRotation = m_CameraTargetRot ;
            }

            UpdateCursorLock();
        }

        public void SetCursorLock(bool value)
        {
            lockCursor = value;
            if(!lockCursor)
            {//we force unlock the cursor if the user disable the cursor locking helper
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public void UpdateCursorLock()
        {
            //if the user set "lockCursor" we check & properly lock the cursos
            if (lockCursor)
                InternalLockUpdate();
        }

        private void InternalLockUpdate()
        {
            if(Input.GetKeyUp(KeyCode.Escape))
            {
                m_cursorIsLocked = false;
            }
            else if(Input.GetMouseButtonUp(0))
            {
                m_cursorIsLocked = true;
            }

            if (m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else if (!m_cursorIsLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        Quaternion ClampRotationAroundXAxis(Quaternion q)
        {
            q.x /= q.w;
            q.y /= q.w;
            q.z /= q.w;
            q.w = 1.0f;

            float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan (q.x);

            angleX = Mathf.Clamp (angleX, MinimumX, MaximumX);

            q.x = Mathf.Tan (0.5f * Mathf.Deg2Rad * angleX);

            return q;
        }

    }
}
