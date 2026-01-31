using UnityEngine;
using UnityEditor;
using Mirror;
using MaskHeist.Core;
using MaskHeist.Traps;

public class GamePlayerCreator
{
    [MenuItem("MaskHeist/Create Game Player Prefab")]
    public static void CreateGamePlayerPrefab()
    {
        // 1. Klasör Kontrolü
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        // 2. GameObject Oluştur (Görseli olsun diye Capsule yapalım)
        GameObject gamePlayerObj = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        gamePlayerObj.name = "MaskHeistGamePlayer";
        
        // 3. Bileşenleri Ekle
        // NetworkIdentity
        if (gamePlayerObj.GetComponent<NetworkIdentity>() == null)
             gamePlayerObj.AddComponent<NetworkIdentity>();
             
        // NetworkTransform (Mirror'da isim değişti, Reliable/Unreliable olarak ayrıldı veya namespace farklı olabilir)
        // Şimdilik NetworkTransform yerine NetworkTransformReliable kullanalım veya genel ekleme yapalım.
        // Mirror'ın yeni sürümlerinde NetworkTransform obsolete olabilir.
        if (gamePlayerObj.GetComponent<NetworkTransformReliable>() == null)
             gamePlayerObj.AddComponent<NetworkTransformReliable>();

        // Bizim Game Player scripti (Bu script RequireComponent ile Inventory'yi de ekler)
        gamePlayerObj.AddComponent<MaskHeistGamePlayer>();
        
        // 4. Prefab Olarak Kaydet
        string path = "Assets/Prefabs/MaskHeistGamePlayer.prefab";
        path = AssetDatabase.GenerateUniqueAssetPath(path);
        
        PrefabUtility.SaveAsPrefabAsset(gamePlayerObj, path);
        
        // 5. Sahnedeki geçici objeyi sil
        Object.DestroyImmediate(gamePlayerObj);

        Debug.Log($"[MaskHeist] GamePlayer Prefab created at: {path}");
        
        // 6. NetworkManager'a otomatik atamayı dene
        MaskHeist.Network.MaskHeistNetworkManager manager = Object.FindObjectOfType<MaskHeist.Network.MaskHeistNetworkManager>();
        if (manager != null)
        {
            manager.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorUtility.SetDirty(manager);
            Debug.Log("[MaskHeist] GamePlayer Prefab automatically assigned to NetworkManager!");
        }
        else
        {
             Debug.LogWarning("[MaskHeist] NetworkManager not found in scene. Please assign the prefab manually.");
        }
    }
}
