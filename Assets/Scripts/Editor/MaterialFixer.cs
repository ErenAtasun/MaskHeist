using UnityEngine;
using UnityEditor;

public class MaterialFixer : EditorWindow
{
    [MenuItem("MaskHeist/Fix Weapon Materials (URP)")]
    public static void FixWeaponMaterialsURP()
    {
        FixMaterials("Universal Render Pipeline/Lit");
    }

    [MenuItem("MaskHeist/Fix Weapon Materials (HDRP)")]
    public static void FixWeaponMaterialsHDRP()
    {
        FixMaterials("HDRP/Lit");
    }

    private static void FixMaterials(string shaderName)
    {
        string folderPath = "Assets/Low Poly ShotGun Weapon Pack 1/Materials";
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folderPath });

        Shader shader = Shader.Find(shaderName);
        if (shader == null)
        {
            Debug.LogError($"❌ Shader '{shaderName}' not found! Make sure the Render Pipeline package is installed.");
            return;
        }

        int count = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null)
            {
                // Backup texture
                Texture mainTex = mat.GetTexture("_MainTex");
                
                // Change shader
                mat.shader = shader;
                
                // Restore texture to new property names if needed
                if (mainTex != null)
                {
                    if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
                        mat.SetTexture("_BaseMap", mainTex); // URP
                        
                    if (mat.HasProperty("_BaseColorMap") && mat.GetTexture("_BaseColorMap") == null)
                        mat.SetTexture("_BaseColorMap", mainTex); // HDRP
                }
                
                EditorUtility.SetDirty(mat);
                count++;
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Updated {count} materials to use '{shaderName}' shader.");
    }
}
