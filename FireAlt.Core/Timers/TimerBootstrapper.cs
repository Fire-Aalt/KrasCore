using FireAlt.Core.Utility;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace FireAlt.Core.Timers
{
    internal static class TimerBootstrapper
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            PlayerLoopUtils.AddPlayerLoopSystem<Update>(typeof(TimerManager), TimerManager.UpdateTimers,
                TimerManager.Clear);
        }
    }
}