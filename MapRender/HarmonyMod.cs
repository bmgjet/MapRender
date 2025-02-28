/*▄▄▄▄    ███▄ ▄███▓  ▄████  ▄▄▄██▀▀▀▓█████▄▄▄█████▓
 ▓█████▄ ▓██▒▀█▀ ██▒ ██▒ ▀█▒   ▒██   ▓█   ▀▓  ██▒ ▓▒
 ▒██▒ ▄██▓██    ▓██░▒██░▄▄▄░   ░██   ▒███  ▒ ▓██░ ▒░
 ▒██░█▀  ▒██    ▒██ ░▓█  ██▓▓██▄██▓  ▒▓█  ▄░ ▓██▓ ░ 
 ░▓█  ▀█▓▒██▒   ░██▒░▒▓███▀▒ ▓███▒   ░▒████▒ ▒██▒ ░ 
 ░▒▓███▀▒░ ▒░   ░  ░ ░▒   ▒  ▒▓▒▒░   ░░ ▒░ ░ ▒ ░░   
 ▒░▒   ░ ░  ░      ░  ░   ░  ▒ ░▒░    ░ ░  ░   ░    
  ░    ░ ░      ░   ░ ░   ░  ░ ░ ░      ░    ░      
  ░             ░         ░  ░   ░      ░  ░
Version 1.0
Console Command - render.map
Hot Keys - ctrl+m
 */
