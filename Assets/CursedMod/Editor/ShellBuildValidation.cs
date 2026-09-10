#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Reject a release with missing or incorrectly imported Shell assets.</summary>
public sealed class ShellBuildValidation : IPreprocessBuildWithReport
{
    public int callbackOrder { get { return -900; } }

    public void OnPreprocessBuild(BuildReport report) { Validate(); }

    [MenuItem("Cursed Baldi/Validate Shell Assets")]
    public static void Validate()
    {
        const string imagePath = "Assets/Resources/CursedMod/Shell.png";
        const string audioPath = "Assets/Resources/CursedMod/ShellUse.wav";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(imagePath);
        TextureImporter importer = AssetImporter.GetAtPath(imagePath) as TextureImporter;
        if (sprite == null || sprite.rect.width < 64f || sprite.rect.height < 64f ||
            importer == null || !importer.DoesSourceTextureHaveAlpha() ||
            importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single)
            throw new BuildFailedException("Shell sprite is missing, damaged or incorrectly imported.");

        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(audioPath);
        if (clip == null)
            throw new BuildFailedException("ShellUse.wav is missing or could not be imported.");

        Debug.Log("Shell asset validation passed: sprite and audio are ready.");
    }
}
#endif
