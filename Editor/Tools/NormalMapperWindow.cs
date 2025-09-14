using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using ArtificeToolkit.Attributes;

namespace KrasCore.Editor
{
    public class NormalMapperWindow : Artifice_EditorWindow
    {
        private const string NORMAL_MAP = "_NormalMap";

        public string folder;
        public float normalStrength = 5f;
        [ReadOnly] public int normalSmoothing = 1;

        [PreviewSprite] public Sprite Sprite;
        public Material Material;
        [PreviewSprite] public Texture UsedTexture;
        
        [Button]
        public void GetTexture()
        {
            UsedTexture = Sprite.texture;
            
            Material.SetTexture("_MainTex", UsedTexture);
        }
        
        [MenuItem("Tools/Normal Mapper")]
        public static void OpenWindow() => GetWindow<NormalMapperWindow>();


        [Button]
        public void CreateNormalMaps()
        {
            if (!folder.StartsWith("Assets/"))
            {
                Debug.LogWarning($"{folder} is not in Assets");
                return;
            }

            List<Object> assets = new();
            TryGetUnityObjectsOfTypeFromPath(folder, assets);

            List<(Texture2D, TextureImporter)> toProcess = new();

            foreach (Object asset in assets)
            {
                string assetPath = AssetDatabase.GetAssetPath(asset);
                TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;

                if (importer != null && importer.textureType == TextureImporterType.Sprite)
                {
                    toProcess.Add((asset as Texture2D, importer));
                }
            }

            if (!EditorUtility.DisplayDialog(typeof(NormalMapperWindow).ToString(), $"{toProcess.Count} Sprites where found.\nGenerate NormalMaps for them?", "Generate", "Cancel"))
            {
                return;
            }

            foreach ((Texture2D texture, TextureImporter importer) in toProcess)
            {
                Texture2D normalMap = CreateNormalMapAsset(texture, importer);

                SecondarySpriteTexture[] secondaryTextures = importer.secondarySpriteTextures;

                bool modified = false;
                for (int i = 0; i < secondaryTextures.Length; i++)
                {
                    if (secondaryTextures[i].name == NORMAL_MAP)
                    {
                        AddToSecondaryTextures(normalMap, secondaryTextures, i);
                        modified = true;
                        break;
                    }
                }

                if (!modified)
                {
                    System.Array.Resize(ref secondaryTextures, secondaryTextures.Length + 1);
                    AddToSecondaryTextures(normalMap, secondaryTextures, secondaryTextures.Length - 1);
                }

                importer.secondarySpriteTextures = secondaryTextures;
            }

            foreach (SceneView scene in SceneView.sceneViews)
            {
                scene.ShowNotification(new GUIContent("Created " + toProcess.Count + " NormalMaps"));
            }
        }

        private static void AddToSecondaryTextures(Texture2D normalMap, SecondarySpriteTexture[] secondaryTextures, int i)
        {
            var normalMapRef = new SecondarySpriteTexture()
            {
                name = NORMAL_MAP,
                texture = normalMap
            };

            secondaryTextures[i] = normalMapRef;
        }

        /// <summary>
        /// Adds newly (if not already in the list) found assets.
        /// Returns how many found (not how many added)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="assetsFound">Adds to this list if it is not already there</param>
        /// <returns></returns>
        public static int TryGetUnityObjectsOfTypeFromPath<T>(string path, List<T> assetsFound) where T : Object
        {
            string[] filePaths = Directory.GetFiles(path);
            string[] directoryPaths = Directory.GetDirectories(path);
            int countFound = 0;

            if (filePaths != null && filePaths.Length > 0)
            {
                for (int i = 0; i < filePaths.Length; i++)
                {
                    var obj = AssetDatabase.LoadAssetAtPath(filePaths[i], typeof(T));
                    if (obj is T asset)
                    {
                        assetsFound.Add(asset);
                        countFound++;
                    }
                }
            }

            if (directoryPaths != null && directoryPaths.Length > 0)
            {
                for (int i = 0; i < directoryPaths.Length; i++)
                {
                    countFound += TryGetUnityObjectsOfTypeFromPath(directoryPaths[i], assetsFound);
                }
            }

            return countFound;
        }

        private Texture2D CreateNormalMapAsset(Texture2D texture, TextureImporter tImporter)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);

            if (tImporter != null)
            {
                tImporter.isReadable = true;
                tImporter.SaveAndReimport();
            }

            Texture2D normalToSave = CreateNormalMap(texture, normalStrength, normalSmoothing);

            string extension = Path.GetExtension(assetPath);
            string path = assetPath[..(assetPath.Length - extension.Length)] + "_n" + extension;

