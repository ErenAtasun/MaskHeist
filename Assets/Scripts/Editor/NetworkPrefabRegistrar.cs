using UnityEditor;
using UnityEngine;
using Mirror;
using System.Collections.Generic;
using System.IO;

namespace MaskHeist.Editor
{
    public static class NetworkPrefabRegistrar
    {
        [MenuItem("MaskHeist/Network/Register All Spawnable Prefabs")]
        public static void RegisterAllSpawnablePrefabs()
        {
            // NetworkManager'ı bul
            var networkManager = Object.FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("[NetworkPrefabRegistrar] Sahnede NetworkManager bulunamadı! Önce LobbyScene'i açın.");
                return;
            }

            // Prefabs klasöründeki tüm prefab'ları tara
            string prefabsPath = "Assets/Prefabs";
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { prefabsPath });

            List<GameObject> registeredPrefabs = new List<GameObject>();
            int addedCount = 0;

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

                if (prefab == null) continue;

                // NetworkIdentity var mı kontrol et
                NetworkIdentity netId = prefab.GetComponent<NetworkIdentity>();
                if (netId == null) continue;

                // Zaten kayıtlı mı kontrol et
                bool alreadyRegistered = false;
                foreach (var existing in networkManager.spawnPrefabs)
                {
                    if (existing == prefab)
                    {
                        alreadyRegistered = true;
                        break;
                    }
                }

                if (!alreadyRegistered)
                {
                    networkManager.spawnPrefabs.Add(prefab);
                    addedCount++;
                    Debug.Log($"[NetworkPrefabRegistrar] Eklendi: {prefab.name}");
                }
                
                registeredPrefabs.Add(prefab);
            }

            // Değişiklikleri kaydet
            EditorUtility.SetDirty(networkManager);
            
            Debug.Log($"[NetworkPrefabRegistrar] Tamamlandı! {addedCount} yeni prefab eklendi. Toplam kayıtlı: {registeredPrefabs.Count}");
            Debug.Log("[NetworkPrefabRegistrar] Sahneyi kaydetmeyi unutmayın (Ctrl+S)!");
        }

        [MenuItem("MaskHeist/Network/List Registered Prefabs")]
        public static void ListRegisteredPrefabs()
        {
            var networkManager = Object.FindObjectOfType<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("[NetworkPrefabRegistrar] Sahnede NetworkManager bulunamadı!");
                return;
            }

            Debug.Log($"[NetworkPrefabRegistrar] Kayıtlı Prefab Sayısı: {networkManager.spawnPrefabs.Count}");
            foreach (var prefab in networkManager.spawnPrefabs)
            {
                if (prefab != null)
                    Debug.Log($"  - {prefab.name}");
                else
                    Debug.LogWarning("  - (null/missing prefab!)");
            }
        }
    }
}
