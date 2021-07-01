using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using KSP.UI.Screens.Flight;
using com.github.lhervier.ksp;

namespace com.github.lhervier.ksp {

    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class SteamControllerDaemon : MonoBehaviour {
        
        // <summary>
        //  Instance to the object
        // </summary>
        public static SteamControllerDaemon INSTANCE = null;

        // <summary>
        //  Logger object
        // </summary>
        private static SteamControllerLogger LOGGER = new SteamControllerLogger("Daemon");

        // ===============================================

        // <summary>
        //  Called when the action set has changed
        // </summary>
        public Action<KSPActionSets> OnActionSetChanged { get; set; }

        // <summary>
        //  Function called to get the action set, depending on the current KSP context
        // </summary>
        public Func<KSPActionSets> ComputeActionSet {get; set; }

        // ==============================================

        // <summary>
        //  Handles to the connected steam controllers.
        //  Don't use. This array is here to prevent from instanciating a new one every seconds.
        // </summary>
        private ControllerHandle_t[] _controllerHandles = new ControllerHandle_t[Constants.STEAM_CONTROLLER_MAX_COUNT];

        // <summary>
        //  Handle to the first connected controller
        // </summary>
        private ControllerHandle_t controllerHandle;

        // <summary>
        //  Has the controller been configured ?
        // </summary>
        private bool controllerConfigured = false;

        // <summary>
        //  The action sets handles defined in the steam controller configuration template
        // </summary>
        private IDictionary<KSPActionSets, ControllerActionSetHandle_t> actionsSetsHandles = new Dictionary<KSPActionSets, ControllerActionSetHandle_t>();

        // <summary>
        //  Frame count at which a controller mode change has been asked.
        //  The real update will be 10 frames later, except if another operation
        //  ask for another update, which will increase this value.
        // </summary>
        private int askedActionSetChange;

        // <summary>
        //  CoRoutine used tu update the controller action set
        // </summary>
        private Coroutine UpdateActionSetCoroutine;

        // =======================================================================
        //              Unity Lifecycle
        // =======================================================================

        // <summary>
        //  Plugin awaked
        // </summary>
        public void Awake() {
            LOGGER.Log("Awaked");
            DontDestroyOnLoad(this);
            INSTANCE = this;
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
                    if( this.controllerConfigured ) {
                        newController = false;
                        disconnectedController = true;
                    } else {
                        newController = false;
                        disconnectedController = false;
                    }
                } else {
                    if( this.controllerConfigured ) {
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
                    this.actionsSetsHandles.Clear();
                    this.controllerConfigured = false;
                }

                // Connect a new controller
                if( newController ) {
                    LOGGER.Log("Steam Controller connected");
                    this.controllerHandle = this._controllerHandles[0];
                    this.LoadActionSetHandles();
                    this.StartCoroutine(this.SayHello());
                    this.controllerConfigured = true;
                    this.TriggerActionSetChange();
                }

                // Wait for 1 second
                yield return updateYield;
            }
        }

        // <summary>
        //  Load action sets handles. As it is independant of a handle on a controller,
        //  it seems to load the action sets of the first controller.
        // </summary>
        private void LoadActionSetHandles() {
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
        //  Trigger a set of pulses on the current controller to say hello
        // </summary>
        public IEnumerator SayHello() {
            LOGGER.Log("Hello new Controller !!");
            for( int i = 0; i < 2; i++ ) {
                SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Right, ushort.MaxValue);
                yield return new WaitForSeconds(0.1f);
                SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Left, ushort.MaxValue);
                yield return new WaitForSeconds(0.1f);
                SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Right, ushort.MaxValue);
                yield return new WaitForSeconds(0.1f);
                SteamController.TriggerHapticPulse(this.controllerHandle, Steamworks.ESteamControllerPad.k_ESteamControllerPad_Left, ushort.MaxValue);
            }
        }

        // ================================================================

        // <param name="ComputeActionSet">Function that will compute the action set to set</param>
        // <summary>
        //  Demande à mettre à jour l'action set courant
        // </summary>
        public void TriggerActionSetChange() {
            if( !this.controllerConfigured ) {
                return;
            }
            
            this.askedActionSetChange = Time.frameCount;
            if( this.UpdateActionSetCoroutine == null ) {
                this.UpdateActionSetCoroutine = this.StartCoroutine(_TriggerActionSetChange());
            }
        }

        // <summary>
        //  Cancel any action set change request
        // </summary>
        public void CancelActionSetChange() {
            if( this.UpdateActionSetCoroutine == null ) {
                return;
            }

            this.StopCoroutine(this.UpdateActionSetCoroutine);
            this.UpdateActionSetCoroutine = null;
        }

        // <param name="actionSet">The action set to set</param>
        // <summary>
        //  Change the current action set NOW (without delay)
        // </summary>
        public void SetActionSet(KSPActionSets actionSet) {
            if( !this.controllerConfigured ) {
                return;
            }

            this.CancelActionSetChange();
            this._SetActionSet(actionSet);
        }

        private IEnumerator _TriggerActionSetChange() {
            while (Time.frameCount - askedActionSetChange < 10) {
                yield return null;
            }
            this._SetActionSet(this.ComputeActionSet());
            this.UpdateActionSetCoroutine = null;
        }

        private void _SetActionSet(KSPActionSets actionSet) {
            LOGGER.Log("=> Setting controller Action Set to " + actionSet.GetLabel());
            SteamController.ActivateActionSet(this.controllerHandle, this.actionsSetsHandles[actionSet]);
            this.OnActionSetChanged(actionSet);
        }
    }
}