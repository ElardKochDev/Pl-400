using UnityEditor;
using UnityEngine;

// Pixel art nítido: todo PNG de Resources/Art/Tiles se importa SIN compresión,
// SIN mipmaps y con filtro Point. Con los ajustes por defecto (ETC + bilinear)
// los sprites de 16/48 px se veían borrosos en el APK aunque el runtime pusiera
// FilterMode.Point: la compresión ya había machacado los píxeles al importar.
public class PixelArtImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        var path = assetPath.Replace('\\', '/');
        if (path.Contains("Resources/Art/Tiles/"))
        {
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Point;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.alphaIsTransparency = true;
        }
        else if (path.Contains("Resources/Art/Copilot/") || path.Contains("Resources/Art/AB410/"))
        {
            // Diagramas/capturas oficiales de los tomos: máxima nitidez. Con los ajustes por
            // defecto (ETC + reducción a 2048) el texto de las capturas se ve borroso/comprimido.
            var imp = (TextureImporter)assetImporter;
            imp.textureType = TextureImporterType.Default;
            imp.filterMode = FilterMode.Bilinear;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.npotScale = TextureImporterNPOTScale.None;
            imp.maxTextureSize = 2048;
            imp.alphaIsTransparency = true;
        }
    }
}
