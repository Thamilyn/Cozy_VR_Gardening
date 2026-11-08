using UnityEngine;

/// <summary>One meadow palette and restrained GPU wind, using the nature pack's existing shaders.</summary>
internal static class GardenNatureStyle
{
    public static void Apply(Material material, Shader flowerLightingShader = null, Shader[] pastelNatureShaders = null)
    {
        string sourceShader = material.shader.name;
        switch (material.shader.name)
        {
            case "Shader Graphs/Grass":
                SetFloat(material, "_Wind_Strength", 0.16f);
                SetFloat(material, "_Wind_Speed", 0.65f);
                SetFloat(material, "_Wind_Gradient", 0.32f);
                SetFloat(material, "_Gradient_Strenght", 1.15f);
                material.SetColor("_Bottom_Color", new Color(0.16f, 0.36f, 0.13f));
                material.SetColor("_Top_Color", new Color(0.48f, 0.68f, 0.24f));
                material.SetColor("_Color_Variation", new Color(0.32f, 0.53f, 0.20f));
                ConfigureMeadowWind(material);
                break;
            case "Shader Graphs/Flower":
                // The pack's original is Unlit: preserve its graph and wind in a Lit variant for day/night.
                if (flowerLightingShader != null) material.shader = flowerLightingShader;
                SetFloat(material, "_Wind_Strength", 0.10f);
                SetFloat(material, "_Wind_Speed", 0.65f);
                SetFloat(material, "_Wind_Gradient", 0.32f);
                ConfigureMeadowWind(material);
                break;
            case "Shader Graphs/Leaves":
                SetFloat(material, "_WindJitter", 0.025f);
                SetFloat(material, "_WindScroll", 0.18f);
                SetFloat(material, "_RotationStrengh", 0.12f);
                SetFloat(material, "_RotationSpeed", 0.018f);
                SetFloat(material, "_RotationScrollSpeed", 0.025f);
                // This is the Fresnel exponent in the pack graph, not an emission intensity.
                SetFloat(material, "_EmissionPower", 3f);
                SetFloat(material, "_Smoothness", 0.08f);
                Color leaves = material.GetColor("_Color");
                // Preserve the landmark's lilac crown while bringing cyan foliage toward the meadow greens.
                if (leaves.g > leaves.r)
                {
                    Color unified = Color.Lerp(leaves, new Color(0.30f, 0.55f, 0.22f, leaves.a), 0.38f);
                    material.SetColor("_Color", unified);
                    material.SetColor("_EmissionColor", unified * 0.7f);
                }
                break;
            case "Shader Graphs/Props":
                SetFloat(material, "_Normal_Strength", 0.45f);
                SetFloat(material, "_Emissive_Intensity", 0f);
                break;
            case "Shader Graphs/Layered":
                // Keep the author's texture layers; soften the normal-map contrast without adding layers.
                SetFloat(material, "_UL_Normal_Intensity", 0.45f);
                break;
        }
        int pastelIndex = sourceShader == "Shader Graphs/Leaves" ? 0 : sourceShader == "Shader Graphs/Layered" ? 1 : sourceShader == "Shader Graphs/Props" ? 2 : -1;
        if (pastelIndex >= 0 && pastelNatureShaders != null && pastelIndex < pastelNatureShaders.Length && pastelNatureShaders[pastelIndex] != null)
            material.shader = pastelNatureShaders[pastelIndex];
    }

    private static void ConfigureMeadowWind(Material material)
    {
        material.SetVector("_Wind_Direction", new Vector4(20f, 14f, 0f, 0f));
        material.SetColor("_Wind_Color", new Color(0.46f, 0.64f, 0.25f, 0.12f));
        SetFloat(material, "_Wind_Line_Scale", 0.025f);
    }

    private static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property)) material.SetFloat(property, value);
    }
}
