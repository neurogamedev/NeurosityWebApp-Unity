using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// This code is based on Ryan Turney's Notion SDK for Unity (https://github.com/ryanturney/notion-unity).
/// As he puts it, the architecture may not be soud, so be careful when Building for mobile or when updating dependencies.
/// This script is conceived to provide Unity with data from the Neurosity Crown or Notion so that you can just worry about designing a game.
/// Enjoy! - Diego Saldivar
/// </summary>

namespace Notion.Unity
{
    public class NotionInterfacer : MonoBehaviour
    {

        // You'll have to provide a Device instance. The scriptable object retains information after the game is over, so clear it if you're concerned about privacy.
        // To have a Device you can drag and drop from the assets, you just click "Assets -> Create -> Device" and a new Device object will be created.
        // You can fill it in with information manually or use a GUI to fill it in like in this example.
        [SerializeField]
        private Device device;
        
        [HideInInspector]
        public FirebaseController controller;

        [HideInInspector]
        public Notion notion;

        [HideInInspector]
        public bool IsLoggedIn = false;

        [HideInInspector]
        public bool IsSubscribed = false;

        [HideInInspector]
        public DeviceStatus currentStatus;

        // The very basics you'll need for a neurogame.
        [Header("Device Status")]

        public string selectedDeviceId;
        public string deviceStatus;
        public float deviceBattery;

        // In case you only want to subscribe to a few channels.
        // Feel free to add more stuff from other handlers.
        [Header("Subscriptions")]

        public bool subscribeToCalm;
        public float calmScore;

        public bool subscribeToFocus;
        public float focusScore;

        public bool subscribeToAccelerometer;
        public float accelerometerAcceleration;
        public float accelerometerInclination;
        public float accelerometerOrientation;
        public float accelerometerPitch;
        public float accelerometerRoll;
        public Vector3 accelerometerVector;


        // This one is pretty obvious if you read the summary above.
        // I tried writing code that didn't need a Device instance but turns out this SDK needs it to pass information between classes.
        // Also, creating an instance during runtime leads to all kinds of pointer headaches.
        private void OnEnable()
        {
            // Let's initialize some values.
            ClearStatusValues();
            ClearSubscriptionValues();

            //  First sanity check.
            if (device == null)
            {
                Debug.LogError( "Provide a device device instance. Assets -> Create -> Device", this );
                return;
            }
        }


        // Turns out you can login without a device ID, in which instance Firabease will just log into your default or last device.
        // Be wary of the await commands, they tend to throw errors if something isn't perfect. I deal with these in the GUI.
        public async Task Login(string _inputEmail, string _inputPassword)
        {
            device.Email = _inputEmail;
            device.Password = _inputPassword;

            controller = new FirebaseController();
            await controller.Initialize();

            notion = new Notion(controller);
            await notion.Login(device);

            IsLoggedIn = true;

            //Debug.Log( "Logged In" );

        }


        // This guy throws some warnings when logging out.
        // Some forums say you have to upgrade the Firebase version...
        // I'll deal with these warnings in a future version.
        // For now, the user isn't bothered too much.
        public async Task Logout()
        {
            if ( notion != null )
            await notion.Logout();

            if( controller.NotionDatabase != null )
            controller.Logout();

            controller = null;
            notion = null;

            ClearDeviceInfo( device );

            IsLoggedIn = false;
            IsSubscribed = false;

            ClearStatusValues();
            ClearSubscriptionValues();

            //Debug.Log( "Logged Out" );
        }


        // I clear the device info in order to not accidentally give you my login information when I build or share this project.
        // You may want to skip this step or save the device login info if you want your game to relog automatically.
        public void ClearDeviceInfo( Device usedDevice )
        {
            usedDevice.Email = "";
            usedDevice.Password = "";
            usedDevice.DeviceId = "";
        }


