using System;

namespace Nan.Core
{
    /// <summary>Runtime state owned by one fishing session, never by a shared asset.</summary>
    public sealed class FishingSession
    {
        public FishingState State { get; private set; } = FishingState.Ready;
        public event Action<FishingState> StateChanged;

        public bool TryBeginCast()
        {
            if (State != FishingState.Ready)
                return false;

            SetState(FishingState.Casting);
            return true;
        }

        public bool TryCompleteCast()
        {
            if (State != FishingState.Casting)
                return false;

            SetState(FishingState.Waiting);
            return true;
        }

        public void Reset()
        {
            SetState(FishingState.Ready);
        }

        private void SetState(FishingState state)
        {
            if (State == state)
                return;

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