            byte[] pngData = normalToSave.EncodeToPNG();
            if (pngData != null) File.WriteAllBytes(path, pngData);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
                importer.textureType = TextureImporterType.NormalMap;
                importer.filterMode = FilterMode.Point;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();

                var resultTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                return resultTexture;
            }
            return null;
        }

        private Texture2D CreateNormalMap(Texture2D t, float normalMult = 5f, int normalSmooth = 0)
        {
            int width = t.width;
            int height = t.height;
            Color[] sourcePixels = t.GetPixels();
            Color[] resultPixels = new Color[width * height];
            Vector3 vScale = new Vector3(0.3333f, 0.3333f, 0.3333f);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + y * width;
                    Vector3 cSampleNegXNegY = GetPixelClamped(sourcePixels, x - 1, y - 1, width, height);
                    Vector3 cSampleZerXNegY = GetPixelClamped(sourcePixels, x, y - 1, width, height);
                    Vector3 cSamplePosXNegY = GetPixelClamped(sourcePixels, x + 1, y - 1, width, height);
                    Vector3 cSampleNegXZerY = GetPixelClamped(sourcePixels, x - 1, y, width, height);
                    Vector3 cSamplePosXZerY = GetPixelClamped(sourcePixels, x + 1, y, width, height);
                    Vector3 cSampleNegXPosY = GetPixelClamped(sourcePixels, x - 1, y + 1, width, height);
                    Vector3 cSampleZerXPosY = GetPixelClamped(sourcePixels, x, y + 1, width, height);
                    Vector3 cSamplePosXPosY = GetPixelClamped(sourcePixels, x + 1, y + 1, width, height);

                    float fSampleNegXNegY = Vector3.Dot(cSampleNegXNegY, vScale);
                    float fSampleZerXNegY = Vector3.Dot(cSampleZerXNegY, vScale);
                    float fSamplePosXNegY = Vector3.Dot(cSamplePosXNegY, vScale);
                    float fSampleNegXZerY = Vector3.Dot(cSampleNegXZerY, vScale);
                    float fSamplePosXZerY = Vector3.Dot(cSamplePosXZerY, vScale);
                    float fSampleNegXPosY = Vector3.Dot(cSampleNegXPosY, vScale);
                    float fSampleZerXPosY = Vector3.Dot(cSampleZerXPosY, vScale);
                    float fSamplePosXPosY = Vector3.Dot(cSamplePosXPosY, vScale);

                    float edgeX = (fSampleNegXNegY - fSamplePosXNegY) * 0.25f + (fSampleNegXZerY - fSamplePosXZerY) * 0.5f + (fSampleNegXPosY - fSamplePosXPosY) * 0.25f;
                    float edgeY = (fSampleNegXNegY - fSampleNegXPosY) * 0.25f + (fSampleZerXNegY - fSampleZerXPosY) * 0.5f + (fSamplePosXNegY - fSamplePosXPosY) * 0.25f;

                    Vector2 vEdge = new Vector2(edgeX, edgeY) * normalMult;
                    Vector3 norm = new Vector3(vEdge.x, vEdge.y, 1.0f).normalized;
                    resultPixels[index] = new Color(norm.x * 0.5f + 0.5f, norm.y * 0.5f + 0.5f, norm.z * 0.5f + 0.5f, 1);
                }
            }

            if (normalSmooth > 0)
            {
                resultPixels = SmoothNormals(resultPixels, width, height, normalSmooth);
            }

            Texture2D texNormal = new Texture2D(width, height, TextureFormat.RGB24, false, false);
            texNormal.SetPixels(resultPixels);
            texNormal.Apply();
            return texNormal;
        }

        private Vector3 GetPixelClamped(Color[] pixels, int x, int y, int width, int height)
        {
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            Color c = pixels[x + y * width];
            return new Vector3(c.r, c.g, c.b);
        }

        private Color[] SmoothNormals(Color[] pixels, int width, int height, int normalSmooth)
        {
            Color[] smoothedPixels = new Color[pixels.Length];
            float step = 0.00390625f * normalSmooth;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float pixelsToAverage = 0.0f;
                    Color c = pixels[x + y * width];
                    pixelsToAverage++;

                    for (int offsetY = -normalSmooth; offsetY <= normalSmooth; offsetY++)
                    {
                        for (int offsetX = -normalSmooth; offsetX <= normalSmooth; offsetX++)
                        {
                            if (offsetX == 0 && offsetY == 0) continue;

                            int sampleX = Mathf.Clamp(x + offsetX, 0, width - 1);
                            int sampleY = Mathf.Clamp(y + offsetY, 0, height - 1);

                            c += pixels[sampleX + sampleY * width];
                            pixelsToAverage++;
                        }
                    }

                    smoothedPixels[x + y * width] = c / pixelsToAverage;
                }
            }

            return smoothedPixels;
        }
    }
}