using System;
using System.Reflection;

namespace KrasCore
{
    public static class ReflectionUtils
    {
        public static void CallMethod(object targetObject, string methodName, params object[] parameters)
        {
            var method = GetCallMethod(targetObject, methodName);

            method.Invoke(targetObject, parameters);
        }

        public static T CallMethodReturn<T>(object targetObject, string methodName, params object[] parameters)
        {
            var method = GetCallMethod(targetObject, methodName);
            
            return (T)method.Invoke(targetObject, parameters);
        }
        
        public static TReturn CallMethodOut<TReturn, TOut>(object targetObject, string methodName, out TOut result, params object[] parameters)
        {
            var method = GetCallMethod(targetObject, methodName);
            
            var newParams = new object[parameters != null ? parameters.Length + 1 : 1];
            
            var ret = (TReturn)method.Invoke(targetObject, newParams);
            result = (TOut)newParams[^1];
            return ret;
        }
        
        public static bool TryCallMethod(object targetObject, string methodName, params object[] parameters)
        {
            if (!TryGetCallMethod(targetObject, methodName, out var method)) return false;

            method.Invoke(targetObject, parameters);
            return true;
        }

        public static bool TryCallMethodReturn<T>(object targetObject, string methodName, out T result, params object[] parameters)
        {
            result = default;
            if (!TryGetCallMethod(targetObject, methodName, out var method)) return false;
            
            result = (T)method.Invoke(targetObject, parameters);
            return true;
        }
        
        private static MethodInfo GetCallMethod(object targetObject, string methodName)
        {
            if (string.IsNullOrEmpty(methodName)) 
                throw new Exception($"Method name is null or empty");
            return GetMethodSignature(targetObject, methodName);
        }

        private static bool TryGetCallMethod(object targetObject, string methodName, out MethodInfo method)
        {
            if (string.IsNullOrEmpty(methodName))
            {
                method = null;
                return false;
            }
            
            method = GetMethodSignature(targetObject, methodName);
            return true;
        }
        
        private static MethodInfo GetMethodSignature(object targetObject, string methodName)
        {
            var method = targetObject.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method == null) 
                throw new Exception($"Method '{methodName}' not found on target object '{targetObject}'.");
            return method;
        }
    }
}