        // I conceived this function to be as user-friendly as possible.
        // This way you can send the name of the device instead of the super long Device ID.
        public async Task SelectDevice( string selectedDeviceNickname )
        {
            var devicesInfo = await notion.GetDevices();

            foreach ( DeviceInfo device in devicesInfo )
            {
                if ( device.DeviceNickname == selectedDeviceNickname )
                {
                    this.device.DeviceId = device.DeviceId;
                    
                    controller = new FirebaseController();
                    await controller.Initialize();

                    notion = new Notion( controller );
                    await notion.Login( this.device );

                    Subscribe();

                    //Debug.Log( selectedDeviceNickname + "  is streaming." );
                }
                return;
            }
        }


        // And the fun begins!
        public void Subscribe()
        {
            if ( IsSubscribed ) return;

            if ( subscribeToCalm ) { SubscribeCalm(); }
            if ( subscribeToFocus ) { SubscribeFocus(); }
            if ( subscribeToAccelerometer ) { SubscribeAccelerometer(); }

            IsSubscribed = true;
        }


        // Get ALL of your devices in your account.
        // This function is legacy. It was made to print all devices in the console.
        // Enable the last line to use as intended.
        public async void GetDevices()
        {
            if ( !notion.IsLoggedIn ) return;
            var devices = await notion.GetDevices();

            //Debug.Log( JsonConvert.SerializeObject( devices ) );
        }


        // Get the Status of the device currently in use.
        // This function is also legacy. It was made to print all of the info in the class DeviceStatus in one fell swoop.
        // Enable the last line to use as intended.
        public void GetStatus()
        {
            if ( notion == null ) return;
            if ( !notion.IsLoggedIn ) return;

            //Debug.Log( JsonConvert.SerializeObject( notion.Status ) );
        }


        // This is where we get the real info every time we call.
        // This is also where some of the problems may arise upon Logout(), since interrupting some of these processes mid-execution may throw some warnings and errors.
        // Since this is also being called every so often, there is a myriad of sanity checks before pulling the data from the Firebse database.
        public async Task UpdateStatus()
        {
            if ( notion == null ) return;                       // If you're logged out, stop!
            if ( !notion.IsLoggedIn ) return;                   // If you're not yet logged in, stop!
            if ( !device ) return ;                             // If the device isn't valid, id est, if it doesn't have an e-mail, a password and a device ID.
            
            selectedDeviceId = device.DeviceId;                 // Let's now check if the device ID is valid or if it has a placeholder string.
            if ( selectedDeviceId.Length < 30 ) return;         // If you have a placeholder string "Not selected", stop!
            if ( controller.NotionDatabase == null ) return;    // If you're logging out or not yet connected to the device, stop!

            // This SDK doesn't subscribe to the status as much as it gives you a snapshot of the status, thus the need to Update() the info.
            var statusSnapshot = await controller.NotionDatabase.GetReference( $"devices/{selectedDeviceId}/status" ).GetValueAsync();
            if ( statusSnapshot == null ) return;               // You can sometimes be given asnapshot with nothing on it. Usually happens during Login() and Logout();
            
            string json = statusSnapshot.GetRawJsonValue();
            if ( json == null ) return;                         // Don't ask me why, but sometimes null values slip by anyway.


            currentStatus = JsonConvert.DeserializeObject<DeviceStatus>( json );    // Consult class DeviceStatus to find out exactly what info is parsed.

            if(currentStatus.SleepModeReason.ToString() != "Null")
            {
                ClearSubscriptionValues();
                deviceStatus = currentStatus.SleepModeReason.ToString();
                deviceBattery = currentStatus.Battery;
                return;
            }

            deviceBattery = currentStatus.Battery;                                  // Batery percentage from 0 to 100.

            if ( currentStatus.SleepMode == true )                                  // Let's get a string giving us a description of the device status. Exempli gratia: Charging, Updating, Online, Offline, et cetera.
            {
                deviceStatus = currentStatus.SleepModeReason.ToString();
            }
            else
            {
                deviceStatus = currentStatus.State.ToString();
            }

        }

        //Clear up Status values
        public void ClearStatusValues()
        {
            selectedDeviceId = "Not selected";
            deviceStatus = "";
            deviceBattery = 0;
        }


