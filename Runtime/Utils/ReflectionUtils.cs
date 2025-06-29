using System;
using System.Reflection;

namespace KrasCore
{
    public static class ReflectionUtils
    {
        public static void CallMethod(object targetObject, string methodName, params object[] parameters)
        {
            if (string.IsNullOrEmpty(methodName)) 
                throw new Exception($"Method name is null or empty");
            
            var method = targetObject.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (method == null) 
                throw new Exception($"Method '{methodName}' not found on target object '{targetObject}'.");
            
            method.Invoke(targetObject, parameters);
        }
        
        public static T CallMethodReturn<T>(object targetObject, string methodName, params object[] parameters)
        {
            if (string.IsNullOrEmpty(methodName)) 
                throw new Exception($"Method name is null or empty");
            
            var method = targetObject.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (method == null) 
                throw new Exception($"Method '{methodName}' not found on target object '{targetObject}'.");
            
            return (T)method.Invoke(targetObject, parameters);
        }
        
        public static bool TryCallMethod(object targetObject, string methodName, params object[] parameters)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                return false;
            }
            
            var method = targetObject.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (method == null) 
                throw new Exception($"Method '{methodName}' not found on target object '{targetObject}'.");
            
            method.Invoke(targetObject, parameters);
            return true;
        }
        
        public static bool TryCallMethodReturn<T>(object targetObject, string methodName, out T result, params object[] parameters)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                result = default;
                return false;
            }
            
            var method = targetObject.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (method == null) 
                throw new Exception($"Method '{methodName}' not found on target object '{targetObject}'.");
            
            result = (T)method.Invoke(targetObject, parameters);
            return true;
        }
    }
}