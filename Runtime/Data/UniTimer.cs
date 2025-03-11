using Unity.Core;

namespace KrasCore
{
    public struct UniTimer
    {
        public float CurrentTime;
        public bool IsRunning;

        private float _initialTime;
        private float _duration;

        public void Start(float duration)
        {
            Reset();
            _duration = duration;
            IsRunning = true;
        }
        
        public void Stop()
        {
            if (IsRunning)
            {
                IsRunning = false;
            }
        }

        public void Resume() => IsRunning = true;
        public void Pause() => IsRunning = false;

        public void Reset() => _initialTime = CurrentTime;
        public float Progress => (CurrentTime - _initialTime) / _duration;
        
        public bool Tick(TimeData timeData)
        {
            return Tick(timeData.DeltaTime);
        }

        public bool Tick(float deltaTime)
        {
            if (IsRunning && CurrentTime - _initialTime < _duration)
            {
                CurrentTime += deltaTime;
            }
            else if (IsRunning)
            {
                IsRunning = false;
                return true;
            }
            return false;
        }
    }
}