        // Sometimes you need to ensure you can read a bunch of zeroes.
        public void ClearSubscriptionValues()
        {
            calmScore = 0;
            focusScore = 0;
            accelerometerAcceleration = 0;
            accelerometerInclination = 0;
            accelerometerOrientation = 0;
            accelerometerPitch = 0;
            accelerometerRoll = 0;
            accelerometerVector = new Vector3 { x = 0, y = 0, z = 0 };
        }


        // The subscribers are mostly self explanatory.
        // I had to modify the handlers to send info to another class instead of merely to the console.
        // That is usually the main hitch with Unity: COMMUNICATION.

        // Subscribe to Calm.
        public void SubscribeCalm()
        {
            if ( !notion.IsLoggedIn ) return;

            notion.Subscribe( new CalmHandler 
            { 
                OnCalmUpdated = ( probability ) => 
                { 
                    calmScore = probability;
                } 
            } );

            //Debug.Log( "Subscribed to calm" );

        }

        // Subscribe to Focus.
        public void SubscribeFocus()
        {
            if ( !notion.IsLoggedIn ) return;

            notion.Subscribe( new FocusHandler
            {
                OnFocusUpdated = ( probability ) =>
                {
                    focusScore = probability;
                }
            } );

            //Debug.Log( "Subscribed to focus" );
        }

        // Subscribe to the accelerometer's direction and acceleration.
        // Remember:
        //              x = roll
        //              y = pitch
        //              z = yaw (not provided by handler)
        public void SubscribeAccelerometer()
        {
            if ( !notion.IsLoggedIn ) return;
            notion.Subscribe( new AccelerometerHandler 
            { 
                OnAccelerometerUpdated = ( accelerometer ) =>
                {
                    accelerometerAcceleration = accelerometer.Acceleration;
                    accelerometerInclination = accelerometer.Inclination;
                    accelerometerOrientation = accelerometer.Orientation;

                    accelerometerPitch = accelerometer.Pitch;
                    accelerometerRoll = accelerometer.Roll;

                    accelerometerVector.x = accelerometer.X;
                    accelerometerVector.y = accelerometer.Y;
                    accelerometerVector.z = accelerometer.Z;
                }
            } );

            //Debug.Log( "Subscribed to accelerometer" );
        }


        // This one is legacy but can be used.
        // I'll leave it here since it's syntax is slightly different from the handlers above.

        /// <summary>
        /// Add kinesisLabel based on the thought you're training.
        /// For instance: leftArm, rightArm, leftIndexFinger, etc
        /// </summary>
        /// <param name="kinesisLabel"></param>
        public void SubscribeKinesis( string kinesisLabel )
        {
            if ( !notion.IsLoggedIn ) return;

            notion.Subscribe( new KinesisHandler
            {
                Label = kinesisLabel,
                OnKinesisUpdated = ( probability ) => 
                {
                    //_textKinesisProbability.text = $"{kinesisLabel} : {probability}";
                }
            } );
        }


        public async void FixedUpdate()
        {
            try 
            {
                await UpdateStatus();
            }
            catch ( Exception e )
            {
                Debug.Log( e.ToString() );
            }
        }


        private async void OnDisable()
        {
            if ( notion == null ) return;
            if ( !notion.IsLoggedIn ) return;

            // Wrapping because Logout is meant to be invoked and forgotten about for use in button callbacks.
            // Also, you have to use try/catch to avoid having warnings and errors stop the game.
            // Unity may still crash if too many warnings come up after a logout.
            // I used a FixedUpdate() to reduce the number of calls that could be interrupted upon logout.
            // You could also use AfterUpdate(). Consult Unity documentation to undertsand the difference.
            try
            {
                await Task.Run( () => Logout() );
            }
            catch( Exception e )
            {
                
                Debug.Log( e.ToString() );
            }

            ClearDeviceInfo(device);
            Debug.Log( $"Logged out from {nameof(OnDisable)}" );
        }

        // This clears the device info when quitting the game.
        // It's a privacy measure for me, but you can get rid of it if you'd like to retain info between game sessions.
        void OnApplicationQuit()
        {
            ClearDeviceInfo(device);
        }
    }
}
