using System;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WordGame.Utilities
{
    public static class ImagePicker
    {
        public static void PickImage(Action<Texture2D, byte[]> onImagePicked, Action onCancelled = null)
        {
#if UNITY_EDITOR
            PickImageEditor(onImagePicked, onCancelled);
#elif UNITY_ANDROID
            PickImageAndroid(onImagePicked, onCancelled);
#elif UNITY_IOS
            PickImageIOS(onImagePicked, onCancelled);
#else
            PickImageStandalone(onImagePicked, onCancelled);
#endif
        }

#if UNITY_EDITOR
        private static void PickImageEditor(Action<Texture2D, byte[]> onImagePicked, Action onCancelled)
        {
            var path = EditorUtility.OpenFilePanel("Select Avatar Image", "", "png,jpg,jpeg");

            if (string.IsNullOrEmpty(path))
            {
                onCancelled?.Invoke();
                return;
            }

            LoadImageFromPath(path, onImagePicked, onCancelled);
        }
#endif

        private static void PickImageStandalone(Action<Texture2D, byte[]> onImagePicked, Action onCancelled)
        {
            // For Windows standalone builds, use file dialog via System.Windows.Forms
            // This is a simplified version - in production you'd want to use a proper file dialog library

            try
            {
                // Simple file path input (fallback for now)
                Debug.Log("Please use NativeFilePicker package for standalone file picking");
                onCancelled?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Image picker error: {ex.Message}");
                onCancelled?.Invoke();
            }
        }

        private static void PickImageAndroid(Action<Texture2D, byte[]> onImagePicked, Action onCancelled)
        {
            // For Android, you need NativeGallery or similar plugin
            // Placeholder for now
            Debug.Log("Android image picking requires NativeGallery plugin");
            onCancelled?.Invoke();
        }

        private static void PickImageIOS(Action<Texture2D, byte[]> onImagePicked, Action onCancelled)
        {
            // For iOS, you need NativeGallery or similar plugin
            // Placeholder for now
            Debug.Log("iOS image picking requires NativeGallery plugin");
            onCancelled?.Invoke();
        }

        private static void LoadImageFromPath(string path, Action<Texture2D, byte[]> onImagePicked, Action onCancelled)
        {
            try
            {
                var fileData = File.ReadAllBytes(path);
                var texture = new Texture2D(2, 2);

                if (texture.LoadImage(fileData))
                {
                    // Resize if too large
                    var resized = ResizeTexture(texture, 512, 512);
                    var bytes = resized.EncodeToPNG();

                    onImagePicked?.Invoke(resized, bytes);
                }
                else
                {
                    Debug.LogError("Failed to load image");
                    onCancelled?.Invoke();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load image: {ex.Message}");
                onCancelled?.Invoke();
            }
        }

        private static Texture2D ResizeTexture(Texture2D source, int maxWidth, int maxHeight)
        {
            int width = source.width;
            int height = source.height;

            // Calculate new size maintaining aspect ratio
            float ratio = Mathf.Min((float)maxWidth / width, (float)maxHeight / height);

            if (ratio >= 1)
            {
                // Already smaller than max size
                return source;
            }

            int newWidth = Mathf.RoundToInt(width * ratio);
            int newHeight = Mathf.RoundToInt(height * ratio);

            var result = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);

            // Simple resize using GetPixel/SetPixel (not the most efficient but works)
            for (int y = 0; y < newHeight; y++)
            {
                for (int x = 0; x < newWidth; x++)
                {
                    float u = (float)x / newWidth;
                    float v = (float)y / newHeight;
                    result.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }

            result.Apply();
            return result;
        }

        public static string ConvertToBase64(byte[] imageBytes)
        {
            return Convert.ToBase64String(imageBytes);
        }

        public static byte[] ConvertFromBase64(string base64)
        {
            return Convert.FromBase64String(base64);
        }

        public static Texture2D LoadTextureFromBase64(string base64)
        {
            try
            {
                var bytes = ConvertFromBase64(base64);
                var texture = new Texture2D(2, 2);
                texture.LoadImage(bytes);
                return texture;
            }
            catch
            {
                return null;
            }
        }
    }
}
