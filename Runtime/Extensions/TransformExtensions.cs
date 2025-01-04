using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace KrasCore
{
    public static class TransformExtensions
    {
        public static bool TryRemoveComponent<T>(this Transform transform) where T : Component
        {
            if (transform == null)
                return false;
            
            if (transform.TryGetComponent<T>(out var comp))
            {
                Object.DestroyImmediate(comp);
                return true;
            }
            return false;
        }
        
        public static bool TryRemoveComponent(this Transform transform, Type type)
        {
            if (transform == null)
                return false;

            var comp = transform.GetComponent(type);
            if (comp != null)
            {
                Object.DestroyImmediate(comp);
                return true;
            }
            return false;
        }
        
        public static T GetOrAddComponent<T>(this Transform transform) where T : Component
        {
            if (transform == null)
                return null;

            var result = transform.GetComponent<T>();
            if (result == null)
                result = transform.gameObject.AddComponent<T>();
            
            return result;
        }
        
        public static Component GetOrAddComponent(this Transform transform, Type type)
        {
            if (transform == null)
                return null;

            var result = transform.GetComponent(type);
            if (result == null)
                result = transform.gameObject.AddComponent(type);
            
            return result;
        }
    }
}