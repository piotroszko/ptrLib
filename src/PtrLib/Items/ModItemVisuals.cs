using System;
using Il2Cpp;
using UnityEngine;

namespace PtrLib.Items;

internal static class ModItemVisuals
{
    private static Material? _nativeServerMaterial;
    private static bool _nativeServerSearched;
    private static Renderer? _nativeServerRenderer;
    private static string[]? _nativeOpaqueKeywords;
    private static bool _nativeOpaqueSearched;

    internal static void ResetCache()
    {
        _nativeServerMaterial = null;
        _nativeServerSearched = false;
        _nativeServerRenderer = null;
        _nativeOpaqueKeywords = null;
        _nativeOpaqueSearched = false;
    }

    internal static void MakeOpaque(Renderer renderer)
    {
        if (renderer == null) return;
        try
        {
            var material = renderer.material;
            if (material == null) return;
            renderer.sortingOrder = 0;
            try { renderer.sortingLayerName = "Default"; } catch (Exception) { }

            var serverMaterial = FindNativeServerMaterial();
            if (serverMaterial != null && !material.name.Contains("(Native Server)"))
            {
                var serverClone = CloneServerMaterial(serverMaterial, material);
                renderer.material = serverClone;
                return;
            }

            if (!material.HasProperty("_ZWrite"))
                material = ReplaceWithOpaqueMaterial(renderer, material);

            material.renderQueue = 2000;
            material.SetOverrideTag("RenderType", "Opaque");
            if (material.HasProperty("_ZWrite"))
                material.SetInt("_ZWrite", 1);
            if (material.HasProperty("_ZTest"))
                material.SetInt("_ZTest", 4);
            if (material.HasProperty("_SrcBlend"))
                material.SetInt("_SrcBlend", 1);
            if (material.HasProperty("_DstBlend"))
                material.SetInt("_DstBlend", 0);
            if (material.HasProperty("_Mode"))
                material.SetFloat("_Mode", 0f);
            if (material.HasProperty("_Surface"))
                material.SetFloat("_Surface", 0f);
            if (material.HasProperty("_SurfaceType"))
                material.SetFloat("_SurfaceType", 0f);
            if (material.HasProperty("_BlendMode"))
                material.SetFloat("_BlendMode", 0f);
            if (material.HasProperty("_ZTestTransparent"))
                material.SetFloat("_ZTestTransparent", 4f);
            if (material.HasProperty("_ZTestGBuffer"))
                material.SetFloat("_ZTestGBuffer", 4f);
            if (material.HasProperty("_TransparentZWrite"))
                material.SetFloat("_TransparentZWrite", 1f);
            if (material.HasProperty("_AlphaCutoffEnable"))
                material.SetFloat("_AlphaCutoffEnable", 0f);
            if (material.HasProperty("_CullMode"))
                material.SetFloat("_CullMode", 0f);
            if (material.HasProperty("_CullModeForward"))
                material.SetFloat("_CullModeForward", 0f);
            material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.DisableKeyword("_BLENDMODE_ALPHA");
            material.DisableKeyword("_BLENDMODE_PREMULTIPLY");
            material.DisableKeyword("_BLENDMODE_ADD");
            material.DisableKeyword("_BLENDMODE_MULTIPLY");
            material.DisableKeyword("_ENABLE_FOG_ON_TRANSPARENT");
            material.DisableKeyword("_DEPTHOFFSET_ON");
            material.DisableKeyword("_WRITE_TRANSPARENT_MOTION_VECTOR");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.DisableKeyword("_ALPHATEST_ON");
            ApplyNativeOpaqueKeywords(material);
        }
        catch (Exception)
        {
        }
    }

    internal static string NativeServerInfo()
    {
        var renderer = _nativeServerRenderer;
        var material = _nativeServerMaterial;
        if (renderer == null)
            return "native server: none found";

        var keywords = material != null ? string.Join(",", material.shaderKeywords) : "<none>";
        return $"native server renderer={renderer.name} layer={renderer.gameObject.layer} " +
               $"bounds={renderer.bounds.size} mat={(material != null ? material.name : "<none>")} " +
               $"shader={(material != null && material.shader != null ? material.shader.name : "<none>")} " +
               $"queue={(material != null ? material.renderQueue : -1)} keywords={keywords}";
    }

    private static Material? FindNativeServerMaterial()
    {
        if (!_nativeServerSearched)
        {
            _nativeServerSearched = true;
            foreach (var server in UnityEngine.Object.FindObjectsOfType<Il2Cpp.Server>())
            {
                foreach (var renderer in server.GetComponentsInChildren<Renderer>(true))
                {
                    var candidate = renderer.sharedMaterial;
                    if (candidate == null || candidate.shader == null) continue;
                    if (candidate.renderQueue < 2000 || candidate.renderQueue >= 3000) continue;
                    _nativeServerMaterial = candidate;
                    _nativeServerRenderer = renderer;
                    break;
                }
                if (_nativeServerMaterial != null) break;
            }
        }

        return _nativeServerMaterial;
    }

