using UnityEngine;
using UnityEditor;
using MaskHeist.Mask;

public class JumperMaskCreator
{
    [MenuItem("MaskHeist/Create Jumper Mask Asset")]
    public static void CreateJumperMask()
    {
        MaskData jumper = ScriptableObject.CreateInstance<MaskData>();
        
        jumper.maskName = "Jumper";
        jumper.description = "Sıçrayıcı Maskesi - Q tuşuna basarak ileri doğru hızlıca fırla! Yüksek yerlere atlamak ve tehlikelerden kaçmak için mükemmel.";
        jumper.maskColor = new Color(0.2f, 0.9f, 0.4f); // Yeşil
        
        // Invisibility (standard)
        jumper.invisibilityDuration = 5f;
        jumper.invisibilityCooldown = 45f;
        jumper.invisibilityNoiseMultiplier = 0.15f;
        
        // Unique ability: Jumper
        jumper.uniqueAbilityType = MaskAbilityType.Jumper;
        jumper.uniqueAbilityDuration = 0.5f; // Dash is instant
        jumper.uniqueAbilityCooldown = 4f;
        
        // Dash settings
        jumper.dashForce = 10f;
        jumper.dashUpwardForce = 12f;
        
        AssetDatabase.CreateAsset(jumper, "Assets/MaskDatas/Jumper.asset");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = jumper;
        
        Debug.Log("[MaskHeist] Jumper mask asset created at Assets/MaskDatas/Jumper.asset");
    }
}
