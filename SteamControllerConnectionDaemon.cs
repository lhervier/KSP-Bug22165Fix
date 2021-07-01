using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using KSP.UI.Screens.Flight;
using com.github.lhervier.ksp;

namespace com.github.lhervier.ksp {

    // <summary>
    //  Daemon in charge of listening to steam controllers connection/disconnection
    // </summary>
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class SteamControllerConnectionDaemon : MonoBehaviour {
        
        // <summary>
        //  Instance to the object
        // </summary>
        public static SteamControllerConnectionDaemon INSTANCE = null;

        // <summary>
        //  Logger object
        // </summary>
        private static SteamControllerLogger LOGGER = new SteamControllerLogger("ConnectionDaemon");

        // ===============================================

        // <summary>
        //  Called when a new controller is connected
        // </summary>
        public List<Action> OnControllerConnected { get; private set; }

        // <summary>
        //  Called when a controller is disconnected
        // </summary>
        public List<Action> OnControllerDisconnected {get; private set; }

        // <summary>
        //  Has the controller been configured ?
        // </summary>
        public bool ControllerConnected { get; private set; }

        // <summary>
        //  Handle to the first connected controller. No sense if controllerConnected = false
        // </summary>
        public ControllerHandle_t ControllerHandle { get; private set; }

        // ==============================================

        // <summary>
        //  Handles to the connected steam controllers.
        //  Don't use. This array is here to prevent from instanciating a new one every seconds.
        // </summary>
        private ControllerHandle_t[] _controllerHandles = new ControllerHandle_t[Constants.STEAM_CONTROLLER_MAX_COUNT];

        // =======================================================================
        //              Unity Lifecycle
        // =======================================================================

        // <summary>
        //  Plugin awaked
        // </summary>
        public void Awake() {
            DontDestroyOnLoad(this);
            INSTANCE = this;

            this.OnControllerConnected = new List<Action>();
            this.OnControllerDisconnected = new List<Action>();
            this.ControllerConnected = false;
            LOGGER.Log("Awaked");
        }

        // <summary>
        //  Plugin destroyed
        // </summary>
        public void OnDestroy() {
            INSTANCE = null;
            LOGGER.Log("Destroyed");
        }

        // <summary>
        //  Startup of the beahviour
        // </summary>
        public void Start() {
            if( !SteamManager.Initialized ) {
                LOGGER.Log("Steam not detected. Unable to start the daemon.");
                return;
            }

            SteamAPI.RunCallbacks();
            SteamController.Init();
            
            this.ControllerConnected = false;

            this.StartCoroutine(this.CheckForController());
            LOGGER.Log("Started");
        }

        // ==============================================================================
        //              Detection of connection/disconnection of controllers
        // ==============================================================================
        
        // <summary>
        //  Main loop to detect controller connection/disconnection
        // </summary>
        private IEnumerator CheckForController() {
            WaitForSeconds updateYield = new WaitForSeconds(1f);
            while( true ) {
                SteamController.RunFrame();

                // Detect connection/disconnection
                int nbControllers = SteamController.GetConnectedControllers(this._controllerHandles);
                bool newController = false;
                bool disconnectedController = false;
                if( nbControllers == 0 ) {
                    if( this.ControllerConnected ) {
                        newController = false;
                        disconnectedController = true;
                    } else {
                        newController = false;
                        disconnectedController = false;
                    }
                } else {
                    if( this.ControllerConnected ) {
                        if( this.ControllerHandle == this._controllerHandles[0] ) {
                            newController = false;
                            disconnectedController = false;
                        } else {
                            newController = true;
                            disconnectedController = true;
                        }
                    } else {
                        newController = true;
                        disconnectedController = false;
                    }
                }

                // Disconnect the current controller
                if( disconnectedController ) {
                    LOGGER.Log("Steam Controller disconnected");
                    this.ControllerConnected = false;
                    foreach( Action action in this.OnControllerDisconnected ) {
                        action();
                    }
                }

                // Connect a new controller
                if( newController ) {
                    LOGGER.Log("Steam Controller connected");
                    this.ControllerHandle = this._controllerHandles[0];
                    this.StartCoroutine(this.SayHello());
                    this.ControllerConnected = true;
                    foreach( Action action in this.OnControllerConnected ) {
                        action();
                    }
                }

                // Wait for 1 second
                yield return updateYield;
            }
        }
        
        // <summary>
        //  Trigger a set of pulses on the current controller to say hello
        // </summary>
        public IEnumerator SayHello() {
            if( this.ControllerConnected ) {
                LOGGER.Log("Hello new Controller !!");
                for( int i = 0; i < 2; i++ ) {
                    SteamController.TriggerHapticPulse(this.ControllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Right, ushort.MaxValue);
                    yield return new WaitForSeconds(0.1f);
                    SteamController.TriggerHapticPulse(this.ControllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Left, ushort.MaxValue);
                    yield return new WaitForSeconds(0.1f);
                    SteamController.TriggerHapticPulse(this.ControllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Right, ushort.MaxValue);
                    yield return new WaitForSeconds(0.1f);
                    SteamController.TriggerHapticPulse(this.ControllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Left, ushort.MaxValue);
                }
            }
        }

    }
}