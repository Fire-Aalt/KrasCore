using KrasCore;
using UnityEditor;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace Game
{
    public static class GlobalUnscaledShaderTimeSystem
    {
        private static readonly int UnscaledTime = Shader.PropertyToID("UnscaledTime");
        
#if UNITY_EDITOR
        [InitializeOnLoadMethod]
#else
        [RuntimeInitializeOnLoadMethod]
#endif
        private static void Initialize()
        {
            PlayerLoopUtils.AddPersistentSystem<Update>(typeof(GlobalUnscaledShaderTimeSystem), UpdateGlobalUnscaledShaderTime);
        }

        private static void UpdateGlobalUnscaledShaderTime()
        {
            Shader.SetGlobalFloat(UnscaledTime, Time.unscaledTime);
        }
    }
}