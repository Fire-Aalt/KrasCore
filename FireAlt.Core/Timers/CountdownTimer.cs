using System;

namespace FireAlt.Core.Timers
{
    /// <summary>
    /// Timer that counts down from a specific value to zero.
    /// </summary>
    public class CountdownTimer : Timer
    {
        public Action OnTimerFinished = delegate { };

        public CountdownTimer(float duration = 0, bool isManual = false, bool useUnscaledTime = false) : base(isManual, useUnscaledTime) 
        {
            Reset(duration);
        }

        public void Start(float duration)
        {
            Reset(duration);
            Start();
        }

        public override void Tick()
        {
            Tick(DeltaTime);
        }

        public override void Tick(float deltaTime)
        {
            if (IsRunning && CurrentTime > 0)
            {
                CurrentTime -= deltaTime;
            }

            if (IsRunning && CurrentTime <= 0)
            {
                Stop();
                OnTimerFinished.Invoke();
            }
        }

        public void Reset(float newDuration)
        {
            initialTime = newDuration;
            Reset();
        }

        public override bool IsFinished => CurrentTime <= 0;
    }
}
