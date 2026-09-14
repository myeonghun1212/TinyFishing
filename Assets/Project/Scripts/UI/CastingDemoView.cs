using Nan.Core;
using Nan.Fishing;
using UnityEngine;

namespace Nan.UI
{
    public sealed class CastingDemoView : MonoBehaviour
    {
        [SerializeField] private CastingController _casting;

        private void OnGUI()
        {
            if (_casting == null)
                return;

            GUI.Label(new Rect(20, 20, 320, 30), "Casting / " + _casting.State);
            GUI.Label(new Rect(20, 50, 420, 30), "Swipe upward or press Space to cast.");
            if (_casting.State == FishingState.Waiting && GUI.Button(new Rect(20, 85, 160, 40), "Cast again"))
                _casting.ResetCast();
        }
    }
}
