using System;
using UnityEngine;

namespace Nan.Input
{
    [CreateAssetMenu(menuName = "Nan/Events/Casting")]
    public sealed class CastingEventChannel : ScriptableObject
    {
        public event Action Raised;

        [ContextMenu("Raise")]
        public void Raise()
        {
            Raised?.Invoke();
        }
    }
}
