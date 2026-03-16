using System;
using System.Collections.Generic;
using System.IO;
using ArtificeToolkit.Attributes;
using UnityEditor;
using UnityEngine;

namespace KrasCore.Editor
{
    public class NormalMapperWindow : Artifice_EditorWindow
    {
        private const string NormalPostfix = "_n";
        private string previousSourcePath;

        public enum HeightSourceMode
        {
            Luminance,
            Red,
            Green,
            Blue,
            Alpha,
            MaxRGB,
            AverageRGB,
        }

        public enum GradientKernelMode
        {
            Sobel,
            Scharr,
        }

        public enum BorderSampleMode
        {
            Clamp,
            Repeat,
        }

        [PreviewSprite] public Texture2D sourceTexture;
        public string exportPath;
        public bool overwriteExisting = true;

        public HeightSourceMode heightSource = HeightSourceMode.Luminance;
        public GradientKernelMode gradientKernel = GradientKernelMode.Scharr;
        public BorderSampleMode borderSample = BorderSampleMode.Clamp;

        [UnityEngine.Range(0.05f, 20f)] public float normalStrength = 5f;
        [UnityEngine.Range(0.1f, 6f)] public float heightContrast = 1.25f;
        [UnityEngine.Range(-1f, 1f)] public float heightBias = 0f;
        [UnityEngine.Range(0, 24)] public int blurRadius = 1;
        [UnityEngine.Range(0, 256)] public int edgePaddingPixels = 96;
        [UnityEngine.Range(0, 1)] public float alphaThreshold = 0.02f;
        public bool alphaWeightedGradients = true;
        [UnityEngine.Range(0f, 4f)] public float alphaFadePower = 1.5f;
        [UnityEngine.Range(1, 8)] public int detailLevels = 4;
        [UnityEngine.Range(0.1f, 1f)] public float detailFalloff = 0.6f;
        [UnityEngine.Range(0.1f, 4f)] public float detailScale = 1f;

        public bool invertX;
        public bool invertY;
        public bool preserveSourceAlpha = true;

        public FilterMode outputFilterMode = FilterMode.Bilinear;
        public TextureWrapMode outputWrapMode = TextureWrapMode.Clamp;
        public TextureImporterCompression outputCompression = TextureImporterCompression.Uncompressed;
        public bool outputMipMaps;

        [MenuItem("Tools/Normal Mapper")]
        public static void OpenWindow() => GetWindow<NormalMapperWindow>();

        public override void CreateGUI()
        {
            base.CreateGUI();
            rootVisualElement.schedule.Execute(SyncDefaultExportPath).Every(250);
        }

        [Button]
        public void UseDefaultExportPath()
        {
            string sourcePath = GetSourceAssetPath();
            if (string.IsNullOrEmpty(sourcePath))
            {
                return;
            }

            exportPath = BuildDefaultExportPath(sourcePath);
        }

        [Button]
        public void CreateNormalMapAsset()
        {
            string sourcePath = GetSourceAssetPath();
            if (sourceTexture == null || string.IsNullOrEmpty(sourcePath))
            {
                Debug.LogWarning("[Normal Mapper] Select a source texture first.");
                return;
            }

            exportPath = NormalizeExportPath(sourcePath, exportPath);
            if (!exportPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                Debug.LogWarning($"[Normal Mapper] Export path must be in Assets. Path: {exportPath}");
                return;
            }

            if (!overwriteExisting && File.Exists(exportPath))
            {
                Debug.LogWarning($"[Normal Mapper] File already exists and overwrite is disabled. Path: {exportPath}");
                return;
            }

            string directory = Path.GetDirectoryName(exportPath);
            if (string.IsNullOrEmpty(directory))
            {
                Debug.LogWarning($"[Normal Mapper] Invalid export path. Path: {exportPath}");
                return;
            }

            Directory.CreateDirectory(directory);
            Texture2D readableSource = CopyToReadableTexture(sourceTexture);
            Texture2D normalTexture = CreateNormalMap(readableSource);

            byte[] encodedData = EncodeTexture(normalTexture, exportPath);
            if (encodedData == null)
            {
                DestroyImmediate(readableSource);
                DestroyImmediate(normalTexture);
                return;
            }

            File.WriteAllBytes(exportPath, encodedData);
            AssetDatabase.ImportAsset(exportPath, ImportAssetOptions.ForceUpdate);
            ConfigureImportedNormalTexture(exportPath, sourceTexture.width, sourceTexture.height);

            DestroyImmediate(readableSource);
            DestroyImmediate(normalTexture);

            Texture2D createdTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(exportPath);
            EditorGUIUtility.PingObject(createdTexture);
        }

