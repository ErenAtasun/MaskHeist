using UnityEngine;
using UnityEditor;
using MaskHeist.Core;
using MaskHeist.Traps;

public class PrefabFixer
{
    [MenuItem("MaskHeist/Fix Player Prefab (Auto-Detect)")]
    public static void FixPlayerPrefab()
    {
        // Önce Player.prefab'ı kontrol et (Kullanıcının belirttiği)
        string path1 = "Assets/Prefabs/Player.prefab";
        string path2 = "Assets/Prefabs/MaskHeistGamePlayer.prefab";

        bool fixedAny = false;

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path1) != null)
        {
            Debug.Log($"🔧 'Player.prefab' bulundu, tamir ediliyor...");
            FixPrefabAtPath(path1);
            fixedAny = true;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(path2) != null)
        {
            Debug.Log($"🔧 'MaskHeistGamePlayer.prefab' bulundu, tamir ediliyor...");
            FixPrefabAtPath(path2);
            fixedAny = true;
        }

        if (!fixedAny)
        {
            Debug.LogError("❌ Ne 'Player.prefab' ne de 'MaskHeistGamePlayer.prefab' bulunabildi!");
        }
    }

    private static void FixPrefabAtPath(string path)
    {
        using (var editScope = new PrefabUtility.EditPrefabContentsScope(path))
        {
            GameObject go = editScope.prefabContentsRoot;

            // 1. PlayerTrapInventory ekle (Yoksa)
            if (go.GetComponent<PlayerTrapInventory>() == null)
            {
                go.AddComponent<PlayerTrapInventory>();
                Debug.Log($"✅ PlayerTrapInventory eklendi: {path}");
            }
            else
            {
                Debug.Log($"ℹ️ PlayerTrapInventory zaten var: {path}");
            }

            // 2. MaskHeistGamePlayer kontrol et
            if (go.GetComponent<MaskHeistGamePlayer>() == null)
            {
                go.AddComponent<MaskHeistGamePlayer>();
                Debug.Log($"✅ MaskHeistGamePlayer eklendi: {path}");
            }

            // 3. NetworkTransformReliable kontrol et
            if (go.GetComponent<Mirror.NetworkTransformReliable>() == null)
            {
                go.AddComponent<Mirror.NetworkTransformReliable>();
                Debug.Log($"✅ NetworkTransformReliable eklendi: {path}");
            }
        }
    }
}
