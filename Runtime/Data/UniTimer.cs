using Mono.CSharp;
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
        
        public void Tick(TimeData timeData)
        {
            Tick(timeData.DeltaTime);
        }
        
        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;
            
            if (CurrentTime - _initialTime < _duration)
            {
                CurrentTime += deltaTime;
                return;
            }

            IsRunning = false;
        }
        
        /// <summary>
        /// Returns true when finished in the same frame
        /// </summary>
        public bool TickUntilFinished(TimeData timeData)
        {
            return TickUntilFinished(timeData.DeltaTime);
        }
        
        /// <summary>
        /// Returns true when finished in the same frame
        /// </summary>
        public bool TickUntilFinished(float deltaTime)
        {
            if (!IsRunning) return false;
            
            if (CurrentTime - _initialTime < _duration)
            {
                CurrentTime += deltaTime;
                return false;
            }

            IsRunning = false;
            return true;
        }
        
        /// <summary>
        /// Returns true when IsRunning is true
        /// </summary>
        public bool TickWhileIsRunning(TimeData timeData)
        {
            return TickWhileIsRunning(timeData.DeltaTime);
        }
        
        /// <summary>
        /// Returns true when IsRunning is true
        /// </summary>
        public bool TickWhileIsRunning(float deltaTime)
        {
            if (!IsRunning) return false;
            
            if (CurrentTime - _initialTime < _duration)
            {
                CurrentTime += deltaTime;
                return true;
            }

            IsRunning = false;
            return false;
        }
    }
}