        private void SyncDefaultExportPath()
        {
            string sourcePath = GetSourceAssetPath();
            if (sourcePath == previousSourcePath)
            {
                return;
            }

            string previousDefault = BuildDefaultExportPath(previousSourcePath);
            bool useDefaultPath = string.IsNullOrWhiteSpace(exportPath) || exportPath == previousDefault;
            previousSourcePath = sourcePath;

            if (useDefaultPath && !string.IsNullOrEmpty(sourcePath))
            {
                exportPath = BuildDefaultExportPath(sourcePath);
                Repaint();
            }
        }

        private string GetSourceAssetPath()
        {
            if (sourceTexture == null)
            {
                return string.Empty;
            }

            return AssetDatabase.GetAssetPath(sourceTexture).Replace('\\', '/');
        }

        private string BuildDefaultExportPath(string sourcePath)
        {
            if (string.IsNullOrEmpty(sourcePath))
            {
                return string.Empty;
            }

            string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(directory))
            {
                directory = "Assets";
            }

            string fileName = Path.GetFileNameWithoutExtension(sourcePath);
            string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (!IsSupportedExportExtension(extension))
            {
                extension = ".png";
            }

            return $"{directory}/{fileName}{NormalPostfix}{extension}";
        }

        private static string NormalizeExportPath(string sourcePath, string requestedPath)
        {
            string path = requestedPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                path = sourcePath;
            }

            path = path.Replace('\\', '/').Trim();
            if (path == sourcePath)
            {
                string directory = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
                string fileName = Path.GetFileNameWithoutExtension(sourcePath);
                string extension = Path.GetExtension(sourcePath).ToLowerInvariant();
                if (!IsSupportedExportExtension(extension))
                {
                    extension = ".png";
                }

                return $"{directory}/{fileName}{NormalPostfix}{extension}";
            }

            string requestedExtension = Path.GetExtension(path).ToLowerInvariant();
            if (!IsSupportedExportExtension(requestedExtension))
            {
                path = Path.ChangeExtension(path, ".png");
            }

