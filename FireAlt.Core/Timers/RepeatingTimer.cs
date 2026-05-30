using System;
using UnityEngine;

namespace FireAlt.Core.Timers
{
    /// <summary>
    /// Timer that counts in intervals that can be changed on the fly.
    /// </summary>
    public class RepeatingTimer : Timer
    {
        public Action OnIntervalDone = delegate { };

        private float intervalDuration;

        public override float Progress => Mathf.Clamp(CurrentTime / intervalDuration, 0, 1);

        public RepeatingTimer(float intervalDuration = 0, bool isManual = false, bool useUnscaledTime = false) : base(isManual, useUnscaledTime) 
        {
            Reset(intervalDuration);
        }

        public void Start(float intervalDuration)
        {
            Reset(intervalDuration);
            Start();
        }

        public override void Tick()
        {
            Tick(DeltaTime);
        }

        public override void Tick(float deltaTime)
        {
            if (IsRunning && CurrentTime < intervalDuration)
            {
                CurrentTime += deltaTime;
            }

            if (IsRunning && CurrentTime >= intervalDuration)
            {
                CurrentTime -= intervalDuration;
                OnIntervalDone.Invoke();
            }
        }

        public void Reset(float newIntervalDuration)
        {
            intervalDuration = newIntervalDuration;
            Reset();
        }

        public override bool IsFinished => false;
    }
}
