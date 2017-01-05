using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class UI_TextManagement : MonoBehaviour {

    //out going text block

    
    [SerializeField]
    private Text Debug_MovementFeed;
    [SerializeField]
    private Text ammoFeed, nameFeed;
    [SerializeField]
    private RectTransform healthBar;
    [SerializeField]
    private RectTransform healthBarUI;

    public RectTransform top, bottom, left, right;

    public GameObject pauseMenu;

    //sensitivity slider stuff
    [SerializeField]
    Slider sensitivitySlider;
    [SerializeField]
    InputField sensitivityInput;

    //fov slider stuff
    [SerializeField]
    Slider fovSlider;
    [SerializeField]
    InputField fovInput;

    [SerializeField]
    UnityStandardAssets.Characters.FirstPerson.FirstPersonController aimController;

    bool tempbool = true;
   
    public void LoadPreferences()
    {
        float sensitivity = PlayerPrefs.GetFloat("aim_Sensitivity");

        if (sensitivity <= 0 || sensitivity > 100)
        {
            sensitivity = 8;
        }

        sensitivitySlider.value = sensitivity;

        int fov = PlayerPrefs.GetInt("aim_FOV");

        if (fov < 50 || fov > 120)
        {
            fov = 90;
        }

        fovSlider.value = fov;

        fov_SetTextFromValue();
        sensitivity_SetTextFromValue();
    }

	void Update () {
	
	}

    //FOV Slider functions

    public void fov_SetTextFromValue()
    {
        if (tempbool)
        {
            int temp = (int)fovSlider.value;

            fovInput.text = temp.ToString();

            //Debug.Log("setting text from value");

            SetFov(temp);
        }


    }

    public void fov_SetValueFromText()
    {
        tempbool = false;

        int i = int.Parse(fovInput.text);

        fovSlider.value = i;
        tempbool = true;

        SetFov(i);

        //Debug.Log("setting value from text");
    }
    //sensitivity slider functions

    public void sensitivity_SetTextFromValue()
    {
        if (tempbool)
        {
            int temp = (int)sensitivitySlider.value;

            sensitivityInput.text = temp.ToString();

            aimController.SetSensitivity( temp);

            Set_Sensitivity_preference(temp);
        }

    }

    public void sensitivity_SetValueFromText()
    {
        tempbool = false;

        float i = float.Parse(sensitivityInput.text);
        sensitivitySlider.value = i;
        tempbool = true;

        aimController.SetSensitivity(i);

        Set_Sensitivity_preference(i);

    }

    public int GetVerticalFov(int hFOV)
    {
        float aspectRatio = ((float)Screen.height / Screen.width);

        float fovRad = Mathf.Deg2Rad * hFOV;

        float vfovRad = 2 * Mathf.Atan(Mathf.Tan(fovRad / 2) * aspectRatio);

        return (Mathf.CeilToInt(Mathf.Rad2Deg * vfovRad));
    }

    public int GetHorizontalFov(float vFOV)
    {
        float aspectRatio = ((float)Screen.width / Screen.height);

        float fovRad = Mathf.Deg2Rad * 59;

        float hfovRad = 2 * Mathf.Atan(Mathf.Tan(fovRad / 2) * aspectRatio);

        return (Mathf.CeilToInt(Mathf.Rad2Deg * hfovRad));
    }

    void SetFov(int inFov)
    {
        Set_Fov_prefence(inFov);

        int toSend = GetVerticalFov(inFov);

        aimController.SetFov(toSend);
    }


    void Set_Fov_prefence(int toSet)
    {
        PlayerPrefs.SetInt("aim_FOV", toSet);
    }

    void Set_Sensitivity_preference(float toSet)
    {
        PlayerPrefs.SetFloat("aim_Sensitivity", toSet);
    }

    public void SetCrossHairBloom( float bloomFactor)
    {
        float desiredPos = (bloomFactor / 2) - 5;

        if(desiredPos < 5)
        {
            desiredPos = 5;
        }
        
        top.localPosition = new Vector3(0, desiredPos, 0);
        bottom.localPosition = new Vector3(0, -desiredPos, 0);

        left.localPosition = new Vector3(-desiredPos, 0, 0);
        right.localPosition = new Vector3(desiredPos, 0, 0);


    }

    public void Change_AmmoCount( string new_String)
    {
        if(ammoFeed != null)
        {
            ammoFeed.text = new_String;
        }
    }

    public void Change_ItemName(string new_string)
    {
        if(nameFeed != null)
        {
            nameFeed.text = new_string;
        }
    }

    public void Change_Debug_MovementText(string new_String)
    {
        if (Debug_MovementFeed != null)
        {
            Debug_MovementFeed.text = ("Debug : " + new_String);
        }
    }

    public void Change_Health_UI(int currentHealth)
    {

        if(healthBarUI != null)
        {
            healthBarUI.sizeDelta = new Vector2(currentHealth, healthBar.sizeDelta.y);
        }
        
    }

    public void Change_health_bar(int currentHealth)
    {
        healthBar.sizeDelta = new Vector2(currentHealth, healthBar.sizeDelta.y);
    }

    public void Set_Pause_Menu(bool inBool)
    {
        pauseMenu.SetActive(inBool);
    }
}