            return path.Replace('\\', '/');
        }

        private static bool IsSupportedExportExtension(string extension)
        {
            return extension == ".png" || extension == ".tga";
        }

        private static Texture2D CopyToReadableTexture(Texture2D texture)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
            RenderTexture previous = RenderTexture.active;
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;

            Texture2D readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false, false);
            readable.ReadPixels(new Rect(0, 0, texture.width, texture.height), 0, 0);
            readable.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            return readable;
        }

        private static byte[] EncodeTexture(Texture2D texture, string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            switch (extension)
            {
                case ".tga":
                    return texture.EncodeToTGA();
                case ".png":
                    return texture.EncodeToPNG();
                default:
                    Debug.LogWarning($"[Normal Mapper] Unsupported extension {extension}. Use .png or .tga.");
                    return null;
            }
        }

        private Texture2D CreateNormalMap(Texture2D source)
        {
            int width = source.width;
            int height = source.height;
            Color[] sourcePixels = source.GetPixels();

            float[] alphaMap = BuildAlphaMap(sourcePixels);
            float[] heightMap = BuildHeightMap(sourcePixels, alphaMap);
            if (edgePaddingPixels > 0)
            {
                heightMap = DilateHeightIntoTransparent(heightMap, alphaMap, width, height, edgePaddingPixels, borderSample, alphaThreshold);
            }

            List<float[]> heightLevels = BuildHeightLevels(heightMap, width, height);
            Color[] normalPixels = new Color[sourcePixels.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = x + (y * width);
                    float gradientX = 0f;
                    float gradientY = 0f;
                    float levelWeight = 1f;

                    for (int level = 0; level < heightLevels.Count; level++)
                    {
                        GetGradientAt(heightLevels[level], alphaMap, width, height, x, y, out float gx, out float gy);
                        gradientX += gx * levelWeight;
                        gradientY += gy * levelWeight;
                        levelWeight *= detailFalloff;
                    }

                    gradientX *= detailScale;
                    gradientY *= detailScale;

                    float nx = -gradientX * normalStrength;
                    float ny = -gradientY * normalStrength;

                    if (invertX)
                    {
                        nx = -nx;
                    }

                    if (invertY)
                    {
                        ny = -ny;
                    }

                    float alphaWeight = GetAlphaWeight(alphaMap[index], alphaThreshold, alphaFadePower);
                    nx *= alphaWeight;
                    ny *= alphaWeight;

                    Vector3 normal = new Vector3(nx, ny, 1f).normalized;
                    float alpha = preserveSourceAlpha ? sourcePixels[index].a : 1f;
                    normalPixels[index] = new Color((normal.x * 0.5f) + 0.5f, (normal.y * 0.5f) + 0.5f, (normal.z * 0.5f) + 0.5f, alpha);
                }
            }

            Texture2D normalTexture = new Texture2D(width, height, TextureFormat.RGBA32, false, true);
            normalTexture.SetPixels(normalPixels);
            normalTexture.Apply();
            return normalTexture;
        }

        private List<float[]> BuildHeightLevels(float[] heightMap, int width, int height)
        {
            List<float[]> levels = new List<float[]>(detailLevels);
            int baseRadius = Mathf.Max(0, blurRadius);
            float[] baseLevel = baseRadius > 0 ? BlurHeightMap(heightMap, width, height, baseRadius, borderSample) : CopyArray(heightMap);
            levels.Add(baseLevel);

            for (int i = 1; i < detailLevels; i++)
            {
                int radius = baseRadius + (i * 2);
                float[] level = BlurHeightMap(heightMap, width, height, radius, borderSample);
                levels.Add(level);
            }

            return levels;
        }

        private void GetGradientAt(float[] heightMap, float[] alphaMap, int width, int height, int x, int y, out float gradientX, out float gradientY)
        {
            float hTL = SampleValue(heightMap, x - 1, y - 1, width, height, borderSample);
            float hT = SampleValue(heightMap, x, y - 1, width, height, borderSample);
            float hTR = SampleValue(heightMap, x + 1, y - 1, width, height, borderSample);
            float hL = SampleValue(heightMap, x - 1, y, width, height, borderSample);
            float hR = SampleValue(heightMap, x + 1, y, width, height, borderSample);
            float hBL = SampleValue(heightMap, x - 1, y + 1, width, height, borderSample);
            float hB = SampleValue(heightMap, x, y + 1, width, height, borderSample);
            float hBR = SampleValue(heightMap, x + 1, y + 1, width, height, borderSample);

            float aTL = SampleValue(alphaMap, x - 1, y - 1, width, height, borderSample);
            float aT = SampleValue(alphaMap, x, y - 1, width, height, borderSample);
            float aTR = SampleValue(alphaMap, x + 1, y - 1, width, height, borderSample);
            float aL = SampleValue(alphaMap, x - 1, y, width, height, borderSample);
            float aR = SampleValue(alphaMap, x + 1, y, width, height, borderSample);
            float aBL = SampleValue(alphaMap, x - 1, y + 1, width, height, borderSample);
            float aB = SampleValue(alphaMap, x, y + 1, width, height, borderSample);
            float aBR = SampleValue(alphaMap, x + 1, y + 1, width, height, borderSample);

            float wTL = alphaWeightedGradients ? GetAlphaWeight(aTL, alphaThreshold, alphaFadePower) : 1f;
            float wT = alphaWeightedGradients ? GetAlphaWeight(aT, alphaThreshold, alphaFadePower) : 1f;
            float wTR = alphaWeightedGradients ? GetAlphaWeight(aTR, alphaThreshold, alphaFadePower) : 1f;
            float wL = alphaWeightedGradients ? GetAlphaWeight(aL, alphaThreshold, alphaFadePower) : 1f;
            float wR = alphaWeightedGradients ? GetAlphaWeight(aR, alphaThreshold, alphaFadePower) : 1f;
            float wBL = alphaWeightedGradients ? GetAlphaWeight(aBL, alphaThreshold, alphaFadePower) : 1f;
            float wB = alphaWeightedGradients ? GetAlphaWeight(aB, alphaThreshold, alphaFadePower) : 1f;
            float wBR = alphaWeightedGradients ? GetAlphaWeight(aBR, alphaThreshold, alphaFadePower) : 1f;

            if (gradientKernel == GradientKernelMode.Scharr)
            {
                float posX = (hTR * (3f * wTR)) + (hR * (10f * wR)) + (hBR * (3f * wBR));
                float negX = (hTL * (3f * wTL)) + (hL * (10f * wL)) + (hBL * (3f * wBL));
                float posY = (hBL * (3f * wBL)) + (hB * (10f * wB)) + (hBR * (3f * wBR));
                float negY = (hTL * (3f * wTL)) + (hT * (10f * wT)) + (hTR * (3f * wTR));

                float normX = Mathf.Max(0.0001f, (3f * wTR) + (10f * wR) + (3f * wBR) + (3f * wTL) + (10f * wL) + (3f * wBL));
                float normY = Mathf.Max(0.0001f, (3f * wBL) + (10f * wB) + (3f * wBR) + (3f * wTL) + (10f * wT) + (3f * wTR));
                gradientX = (posX - negX) / normX;
                gradientY = (posY - negY) / normY;
            }
            else
            {
                float posX = (hTR * wTR) + (hR * (2f * wR)) + (hBR * wBR);
                float negX = (hTL * wTL) + (hL * (2f * wL)) + (hBL * wBL);
                float posY = (hBL * wBL) + (hB * (2f * wB)) + (hBR * wBR);
                float negY = (hTL * wTL) + (hT * (2f * wT)) + (hTR * wTR);

                float normX = Mathf.Max(0.0001f, wTR + (2f * wR) + wBR + wTL + (2f * wL) + wBL);
                float normY = Mathf.Max(0.0001f, wBL + (2f * wB) + wBR + wTL + (2f * wT) + wTR);
                gradientX = (posX - negX) / normX;
                gradientY = (posY - negY) / normY;
            }
        }

        private static float[] BuildAlphaMap(Color[] sourcePixels)
        {
            float[] alphaMap = new float[sourcePixels.Length];
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                alphaMap[i] = sourcePixels[i].a;
            }

            return alphaMap;
        }

        private float[] BuildHeightMap(Color[] sourcePixels, float[] alphaMap)
        {
            float[] heightMap = new float[sourcePixels.Length];
            for (int i = 0; i < sourcePixels.Length; i++)
            {
                float value = SampleHeightValue(sourcePixels[i], heightSource);
                value = ((value - 0.5f) * heightContrast) + 0.5f + heightBias;
                float alphaWeight = GetAlphaWeight(alphaMap[i], alphaThreshold, alphaFadePower);
                heightMap[i] = Mathf.Lerp(0.5f, Mathf.Clamp01(value), alphaWeight);
            }

            return heightMap;
        }

        private static float SampleHeightValue(Color color, HeightSourceMode mode)
        {
            switch (mode)
            {
                case HeightSourceMode.Red:
                    return color.r;
                case HeightSourceMode.Green:
                    return color.g;
                case HeightSourceMode.Blue:
                    return color.b;
                case HeightSourceMode.Alpha:
                    return color.a;
                case HeightSourceMode.MaxRGB:
                    return Mathf.Max(color.r, color.g, color.b);
                case HeightSourceMode.AverageRGB:
                    return (color.r + color.g + color.b) / 3f;
                default:
                    return color.grayscale;
            }
        }

        private static float[] DilateHeightIntoTransparent(float[] sourceHeight, float[] alphaMap, int width, int height, int maxSteps, BorderSampleMode sampleMode, float threshold)
        {
            int length = sourceHeight.Length;
            float[] paddedHeight = CopyArray(sourceHeight);
            bool[] valid = new bool[length];
            bool[] nextValid = new bool[length];
            float[] nextHeight = new float[length];

            for (int i = 0; i < length; i++)
            {
                valid[i] = alphaMap[i] > threshold;
            }

            for (int step = 0; step < maxSteps; step++)
            {
                bool changed = false;
                Array.Copy(paddedHeight, nextHeight, length);
                Array.Copy(valid, nextValid, length);

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int index = x + (y * width);
                        if (valid[index])
                        {
                            continue;
                        }

                        float sum = 0f;
                        float count = 0f;
                        for (int offsetY = -1; offsetY <= 1; offsetY++)
                        {
                            for (int offsetX = -1; offsetX <= 1; offsetX++)
                            {
                                if (offsetX == 0 && offsetY == 0)
                                {
                                    continue;
                                }

                                int sampleX = ResolveCoordinate(x + offsetX, width, sampleMode);
                                int sampleY = ResolveCoordinate(y + offsetY, height, sampleMode);
                                int sampleIndex = sampleX + (sampleY * width);
                                if (!valid[sampleIndex])
                                {
                                    continue;
                                }

                                sum += paddedHeight[sampleIndex];
                                count += 1f;
                            }
                        }

                        if (count <= 0f)
                        {
                            continue;
                        }

                        nextHeight[index] = sum / count;
                        nextValid[index] = true;
                        changed = true;
                    }
                }

                float[] swapHeight = paddedHeight;
                paddedHeight = nextHeight;
                nextHeight = swapHeight;

                bool[] swapValid = valid;
                valid = nextValid;
                nextValid = swapValid;

                if (!changed)
                {
                    break;
                }
            }

            if (HasInvalid(valid))
            {
                FillInvalidWithNearest(paddedHeight, valid, width, height, sampleMode);
            }

            return paddedHeight;
        }

        private static bool HasInvalid(bool[] valid)
        {
            for (int i = 0; i < valid.Length; i++)
            {
                if (!valid[i])
                {
                    return true;
                }
            }

            return false;
        }

        private static void FillInvalidWithNearest(float[] heightMap, bool[] valid, int width, int height, BorderSampleMode sampleMode)
        {
            int length = heightMap.Length;
            int[] nearest = new int[length];
            int[] distance = new int[length];
            for (int i = 0; i < length; i++)
            {
                nearest[i] = -1;
                distance[i] = int.MaxValue;
            }

            Queue<int> queue = new Queue<int>(length);
            for (int i = 0; i < length; i++)
            {
                if (!valid[i])
                {
                    continue;
                }

                nearest[i] = i;
                distance[i] = 0;
                queue.Enqueue(i);
            }

            int[] offsetX = { -1, 1, 0, 0, -1, 1, -1, 1 };
            int[] offsetY = { 0, 0, -1, 1, -1, -1, 1, 1 };

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                int currentX = current % width;
                int currentY = current / width;
                int nextDistance = distance[current] + 1;

                for (int n = 0; n < offsetX.Length; n++)
                {
                    int nx = ResolveCoordinate(currentX + offsetX[n], width, sampleMode);
                    int ny = ResolveCoordinate(currentY + offsetY[n], height, sampleMode);
                    int neighbor = nx + (ny * width);
                    if (nextDistance >= distance[neighbor])
                    {
                        continue;
                    }

                    distance[neighbor] = nextDistance;
                    nearest[neighbor] = nearest[current];
                    queue.Enqueue(neighbor);
                }
            }

            for (int i = 0; i < length; i++)
            {
                if (valid[i] || nearest[i] < 0)
                {
                    continue;
                }

                heightMap[i] = heightMap[nearest[i]];
            }
        }

        private static float GetAlphaWeight(float alpha, float threshold, float power)
        {
            if (alpha <= threshold)
            {
                return 0f;
            }

            float denominator = Mathf.Max(0.0001f, 1f - threshold);
            float normalized = Mathf.Clamp01((alpha - threshold) / denominator);
            return Mathf.Pow(normalized, Mathf.Max(0.0001f, power));
        }

        private static float[] BlurHeightMap(float[] source, int width, int height, int radius, BorderSampleMode sampleMode)
        {
            if (radius <= 0)
            {
                return CopyArray(source);
            }

            float[] kernel = BuildGaussianKernel(radius);
            float[] horizontal = new float[source.Length];
            float[] output = new float[source.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float weightedValue = 0f;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int sampleX = ResolveCoordinate(x + offset, width, sampleMode);
                        float weight = kernel[offset + radius];
                        weightedValue += source[sampleX + (y * width)] * weight;
                    }

                    horizontal[x + (y * width)] = weightedValue;
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float weightedValue = 0f;
                    for (int offset = -radius; offset <= radius; offset++)
                    {
                        int sampleY = ResolveCoordinate(y + offset, height, sampleMode);
                        float weight = kernel[offset + radius];
                        weightedValue += horizontal[x + (sampleY * width)] * weight;
                    }

                    output[x + (y * width)] = weightedValue;
                }
            }

            return output;
        }

        private static float[] BuildGaussianKernel(int radius)
        {
            int size = (radius * 2) + 1;
            float[] kernel = new float[size];
            float sigma = Mathf.Max(0.5f, radius * 0.5f);
            float sigmaSquare2 = 2f * sigma * sigma;
            float sum = 0f;

            for (int i = -radius; i <= radius; i++)
            {
                float value = Mathf.Exp(-(i * i) / sigmaSquare2);
                kernel[i + radius] = value;
                sum += value;
            }

            float inverseSum = 1f / sum;
            for (int i = 0; i < kernel.Length; i++)
            {
                kernel[i] *= inverseSum;
            }

            return kernel;
        }

        private static float SampleValue(float[] values, int x, int y, int width, int height, BorderSampleMode sampleMode)
        {
            int sampleX = ResolveCoordinate(x, width, sampleMode);
            int sampleY = ResolveCoordinate(y, height, sampleMode);
            return values[sampleX + (sampleY * width)];
        }

        private static int ResolveCoordinate(int value, int size, BorderSampleMode sampleMode)
        {
            if (sampleMode == BorderSampleMode.Repeat)
            {
                int wrapped = value % size;
                return wrapped < 0 ? wrapped + size : wrapped;
            }

            return Mathf.Clamp(value, 0, size - 1);
        }

        private void ConfigureImportedNormalTexture(string path, int sourceWidth, int sourceHeight)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.NormalMap;
            importer.convertToNormalmap = false;
            importer.mipmapEnabled = outputMipMaps;
            importer.filterMode = outputFilterMode;
            importer.wrapMode = outputWrapMode;
            importer.textureCompression = outputCompression;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = GetImporterMaxTextureSize(sourceWidth, sourceHeight);
            importer.SaveAndReimport();
        }

        private static int GetImporterMaxTextureSize(int width, int height)
        {
            int maxDimension = Mathf.Max(width, height);
            int[] validSizes = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192, 16384 };

            for (int i = 0; i < validSizes.Length; i++)
            {
                if (validSizes[i] >= maxDimension)
                {
                    return validSizes[i];
                }
            }

            return validSizes[validSizes.Length - 1];
        }

        private static float[] CopyArray(float[] source)
        {
            float[] copy = new float[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
