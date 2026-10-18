#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Arregla los materiales ROSADOS de paquetes hechos para el render viejo (Nature/Tree, Legacy, Standard...).
// Los pasa a "Universal Render Pipeline/Lit" conservando la textura, el color y el mapa de relieve.
// Las hojas quedan con recorte (Alpha Clipping) y visibles por las dos caras.
//
// USO: menu de arriba  Grumete > Convertir materiales rosados a URP
//   - Si tienes una carpeta seleccionada en la ventana Project, convierte solo esa carpeta.
//   - Si no, convierte todo el proyecto.
// Este archivo DEBE estar dentro de una carpeta llamada "Editor".
public static class ConvertirMaterialesURP
{
    [MenuItem("Grumete/Convertir materiales rosados a URP")]
    static void Convertir()
    {
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        if (!lit) { EditorUtility.DisplayDialog("Convertir a URP", "No encontré el shader URP/Lit. ¿El proyecto usa URP?", "OK"); return; }

        string[] carpetas = Selection.assetGUIDs.Select(AssetDatabase.GUIDToAssetPath).Where(AssetDatabase.IsValidFolder).ToArray();
        if (carpetas.Length == 0) carpetas = new[] { "Assets" };

        var guids = AssetDatabase.FindAssets("t:Material", carpetas);
        int convertidos = 0;
        try
        {
            for (int i = 0; i < guids.Length; i++)
            {
                string ruta = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Convertir a URP", ruta, (float)i / guids.Length);
                var m = AssetDatabase.LoadAssetAtPath<Material>(ruta);
                if (!m || !m.shader) continue;
                string sh = m.shader.name;
                if (sh.StartsWith("Universal Render Pipeline") || sh.StartsWith("Shader Graphs") || sh.StartsWith("UI/") || sh.StartsWith("Sprites/") || sh.StartsWith("Skybox")) continue;
                bool viejo = sh.StartsWith("Nature/") || sh.StartsWith("Legacy Shaders/") || sh == "Standard" || sh == "Standard (Specular setup)"
                             || !m.shader.isSupported;
                if (!viejo) continue;

                // guardar lo importante antes de cambiar el shader
                Texture tex = m.HasProperty("_MainTex") ? m.GetTexture("_MainTex") : null;
                Color col = m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white;
                Texture normal = m.HasProperty("_BumpMap") ? m.GetTexture("_BumpMap") : null;
                Vector2 escala = m.HasProperty("_MainTex") ? m.GetTextureScale("_MainTex") : Vector2.one;
                Vector2 desfase = m.HasProperty("_MainTex") ? m.GetTextureOffset("_MainTex") : Vector2.zero;
                float corte = m.HasProperty("_Cutoff") ? m.GetFloat("_Cutoff") : 0.5f;
                string nombre = m.name.ToLower();
                bool hojas = sh.Contains("Leaves") || sh.Contains("Cutout") || sh.Contains("Transparent")
                             || nombre.Contains("leaf") || nombre.Contains("leaves") || nombre.Contains("hoja") || nombre.Contains("foliage")
                             || nombre.Contains("grass") || nombre.Contains("bush") || nombre.Contains("fern") || nombre.Contains("needle");

                Undo.RecordObject(m, "Convertir a URP");
                m.shader = lit;
                if (tex) { m.SetTexture("_BaseMap", tex); m.SetTextureScale("_BaseMap", escala); m.SetTextureOffset("_BaseMap", desfase); }
                m.SetColor("_BaseColor", col);
                if (normal) { m.SetTexture("_BumpMap", normal); m.EnableKeyword("_NORMALMAP"); }
                m.SetFloat("_Smoothness", 0.1f);
                m.SetFloat("_Metallic", 0f);

                if (hojas)
                {
                    m.SetFloat("_Surface", 0f);            // opaco
                    m.SetFloat("_AlphaClip", 1f);          // recorta lo transparente de la textura
                    m.SetFloat("_Cutoff", Mathf.Clamp(corte, 0.2f, 0.8f));
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", (float)CullMode.Off);   // las hojas se ven por los dos lados
                    m.SetOverrideTag("RenderType", "TransparentCutout");
                    m.renderQueue = (int)RenderQueue.AlphaTest;
                }
                EditorUtility.SetDirty(m);
                convertidos++;
            }
        }
        finally { EditorUtility.ClearProgressBar(); }

        AssetDatabase.SaveAssets();
        EditorUtility.DisplayDialog("Convertir a URP", "Listo: " + convertidos + " materiales convertidos a URP/Lit\n(revisados " + guids.Length + " en " + string.Join(", ", carpetas) + ").", "OK");
    }
}
#endif
