using UnityEngine;

namespace com.github.lhervier.ksp {

    public class SteamControllerLogger {
        private string prefix = "[SteamControllerPlugin]";

        public SteamControllerLogger() {}
        public SteamControllerLogger(string additionalPrefix) {
            this.prefix += "[" + additionalPrefix + "]"; 
        }

        public void Log(string message) {
            Debug.Log(this.prefix + " " + message);
        }
    }
}