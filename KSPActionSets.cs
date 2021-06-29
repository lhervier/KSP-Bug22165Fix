namespace com.github.lhervier.ksp {

    public enum KSPActionSets {
        Menu,
        Flight,
        Docking,
        Editor,
        EVA,
        Map
    }

    public static class KSPActionSetsUtils {
        public static string GetLabel(this KSPActionSets kac) {
            return kac.ToString() + " Controls";
        }

        public static string GetId(this KSPActionSets kac) {
            return kac.ToString() + "Controls";
        }
    }
}