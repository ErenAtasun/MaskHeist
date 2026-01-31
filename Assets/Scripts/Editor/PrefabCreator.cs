using UnityEngine;
using UnityEditor;
using Mirror;
using MaskHeist.Network;

public class PrefabCreator
{
    [MenuItem("MaskHeist/Create Room Player Prefab")]
    public static void CreateRoomPlayerPrefab()
    {
        // 1. Klasör Kontrolü
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 2. GameObject Oluştur
        GameObject roomPlayerObj = new GameObject("MaskHeistRoomPlayer");
        
        // 3. Bileşenleri Ekle
        // NetworkIdentity (Mirror zorunlu kılar)
        roomPlayerObj.AddComponent<NetworkIdentity>();
        
        // Bizim yazdığımız Room Player scripti
        roomPlayerObj.AddComponent<MaskHeistRoomPlayer>();

        // 4. Prefab Olarak Kaydet
        string path = "Assets/Prefabs/MaskHeistRoomPlayer.prefab";
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        
        PrefabUtility.SaveAsPrefabAsset(roomPlayerObj, path);
        
        // 5. Sahnedeki geçici objeyi sil
        Object.DestroyImmediate(roomPlayerObj);

        Debug.Log($"[MaskHeist] RoomPlayer Prefab created at: {path}");
        
        // 6. NetworkManager'a otomatik atamayı dene
        MaskHeistNetworkManager manager = Object.FindObjectOfType<MaskHeistNetworkManager>();
        if (manager != null)
        {
            manager.roomPlayerPrefab = AssetDatabase.LoadAssetAtPath<NetworkRoomPlayer>(path);
            EditorUtility.SetDirty(manager);
            Debug.Log("[MaskHeist] RoomPlayer Prefab automatically assigned to NetworkManager!");
        }
        else
        {
             Debug.LogWarning("[MaskHeist] NetworkManager not found in scene. Please assign the prefab manually.");
        }
    }
}
