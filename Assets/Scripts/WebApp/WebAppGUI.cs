using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Notion.Unity
{ 
    public class WebAppGUI : MonoBehaviour
    {
        // Drag and drop these from the Hierarchy into the Inspector.

        [Header("Login Info")]
        public GameObject panelLogin;
        public InputField inputEmail;
        public InputField inputPassword;
        public Button buttonLogin;
        public Text infoText;

        [Header("Device Selection")]
        public GameObject panelDevices;
        public Dropdown dropdownDevices;
        public Button buttonSelect;

        [Header("Device Status")]
        public GameObject panelStatus;
        public Button buttonGear;
        public Text deviceName;
        public Image statusIcon;
        public Sprite dotSprite;            // This is an asset, not in the scene.
        public Sprite moonSprite;           // This is an asset, not in the scene.
        public Text statusText;
        public Text chargeText;
        public Button buttonLogout;
        public Text focusScore;
        public Text calmScore;


        private NotionInterfacer deviceInterface;
        private string currentDeviceNickname;
        private List<GameObject> canvasPanels;

        void OnEnable()
        {
            // Let's start on the Login panel.
            canvasPanels = new List<GameObject>();
            canvasPanels.AddRange(new List<GameObject>{panelLogin, panelDevices, panelStatus});
            SwitchToPanel(panelLogin);

            // Look for the NotionInterfacer script where we'll get all of our values from. 
            deviceInterface = FindObjectOfType<NotionInterfacer>();

            // Let's initialize all the buttons.
            buttonLogin.onClick.AddListener(() => Login());
            buttonSelect.onClick.AddListener(() => SelectDevice(dropdownDevices.captionText.text));
            buttonGear.onClick.AddListener(() => Gear());
            buttonLogout.onClick.AddListener(() => Logout());
        }


        public async Task Login()
        {

            try // Let's try to log in.
            {
                buttonLogin.interactable = false;
                buttonLogin.GetComponentInChildren<Text>().text = "Logging in...";
                await deviceInterface.Login(inputEmail.text, inputPassword.text);   
            } 
            catch (Exception e) // Otherwise tell the user what went wrong.
            {
                infoText.text = e.InnerException.Message.ToString();
                buttonLogin.GetComponentInChildren<Text>().text = "Login";
                buttonLogin.interactable = true;
                return;
            }

            
            try
            {
                // If the login was successful, let's try to get the list of devices registered to your account.
                var devicesInfo = await deviceInterface.notion.GetDevices();

                //If successful, let's move to the Device selection panel and fill the Dropdown list.
                SwitchToPanel(panelDevices);

                dropdownDevices.ClearOptions();

                foreach (DeviceInfo device in devicesInfo)
                {
                    var nextDatum = new Dropdown.OptionData();
                    nextDatum.text = device.DeviceNickname;
                    dropdownDevices.options.Add(nextDatum);
                }

                dropdownDevices.RefreshShownValue();

                // Cleanup of precedent panel for later use.
                buttonLogin.GetComponentInChildren<Text>().text = "Login";
                infoText.text = "";
                buttonLogin.interactable = true;
            }
            catch (NullReferenceException) //If getting the devices was unsuccessful, let's inform the user.
            {
                infoText.text = "No devices could be fetched.";
                buttonLogin.GetComponentInChildren<Text>().text = "Login";
                buttonLogin.interactable = true;
                return;
            }

        }


        public async void SelectDevice(string selectedDeviceNickname) 
        {
            try // Let's try to stream data from the selected device.
            {
                buttonSelect.interactable = false;
                buttonSelect.GetComponentInChildren<Text>().text = "Fetching data...";
                await deviceInterface.SelectDevice(selectedDeviceNickname);
                currentDeviceNickname = selectedDeviceNickname;
                deviceInterface.Subscribe();

                deviceName.text = currentDeviceNickname;
                SwitchToPanel(panelStatus);                     //If successfully connected to the device, let's get to the Status panel.

                // Cleanup of precedent panel for later use.
                buttonSelect.GetComponentInChildren<Text>().text = "Select";
                buttonSelect.interactable = true;
            }
            catch (Exception e) //At this stage, it would be very difficult to fail, but you never know.
            {
                buttonSelect.GetComponentInChildren<Text>().text = "Select";
                Debug.Log(e.ToString());
            }   
        }

        // The gear button let's you go back into the Device selection panel.
        // We have to relog to clear subscriptions and trackers, etc.
        // I didn't find a cleaner solution in this SDK.
        public async void Gear()
        {
            SwitchToPanel(panelDevices);
            dropdownDevices.ClearOptions();
            var placeholder = new Dropdown.OptionData();
            placeholder.text = "Fetching devices...";
            dropdownDevices.options.Add(placeholder);
            dropdownDevices.RefreshShownValue();

            await deviceInterface.Logout(); // I'm directly calling the device's Logout() function to avoid going back to the Login panel, which is part of this GUI's Logout() function.
            await Login();                  //The GUI Login() function has some necessary steps that affect the Device Selection panel, so I just call it outright.
        }

        public async Task Logout()
        {
            focusScore.text = "00%";
            calmScore.text = "00%";
            buttonLogout.interactable = false;
            buttonLogout.GetComponentInChildren<Text>().text = "Logging out...";

            await deviceInterface.Logout();

            // Back to where we started!
            SwitchToPanel(panelLogin);

            // Cleanup of precedent panel for later use.
            buttonLogout.GetComponentInChildren<Text>().text = "Logout";
            buttonLogout.interactable = true;
        }


        // Quickly move between panels.
        // If you add or remove panels, make sure to update the canvasPanels initialization in the OnEnable() fucntion;
        void SwitchToPanel(GameObject nextPanel) 
        { 
            foreach (GameObject panel in canvasPanels)
            {
                if (panel != nextPanel) panel.SetActive(false);
                else panel.SetActive(true);
            }
        }

        // This is the fun part where you tell the GUI how to show the Status
        void StatusUpdate()
        {
            if (deviceInterface.deviceStatus == "") return;

            if (deviceInterface.deviceStatus == "Updating")
            {
                statusIcon.color = Color.white;
                statusIcon.sprite = moonSprite;
                statusText.text = "Sleeping while updating";
                chargeText.text = "Charge " + deviceInterface.deviceBattery + "%";
                calmScore.text = "00%";
                focusScore.text = "00%";
                return;
            }

            if (deviceInterface.deviceStatus == "Charging")
            {
                statusIcon.color = Color.white;
                statusIcon.sprite = moonSprite;
                statusText.text = "Sleeping while charging";
                chargeText.text = "Charging " + deviceInterface.deviceBattery + "%";
                calmScore.text = "00%";
                focusScore.text = "00%";
                return;
            }

            if (deviceInterface.deviceStatus == "Online")
            {
                statusIcon.color = Color.green;
                statusIcon.sprite = dotSprite;
                statusText.text = "Online";
                chargeText.text = "Charged " + deviceInterface.deviceBattery + "%";
                calmScore.text = string.Format("{0:00}%", (deviceInterface.calmScore * 100));       // The scores are usually given from 0 to 1, so I formatted them to be presented as something like 23% instead of 0.2356010....
                focusScore.text = string.Format("{0:00}%", (deviceInterface.focusScore * 100));
                return;
            }

            if (deviceInterface.deviceStatus == "Offline" || deviceInterface.deviceStatus == "Booting" || deviceInterface.deviceStatus == "ShuttingOff")
            {
                statusIcon.color = Color.red;
                statusIcon.sprite = dotSprite;
                statusText.text = deviceInterface.deviceStatus;
                chargeText.text = "No battery information";
                calmScore.text = "00%";
                focusScore.text = "00%";
                return;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (!deviceInterface) return;

            if (panelStatus.activeSelf) 
            {
                StatusUpdate();
            }

        }
    }
}

