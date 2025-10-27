using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "GradientTexture", menuName = "Gradient Texture", order = 310)]
public class GradientTexture : ScriptableObject
{
    [Delayed]
    public int width = 64;
    
    public List<Gradient> gradients = new List<Gradient>();
    private Texture2D _texture;
    
    private void OnEnable()
    {
        if(_texture == null && gradients.Count > 0)
            Generate();
    }

    [ContextMenu("Generate")]
    private void Generate()
    {
        // create texture that is width x gradients.Count
        // for each gradient, draw a line from left to right with the gradient
        // save to disk
        
        if(_texture != null)
            DestroyImmediate(_texture);
        _texture = new Texture2D(Mathf.Max(1, width), Mathf.Max(1, gradients.Count))
        {
            name = name
        };
        _texture.wrapMode = TextureWrapMode.Clamp;
        _texture.filterMode = FilterMode.Bilinear;
        
        for (int i = 0; i < gradients.Count; i++)
        {
            var gradient = gradients[i];
            for (int x = 0; x < width; x++)
            {
                var t = width > 1 ? (float)x / (width - 1) : 0f;
                var color = gradient.Evaluate(t);
                _texture.SetPixel(x, i, color);
            }
        }
        _texture.Apply();
        
        #if UNITY_EDITOR
        // save/update the embedded preview texture immediately to avoid referencing a destroyed object later
        SaveIcon();
        #endif
    }

    private void OnValidate()
    {
        if(gradients.Count > 0)
            Generate();
    }

    [ContextMenu("Save")]
    private void SaveToDisk()
    {
#if UNITY_EDITOR
        // save to disk the texture using the AssetDatabase API
        if (_texture == null)
            return;
        var path = UnityEditor.AssetDatabase.GetAssetPath(this);
        path = path.Replace(".asset", ".png");
        var bytes = _texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();

#endif
    }
    
    #if UNITY_EDITOR

    private void SaveIcon()
    {
        // use _texture as an embedded preview for this asset
        if (_texture == null)
            return;

        var path = UnityEditor.AssetDatabase.GetAssetPath(this);
        if (string.IsNullOrEmpty(path))
            return; // asset not on disk yet

        // find existing embedded Texture2D (if any)
        var existing = UnityEditor.AssetDatabase
            .LoadAllAssetRepresentationsAtPath(path)
            .OfType<Texture2D>()
            .FirstOrDefault();

        _texture.name = name + "_Preview";
        _texture.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

        if (existing == null)
        {
            // Add using path overload to avoid referencing a possibly destroyed 'this'
            UnityEditor.AssetDatabase.AddObjectToAsset(_texture, path);
        }
        else
        {
            UnityEditor.EditorUtility.CopySerialized(_texture, existing);
        }

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.AssetDatabase.Refresh();
    }
    
    #endif
    
}
