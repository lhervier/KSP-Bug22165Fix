using UnityEngine;

namespace com.github.lhervier.ksp {
    
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class SteamControllerPlugin : MonoBehaviour {
        
        // <summary>
        //  Logger
        // </summary>
        private static SteamControllerLogger LOGGER = new SteamControllerLogger();
        
        // <summary>
        //  Delay before changing an action set (in frames)
        // </summary>
        private static int DELAY = 10;
        
        // ==================================================================================

        // <summary>
        //  Message indicating when on Steam Controller mode change
        // </summary>    
        private ScreenMessage screenMessage;

        // <summary>
        //  Previous action set (so we don't display the message when the value has not changed)
        // </summary>
        private KSPActionSets prevActionSet;

        // <summary>
        //  Connection Daemon
        // </summary>
        private SteamControllerDaemon connectionDaemon;
        
        // <summary>
        //  Delayed Action daemon
        // </summary>
        private DelayedActionDaemon delayedActionDaemon;

        // ===============================================================================
        //                      Unity initialization
        // ===============================================================================

        // <summary>
        //  Make our plugin survive between scene loading
        // </summary>
        protected void Awake() {
            LOGGER.Log("Awaked");
            DontDestroyOnLoad(this);
        }

        // <summary>
        //  Start of the plugin
        // </summary>
        protected void Start() {
            if( !SteamManager.Initialized ) {
                LOGGER.Log("Steam not detected. Unable to start the plugin.");
                return;
            }
            
            // Attach to connection Daemon
            this.connectionDaemon = gameObject.AddComponent<SteamControllerDaemon>();
            this.connectionDaemon.OnControllerConnected.Add(OnControllerConnected);
            this.connectionDaemon.OnControllerDisconnected.Add(OnControllerDisconnected);
            LOGGER.Log("Connection Daemon attached");

            // Attach to delayed action daemon
            this.delayedActionDaemon = gameObject.AddComponent<DelayedActionDaemon>();
            LOGGER.Log("Delayed Actions Daemon attached");
            
            // Prepare screen message
            this.screenMessage = new ScreenMessage(
                string.Empty, 
                5f, 
                ScreenMessageStyle.UPPER_RIGHT
            );
            LOGGER.Log("Status message ready");

            // Hooks to KSP
            GameEvents.onLevelWasLoadedGUIReady.Add(OnLevelWasLoadedGUIReady);
            GameEvents.onGamePause.Add(OnGamePause);
            GameEvents.onGameUnpause.Add(OnGameUnpause);
            GameEvents.OnFlightUIModeChanged.Add(OnFlightUIModeChanged);
            GameEvents.OnMapEntered.Add(OnMapEntered);
            GameEvents.onVesselChange.Add(OnVesselChange);
            LOGGER.Log("KSP hooks created");

            // When a controller is already connected
            if( this.connectionDaemon.ControllerConnected ) {
                this.OnControllerConnected();
            }
            
            LOGGER.Log("Started");
        }

        // <summary>
        //  Plugin destroyed
        // </summary>
        public void OnDestroy() {
            Destroy(this.connectionDaemon);
            Destroy(this.delayedActionDaemon);
            LOGGER.Log("Destroyed");
        }

        // ====================================================================================

        // <summary>
        //  Trigger an action set change
        // </summary>
        public void TriggerActionSetChange() {
            this.delayedActionDaemon.TriggerDelayedAction(this._TriggerActionSetChange, DELAY);
        }
        private void _TriggerActionSetChange() {
            this._SetActionSet(this.ComputeActionSet());
        }

        // <summary>
        //  Cancel an action set change
        // </summary>
        private void CancelActionSetChange() {
            this.delayedActionDaemon.CancelDelayedAction(this._TriggerActionSetChange);
        }

        // <summary>
        //  Change action set NOW
        // </summary>
        public void SetActionSet(KSPActionSets actionSet) {
            this.CancelActionSetChange();
            this._SetActionSet(actionSet);
        }
        private void _SetActionSet(KSPActionSets actionSet) {
            if( actionSet == this.prevActionSet ) {
                return;
            }

            this.connectionDaemon.setActionSet(actionSet);
            
            this.screenMessage.message = "Controller: " + actionSet.GetLabel() + ".";
            ScreenMessages.PostScreenMessage(this.screenMessage);
            this.prevActionSet = actionSet;
        }

        // <summary>
        //  Compute the action set to use, depending on the KSP context
        // </summary>
        private KSPActionSets ComputeActionSet() {
            if( HighLogic.LoadedSceneIsFlight ) {
                
                if( MapView.MapIsEnabled ) {
                    return KSPActionSets.Map;
                }
                
                if( FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.isEVA ) {
                    return KSPActionSets.EVA;
                }
                
                FlightUIMode mode = FlightUIModeController.Instance.Mode;
                switch( mode ) {
                
                case FlightUIMode.STAGING:
                case FlightUIMode.MANEUVER_EDIT:     // May not happen has editing maneuvre is only available in map view
                case FlightUIMode.MANEUVER_INFO:
                    return KSPActionSets.Flight;
                
                case FlightUIMode.DOCKING:
                    return KSPActionSets.Docking;
                
                case FlightUIMode.MAPMODE:          // Seems to never be called without another mode called just after (exception in tracking station maybe ?)
                    return KSPActionSets.Map;
                }
            
            } else if( HighLogic.LoadedScene == GameScenes.TRACKSTATION ) {
                return KSPActionSets.Map;
            
            } else if( HighLogic.LoadedSceneIsEditor) {
                return KSPActionSets.Editor;
            
            } else if( HighLogic.LoadedScene == GameScenes.MISSIONBUILDER ) {
                return KSPActionSets.Editor;
            }
            
            return KSPActionSets.Menu;
        }
        
        // ==============================================================================
        //              Connection/disconnection events of controller
        // ==============================================================================
        
        // <summary>
        //  New controller connected
        // </summary>
        private void OnControllerConnected() {
            // Trigger an action set change to load the right action set
            this.TriggerActionSetChange();
        }

        // <summary>
        //  Controller disconnected
        // </summary>
        private void OnControllerDisconnected() {
            // Canceling eventual action set change
            this.CancelActionSetChange();
        }

        // ========================================================================================
        //                                      KSP Events
        // ========================================================================================

        // <summary>
        //  A new scene has been loaded
        // </summary>
        protected void OnLevelWasLoadedGUIReady(GameScenes scn) {
            if( !this.connectionDaemon.ControllerConnected ) {
                return;
            }
            this.TriggerActionSetChange();
        }

        // <summary>
        //  Will be fired when pause main menu is displayed, but also when entering
        //  astronaut complex, R&D, Mission Control or administration building.
        // </summary>
        protected void OnGamePause() {
            if( !this.connectionDaemon.ControllerConnected ) {
                return;
            }
            this.SetActionSet(KSPActionSets.Menu);
        }
        
        // <summary>
        //  Will be fired when game is unpaused, but also when leaving
        //  astronaut complex, R&D, Mission Control or administration building 
        // </summary>
        protected void OnGameUnpause() {
            if( !this.connectionDaemon.ControllerConnected ) {
                return;
            }
            this.SetActionSet(this.ComputeActionSet());
        }
        
        // <summary>
        //  User toggle the flightUI buttons (staging, docking, maps or maneuvre)
        // </summary>
        protected void OnFlightUIModeChanged(FlightUIMode mode) {
            if( !this.connectionDaemon.ControllerConnected ) {
                return;
            }
            this.TriggerActionSetChange();
        }

        // <summary>
        //  Map mode entered (mainly in tracking station)
        // </summary>
        protected void OnMapEntered() {
            if( !this.connectionDaemon.ControllerConnected ) {
                return;
            }
            this.SetActionSet(KSPActionSets.Map);
        }

        // <summary>
        //  Vessel changed
        // </summary>
        protected void OnVesselChange(Vessel ves) {
            if( !this.connectionDaemon.ControllerConnected ) {
                return;
            }
            this.TriggerActionSetChange();
        }
    }
}
