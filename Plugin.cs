using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using ZeepSDK.Scripting;

namespace PhotomodeMultiview
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string pluginGuid = "com.metalted.zeepkist.photodrone";
        public const string pluginName = "PhotoDrone";
        public const string pluginVersion = "1.6";

        public static Plugin Instance;

        public ConfigEntry<KeyCode> createDroneKey;
        public ConfigEntry<bool> clearDronesOnPreset;
        public ConfigEntry<string> presets;

        public ConfigEntry<KeyCode> showPresetUIKey;
        public ConfigEntry<bool> showPresetUI;

        public ConfigEntry<KeyCode> activeKey;
        public ConfigEntry<bool> active;

        public ConfigEntry<bool> showDroneUI;
        public ConfigEntry<KeyCode> showDroneUIKey;
        public ConfigEntry<KeyCode> closeAllDrones;

        public ConfigEntry<KeyCode> toggleCursor;
        public ConfigEntry<KeyCode> luaKey;

        public bool shouldShowGUI = false;
        public bool inPhotoMode = false;
        public List<DronePresetGroup> presetGroups = new List<DronePresetGroup>();
        public List<string> groupNames = new List<string>();

        private void Awake()
        {
            Instance = this;

            Harmony harmony = new Harmony(pluginGuid);
            harmony.PatchAll();

            InitializeConfig();
            RegisterLua();
            DroneCommand.Initialize();

            // Plugin startup logic
            Logger.LogInfo($"Plugin PhotoDrone is loaded!");
        }

        private void InitializeConfig()
        {
            createDroneKey = Config.Bind("Settings", "Create Drone", KeyCode.None, "");
            clearDronesOnPreset = Config.Bind("Settings", "Clear Drones On Preset", true, "");
            presets = Config.Bind("Settings", "Presets", "", "");

            showPresetUIKey = Config.Bind("Settings", "Show Preset UI Key", KeyCode.None, "");
            showPresetUI = Config.Bind("Settings", "Show Preset UI", true, "");

            activeKey = Config.Bind("Settings", "Active Key", KeyCode.None, "");
            active = Config.Bind("Settings", "Active", true, "");

            showDroneUI = Config.Bind("Settings", "Show Drone UI", true, "");
            showDroneUIKey = Config.Bind("Settings", "Show Drone UI Key", KeyCode.None, "");

            closeAllDrones = Config.Bind("Settings", "Close All Drones", KeyCode.None, "");
            toggleCursor = Config.Bind("Settings", "Toggle Cursor", KeyCode.None, "");
            luaKey = Config.Bind("Settings", "Lua Key", KeyCode.None, "");
        }

        private void RegisterLua()
        {
            ScriptingApi.RegisterEvent<OnPhotoDroneCommand>();
            ScriptingApi.RegisterFunction<CreateDrone>();
            ScriptingApi.RegisterFunction<CloseDrone>();
            ScriptingApi.RegisterFunction<CloseAllDrones>();
            ScriptingApi.RegisterFunction<SetDroneTarget>();
            ScriptingApi.RegisterFunction<SetDroneFollowMode>();
            ScriptingApi.RegisterFunction<SetDronePosition>();
            ScriptingApi.RegisterFunction<SetDroneSize>();
            ScriptingApi.RegisterFunction<SetDroneRect>();
            ScriptingApi.RegisterFunction<SetDroneUI>();
            ScriptingApi.RegisterFunction<SetDroneLocked>();
            ScriptingApi.RegisterFunction<GetPlayerNames>();
            ScriptingApi.RegisterFunction<GetDroneNames>();
            ScriptingApi.RegisterFunction<TogglePhotomode>();
            ScriptingApi.RegisterFunction<ShowPlayer>();
            ScriptingApi.RegisterFunction<RunAnimation>();
            ScriptingApi.RegisterFunction<SetGameChat>();
            ScriptingApi.RegisterFunction<SetPhotomodeUI>();
        }

        private void Start()
        {
            presetGroups = DronePresetParser.ParseAllGroups(presets.Value);
            groupNames = presetGroups.Select(g => g.Name).ToList();
            Config.SettingChanged += Config_SettingChanged;
        }

        private void Config_SettingChanged(object sender, SettingChangedEventArgs e)
        {
            presetGroups = DronePresetParser.ParseAllGroups(presets.Value);
            groupNames = presetGroups.Select(g => g.Name).ToList();
        }

        public void Update()
        {
            if (Input.GetKeyDown(createDroneKey.Value) && active.Value && inPhotoMode)
            {
                DroneCommand.CreateDrone(System.Guid.NewGuid().ToString());
            }

            if(Input.GetKeyDown(showPresetUIKey.Value))
            {
                showPresetUI.Value = !showPresetUI.Value;
            }

            if(Input.GetKeyDown(activeKey.Value))
            {
                active.Value = !active.Value;
            }

            if(Input.GetKeyDown(showDroneUIKey.Value))
            {
                showDroneUI.Value = !showDroneUI.Value;
                DroneCommand.SetUI(showDroneUI.Value);
            }

            if(Input.GetKeyDown(closeAllDrones.Value))
            {
                DroneCommand.ShutDown();
            }

            if(Input.GetKeyDown(toggleCursor.Value) && active.Value && inPhotoMode)
            {
                PlayerManager.Instance.cursorManager.SetCursorEnabled(!Cursor.visible);
            }

            if(Input.GetKeyDown(luaKey.Value) && active.Value && inPhotoMode)
            {
                DroneCommand.OnCommand?.Invoke("luakey");
            }
        }

        public void ApplyGroup(DronePresetGroup group)
        {
            if(clearDronesOnPreset.Value)
            {
                DroneCommand.ShutDown();
            }

            foreach(DronePreset preset in group.Presets)
            {
                DroneCommand.CreateDrone(System.Guid.NewGuid().ToString(), preset);
            }
        }

        public void OnGUI()
        {
            if (!shouldShowGUI || !showPresetUI.Value || !active.Value || !inPhotoMode)
            {
                return;
            }     
            
            if(groupNames.Count == 0)
            {
                return;
            }

            try
            {
                for (int i = 0; i < groupNames.Count; i++)
                {
                    if(GUI.Button(new Rect(0 + i * 100f, Screen.height - 25f, 100f, 25f), groupNames[i]))
                    {
                        DronePresetGroup group = presetGroups.FirstOrDefault(p => p.Name == groupNames[i]);
                        if(group != null)
                        {
                            ApplyGroup(group);
                        }
                    }
                }
            }
            catch { }
        }

        public void WriteStringToFile(string content)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + @"\BepInEx\plugins\DronePresetLog.txt";

            try
            {
                if (!File.Exists(path))
                {
                    File.Create(path).Close();
                }

                File.AppendAllText(path, content + Environment.NewLine);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to write to file: " + e.Message);
            }
        }

    }
}