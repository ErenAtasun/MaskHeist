using UnityEngine;
using UnityEditor;
using Mirror;
using MaskHeist.Traps;

public class TrapPrefabCreator
{
    [MenuItem("MaskHeist/Create Laser Trap Prefab")]
    public static void CreateLaserTrap()
    {
        // 1. Create GameObject
        GameObject go = new GameObject("LaserTrap");
        
        // 2. Add Components
        // NetworkIdentity
        go.AddComponent<NetworkIdentity>();
        
        // Collider (Trigger)
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(3f, 1f, 0.1f); // Geniş bir lazer alanı
        col.center = new Vector3(0, 0.5f, 0);

        // LineRenderer (Visual)
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.SetPosition(0, new Vector3(-1.5f, 0.5f, 0));
        lr.SetPosition(1, new Vector3(1.5f, 0.5f, 0));
        // Kırmızı materyal (Varsayılan materyal ile)
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = Color.red;
        lr.endColor = Color.red;

        // Script
        LaserTrap script = go.AddComponent<LaserTrap>();
        // Ayarlar
        // script.armingDelay = 2f; // Protected, editörden ayarlanır

        // 3. Save as Prefab
        string path = "Assets/Prefabs/LaserTrap.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        
        // 4. Cleanup scene object
        Object.DestroyImmediate(go);

        Debug.Log($"Laser Trap Prefab Created at: {path}");

        // 5. Register to NetworkManager
        RegisterPrefabToNetworkManager(path);
    }

    private static void RegisterPrefabToNetworkManager(string prefabPath)
    {
        MaskHeist.Network.MaskHeistNetworkManager manager = Object.FindObjectOfType<MaskHeist.Network.MaskHeistNetworkManager>();
        if (manager != null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab != null && !manager.spawnPrefabs.Contains(prefab))
            {
                manager.spawnPrefabs.Add(prefab);
                EditorUtility.SetDirty(manager);
                Debug.Log("Laser Trap added to NetworkManager spawn list.");
            }
        }
    }
}
