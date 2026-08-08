using System;

namespace TinyFishing.Input
{
    // Abstraction over the physical input for TinyFishing so the game manager
    // never talks to sensors/touch directly.
    public interface ITinyFishingInput
    {
        // Raised when the player shakes the phone (or presses the editor fallback key) to cast.
        event Action CastPerformed;

        // Raised on each discrete tap/click while reeling.
        event Action ReelTapPerformed;

        // Current player horizontal aim direction in the -1..1 range (left/right), driven by phone
        // roll tilt, mouse/touch drag, or keyboard as an editor fallback.
        float Direction { get; }

        // Current player vertical aim direction in the -1..1 range (up/down), driven by phone
        // pitch tilt, mouse/touch drag, or keyboard as an editor fallback.
        float VerticalDirection { get; }

        // True once the input service has a valid baseline to compute tilt from.
        bool IsReady { get; }

        // True while the player currently has the screen/mouse pressed down.
        bool IsPressed { get; }


        // Recenters the tilt baseline to the phone's current resting position.
        void Recalibrate();
    }
}
