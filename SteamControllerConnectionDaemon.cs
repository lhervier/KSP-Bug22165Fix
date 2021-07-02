using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;

namespace com.github.lhervier.ksp {

    // <summary>
    //  Daemon in charge of listening to steam controllers connection/disconnection
    //  It also allow to change the current action set of the controller
    // </summary>
    public class SteamControllerConnectionDaemon : MonoBehaviour {
        
        // ==========================================================================================
        //                          Static properties
        // ==========================================================================================

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

        // ==============================================

// <summary>
        //  Handle to the first connected controller. No sense if controllerConnected = false
        // </summary>
        private ControllerHandle_t controllerHandle;

        // <summary>
        //  The action sets handles defined in the steam controller configuration template
        // </summary>
        private IDictionary<KSPActionSets, ControllerActionSetHandle_t> actionsSetsHandles = new Dictionary<KSPActionSets, ControllerActionSetHandle_t>();

        // <summary>
        //  Handles to the connected steam controllers.
        //  Don't use. This array is here to prevent from instanciating a new one every cycle.
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
            
            this.OnControllerConnected = new List<Action>();
            this.OnControllerDisconnected = new List<Action>();
            this.ControllerConnected = false;
            LOGGER.Log("Awaked");
        }

        // <summary>
        //  Plugin destroyed
        // </summary>
        public void OnDestroy() {
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
            WaitForSeconds waitFor1Second = new WaitForSeconds(1);
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
                        if( this.controllerHandle == this._controllerHandles[0] ) {
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
                    this.UnloadActionSets();
                    foreach( Action action in this.OnControllerDisconnected ) {
                        action();
                    }
                }

                // Connect a new controller
                if( newController ) {
                    LOGGER.Log("Steam Controller connected");
                    this.controllerHandle = this._controllerHandles[0];
                    this.ControllerConnected = true;
                    this.LoadActionSets();
                    this.StartCoroutine(this.SayHello());
                    foreach( Action action in this.OnControllerConnected ) {
                        action();
                    }
                }

                // Wait for 1 second
                yield return waitFor1Second;
            }
        }
        
        // <summary>
        //  Load action sets handles. The API don' ask for a handle on a controller.
        //  It seems to load the action sets of the first controller.
        // </summary>
        private void LoadActionSets() {
            if( !this.ControllerConnected ) {
                return;
            }

            LOGGER.Log("Loading Action Set Handles");
            foreach(KSPActionSets actionSet in Enum.GetValues(typeof(KSPActionSets))) {
                string actionSetName = actionSet.GetId();
                LOGGER.Log("- Getting action set handle for " + actionSetName);
                // Action Sets list should depend on the used controller. But that's not what the API is waiting for...
                ControllerActionSetHandle_t actionSetHandle = SteamController.GetActionSetHandle(actionSetName);
                if( actionSetHandle.m_ControllerActionSetHandle == 0L ) {
                    LOGGER.Log("ERROR : Action set handle for " + actionSetName + " not found. I will use the default action set instead");
                }
                this.actionsSetsHandles[actionSet] = actionSetHandle;
            }
        }

        // <summary>
        //  Unloads the action sets
        // </summary>
        private void UnloadActionSets() {
            this.actionsSetsHandles.Clear();
        }

        // <summary>
        //  Trigger a set of pulses on the current controller to say hello
        // </summary>
        private IEnumerator SayHello() {
            if( this.ControllerConnected ) {
                LOGGER.Log("Hello new Controller !!");
                for( int i = 0; i < 4; i++ ) {
                    SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Right, ushort.MaxValue);
                    yield return new WaitForSeconds(0.1f);
                    SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Left, ushort.MaxValue);
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        // =========================================================================================

        // <param name="actionSet">The action set to set</param>
        // <summary>
        //  Change the current action set
        // </summary>
        public void setActionSet(KSPActionSets actionSet) {
            if( !this.ControllerConnected ) {
                return;
            }

            SteamController.ActivateActionSet(
                this.controllerHandle, 
                this.actionsSetsHandles[actionSet]
            );
        }
    }
}