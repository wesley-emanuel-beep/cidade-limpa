using UnityEditor;
using UnityEngine;

namespace CidadeLimpa.EditorTools
{
    /// <summary>
    /// Configura automaticamente as texturas de identidade (logo, ícone, ícones de
    /// UI) como Sprite ao serem importadas, para que possam ser usadas pela UI e
    /// carregadas via Resources sem ajuste manual no Inspector.
    /// </summary>
    public class ArteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string p = assetPath.Replace('\\', '/');
            bool ehArte = p.Contains("/_CidadeLimpa/Art/")
                       || p.Contains("/_CidadeLimpa/Resources/");
            if (!ehArte) return;

            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 100f;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