    private static Material CloneServerMaterial(Material native, Material source)
    {
        var nativeKeywords = native.shaderKeywords;
        var clone = new Material(native);
        clone.name = source.name + " (Native Server)";

        clone.CopyPropertiesFromMaterial(source);

        var depthProperties = new[]
        {
            "_SurfaceType", "_BlendMode", "_ZWrite", "_ZTest", "_ZTestTransparent",
            "_ZTestGBuffer", "_TransparentZWrite", "_CullMode", "_CullModeForward",
            "_AlphaCutoffEnable", "_Stencil", "_StencilRef", "_StencilWriteMask",
            "_StencilReadMask", "_StencilComp", "_StencilPass", "_StencilFail", "_StencilZFail",
        };
        foreach (var property in depthProperties)
        {
            if (native.HasProperty(property) && clone.HasProperty(property))
                clone.SetFloat(property, native.GetFloat(property));
        }
        if (clone.HasProperty("_CullMode"))
            clone.SetFloat("_CullMode", 0f);
        if (clone.HasProperty("_CullModeForward"))
            clone.SetFloat("_CullModeForward", 0f);

        clone.shaderKeywords = nativeKeywords;
        clone.renderQueue = native.renderQueue;
        return clone;
    }

    private static void ApplyNativeOpaqueKeywords(Material material)
    {
        if (!_nativeOpaqueSearched)
        {
            foreach (var renderer in UnityEngine.Object.FindObjectsOfType<Renderer>())
            {
                var candidate = renderer.sharedMaterial;
                if (candidate == null || candidate.shader == null) continue;
                if (candidate.shader.name != "HDRP/Lit") continue;
                if (!candidate.HasProperty("_SurfaceType")) continue;
                if (candidate.GetFloat("_SurfaceType") > 0.5f) continue;
                if (candidate.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")) continue;
                _nativeOpaqueKeywords = candidate.shaderKeywords;
                break;
            }
            _nativeOpaqueSearched = _nativeOpaqueKeywords != null;
        }

        if (_nativeOpaqueKeywords != null)
            material.shaderKeywords = _nativeOpaqueKeywords;
    }

    private static Material ReplaceWithOpaqueMaterial(Renderer renderer, Material source)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Standard")
            ?? Shader.Find("HDRP/Lit");
        if (shader == null) return source;

        var replacement = new Material(shader);
        replacement.name = source.name + " (Opaque)";

        if (source.HasProperty("_Color"))
            replacement.color = source.color;
        else if (source.HasProperty("_BaseColor"))
            replacement.color = source.GetColor("_BaseColor");

        var texture = source.mainTexture;
        if (texture != null)
        {
            if (replacement.HasProperty("_MainTex"))
                replacement.SetTexture("_MainTex", texture);
            if (replacement.HasProperty("_BaseMap"))
                replacement.SetTexture("_BaseMap", texture);
        }

        replacement.renderQueue = 2000;
        replacement.SetOverrideTag("RenderType", "Opaque");
        replacement.SetInt("_ZWrite", 1);
        replacement.SetInt("_ZTest", 4);
        replacement.SetInt("_SrcBlend", 1);
        replacement.SetInt("_DstBlend", 0);
        replacement.SetFloat("_Mode", 0f);
        replacement.SetFloat("_Surface", 0f);
        if (replacement.HasProperty("_SurfaceType"))
            replacement.SetFloat("_SurfaceType", 0f);
        if (replacement.HasProperty("_BlendMode"))
            replacement.SetFloat("_BlendMode", 0f);
        if (replacement.HasProperty("_ZTestTransparent"))
            replacement.SetFloat("_ZTestTransparent", 4f);
        if (replacement.HasProperty("_ZTestGBuffer"))
            replacement.SetFloat("_ZTestGBuffer", 4f);
        if (replacement.HasProperty("_TransparentZWrite"))
            replacement.SetFloat("_TransparentZWrite", 1f);
        replacement.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        replacement.DisableKeyword("_BLENDMODE_ALPHA");
        replacement.DisableKeyword("_BLENDMODE_PREMULTIPLY");
        replacement.DisableKeyword("_BLENDMODE_ADD");
        replacement.DisableKeyword("_BLENDMODE_MULTIPLY");
        ApplyNativeOpaqueKeywords(replacement);
        renderer.material = replacement;
        return replacement;
    }
}
