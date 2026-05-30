namespace FireAlt.Core.Timers
{
    /// <summary>
    /// Timer that goes from 0 to infinity.
    /// </summary>
    public class StopwatchTimer : Timer
    {
        public StopwatchTimer(bool isManual = false, bool useUnscaledTime = false) : base(isManual, useUnscaledTime) { }

        public override void Tick()
        {
            Tick(DeltaTime);
        }

        public override void Tick(float deltaTime)
        {
            if (IsRunning)
            {
                CurrentTime += deltaTime;
            }
        }

        public override bool IsFinished => false;
    }
}
