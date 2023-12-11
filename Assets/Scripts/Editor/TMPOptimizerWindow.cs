using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using UnityEngine.UIElements;

public class TMPOptimizerWindow : EditorWindow
{
  private TMP_FontAsset selectedFontAsset;
  private Texture2D originalTexture;
  private const int MAX_SIZE = 8192;

  [MenuItem("U2SB/TMP/一键优化")]
  public static void ShowWindow()
  {
    GetWindow<TMPOptimizerWindow>("TMP优化工具");
  }

  void OnGUI()
  {
    GUILayout.Label("TMP字体优化设置", EditorStyles.boldLabel);

    // 选择字体文件
    selectedFontAsset = (TMP_FontAsset)EditorGUILayout.ObjectField(
        "TMP字体文件",
        selectedFontAsset,
        typeof(TMP_FontAsset),
        false);

    if (selectedFontAsset != null)
    {
      EditorGUILayout.HelpBox($"当前字体贴图尺寸: {selectedFontAsset.atlasTexture.width}x{selectedFontAsset.atlasTexture.height}", MessageType.Info);
    }

    GUI.enabled = selectedFontAsset != null;
    if (GUILayout.Button("一键优化", GUILayout.Height(30)))
    {
      OptimizeFontTexture();
    }
    GUI.enabled = true;
  }

  void OptimizeFontTexture()
  {
    try
    {
      // 获取原始资源路径
      string fontPath = AssetDatabase.GetAssetPath(selectedFontAsset);
      string directory = Path.GetDirectoryName(fontPath);
      string textureName = $"{selectedFontAsset.name}.png";
      string newTexturePath = Path.Combine(directory, textureName);

      // 创建新纹理
      Texture2D original = selectedFontAsset.atlasTexture;
      Texture2D newTexture = new Texture2D(
          original.width,
          original.height,
          TextureFormat.Alpha8,
          false);

      // 复制纹理数据
      Graphics.CopyTexture(original, newTexture);

      // 保存PNG文件
      File.WriteAllBytes(newTexturePath, newTexture.EncodeToPNG());
      AssetDatabase.Refresh();

      // 设置纹理导入参数
      TextureImporter importer = AssetImporter.GetAtPath(newTexturePath) as TextureImporter;
      if (importer != null)
      {
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;

        // 设置最大分辨率
        importer.maxTextureSize = Mathf.Max(
            Mathf.NextPowerOfTwo(original.width),
            Mathf.NextPowerOfTwo(original.height),
            MAX_SIZE);

        // 设置Alpha8格式
        TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings
        {
          format = TextureImporterFormat.Alpha8,
          overridden = true,
          maxTextureSize = importer.maxTextureSize
        };

        importer.SetPlatformTextureSettings(settings);
        importer.SaveAndReimport();
      }

      // 更新字体引用
      Texture2D optimizedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(newTexturePath);
      Undo.RecordObject(selectedFontAsset, "Update TMP Font Texture");

      // 清除原有贴图引用
      selectedFontAsset.atlasTextures[0] = null;
      EditorUtility.SetDirty(selectedFontAsset);
      AssetDatabase.RemoveObjectFromAsset(selectedFontAsset.atlasTexture);

      // 设置新贴图
      selectedFontAsset.atlasTextures[0] = optimizedTexture;
      selectedFontAsset.material.mainTexture = optimizedTexture;

      AssetDatabase.SaveAssets();
      EditorUtility.DisplayDialog("优化完成", $"优化后的贴图已保存至: {newTexturePath}", "确定");
    }
    catch (System.Exception e)
    {
      EditorUtility.DisplayDialog("错误", $"优化过程中发生错误: {e.Message}", "确定");
      Debug.LogError(e);
    }
  }
}