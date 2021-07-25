At the time of writing, the introduction of Maneuvre Mode in KSP 1.6 broke the detection of the action set to activate in game. See bug [https://bugs.kerbalspaceprogram.com/issues/22165](https://bugs.kerbalspaceprogram.com/issues/22165) for more details.

Thanks to the well documented public class "SteamController", writing a plugin that will activate Action Sets is not difficult. So, I reimplemented mine... even with some "help". 

To use this plugin :

- Delete the Squad Plugin : /GameData/Squad/Plugins/KSPSteamCtrlr.dll
- Add the following plugin in /GameData/SteamControllerPlugin/SteamControllerPlugin.dll

Et voilà !