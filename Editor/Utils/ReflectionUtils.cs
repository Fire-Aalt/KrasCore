using System;
using System.Reflection;

namespace KrasCore.Editor
{
    public class ReflectionUtils
    {
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
    }
}