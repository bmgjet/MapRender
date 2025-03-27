/*▄▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
 ▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
 ▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
 ▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
 ░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
 ░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
 ▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
  ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
  ░             ░         ░  ░   ░      ░  ░*/
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace MapRender
{
    public class HarmonyMod : IHarmonyModHooks
    {
        public static HarmonyMod Main;
        public MapRender Instance;
        public Toggle toggle;
        public Config config;
        public string Status;
        public string ConfigFile;
        public string Logo =
@"    __  ___            ____                 __         
   /  |/  /___ _____  / __ \___  ____  ____/ /__  _____
  / /|_/ / __ `/ __ \/ /_/ / _ \/ __ \/ __  / _ \/ ___/
 / /  / / /_/ / /_/ / _, _/  __/ / / / /_/ /  __/ /    
/_/  /_/\__,_/ .___/_/ |_|\___/_/ /_/\__,_/\___/_/     
            /_/
V1.2.0 by bmgjet";

        public string HelpInfo = @"MapRender Help:
Console Commands:
  maprender.ui     - Opens the MapRender user interface when a new or saved map is open.
  maprender.help   - Prints this help information.
  maprender.reload - Reloads the configuration file used for image generation.
  maprender.reset  - Resets the configuration file to its default settings.

Hotkeys:
  Ctrl+M - Opens the MapRender user interface when a new or saved map is open.
  Ctrl+L - Resets the image and controls back to their defaults.
  ESC    - Close MapRender user interface if its open.";

        //OnLoaded called by harmony when plugin loads
        public void OnLoaded(OnHarmonyModLoadedArgs args)
        {
            Main = this; //Create static reference
            LoadConfig(); //Load/Create Config
            toggle = MenuManager.Instance.CreateWindowToggle("HarmonyMods//MapRender.png");
            toggle.onValueChanged.AddListener((value) => CreateWindow(value));
        }

        //OnUnloaded called by harmony when plugin unloads
        public void OnUnloaded(OnHarmonyModUnloadedArgs args)
        {
            if (Main.Instance != null) { Main.Instance.CloseWindow(); }
            Main = null; //Clear reference
        }

        //Hooks Console Input
        [HarmonyPatch(typeof(ConsoleWindow), "OnSubmit")]
        public class ConsoleWindow_OnSubmit
        {
            static bool Prefix(ConsoleWindow __instance)
            {
                //Switch case to catch keywords
                switch (__instance.consoleInput.text.ToLower())
                {
                    case "maprender.ui":
                        {
                            try
                            {
                                __instance.Post("Creating MapRender");
                                CreateWindow(true);
                            }
                            catch (Exception e) { __instance.Post(e.ToString()); }
                            __instance.consoleInput.text = string.Empty; //Blank console text input
                            return false;
                        }
                    case "maprender.help":
                        {
                            __instance.PostMultiLine(HarmonyMod.Main.HelpInfo);
                            __instance.consoleInput.text = string.Empty;
                            return false;
                        }
                    case "maprender.reload":
                        {
                            __instance.Post("Reloading Config file!");
                            HarmonyMod.Main.LoadConfig();
                            __instance.consoleInput.text = string.Empty;
                            return false;
                        }
                    case "maprender.reset":
                        {
                            __instance.Post("Resettings Config file!");
                            HarmonyMod.Main.CreateConfig();
                            File.WriteAllText(HarmonyMod.Main.ConfigFile, JsonUtility.ToJson(HarmonyMod.Main.config, true)); //Save config
                            __instance.consoleInput.text = string.Empty;
                            return false;
                        }
                }
                return true;
            }
        }

        //Hook Keyboard Input
        [HarmonyPatch(typeof(CameraManager), "Update")]
        public class CameraManager_Update
        {
            static bool Prefix(CameraManager __instance)
            {
                try
                {
                    if (__instance.cam == null) { return false; } //Do nothing there is no camera (Don't run original code)
                    if (Main.Instance != null) //MapRender UI Open
                    {
                        if (Keyboard.current.escapeKey.wasPressedThisFrame) //Catch esc key as hotkey from maprender
                        {
                            Main.Instance.CloseWindow();
                            return false; //Block Original Code
                        }
                        if (Keyboard.current.ctrlKey.isPressed && Keyboard.current.lKey.wasPressedThisFrame) //Catch CTRL+L, Reset Layout
                        {
                            if (Main.Instance != null) { Main.Instance.ResetLayout(); }
                        }
                        return true; //Run Normal Code
                    }
                    if (!LoadScreen.Instance.isEnabled && Keyboard.current.ctrlKey.isPressed && Keyboard.current.mKey.wasPressedThisFrame) //CTRL+M keys While Not In Loading Screen
                    {
                        CreateWindow(true);
                        return false; //Block Original Code
                    }
                }
                catch { }
                return true;
            }
        }

        //Hook ConsoleWindow Load
        [HarmonyPatch(typeof(ConsoleWindow), "Startup")]
        public class ConsoleWindow_Startup
        {
            static void Postfix(ConsoleWindow __instance)
            {
                try
                {
                    __instance.PostMultiLine(Main.Logo);//Post logo to console window
                    if (!string.IsNullOrEmpty(Main.Status))
                    {
                        __instance.Post(Main.Status);
                        __instance.consoleInput.text = "";
                        __instance.consoleInput.ActivateInputField();
                        Main.Status = null;
                    }
                }
                catch { }
            }
        }

        #region Config
        //Config File Layout And Defaults
        public class Config
        {
            public Vector4 StartColor = new Vector4(0.28627452f, 0.27058825f, 0.24705884f, 1f);

            public Vector4 WaterColor = new Vector4(0.16941601f, 0.31755757f, 0.36200002f, 1f);

            public Vector4 GravelColor = new Vector4(0.25f, 0.24342105f, 0.22039475f, 1f);

            public Vector4 DirtColor = new Vector4(0.6f, 0.47959462f, 0.33f, 1f);

            public Vector4 SandColor = new Vector4(0.7f, 0.65968585f, 0.5277487f, 1f);

            public Vector4 GrassColor = new Vector4(0.35486364f, 0.37f, 0.2035f, 1f);

            public Vector4 ForestColor = new Vector4(0.24843751f, 0.3f, 0.0703125f, 1f);

            public Vector4 RockColor = new Vector4(0.4f, 0.39379844f, 0.37519377f, 1f);

            public Vector4 SnowColor = new Vector4(0.86274517f, 0.9294118f, 0.94117653f, 1f);

            public Vector4 PebbleColor = new Vector4(0.13725491f, 0.2784314f, 0.2761563f, 1f);

            public Vector4 OffShoreColor = new Vector4(0.04090196f, 0.22060032f, 0.27450982f, 1f);

            public float minZoom = 0.05f;

            public float maxZoom = 6f;

            public float zoomSpeed = 0.01f;

            public int GridLabelFontSize = 4;

            public float GridLineWidth = 0.2f;

            public int CargoPathNodeSize = 15;

            public Vector2 ButtonScale = Vector2.zero;

            public Vector2 ButtonPosition = Vector3.zero;

            public string[] MonumentMarkerNames = new string[0];

            public class Lookup
            {
                public uint PrefabID;
                public string Text;
                public int FontSize;
            }
        }

        public void LoadConfig()
        {
            string ConfigPath = Path.Combine(AccessTools.Field(typeof(HarmonyLoader), "modPath").GetValue(null).ToString(), "HarmonyConfig");
            if (!Directory.Exists(ConfigPath)) { Directory.CreateDirectory(ConfigPath); }
            ConfigFile = Path.Combine(ConfigPath, "MapRender.json");
            Status = "[MapRender] ";
            if (File.Exists(ConfigFile))
            {
                try
                {
                    config = JsonUtility.FromJson<Config>(File.ReadAllText(ConfigFile));
                    Status += "Config Loaded";
                }
                catch
                {
                    CreateConfig();
                    Status += "Error Loading Config";
                }
            }
            else
            {
                CreateConfig();
                File.WriteAllText(ConfigFile, JsonUtility.ToJson(config, true)); //Save config
                Status += "Created New Config";
            }
            //Output to console window if its loaded.
            if (ConsoleWindow.Instance != null)
            {
                ConsoleWindow.Instance.PostMultiLine(Logo);
                ConsoleWindow.Instance.Post(Status);
                ConsoleWindow.Instance.consoleInput.text = "";
                ConsoleWindow.Instance.consoleInput.ActivateInputField();
                Status = null;
            }
        }

        private void CreateConfig()
        {
            // Create Default Config File
            config = new Config();

            // Define monument marker names
            var monumentMarkers = new (uint Id, string Name)[]
            {
            (1724395471, "Monument Marker"),
            (1464688103, "Oasis C"),
            (287240458, "Oasis B"),
            (1241151746, "Oasis A"),
            (2909070250, "Lake C"),
            (3563180799, "Lake B"),
            (675967600, "Lake A"),
            (1879405026, "Outpost"),
            (2074025910, "Bandit Town"),
            (610633799, "Lighthouse"),
            (3422899798, "Water Treatment"),
            (4273840478, "Trainyard"),
            (158704173, "Powerplant"),
            (72186600, "Military Tunnel"),
            (1309495556, "Excavator"),
            (2720666271, "Airfield"),
            (2958463062, "Harbor"),
            (3348191966, "Harbor"),
            (779654519, "Ferry Terminal"),
            (179019713, "Fishing Village C"),
            (3605784707, "Fishing Village B"),
            (1873767918, "Fishing Village A"),
            (3968358155, "Arctic Research"),
            (2200048243, "Junkyard"),
            (454470717, "Nuclear Missile Silo"),
            (3257319375, "Sewer Branch"),
            (3965959566, "Desert Military A"),
            (2778438472, "Desert Military B"),
            (2940558852, "Desert Military C"),
            (63296637, "Desert Military D"),
            (2839094107, "Large Oilrig"),
            (2038914978, "Small Oilrig"),
            (973576591, "Gas Station"),
            (400079324, "Radtown"),
            (4255620866, "Supermarket"),
            (891304283, "Warehouse"),
            (2148214341, "Mining Quarry A"),
            (2046822015, "Mining Quarry B"),
            (3899974108, "Mining Quarry C"),
            (182769745, "Satellite Dish"),
            (3903103539, "Dome"),
            (2250041975, "Stables A"),
            (2973946649, "Stables B"),
            (873508118, "Swamp A"),
            (3530204693, "Swamp B"),
            (2563750503, "Swamp C"),
            (3653679896, "Water Well A"),
            (833926741, "Water Well B"),
            (1213154741, "Water Well C"),
            (976058547, "Water Well D"),
            (2765648982, "Water Well E"),
            (3873722037, "Bunker A"),
            (2362750489, "Bunker B"),
            (2307539102, "Bunker C"),
            (2393056659, "Bunker D"),
            (2859012016, "Underwater Lab A"),
            (693730846, "Underwater Lab B"),
            (1462376378, "Underwater Lab C"),
            (2357885450, "Underwater Lab D"),
            (1073114437, "Launch Site"),
            };
            // Convert to string array (Unity's JSON serialization workaround)
            config.MonumentMarkerNames = monumentMarkers.Select(m => $"{m.Id};{m.Name.ToUpper()};10").ToArray();
        }
        #endregion

        public static void CreateWindow(bool togglevalue)
        {
            if(Main.Instance != null || togglevalue == false) { 
                Main.Instance.CloseWindow();
                return;
            }
            Main.Instance = new MapRender().Init();
            if (Main.Instance != null) { Main.Instance.Render(); }
            else { if (ConsoleWindow.Instance != null) { ConsoleWindow.Instance.Post("Failed To Create Map, Is One Loaded?"); } }
        }
    }

    public class MapRender
    {
        #region Vars
        public short[] Height;
        public byte[] Splat;
        public int[] Topology;
        public Color[] Colours;
        public Color[] NewColours;
        private Canvas canvas;
        private GameObject controlPanel;
        private RawImage rawImage;
        private GameObject inputPopup;
        private InputField inputField;
        private Font font;
        public List<Vector3> CargoPath;
        private Array2D<Color> output;
        public LookupDictionary lookupDictionary = new LookupDictionary(HarmonyMod.Main.config.MonumentMarkerNames);
        #endregion

        #region Classes And Structures
        public class SerializedPathList { public List<WorldSerialization.VectorData> vectorData = new List<WorldSerialization.VectorData>(); }
        public class LookupDictionary
        {
            private readonly Dictionary<uint, HarmonyMod.Config.Lookup> _lookupTable = new Dictionary<uint, HarmonyMod.Config.Lookup>();

            public LookupDictionary(string[] strings)
            {
                foreach (var item in strings) //Convert from string array split by ; back to LookUp Items
                {
                    var items = item.Split(';');
                    if (items.Length < 3 || !uint.TryParse(items[0], out uint prefabID) || !int.TryParse(items[2], out int fontSize)) { continue; }// Skip bad entries
                    Add(new HarmonyMod.Config.Lookup { PrefabID = prefabID, Text = items[1], FontSize = fontSize });
                }
            }

            public void Add(HarmonyMod.Config.Lookup item)
            {
                if (item == null) throw new ArgumentNullException(nameof(item));
                _lookupTable[item.PrefabID] = item;
            }

            public bool TryGetValue(uint prefabID, out HarmonyMod.Config.Lookup lookup) => _lookupTable.TryGetValue(prefabID, out lookup);
        }

        struct Array2D<T>
        {
            private T[] _items;
            private int _width;
            private int _height;

            public Array2D(T[] items, int width, int height)
            {
                _items = items;
                _width = width;
                _height = height;
            }

            public ref T this[int x, int y]
            {
                get
                {
                    int num = Mathf.Clamp(x, 0, _width - 1);
                    int num2 = Mathf.Clamp(y, 0, _height - 1);
                    return ref _items[num2 * _width + num];
                }
            }
        }
        #endregion

        public MapRender Init() //Read Data Needed for Map Image Generation
        {
            //Save Current Data
            TerrainManager.SaveLayer();
            //Height Map
            byte[] array = RustMapEditor.Maths.Array.FloatArrayToByteArray(TerrainManager.Land.terrainData.GetHeights(0, 0, TerrainManager.HeightMapRes, TerrainManager.HeightMapRes));
            Height = new short[array.Length / 2];
            Buffer.BlockCopy(array, 0, Height, 0, array.Length);
            //Splat Map
            byte[] splatBytes = new byte[TerrainManager.SplatMapRes * TerrainManager.SplatMapRes * 8];
            TerrainMap<byte> splatMap = new TerrainMap<byte>(splatBytes, 8);
            Parallel.For(0, 8, i => { for (int l = 0; l < TerrainManager.SplatMapRes; l++) { for (int m = 0; m < TerrainManager.SplatMapRes; m++) { splatMap[i, l, m] = BitUtility.Float2Byte(TerrainManager.Ground[l, m, i]); } } });
            Splat = splatMap.ToByteArray();
            //Topology Map
            try //Try Catch Since Will Fail On Empty Editor (No New Map Or Loaded Map)
            {
                TopologyData.SaveTopologyLayers();
                array = TopologyData.GetTerrainMap()?.ToByteArray();
            }
            catch { }
            if (array == null || array.Length == 0) { array = new TerrainMap<int>(new byte[(int)Mathf.Pow((float)TerrainManager.SplatMapRes, 2f) * 4], 1)?.ToByteArray(); } //No Map Loaded Make Blank
            Topology = new int[array.Length];
            Buffer.BlockCopy(array, 0, Topology, 0, Topology.Length);
            //Load Font
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return this;
        }

        #region Functions
        short GetHeight(int x, int z) { return Height[z * TerrainManager.HeightMapRes + x]; } //Get Height From X,Z as short

        int GetTopology(int x, int z) { return (Topology[z * TerrainManager.SplatMapRes + x]); } //Get Topology from X,Z as Int (bitmask)

        float GetSplat(int x, int z, int mask) //Get Splat with Mask Filter
        {
            if (mask > 0 && (mask & (mask - 1)) == 0) { return BitUtility.Byte2Float((int)Splat[(TerrainSplat.TypeToIndex(mask) * TerrainManager.SplatMapRes + z) * TerrainManager.SplatMapRes + x]); }
            int num = 0;
            for (int i = 0; i < 8; i++) { if ((TerrainSplat.IndexToType(i) & mask) != 0) { num += (int)Splat[(i * TerrainManager.SplatMapRes + z) * TerrainManager.SplatMapRes + x]; } }
            return Mathf.Clamp01(BitUtility.Byte2Float(num));
        }

        double ScaleValue(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            //Scale Values between two ranges
            if (fromMax == fromMin) { return toMin; }
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }

        string ConvertToExcelColumn(int colIndex)
        {
            //Grid Labels
            string columnName = "";
            while (colIndex >= 0)
            {
                columnName = (char)('A' + (colIndex % 26)) + columnName;
                colIndex = (colIndex / 26) - 1;
            }
            return columnName;
        }

        T DeSeriliseMapData<T>(byte[] bytes)
        {
            //Deserialize XML file embedded in MapData
            T result;
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(bytes))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                    T t = (T)((object)xmlSerializer.Deserialize(memoryStream));
                    result = t;
                }
            }
            catch { result = default(T); }
            return result;
        }

        UnityEngine.Color ConvertToUnityColor(System.Drawing.Color sysColor)
        {
            return new UnityEngine.Color(
                sysColor.R / 255f,  // Convert Red (0-255) to 0-1 range
                sysColor.G / 255f,  // Convert Green (0-255) to 0-1 range
                sysColor.B / 255f,  // Convert Blue (0-255) to 0-1 range
                sysColor.A / 255f   // Convert Alpha (0-255) to 0-1 range
            );
        }

        List<Vector3> RemoveNearbyPositions(List<Vector3> positions, float threshold)
        {
            List<Vector3> filtered = new List<Vector3>();

            foreach (var pos in positions)
            {
                // Check if any existing position in 'filtered' is too close
                if (!filtered.Any(filteredPos => Vector3.Distance(pos, filteredPos) < threshold)) { filtered.Add(pos); }
            }
            return filtered;
        }
        #endregion

        #region Methods
        public void Render()
        {
            if (Height == null || Topology == null || Splat == null) { return; }
            Colours = new Color[TerrainManager.SplatMapRes * TerrainManager.SplatMapRes];
            output = new Array2D<Color>(Colours, TerrainManager.SplatMapRes, TerrainManager.SplatMapRes);
            //Scan each pixel of Splat Map
            Parallel.For(0, TerrainManager.SplatMapRes, x =>
            {
                float[] splatValues = new float[8];
                int newx = (int)ScaleValue((double)x, 0.0, (double)TerrainManager.SplatMapRes, 0.0, (double)TerrainManager.HeightMapRes);
                for (int z = 0; z < TerrainManager.SplatMapRes; z++)
                {
                    Vector4 vector = HarmonyMod.Main.config.StartColor;
                    int newz = (int)ScaleValue((double)z, 0.0, (double)TerrainManager.SplatMapRes, 0.0, (double)TerrainManager.HeightMapRes);
                    float height = BitUtility.Short2Float(GetHeight(newx, newz));
                    TerrainTopology.Enum topology = (TerrainTopology.Enum)GetTopology(x, z);
                    if (height < 0.5f && topology.HasFlag(TerrainTopology.Enum.Ocean)) { vector = Vector4.Lerp(vector, HarmonyMod.Main.config.WaterColor, height); vector = Vector4.Lerp(vector, HarmonyMod.Main.config.OffShoreColor, height); } //Ocean Topology (Paint Ocean)
                    else
                    {
                        //Blend Splat Colours
                        splatValues[0] = GetSplat(x, z, 128);
                        splatValues[1] = GetSplat(x, z, 64);
                        splatValues[2] = GetSplat(x, z, 8);
                        splatValues[3] = GetSplat(x, z, 1);
                        splatValues[4] = GetSplat(x, z, 16);
                        splatValues[5] = GetSplat(x, z, 32);
                        splatValues[6] = GetSplat(x, z, 4);
                        splatValues[7] = GetSplat(x, z, 2);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.GravelColor, splatValues[0] * HarmonyMod.Main.config.GravelColor.w);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.PebbleColor, splatValues[1] * HarmonyMod.Main.config.PebbleColor.w);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.RockColor, splatValues[2] * HarmonyMod.Main.config.RockColor.w);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.DirtColor, splatValues[3] * HarmonyMod.Main.config.DirtColor.w);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.GrassColor, splatValues[4] * HarmonyMod.Main.config.GrassColor.w);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.ForestColor, splatValues[5] * HarmonyMod.Main.config.ForestColor.w);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.SandColor, splatValues[6] * HarmonyMod.Main.config.SandColor.w);
                        vector = Vector4.Lerp(vector, HarmonyMod.Main.config.SnowColor, splatValues[7] * HarmonyMod.Main.config.SnowColor.w);
                        if (topology.HasFlag(TerrainTopology.Enum.Lake) || topology.HasFlag(TerrainTopology.Enum.River)) { vector = Vector4.Lerp(vector, HarmonyMod.Main.config.WaterColor, 0.9f); } //Lake Or River
                    }
                    vector *= 1.05f;
                    output[x, z] = new Color(vector.x, vector.y, vector.z);
                }
            });
            //Free memory no longer used
            Height = null;
            Splat = null;
            Topology = null;
            //Create UI in RustMapper
            CreateCanvas();
            CreateImagePanel();
            CreateControlPanel();
        }

        void SaveImageToPNG()
        {
            //Validate File Name
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) { return; }
            string filePath = inputField.text;
            if (!filePath.EndsWith(".png")) { filePath += ".png"; }
            try
            {
                //Save PNG from Texture2D
                Texture2D texture = rawImage.texture as Texture2D;
                if (texture == null) { return; }
                System.IO.File.WriteAllBytes(filePath, texture.EncodeToPNG());
                GameObject.Destroy(inputPopup);
                ShowMessageBox("Saved Successfully! \n" + filePath, 2f);
            }
            catch { ShowMessageBox("Error Saving Image!", 2f); }
        }

        void Scale(bool save)
        {
            if (controlPanel != null)
            {
                RectTransform rect = controlPanel.GetComponent<RectTransform>();
                if (rect != null)
                {
                    if (save)
                    {
                        // Save scale and position
                        HarmonyMod.Main.config.ButtonScale = rect.sizeDelta;
                        HarmonyMod.Main.config.ButtonPosition = rect.anchoredPosition;

                        File.WriteAllText(HarmonyMod.Main.ConfigFile, JsonUtility.ToJson(HarmonyMod.Main.config, true)); // Save config file
                    }
                    else
                    {
                        //Restore scale and position
                        if (HarmonyMod.Main.config.ButtonScale != null && HarmonyMod.Main.config.ButtonScale != Vector2.zero)
                        {
                            rect.sizeDelta = HarmonyMod.Main.config.ButtonScale;
                        }
                        if (HarmonyMod.Main.config.ButtonPosition != null && HarmonyMod.Main.config.ButtonPosition != Vector2.zero)
                        {
                            rect.anchoredPosition = HarmonyMod.Main.config.ButtonPosition;
                        }
                    }
                }
            }
        }

        public void ResetLayout()
        {
            //Reset to defaults when hot key used
            if (controlPanel != null)
            {
                RectTransform rect = controlPanel.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.sizeDelta = new Vector2(90, 90);
                    rect.anchoredPosition = new Vector2(400, 200);
                    HarmonyMod.Main.config.ButtonScale = rect.sizeDelta;
                    HarmonyMod.Main.config.ButtonPosition = rect.anchoredPosition;
                    File.WriteAllText(HarmonyMod.Main.ConfigFile, JsonUtility.ToJson(HarmonyMod.Main.config, true)); // Save config file
                }
            }
            if (rawImage != null)
            {
                ImageZoomAndDrag zoom = rawImage.GetComponent<ImageZoomAndDrag>();
                if (zoom != null) { zoom.ResetLayout(); }
            }
        }

        void DrawFont(List<Dictionary<string, Vector3>> Labels, System.Drawing.Color fc, System.Drawing.FontStyle style = System.Drawing.FontStyle.Bold)
        {
            //Draw text on a transparent bitmap
            if (Labels.Count == 0) { return; }
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(TerrainManager.SplatMapRes, TerrainManager.SplatMapRes))
            {
                using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                    using (System.Drawing.Brush brush = new System.Drawing.SolidBrush(fc))
                    {
                        foreach (var l in Labels)
                        {
                            var label = l.ElementAt(0);
                            using (System.Drawing.Font font = new System.Drawing.Font("Arial", label.Value.z, style))
                            {
                                g.TranslateTransform(label.Value.x, label.Value.y);
                                g.RotateTransform(90);  // Correct 90-degree rotation
                                g.DrawString(label.Key, font, brush, label.Value.z * -1, (label.Value.z * 3) * -1); //Correct offset from rotating
                                g.ResetTransform();
                            }
                        }
                    }
                }
                //Merge bitmap and Color array of texture
                FastDraw(bmp);
            }
        }

        void FastDraw(System.Drawing.Bitmap bmp)
        {
            //Lock bits to improve performance 10X
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(new System.Drawing.Rectangle(0, 0, TerrainManager.SplatMapRes, TerrainManager.SplatMapRes), System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            int stride = bmpData.Stride;
            IntPtr scan0 = bmpData.Scan0;
            int bytesPerPixel = 4;
            unsafe
            {
                byte* ptr = (byte*)scan0;
                for (int xp = 0; xp < TerrainManager.SplatMapRes; xp++)
                {
                    for (int zp = 0; zp < TerrainManager.SplatMapRes; zp++)
                    {
                        int index = (zp * stride) + (xp * bytesPerPixel);
                        byte b = ptr[index];
                        byte g = ptr[index + 1];
                        byte r = ptr[index + 2];
                        byte a = ptr[index + 3];
                        //Ignore Transparent pixels
                        if (a == 255) { NewColours[xp * TerrainManager.SplatMapRes + zp] = ConvertToUnityColor(System.Drawing.Color.FromArgb(a, r, g, b)); }
                    }
                }
            }
            bmp.UnlockBits(bmpData);
        }

        void DrawImage()
        {
            //Updates the UI image
            Texture2D texture = new Texture2D(TerrainManager.SplatMapRes, TerrainManager.SplatMapRes);
            texture.SetPixels(NewColours);
            texture.Apply();
            rawImage.texture = texture;
        }
        #endregion

        #region Buttons
        void ButtonClicked(string label)
        {
            //Process different buttons clicks
            switch (label)
            {
                case "Refresh":
                    DrawClear();
                    break;
                case "Show Grid":
                    DrawGrid();
                    break;
                case "Show Markers":
                    DrawMarkers();
                    break;
                case "Show CargoPath":
                    DrawCargoPath();
                    break;
                case "Save":
                    OpenSavePathPopup();
                    break;
                case "Close":
                    CloseWindow();
                    break;
            }
        }

        public void CloseWindow()
        {
            Scale(true); //Save Scale/position
            if (canvas != null) { GameObject.Destroy(canvas.gameObject); }//Destroy Map Render Window
            LoadScreen.Instance.isEnabled = false; //Allow Key/Mouse Input On RustMapper
            Compass.Instance.transform.position -= (Compass.Instance.transform.up * 500); //Restore compass position
            HarmonyMod.Main.Instance = null;
            if(HarmonyMod.Main.toggle != null)
            {
                HarmonyMod.Main.toggle.isOn = false;
            }
        }

        void CloseSavePopup() { if (inputPopup != null) { GameObject.Destroy(inputPopup); } } //Closes Save popup window

        void DrawClear()
        {
            //Reset Map Render Back To Terrain Only
            NewColours = new Color[Colours.Length];
            Colours.CopyTo(NewColours, 0);
            DrawImage();
        }

        void DrawGrid()
        {
            //Create a Grid On A Transparent Bitmap
            int grid = (int)Math.Floor(TerrainManager.Land.terrainData.size.x / 146.28672f); //Calculate Grid On Map Size
            int gridSize = (int)Math.Ceiling((float)TerrainManager.SplatMapRes / grid); //Calculate Size for Image Size
            using (System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(TerrainManager.SplatMapRes, TerrainManager.SplatMapRes))
            using (System.Drawing.Graphics gph = System.Drawing.Graphics.FromImage(bmp))
            {
                gph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                gph.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                using (System.Drawing.Pen pen = new System.Drawing.Pen(System.Drawing.Color.Black, HarmonyMod.Main.config.GridLineWidth))
                using (System.Drawing.Font font = new System.Drawing.Font("Arial", HarmonyMod.Main.config.GridLabelFontSize, System.Drawing.FontStyle.Regular))
                using (System.Drawing.Brush textBrush = new System.Drawing.SolidBrush(System.Drawing.Color.Black))
                {
                    int columns = (int)Math.Ceiling((float)bmp.Width / gridSize);
                    int rows = (int)Math.Ceiling((float)bmp.Height / gridSize);
                    for (int col = 0; col < columns; col++)
                    {
                        for (int row = 0; row < rows; row++)
                        {
                            int x = col * gridSize;
                            int y = row * gridSize;
                            int cellWidth = (x + gridSize > bmp.Width) ? bmp.Width - x : gridSize;
                            int cellHeight = (y + gridSize > bmp.Height) ? bmp.Height - y : gridSize;
                            gph.DrawRectangle(pen, x, y, cellWidth, cellHeight);
                            string columnLabel = ConvertToExcelColumn(col);
                            string gridLabel = $"{columnLabel}{row}";
                            gph.DrawString(gridLabel, font, textBrush, x + 5, y + 5);
                        }
                    }
                }
                bmp.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone); //Correct 90 degree Rotation
                //Merge bitmap to Texture Color
                FastDraw(bmp);
            }
            //Update Image
            DrawImage();
        }

        void DrawMarkers()
        {
            //Draw Map Markers From Config List
            var size = TerrainManager.Land.terrainData.size.x / 2;
            List<Dictionary<string, Vector3>> Labels = new List<Dictionary<string, Vector3>>();
            foreach (var pd in PrefabManager.CurrentMapPrefabs)
            {
                if (!lookupDictionary.TryGetValue(pd.prefabData.id, out var lookup)) { continue; }  // Try to get Lookup object
                int Xpos = (int)ScaleValue(pd.prefabData.position.z + size, 0, TerrainManager.Land.terrainData.size.x, 0, TerrainManager.SplatMapRes);
                int Ypos = (int)ScaleValue(pd.prefabData.position.x + size, 0, TerrainManager.Land.terrainData.size.x, 0, TerrainManager.SplatMapRes);
                string markerName = lookup.Text;
                if (markerName == "MONUMENT MARKER") { markerName = pd.prefabData.category; }
                int offset = (markerName.Length * lookup.FontSize) / 2;
                Labels.Add(new Dictionary<string, Vector3> { { markerName, new Vector3(Xpos, Ypos - offset, lookup.FontSize) } });
            }
            DrawFont(Labels, System.Drawing.Color.Black, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic);
            DrawImage();
        }

        void DrawCargoPath()
        {
            //Draw the embeded cargo path on maprender
            if (CargoPath == null)
            {
                WorldSerialization.MapData mapData = ModManager.moddingData.Find((WorldSerialization.MapData md) => md.name == "oceanpathpoints"); //Read rustedit data
                if (mapData != null && mapData.data.Length > 0)
                {
                    var serializedPathList = DeSeriliseMapData<SerializedPathList>(mapData.data); //Convert XML to Vector3 List
                    if (serializedPathList != null)
                    {
                        CargoPath = new List<Vector3>();
                        foreach (WorldSerialization.VectorData vd in serializedPathList.vectorData) { CargoPath.Add(new Vector3(vd.x, vd.y, vd.z)); }
                        CargoPath = RemoveNearbyPositions(CargoPath, 60); //Remove excessive nodes from list 
                        ShowMessageBox("CargoPath Nodes: " + CargoPath.Count, 4);
                    }
                }
            }
            if (CargoPath != null && CargoPath.Count > 0)
            {
                //Correct positons for size of image
                var size = TerrainManager.Land.terrainData.size.x / 2;
                List<Dictionary<string, Vector3>> Labels = new List<Dictionary<string, Vector3>>();
                foreach (var path in CargoPath)
                {
                    int Xpos = (int)ScaleValue(path.z + size, 0, TerrainManager.Land.terrainData.size.x, 0, TerrainManager.SplatMapRes);
                    int Ypos = (int)ScaleValue(path.x + size, 0, TerrainManager.Land.terrainData.size.x, 0, TerrainManager.SplatMapRes);
                    Labels.Add(new Dictionary<string, Vector3> { { ".", new Vector3(Xpos, Ypos, HarmonyMod.Main.config.CargoPathNodeSize) } });
                }
                //Draw on map image
                DrawFont(Labels, System.Drawing.Color.Gray);
                DrawImage();
                return;
            }
            ShowMessageBox("No CargoPath In Map!", 4);
        }
        #endregion

        #region UI
        void CreateCanvas()
        {
            if (canvas != null) { GameObject.Destroy(canvas); }
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPhysicalSize;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        void CreateControlPanel()
        {
            controlPanel = new GameObject("ControlPanel");
            controlPanel.transform.SetParent(canvas.transform);
            RectTransform rect = controlPanel.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90, 90);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(400, 200);
            // Set background color to #2D2C24
            Image panelImage = controlPanel.AddComponent<Image>();
            panelImage.color = new Color(0.17f, 0.17f, 0.14f);
            controlPanel.AddComponent<Draggable>();
            CreateControlButtons();
            CreateScaleHandle();
            Scale(false);
        }

        void CreateImagePanel()
        {
            float imageSize = Mathf.Min(Screen.width * 0.5f, Screen.height * 0.5f);
            // Create Panel for Image
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvas.transform);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(imageSize, imageSize + 100); // Extra space for buttons
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup layoutGroup = panelObj.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 10;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            // Create Image Object (inside panel)
            GameObject imageObj = new GameObject("RawImage");
            imageObj.transform.SetParent(panelObj.transform);
            rawImage = imageObj.AddComponent<RawImage>();
            RectTransform imageRect = rawImage.GetComponent<RectTransform>();
            imageRect.sizeDelta = new Vector2(imageSize, imageSize);
            imageRect.anchorMin = new Vector2(0.5f, 0.5f);
            imageRect.anchorMax = new Vector2(0.5f, 0.5f);
            imageRect.pivot = new Vector2(0.5f, 0.5f);
            imageRect.anchoredPosition = Vector2.zero;
            rawImage.gameObject.AddComponent<ImageZoomAndDrag>();
            //Copy Orignal Image To Working Image
            NewColours = new Color[Colours.Length];
            Colours.CopyTo(NewColours, 0);
            //Set Image
            DrawImage();
            //Prevent Key/Mouse Input On RustMapper
            LoadScreen.Instance.isEnabled = true;
            Compass.Instance.transform.position += (Compass.Instance.transform.up * 500);
        }

        void CreateControlButtons()
        {
            string[] buttonLabels = { "Refresh", "Show Grid", "Show Markers", "Show CargoPath", "Save", "Close" };
            float totalHeight = 0.8f; // Total height occupied by all buttons
            float buttonHeight = totalHeight / buttonLabels.Length; // Individual button height
            float gap = 0.02f; // Small gap between buttons
            for (int i = 0; i < buttonLabels.Length; i++)
            {
                GameObject button = new GameObject(buttonLabels[i]);
                button.transform.SetParent(controlPanel.transform);
                RectTransform btnRect = button.AddComponent<RectTransform>();
                float topAnchor = 1 - (i * (buttonHeight + gap)) - gap;
                float bottomAnchor = topAnchor - buttonHeight;
                btnRect.anchorMin = new Vector2(0.1f, bottomAnchor);
                btnRect.anchorMax = new Vector2(0.9f, topAnchor);
                btnRect.offsetMin = Vector2.zero;
                btnRect.offsetMax = Vector2.zero;
                Image buttonImage = button.AddComponent<Image>();
                if (buttonLabels[i] == "Close") { buttonImage.color = new Color(0.70f, 0.22f, 0.16f); }// Red
                else if (buttonLabels[i] == "Save") { buttonImage.color = new Color(0.45f, 0.55f, 0.26f); } // Green
                else { buttonImage.color = new Color(0.6f, 0.6f, 0.6f); } // Gray
                buttonImage.raycastTarget = true;
                Button btnComponent = button.AddComponent<Button>();
                string buttonLabel = buttonLabels[i];
                btnComponent.onClick.AddListener(() => ButtonClicked(buttonLabel));
                // Create Text
                GameObject textGO = new GameObject("Text");
                textGO.transform.SetParent(button.transform);
                RectTransform textRect = textGO.AddComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                Text text = textGO.AddComponent<Text>();
                text.text = buttonLabels[i];
                text.alignment = TextAnchor.MiddleCenter;
                text.color = new Color(0.9f, 0.9f, 0.9f);
                text.font = font;
                text.resizeTextForBestFit = true;
                text.resizeTextMinSize = 5;
                text.resizeTextMaxSize = 50;
                text.verticalOverflow = VerticalWrapMode.Truncate;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                Shadow shadow = textGO.AddComponent<Shadow>();
                shadow.effectColor = Color.black;
                shadow.effectDistance = new Vector2(1f, -1f);
            }
        }

        void CreateScaleHandle()
        {
            // Create Scale Handle (Clone from MenuManager.scaleButton)
            if (MenuManager.Instance != null && MenuManager.Instance.scaleButton != null)
            {
                var scaleHandle = GameObject.Instantiate(MenuManager.Instance.scaleButton.gameObject, controlPanel.transform);
                scaleHandle.name = "ScaleHandle";
                RectTransform rect2 = scaleHandle.GetComponent<RectTransform>();
                rect2.anchorMin = Vector2.zero;
                rect2.anchorMax = Vector2.zero;
                rect2.pivot = new Vector2(0, 0);
                rect2.anchoredPosition = new Vector2(rect2.sizeDelta.x * 0.5f, 0f);
                Image scaleImage = scaleHandle.GetComponent<Image>();
                if (scaleImage != null) { scaleImage.color = MenuManager.Instance.scaleButton.image.color; }
                scaleHandle.AddComponent<DraggableScaler>().target = controlPanel.GetComponent<RectTransform>();
            }
        }

        public void ShowMessageBox(string message, float duration)
        {
            //Generic looking message box that closes after delay
            GameObject messageBox = new GameObject("MessageBox");
            messageBox.transform.SetParent(canvas.transform);
            RectTransform messageRect = messageBox.AddComponent<RectTransform>();
            messageRect.sizeDelta = new Vector2(300, 80);
            messageRect.anchorMin = messageRect.anchorMax = messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = Vector2.zero;
            Image bgImage = messageBox.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            GameObject textObj = new GameObject("MessageText");
            textObj.transform.SetParent(messageBox.transform);
            Text messageText = textObj.AddComponent<Text>();
            messageText.text = message;
            messageText.color = Color.white;
            messageText.fontSize = 20;
            messageText.alignment = TextAnchor.MiddleCenter;
            messageText.font = font;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = messageRect.sizeDelta;
            textRect.anchoredPosition = Vector2.zero;
            GameObject.Destroy(messageBox, duration);
        }

        void OpenSavePathPopup()
        {
            //Create Save Path Window
            if (inputPopup != null) { GameObject.Destroy(inputPopup); }
            inputPopup = new GameObject("InputPopup");
            inputPopup.transform.SetParent(canvas.transform);
            Image popupBg = inputPopup.AddComponent<Image>();
            popupBg.color = new Color(0, 0, 0, 0.8f);
            RectTransform popupRect = inputPopup.GetComponent<RectTransform>();
            popupRect.sizeDelta = new Vector2(400, 180);
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.pivot = new Vector2(0.5f, 0.5f);
            popupRect.anchoredPosition = Vector2.zero;
            GameObject labelObj = new GameObject("SaveLabel");
            labelObj.transform.SetParent(inputPopup.transform);
            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = "Save as:";
            labelText.color = Color.white;
            labelText.fontSize = 18;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.font = font;
            RectTransform labelRect = labelObj.GetComponent<RectTransform>();
            labelRect.sizeDelta = new Vector2(300, 30);
            labelRect.anchoredPosition = new Vector2(0, 60);
            GameObject inputFieldObj = new GameObject("SavePathInput");
            inputFieldObj.transform.SetParent(inputPopup.transform);
            inputField = inputFieldObj.AddComponent<InputField>();
            Image inputBg = inputFieldObj.AddComponent<Image>();
            inputBg.color = Color.white;
            RectTransform inputRect = inputFieldObj.GetComponent<RectTransform>();
            inputRect.sizeDelta = new Vector2(300, 30);
            inputRect.anchoredPosition = new Vector2(0, 20);
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(inputFieldObj.transform);
            Text placeholder = placeholderObj.AddComponent<Text>();
            placeholder.text = "Enter map file name...";
            placeholder.color = Color.gray;
            placeholder.fontSize = 16;
            placeholder.alignment = TextAnchor.MiddleLeft;
            placeholder.font = font;
            RectTransform placeholderRect = placeholderObj.GetComponent<RectTransform>();
            placeholderRect.sizeDelta = inputRect.sizeDelta;
            placeholderRect.anchoredPosition = Vector2.zero;
            inputField.placeholder = placeholder;
            GameObject textObj = new GameObject("InputText");
            textObj.transform.SetParent(inputFieldObj.transform);
            Text inputText = textObj.AddComponent<Text>();
            inputText.color = Color.black;
            inputText.fontSize = 16;
            inputText.alignment = TextAnchor.MiddleLeft;
            inputText.font = font;
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = inputRect.sizeDelta;
            textRect.anchoredPosition = Vector2.zero;
            inputField.textComponent = inputText;
            GameObject buttonContainer = new GameObject("ButtonContainer");
            buttonContainer.transform.SetParent(inputPopup.transform);
            RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(220, 40);
            containerRect.anchoredPosition = new Vector2(0, -30);
            GameObject confirmButton = new GameObject("ConfirmSaveButton");
            confirmButton.transform.SetParent(buttonContainer.transform);
            Button saveButton = confirmButton.AddComponent<Button>();
            Image buttonImage = confirmButton.AddComponent<Image>();
            buttonImage.color = Color.blue;
            RectTransform buttonRect = confirmButton.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(100, 40);
            buttonRect.anchoredPosition = new Vector2(-60, 0);
            GameObject buttonTextObj = new GameObject("ConfirmSaveText");
            buttonTextObj.transform.SetParent(confirmButton.transform);
            Text buttonText = buttonTextObj.AddComponent<Text>();
            buttonText.text = "Save";
            buttonText.color = Color.white;
            buttonText.fontSize = 18;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.font = font;
            RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
            buttonTextRect.sizeDelta = buttonRect.sizeDelta;
            buttonTextRect.anchoredPosition = Vector2.zero;
            saveButton.onClick.AddListener(SaveImageToPNG);
            GameObject cancelButton = new GameObject("CancelButton");
            cancelButton.transform.SetParent(buttonContainer.transform);
            Button cancelBtn = cancelButton.AddComponent<Button>();
            Image cancelImage = cancelButton.AddComponent<Image>();
            cancelImage.color = Color.red;
            RectTransform cancelRect = cancelButton.GetComponent<RectTransform>();
            cancelRect.sizeDelta = new Vector2(100, 40);
            cancelRect.anchoredPosition = new Vector2(60, 0);
            GameObject cancelTextObj = new GameObject("CancelText");
            cancelTextObj.transform.SetParent(cancelButton.transform);
            Text cancelText = cancelTextObj.AddComponent<Text>();
            cancelText.text = "Cancel";
            cancelText.color = Color.white;
            cancelText.fontSize = 18;
            cancelText.alignment = TextAnchor.MiddleCenter;
            cancelText.font = font;
            RectTransform cancelTextRect = cancelTextObj.GetComponent<RectTransform>();
            cancelTextRect.sizeDelta = cancelRect.sizeDelta;
            cancelTextRect.anchoredPosition = Vector2.zero;
            cancelBtn.onClick.AddListener(CloseSavePopup);
        }
        #endregion

        #region MonoBehaviours
        public class ImageZoomAndDrag : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
        {
            //Allow Zoom and Dragging
            private RectTransform rectTransform;
            private Vector3 originalSize;
            private Vector2 originalPosition;
            private Vector2 lastMousePosition;
            private bool isDragging = false;
            public float zoomScale = 0.5f;

            void Start()
            {
                rectTransform = GetComponent<RectTransform>();
                if (rectTransform == null) { return; }
                rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                originalSize = rectTransform.localScale; // Store original size
                originalPosition = rectTransform.anchoredPosition;
                rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1f);
            }

            public void ResetLayout()
            {
                rectTransform.sizeDelta = originalSize;
                rectTransform.anchoredPosition = originalPosition;
            }

            public void OnScroll(PointerEventData eventData)
            {
                float scroll = eventData.scrollDelta.y; // Scroll up/down
                if (scroll != 0)
                {
                    zoomScale = Mathf.Clamp(zoomScale + scroll * HarmonyMod.Main.config.zoomSpeed, HarmonyMod.Main.config.minZoom, HarmonyMod.Main.config.maxZoom);
                    rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1f);
                }
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                lastMousePosition = eventData.position;
                isDragging = true;
            }

            public void OnDrag(PointerEventData eventData)
            {
                if (!isDragging) { return; }
                Vector2 delta = eventData.position - lastMousePosition;
                rectTransform.anchoredPosition += (delta / 4);
                lastMousePosition = eventData.position;
            }
        }

        public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler
        {
            private RectTransform rectTransform;
            private Vector2 offset;
            private Canvas canvas;

            void Awake()
            {
                rectTransform = GetComponent<RectTransform>();
                canvas = GetComponentInParent<Canvas>(); // Ensure the object is inside a Canvas
            }

            public void OnBeginDrag(PointerEventData eventData)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
                offset = rectTransform.anchoredPosition - localPoint;
            }

            public void OnDrag(PointerEventData eventData)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform.parent as RectTransform, eventData.position, canvas.worldCamera, out Vector2 localPoint);
                rectTransform.anchoredPosition = localPoint + offset;
            }
        }

        public class DraggableScaler : MonoBehaviour, IDragHandler
        {
            public RectTransform target;
            public Vector2 minSize = new Vector2(50, 50);  // Minimum width and height
            public Vector2 maxSize = new Vector2(220, 220); // Maximum width and height
            public void OnDrag(PointerEventData eventData)
            {
                if (target == null) { return; }
                // Determine the dominant delta (use the most significant movement)
                float delta = Mathf.Max(Mathf.Abs(eventData.delta.x), Mathf.Abs(eventData.delta.y));
                delta *= -1; //Reverse Direction
                if (eventData.delta.x < 0 || eventData.delta.y < 0) { delta = -delta; }
                // Maintain aspect ratio while resizing
                float aspectRatio = target.sizeDelta.x / target.sizeDelta.y;
                Vector2 newSize = target.sizeDelta + new Vector2(delta, delta / aspectRatio);
                // Clamp the size within min and max limits
                newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
                newSize.y = newSize.x / aspectRatio;
                // Ensure y size also respects limits
                if (newSize.y < minSize.y)
                {
                    newSize.y = minSize.y;
                    newSize.x = newSize.y * aspectRatio;
                }
                else if (newSize.y > maxSize.y)
                {
                    newSize.y = maxSize.y;
                    newSize.x = newSize.y * aspectRatio;
                }
                // Apply the clamped size
                target.sizeDelta = newSize;
            }
        }
        #endregion
    }
}