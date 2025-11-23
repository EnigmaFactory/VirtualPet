using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

/// <summary>
/// Defines cat model types and their Addressable references
/// Handles body types (Simple, Kitten, Stray, Skinny) and quality variants
/// </summary>
[CreateAssetMenu(fileName = "CatModelConfig", menuName = "VirtualPet/Cat Model Config")]
public class CatModelConfig : ScriptableObject
{
    [System.Serializable]
    public class ModelVariant
    {
        public string name;
        public GraphicsQuality quality;
        public AssetReferenceGameObject modelReference;
        public int triCount; // For display/debugging
        public bool useMobileTextures;
    }

    [System.Serializable]
    public class BodyTypeConfig
    {
        public CatBodyType bodyType;
        public ModelVariant[] variants; // High, Medium, Low

        public ModelVariant GetVariantForQuality(GraphicsQuality quality)
        {
            // Find exact match
            foreach (var variant in variants)
            {
                if (variant.quality == quality)
                    return variant;
            }

            // Fallback: return best available
            if (quality == GraphicsQuality.High && variants.Length > 0)
                return variants[0]; // First is usually highest

            // Return medium or low
            foreach (var variant in variants)
            {
                if (variant.quality == GraphicsQuality.Medium)
                    return variant;
            }

            // Last resort: return any
            return variants.Length > 0 ? variants[0] : null;
        }
    }

    [Header("Cat Body Types")]
    public BodyTypeConfig simpleConfig; // CatSimple (default adult cat)
    public BodyTypeConfig kittenConfig; // Kitten (babies)
    public BodyTypeConfig strayConfig;  // CatStray (scruffier)
    public BodyTypeConfig skinnyConfig; // CatSkinny (malnourished/neglect)

    [Header("Color Variants")]
    public CatColorConfig[] colorVariants;

    /// <summary>
    /// Get model variant for body type and current quality
    /// </summary>
    public ModelVariant GetModelVariant(CatBodyType bodyType, GraphicsQuality quality)
    {
        BodyTypeConfig config = bodyType switch
        {
            CatBodyType.Simple => simpleConfig,
            CatBodyType.Kitten => kittenConfig,
            CatBodyType.Stray => strayConfig,
            CatBodyType.Skinny => skinnyConfig,
            _ => simpleConfig
        };

        return config?.GetVariantForQuality(quality);
    }

    /// <summary>
    /// Get color configuration
    /// </summary>
    public CatColorConfig GetColorConfig(string colorId)
    {
        foreach (var color in colorVariants)
        {
            if (color.colorId == colorId)
                return color;
        }

        return colorVariants.Length > 0 ? colorVariants[0] : null;
    }
}

/// <summary>
/// Cat body types from Red Deer pack
/// </summary>
public enum CatBodyType
{
    Simple,  // CatSimple - standard adult house cat
    Kitten,  // KittenSimple - baby cat (grows into Simple)
    Stray,   // CatStray - street cat look (scruffier)
    Skinny   // CatSkinny - malnourished (gains weight → Simple)
}

/// <summary>
/// Color/coat variants with textures
/// </summary>
[System.Serializable]
public class CatColorConfig
{
    public string colorId;         // e.g., "tabby", "calico", "black"
    public string displayName;     // e.g., "Tabby"

    [Header("Regular Textures")]
    public Material regularMaterial;

    [Header("Mobile Textures")]
    public Material mobileMaterial;

    [Header("Visual Info")]
    public Sprite previewSprite;   // For UI
    public Color uiTintColor;      // For icons

    public Material GetMaterialForQuality(GraphicsQuality quality)
    {
        return quality == GraphicsQuality.Low ? mobileMaterial : regularMaterial;
    }
}
