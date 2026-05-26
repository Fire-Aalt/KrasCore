using System;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace KrasCore.Editor
{
    public static class ScopedEditorPrefs
    {
        private const string KeyPrefix = "krascore.v1";
        
        public static void SetBool(string key, bool value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key must not be null or empty", nameof(key));

            string fullKey = MakeScopedKey(key);
            EditorPrefs.SetInt(fullKey, value ? 1 : 0);
        }
        
        public static bool GetBool(string key, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentException("key must not be null or empty", nameof(key));

            string fullKey = MakeScopedKey(key);
            if (!EditorPrefs.HasKey(fullKey))
                return defaultValue;

            return EditorPrefs.GetInt(fullKey, defaultValue ? 1 : 0) != 0;
        }

        private static string MakeScopedKey(string key)
        {
            string projectHash = HashString(Application.dataPath);
            string userHash = HashString(Environment.UserName);

            return $"{KeyPrefix}.{projectHash}.{userHash}.{SanitizeKey(key)}";
        }

        private static string SanitizeKey(string key)
        {
            return key.Replace(" ", "_").Replace(".", "_");
        }

        private static string HashString(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
    }
}