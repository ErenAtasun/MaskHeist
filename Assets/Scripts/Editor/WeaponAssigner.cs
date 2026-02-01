using UnityEngine;
using UnityEditor;
using MaskHeist.Player; // Assuming namespace

namespace MaskHeist.Editor
{
    public class WeaponAssigner
    {
        [MenuItem("MaskHeist/Assign Shotgun to Player")]
        [InitializeOnLoadMethod] // Runs automatically after compilation
        public static void AssignShotgun()
        {
            // 1. Load Player Prefab
            string playerPrefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playerPrefabPath);

            if (playerPrefab == null)
            {
                Debug.LogError($"Player prefab not found at {playerPrefabPath}");
                return;
            }

            // 2. Load Shotgun Model
            string shotgunPath = "Assets/Low Poly ShotGun Weapon Pack 1/Models/Weapons/ShotGun_A.fbx";
            GameObject shotgunModel = AssetDatabase.LoadAssetAtPath<GameObject>(shotgunPath);

            if (shotgunModel == null)
            {
                Debug.LogError($"Shotgun model not found at {shotgunPath}");
                return;
            }

            // 3. Get WeaponController
            WeaponController weaponController = playerPrefab.GetComponent<WeaponController>();
            if (weaponController == null)
            {
                Debug.LogError("WeaponController component not found on Player prefab!");
                return;
            }

            // 4. Assign
            Undo.RecordObject(weaponController, "Assign Shotgun Model");
            // Reflection might be needed if field is private, but let's assume I can modify it or it's serialized.
            // Wait, previous analysis showed it is [SerializeField] private.
            // Editor scripts can access private serialized fields via SerializedObject.
            
            SerializedObject so = new SerializedObject(weaponController);
            SerializedProperty prop = so.FindProperty("weaponModelPrefab");
            if (prop != null)
            {
                prop.objectReferenceValue = shotgunModel;
                so.ApplyModifiedProperties();
                Debug.Log("Successfully assigned ShotGun_A.fbx to WeaponController.weaponModelPrefab");
            }
            else
            {
                Debug.LogError("Could not find property 'weaponModelPrefab' in WeaponController");
            }

            // 5. Save
            PrefabUtility.SavePrefabAsset(playerPrefab);
        }
    }
}
