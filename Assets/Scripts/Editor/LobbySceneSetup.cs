using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MaskHeist.UI.Lobby;

public class LobbySceneSetup
{
    [MenuItem("MaskHeist/Setup Lobby Scene")]
    public static void CreateLobbyScene()
    {
        // 1. Yeni Sahne Oluştur
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        
        // 2. Klasör Kontrolü
        if (!System.IO.Directory.Exists("Assets/Scenes"))
        {
             System.IO.Directory.CreateDirectory("Assets/Scenes");
        }

        // 3. Canvas Kurulumu
        GameObject canvasObj = new GameObject("LobbyCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // Arkaplan (Panel)
        GameObject bgObj = new GameObject("BackgroundPanel");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 1f);
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta = Vector2.zero;

        // 4. LobbyUIManager Ekle
        LobbyUIManager uiManager = canvasObj.AddComponent<LobbyUIManager>();

        // 5. UI Elemanlarını Oluştur
        // Font asset'i bulmaya çalış (varsayılan)
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        // --- Title ---
        CreateText(bgObj.transform, "TitleText", "MASK HEIST", 0, 300, 80, defaultFont);

        // --- Name Input ---
        TMP_InputField nameInput = CreateInputField(bgObj.transform, "NameInput", "Enter Name...", 0, 100, defaultFont);
        
        // --- IP Input ---
        TMP_InputField ipInput = CreateInputField(bgObj.transform, "IPInput", "localhost", 0, 0, defaultFont);

        // --- Host Button ---
        Button hostBtn = CreateButton(bgObj.transform, "HostButton", "HOST GAME", -200, -150, Color.green, defaultFont);
        
        // --- Join Button ---
        Button joinBtn = CreateButton(bgObj.transform, "JoinButton", "JOIN GAME", 200, -150, Color.cyan, defaultFont);

        // --- Status Text ---
        TextMeshProUGUI statusTxt = CreateText(bgObj.transform, "StatusText", "Ready...", 0, -300, 40, defaultFont);

        // 6. Referansları Bağla (Reflection kullanarak private field'lara erişiyoruz çünkü serializefield)
        SerializedObject so = new SerializedObject(uiManager);
        so.FindProperty("nameInput").objectReferenceValue = nameInput;
        so.FindProperty("ipInput").objectReferenceValue = ipInput;
        so.FindProperty("hostButton").objectReferenceValue = hostBtn;
        so.FindProperty("joinButton").objectReferenceValue = joinBtn;
        so.FindProperty("statusText").objectReferenceValue = statusTxt;
        so.ApplyModifiedProperties();

        // 7. EventSystem Ekle
        GameObject esObj = new GameObject("EventSystem");
        esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // 8. Sahneyi Kaydet
        string scenePath = "Assets/Scenes/LobbyScene.unity";
        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log($"[MaskHeist] Lobby Scene Created at {scenePath}");

        // 9. Build Settings'e Ekle
        AddSceneToBuildSettings(scenePath);
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string content, float x, float y, float fontSize, TMP_FontAsset font)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        if (font != null) tmp.font = font;
        
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(600, 100);
        return tmp;
    }

    private static TMP_InputField CreateInputField(Transform parent, string name, string placeholder, float x, float y, TMP_FontAsset font)
    {
        // Basit Input Field Yapısı
        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        Image bg = root.AddComponent<Image>();
        bg.color = Color.white;
        
        RectTransform rt = root.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(400, 60);

        // Text Area
        GameObject textArea = new GameObject("TextArea");
        textArea.transform.SetParent(root.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10, 5);
        textAreaRect.offsetMax = new Vector2(-10, -5);

        // Text Component
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI textComp = textObj.AddComponent<TextMeshProUGUI>();
        textComp.fontSize = 32;
        textComp.color = Color.black;
        textComp.alignment = TextAlignmentOptions.Left;
        if (font != null) textComp.font = font;

        // Placeholder
        GameObject placeObj = new GameObject("Placeholder");
        placeObj.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeComp = placeObj.AddComponent<TextMeshProUGUI>();
        placeComp.text = placeholder;
        placeComp.fontSize = 32;
        placeComp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeComp.fontStyle = FontStyles.Italic;
        placeComp.alignment = TextAlignmentOptions.Left;
        if (font != null) placeComp.font = font;

        TMP_InputField input = root.AddComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = textComp;
        input.placeholder = placeComp;

        return input;
    }

    private static Button CreateButton(Transform parent, string name, string label, float x, float y, Color color, TMP_FontAsset font)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        Image img = btnObj.AddComponent<Image>();
        img.color = color;
        
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(x, y);
        rt.sizeDelta = new Vector2(300, 80);

        Button btn = btnObj.AddComponent<Button>();

        // Label
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(btnObj.transform, false);
        TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 32;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.black;
        if (font != null) tmp.font = font;
        
        RectTransform txtRect = txtObj.GetComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;

        return btn;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        EditorBuildSettingsScene[] original = EditorBuildSettings.scenes;
        EditorBuildSettingsScene[] newSettings = new EditorBuildSettingsScene[original.Length + 1];
        System.Array.Copy(original, newSettings, original.Length);
        newSettings[newSettings.Length - 1] = new EditorBuildSettingsScene(path, true);
        EditorBuildSettings.scenes = newSettings;
    }
}
