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
        //  Connection Daemon
        // </summary>
        private SteamControllerConnectionDaemon connectionDaemon;
        
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
            
            // Attach to connection Daemon
            this.connectionDaemon = SteamControllerConnectionDaemon.INSTANCE;
            this.connectionDaemon.OnControllerConnected.Add(OnControllerConnected);
            this.connectionDaemon.OnControllerDisconnected.Add(OnControllerDisconnected);
            LOGGER.Log("Connection Daemon attached");

            // In case controller is already connected
            if( this.connectionDaemon.ControllerConnected ) {
                this.OnControllerConnected();
            }

            LOGGER.Log("Started");
        }

        // ==============================================================================
        //              Detection of connection/disconnection of controllers
        // ==============================================================================
        
        // <summary>
        //  New controller connected
        // </summary>
        private void OnControllerConnected() {
            // Load action sets handles. The API don' ask for a handle on a controller.
            // It seems to load the action sets of the first controller.
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

            // Trigger an action set change to load the right action set
            this.TriggerActionSetChange();
        }

        // <summary>
        //  Controller disconnected
        // </summary>
        private void OnControllerDisconnected() {
            // Unload action sets handles
            LOGGER.Log("Unloading Action Set Handles");
            this.actionsSetsHandles.Clear();
        }

        // ================================================================

        // <param name="ComputeActionSet">Function that will compute the action set to set</param>
        // <summary>
        //  Demande à mettre à jour l'action set courant
        // </summary>
        public void TriggerActionSetChange() {
            if( !this.connectionDaemon.ControllerConnected ) {
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
            if( !this.connectionDaemon.ControllerConnected ) {
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
            SteamController.ActivateActionSet(
                this.connectionDaemon.ControllerHandle, 
                this.actionsSetsHandles[actionSet]
            );
            this.OnActionSetChanged(actionSet);
        }
    }
}