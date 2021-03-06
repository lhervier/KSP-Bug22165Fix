using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace com.github.lhervier.ksp 
{

    // <summary>
    //  Allows to launch an action in a set of frames. If the same action is triggered
    //  a second time, the launch of the action will be delayed again.
    // </summary>
    public class DelayedActionDaemon : MonoBehaviour 
    {
        
        // <summary>
        //  Logger
        // </summary>
        private static SteamControllerLogger LOGGER = new SteamControllerLogger("DelayedActionDaemon");

        // ===============================================

        // <summary>
        //  Frame count at which the delayed action will occur
        //  except if another operation
        //  ask for another update, which will increase this value.
        // </summary>
        private IDictionary<Action, int> actionThreshold = new Dictionary<Action, int>();

        // <summary>
        //  Co-routines used to delay the actions
        // </summary>
        private IDictionary<Action, Coroutine> coroutines = new Dictionary<Action, Coroutine>();

        // =======================================================================
        //              Unity Lifecycle
        // =======================================================================

        // <summary>
        //  Component awaked
        // </summary>
        public void Awake() 
        {
            LOGGER.Log("Awaked");
            DontDestroyOnLoad(this);
        }

        // <summary>
        //  Component destroyed
        // </summary>
        public void OnDestroy() 
        {
            LOGGER.Log("Destroyed");
        }

        // <summary>
        //  Startup of the component
        // </summary>
        public void Start() 
        {
            LOGGER.Log("Started");
        }

        // ===============================================================

        // <summary>
        //  Trigger an action in the future
        // </summary>
        public void TriggerDelayedAction(Action action, int inFrames) 
        {
            int threshold;
            if( this.actionThreshold.ContainsKey(action) ) 
            {
                threshold = Math.Max(Time.frameCount + inFrames, this.actionThreshold[action]);
            } else {
                threshold = Time.frameCount + inFrames;
            }
            this.actionThreshold[action] = threshold;
            if( !this.coroutines.ContainsKey(action) ) 
            {
                this.coroutines[action] = this.StartCoroutine(_TriggerDelayedAction(action));
            }
        }

        private IEnumerator _TriggerDelayedAction(Action action) 
        {
            while( this.actionThreshold.ContainsKey(action) && Time.frameCount < this.actionThreshold[action] ) 
            {
                yield return null;
            }
            if( this.actionThreshold.ContainsKey(action) ) 
            {
                action();
            }
            this.coroutines.Remove(action);
            this.actionThreshold.Remove(action);
        }

        // <summary>
        //  Cancel any action set change request
        // </summary>
        public void CancelDelayedAction(Action action) 
        {
            if( this.coroutines.ContainsKey(action) ) 
            {
                this.StopCoroutine(this.coroutines[action]);
            }
            this.coroutines.Remove(action);
            this.actionThreshold.Remove(action);
        }
    }

}