using System;

namespace FireAlt.Core.Timers
{
    /// <summary>
    /// Timer that ticks at a specific frequency. (N times per second)
    /// </summary>
    public class FrequencyTimer : Timer
    {
        public int TicksPerSecond { get; private set; }

        public Action<float> OnTick = delegate { };

        private float timeThreshold;

        public FrequencyTimer(int ticksPerSecond, bool isManual = false, bool useUnscaledTime = false) : base(isManual, useUnscaledTime)
        {
            CalculateTimeThreshold(ticksPerSecond);
        }

        public override void Tick()
        {
            Tick(DeltaTime);
        }

        public override void Tick(float deltaTime)
        {
            if (IsRunning && CurrentTime < timeThreshold)
            {
                CurrentTime += deltaTime;
            }

            if (IsRunning && CurrentTime >= timeThreshold)
            {
                CurrentTime -= timeThreshold;
                OnTick.Invoke(timeThreshold);
            }
        }

        public override bool IsFinished => !IsRunning;

        public override void Reset()
        {
            CurrentTime = 0;
        }

        public void Reset(int newTicksPerSecond)
        {
            CalculateTimeThreshold(newTicksPerSecond);
            Reset();
        }

        void CalculateTimeThreshold(int ticksPerSecond)
        {
            TicksPerSecond = ticksPerSecond;
            timeThreshold = 1f / TicksPerSecond;
        }

    }
}
