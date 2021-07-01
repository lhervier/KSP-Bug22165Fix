using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using KSP.UI.Screens.Flight;
using com.github.lhervier.ksp;

namespace com.github.lhervier.ksp {
    
    [KSPAddon(KSPAddon.Startup.PSystemSpawn, true)]
    public class SteamControllerPlugin : MonoBehaviour {
        
        // <summary>
        //  Logger
        // </summary>
        private static SteamControllerLogger LOGGER = new SteamControllerLogger();
        
        // <summary>
        //  Message indicating when on Steam Controller mode change
        // </summary>    
        private ScreenMessage screenMessage;

        // <summary>
        //  Previous action set (so we don't display the message when the value has not changed)
        // </summary>
        private KSPActionSets prevActionSet;

        // <summary>
        //  Daemon to communicate with the steam controller
        // </summary>
        private SteamControllerDaemon daemon;

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
        //  Plugin destroyed
        // </summary>
        public void OnDestroy() {
            LOGGER.Log("Destroyed");
        }

        // <summary>
        //  Start of the plugin
        // </summary>
        protected void Start() {
            if( !SteamManager.Initialized ) {
                LOGGER.Log("Steam not detected. Unable to start the plugin.");
                return;
            }
            LOGGER.Log("Started");
            
            this.screenMessage = new ScreenMessage(
                string.Empty, 
                10f, 
                ScreenMessageStyle.UPPER_RIGHT
            );
            LOGGER.Log("Status message ready");

            GameEvents.onLevelWasLoadedGUIReady.Add(OnLevelWasLoadedGUIReady);
            GameEvents.onGamePause.Add(OnGamePause);
            GameEvents.onGameUnpause.Add(OnGameUnpause);
            GameEvents.OnFlightUIModeChanged.Add(OnFlightUIModeChanged);
            GameEvents.OnMapEntered.Add(OnMapEntered);
            GameEvents.onVesselChange.Add(OnVesselChange);
            LOGGER.Log("KSP events callbacks created");

            this.daemon = SteamControllerDaemon.INSTANCE;
            this.daemon.OnActionSetChanged = this.OnActionSetChanged;
            this.daemon.ComputeActionSet = this.ComputeActionSet;
            LOGGER.Log("Daemon created");
        }

        // =================================================================

        // <summary>
        //  Method called by the controller daemon when a change action set action is triggered
        // </summary>
        public KSPActionSets ComputeActionSet() {
            LOGGER.Log("Detecting Steam Controller Mode");
            if( HighLogic.LoadedSceneIsFlight ) {
                LOGGER.Log("- Loaded Scene is Flight");
                
                if( MapView.MapIsEnabled ) {
                    LOGGER.Log("=> MapView is enabled");
                    return KSPActionSets.Map;
                }
                
                if( FlightGlobals.ActiveVessel != null && FlightGlobals.ActiveVessel.isEVA ) {
                    LOGGER.Log("=> EVA is in progress");
                    return KSPActionSets.EVA;
                }
                
                FlightUIMode mode = FlightUIModeController.Instance.Mode;
                LOGGER.Log("=> Flight UI is in " + mode.ToString() + " mode");
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
                LOGGER.Log("- Loaded scene is Tracking station");
                return KSPActionSets.Map;
            
            } else if( HighLogic.LoadedSceneIsEditor) {
                LOGGER.Log("- Loaded scene is Editor");
                return KSPActionSets.Editor;
            
            } else if( HighLogic.LoadedScene == GameScenes.MISSIONBUILDER ) {
                LOGGER.Log("- Loaded scene is Mission Builder");
                return KSPActionSets.Editor;
            }
            
            return KSPActionSets.Menu;
        }

        protected void OnActionSetChanged(KSPActionSets actionSet) {
            if( actionSet == this.prevActionSet ) {
                return;
            }

            this.screenMessage.message = "Action Set: " + actionSet.GetLabel() + ".";
            ScreenMessages.PostScreenMessage(this.screenMessage);
            this.prevActionSet = actionSet;
        }

        // ============================
        //          KSP Events
        // ============================

        // <summary>
        //  A new scene has been loaded
        // </summary>
        protected void OnLevelWasLoadedGUIReady(GameScenes scn) {
            LOGGER.Log("Level was loaded on scene : " + scn.ToString());
            this.daemon.TriggerActionSetChange();
        }

        // <summary>
        //  Will be fired when pause main menu is displayed, but also when entering
        //  astronaut complex, R&D, Mission Control or administration building.
        // </summary>
        protected void OnGamePause() {
            LOGGER.Log("Game has been paused");
            this.daemon.SetActionSet(KSPActionSets.Menu);
        }
        
        // <summary>
        //  Will be fired when game is unpaused, but also when leaving
        //  astronaut complex, R&D, Mission Control or administration building 
        // </summary>
        protected void OnGameUnpause() {
            LOGGER.Log("Game has been unpaused");
            this.daemon.SetActionSet(this.ComputeActionSet());
        }
        
        // <summary>
        //  User toggle the flightUI buttons (staging, docking, maps or maneuvre)
        // </summary>
        protected void OnFlightUIModeChanged(FlightUIMode mode) {
            LOGGER.Log("Flight UI mode changed to " + mode.ToString());
            this.daemon.TriggerActionSetChange();
        }

        // <summary>
        //  Map mode entered (mainly in tracking station)
        // </summary>
        protected void OnMapEntered() {
            LOGGER.Log("Entered Map view");
            this.daemon.SetActionSet(KSPActionSets.Map);
        }

        // <summary>
        //  Vessel changed
        // </summary>
        protected void OnVesselChange(Vessel ves) {
            LOGGER.Log("Vessel changed");
            this.daemon.TriggerActionSetChange();
        }
    }
}