using HarmonyLib;
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MapRender
{
    public class HarmonyMod : IHarmonyModHooks
    {
        public static MapRender Instance;
        public static Config config;

        //OnLoaded called by harmony when plugin loads
        public void OnLoaded(OnHarmonyModLoadedArgs args)
        {
            //Load/Create Config
            string ConfigPath = Path.Combine(SettingsManager.AppDataPath(), "HarmonyConfig");
            if (!Directory.Exists(ConfigPath)) { Directory.CreateDirectory(ConfigPath); }
            string ConfigFile = Path.Combine(ConfigPath, "MapRender.json");
            if (File.Exists(ConfigFile))
            {
                try { config = JsonUtility.FromJson<Config>(File.ReadAllText(ConfigFile)); }
                catch { config = new Config(); }
            }
            else
            {
                config = new Config();
                File.WriteAllText(ConfigFile, JsonUtility.ToJson(config));
            }
        }

        //OnUnloaded called by harmony when plugin unloads/reloads
        public void OnUnloaded(OnHarmonyModUnloadedArgs args)
        {
            if (Instance != null)
            {
                Instance.CloseWindow();
                Instance = null;
            }
            config = null;
        }

        //Hooks Console Input
        [HarmonyPatch(typeof(ConsoleWindow), "ExecuteCommand")]
        public class ConsoleWindow_ExecuteCommand
        {
            static bool Prefix(string command, ConsoleWindow __instance)
            {
                if (command.StartsWith("render.map")) //Catch keyword
                {
                    try
                    {
                        __instance.Post("Creating Map Render");
                        Instance = new MapRender().Init();
                        Instance.Render();
                    }
                    catch (Exception e)
                    {
                        __instance.Post(e.ToString());
                    }
                    __instance.consoleInput.text = string.Empty;
                    return false;
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
                    //Catch esc key as kit from maprender
                    if (Instance != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                    {
                        Instance.CloseWindow();
                    }
                    if (__instance.cam == null || LoadScreen.Instance.isEnabled)
                    {
                        return false;
                    }
                    if (!Mouse.current.rightButton.isPressed && Keyboard.current.ctrlKey.isPressed && Keyboard.current.mKey.wasPressedThisFrame) //CTRL+M keys
                    {
                        Instance = new MapRender().Init();
                        Instance.Render();
                        return false;
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
                    //Post logo to console window
                    __instance.PostMultiLine(
@"
    __  ___            ____                 __         
   /  |/  /___ _____  / __ \___  ____  ____/ /__  _____
  / /|_/ / __ `/ __ \/ /_/ / _ \/ __ \/ __  / _ \/ ___/
 / /  / / /_/ / /_/ / _, _/  __/ / / / /_/ /  __/ /    
/_/  /_/\__,_/ .___/_/ |_|\___/_/ /_/\__,_/\___/_/     
            /_/                        V 1.0 by bmgjet
");
                }
                catch { }
            }
        }
    }

    #region Config
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

        public float minZoom = 0.25f;

        public float maxZoom = 5f;

        public float zoomSpeed = 0.02f;

        public Vector2 CloseButtonPosition = new Vector2(-30, -10);

        public Vector2 SaveButtonPosition = new Vector2(-70, -10);

        public Vector2 ButtonSize = new Vector2(38, 38);

        public int ButtonFontSize = 12;
    }
    #endregion

    public class MapRender
    {
        private WorldSerialization world;
        public int heightres;
        public int splatres;
        public short[] Height;
        public byte[] Splat;
        public int[] Topology;

        private Canvas canvas;
        private RawImage rawImage;
        private GameObject inputPopup;
        private InputField inputField;
        private Font font;

        private Array2D<Color> output;
        public struct Array2D<T>
        {
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

            private T[] _items;

            private int _width;

            private int _height;
        }

        public MapRender Init()
        {
            TerrainManager.SaveLayer(); //Save current layers
            world = WorldConverter.TerrainToWorld(TerrainManager.Land, TerrainManager.Water, new ValueTuple<int, int, int>(0, 0, 0)); //Convert current project into WorldSerialization

            //Read Data Needed for Map Image Generation
            byte[] array = this.world.GetMap("height").data;
            Height = new short[array.Length / 2];
            Buffer.BlockCopy(array, 0, Height, 0, array.Length);
            heightres = Mathf.RoundToInt(Mathf.Sqrt(array.Length / 2));
            Splat = world.GetMap("splat").data;
            splatres = Mathf.RoundToInt(Mathf.Sqrt(Splat.Length / 8));
            var tempdata = world.GetMap("topology").data;
            Topology = new int[tempdata.Length];
            Buffer.BlockCopy(tempdata, 0, Topology, 0, Topology.Length);
            //Free memory no longer used
            world = null;
            //Load Font
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return this;
        }

        #region Functions
        short GetHeight(int x, int z) { return Height[z * heightres + x]; }

        int GetTopology(int x, int z) { return (Topology[z * splatres + x]); }

        float GetSplat(int x, int z, int mask)
        {
            if (mask > 0 && (mask & (mask - 1)) == 0)
            {
                return BitUtility.Byte2Float((int)Splat[(TerrainSplat.TypeToIndex(mask) * splatres + z) * splatres + x]);
            }
            int num = 0;
            for (int i = 0; i < 8; i++)
            {
                if ((TerrainSplat.IndexToType(i) & mask) != 0)
                {
                    num += (int)Splat[(i * splatres + z) * splatres + x];
                }
            }
            return Mathf.Clamp01(BitUtility.Byte2Float(num));
        }

        double ScaleValue(double value, double fromMin, double fromMax, double toMin, double toMax)
        {
            if (fromMax == fromMin)
            {
                return toMin;
            }
            return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
        }
        #endregion

        #region Methods
        public void Render()
        {
            if (Height == null || Topology == null || Splat == null) { return; }
            var config = new Config();
            Color[] array = new Color[splatres * splatres];
            output = new Array2D<Color>(array, splatres, splatres);
            //Scan each pixel of Splat Map
            Parallel.For(0, splatres, x =>
            {
                float[] splatValues = new float[8];
                int newx = (int)ScaleValue((double)x, 0.0, (double)splatres, 0.0, (double)heightres);
                for (int z = 0; z < splatres; z++)
                {
                    Vector4 vector = config.StartColor;
                    int newz = (int)ScaleValue((double)z, 0.0, (double)splatres, 0.0, (double)heightres);
                    float height = BitUtility.Short2Float(GetHeight(newx, newz));
                    TerrainTopology.Enum topology = (TerrainTopology.Enum)GetTopology(x, z);
                    if (height < 0.5f && topology.HasFlag(TerrainTopology.Enum.Ocean))
                    {
                        vector = Vector4.Lerp(vector, config.WaterColor, height);
                        vector = Vector4.Lerp(vector, config.OffShoreColor, height);
                    } //Ocean Topology (Paint Ocean)
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

                        vector = Vector4.Lerp(vector, config.GravelColor, splatValues[0] * config.GravelColor.w);
                        vector = Vector4.Lerp(vector, config.PebbleColor, splatValues[1] * config.PebbleColor.w);
                        vector = Vector4.Lerp(vector, config.RockColor, splatValues[2] * config.RockColor.w);
                        vector = Vector4.Lerp(vector, config.DirtColor, splatValues[3] * config.DirtColor.w);
                        vector = Vector4.Lerp(vector, config.GrassColor, splatValues[4] * config.GrassColor.w);
                        vector = Vector4.Lerp(vector, config.ForestColor, splatValues[5] * config.ForestColor.w);
                        vector = Vector4.Lerp(vector, config.SandColor, splatValues[6] * config.SandColor.w);
                        vector = Vector4.Lerp(vector, config.SnowColor, splatValues[7] * config.SnowColor.w);
                        if (topology.HasFlag(TerrainTopology.Enum.Lake) || topology.HasFlag(TerrainTopology.Enum.River)) //Lake Or River
                        {
                            vector = Vector4.Lerp(vector, config.WaterColor, 0.9f);
                        }
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
            CreateUIWindow(array);
        }

        void SaveImageToPNG()
        {
            //Write Render Image To Root Folder With Specified Name
            if (inputField == null || string.IsNullOrWhiteSpace(inputField.text)) { return; }
            string filePath = inputField.text;
            if (!filePath.EndsWith(".png")) { filePath += ".png"; }
            try
            {
                Texture2D texture = rawImage.texture as Texture2D;
                if (texture == null) { return; }
                System.IO.File.WriteAllBytes(filePath, texture.EncodeToPNG());
                GameObject.Destroy(inputPopup);
                ShowMessageBox("Saved Successfully! \n" + filePath, 2f);
            }
            catch (Exception ex)
            {
                ShowMessageBox("Error Saving Image!", 2f);
                Debug.LogError("Error saving image: " + ex.Message);
            }
        }
        #endregion

        #region UI
        public void CloseWindow()
        {
            if (canvas != null) { GameObject.Destroy(canvas.gameObject); }//Destroy Map Render Window
            LoadScreen.Instance.isEnabled = false; //Allow Key/Mouse Input On RustMapper
            HarmonyMod.Instance = null;
        }

        void CloseSavePopup() { if (inputPopup != null) { GameObject.Destroy(inputPopup); } }

        void CreateUIWindow(Color[] array)
        {
            if (canvas != null) { GameObject.Destroy(canvas); }
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObj.AddComponent<GraphicRaycaster>();
            float imageSize = Mathf.Min(Screen.width * 0.5f, Screen.height * 0.5f);

            // Create Panel for Image + Buttons
            GameObject panelObj = new GameObject("Panel");
            panelObj.transform.SetParent(canvasObj.transform);
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

            // Create Close Button
            GameObject closeButton = CreateUIButton("CloseButton", "Close", new Color(0.70f, 0.22f, 0.16f), HarmonyMod.config.CloseButtonPosition);
            closeButton.GetComponent<Button>().onClick.AddListener(CloseWindow);

            // Create Save Button
            GameObject saveButton = CreateUIButton("SaveButton", "Save", new Color(0.45f, 0.55f, 0.26f), HarmonyMod.config.SaveButtonPosition);
            saveButton.GetComponent<Button>().onClick.AddListener(OpenSavePathPopup);

            //Set Image
            Texture2D texture = new Texture2D(splatres, splatres);
            texture.SetPixels(array);
            texture.Apply();
            rawImage.texture = texture;

            //Prevent Key/Mouse Input On RustMapper
            LoadScreen.Instance.isEnabled = true;
            rawImage.gameObject.AddComponent<ImageZoomAndDrag>();
        }

        GameObject CreateUIButton(string name, string text, Color color, Vector2 position)
        {
            //Create Button
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(canvas.transform);
            Button button = buttonObj.AddComponent<Button>();
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = color;
            RectTransform rectTransform = buttonObj.GetComponent<RectTransform>();
            rectTransform.sizeDelta = HarmonyMod.config.ButtonSize;
            rectTransform.anchorMin = new Vector2(1, 1);
            rectTransform.anchorMax = new Vector2(1, 1);
            rectTransform.pivot = new Vector2(1, 1);
            rectTransform.anchoredPosition = position;
            GameObject textObj = new GameObject(name + "Text");
            textObj.transform.SetParent(buttonObj.transform);
            Text buttonText = textObj.AddComponent<Text>();
            buttonText.text = text;
            buttonText.color = new Color(0.59f, 0.59f, 0.59f);
            buttonText.fontSize = HarmonyMod.config.ButtonFontSize;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.font = font;
            // Add an outline component for a black border
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;  // Border color
            outline.effectDistance = new Vector2(1f, -1f);  // Outline thickness
            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.sizeDelta = rectTransform.sizeDelta;
            textRect.anchoredPosition = Vector2.zero;
            return buttonObj;
        }

        public void ShowMessageBox(string message, float duration)
        {
            //Create generic message box with auto close after delay
            GameObject messageBox = new GameObject("MessageBox");
            messageBox.transform.SetParent(canvas.transform);
            Image bgImage = messageBox.AddComponent<Image>();
            bgImage.color = new Color(0, 0, 0, 0.7f);
            RectTransform messageRect = messageBox.GetComponent<RectTransform>();
            messageRect.sizeDelta = new Vector2(300, 80);
            messageRect.anchorMin = new Vector2(0.5f, 0.5f);
            messageRect.anchorMax = new Vector2(0.5f, 0.5f);
            messageRect.pivot = new Vector2(0.5f, 0.5f);
            messageRect.anchoredPosition = Vector2.zero;
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

        public class ImageZoomAndDrag : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
        {
            //Allow Zoom and Dragging
            private RectTransform rectTransform;
            private Vector2 originalSize;
            private Vector2 lastMousePosition;
            private bool isDragging = false;
            public float zoomScale = 0.25f;

            void Start()
            {
                rectTransform = GetComponent<RectTransform>();
                if (rectTransform == null)
                {
                    Debug.LogError("No RectTransform found on " + gameObject.name);
                    return;
                }

                rectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
                originalSize = rectTransform.sizeDelta; // Store original size
                rectTransform.localScale = new Vector3(zoomScale, zoomScale, 1f);
            }

            public void OnScroll(PointerEventData eventData)
            {
                float scroll = eventData.scrollDelta.y; // Scroll up/down
                if (scroll != 0)
                {
                    zoomScale = Mathf.Clamp(zoomScale + scroll * HarmonyMod.config.zoomSpeed, HarmonyMod.config.minZoom, HarmonyMod.config.maxZoom);
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
                if (!isDragging) return;

                Vector2 delta = eventData.position - lastMousePosition;
                rectTransform.anchoredPosition += (delta / 4);
                lastMousePosition = eventData.position;
            }
        }
        #endregion
    }
}