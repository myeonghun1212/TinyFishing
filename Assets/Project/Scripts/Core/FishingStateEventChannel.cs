using System;
using UnityEngine;

namespace Nan.Core
{
    [CreateAssetMenu(menuName = "Nan/Events/Fishing State")]
    public sealed class FishingStateEventChannel : ScriptableObject
    {
        public event Action<FishingState> Raised;

        public void Raise(FishingState state)
        {
            Raised?.Invoke(state);
        }
    }
}
