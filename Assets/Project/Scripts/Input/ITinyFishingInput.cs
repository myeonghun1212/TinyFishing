using System;
using TinyFishing.Data;

namespace TinyFishing.Input
{
    public enum TinyFishingInputState
    {
        TouchScreen,
        Gyro
    }

    // Abstraction over the physical input for TinyFishing so the game manager
    // never talks to sensors/touch directly.
    public interface ITinyFishingInput
    {
        // Raised when the player casts. The argument is the input's relative cast strength
        // normalized to 0..1, regardless of whether it came from touch, gyro, or a fallback key.
        event Action<float> CastPerformed;

        // Raised on each discrete tap/click while reeling.
        event Action ReelTapPerformed;

        // Raised when the player attempts to hook a biting fish. In touch-screen mode,
        // the first finger can hook even though it remains the primary aiming pointer.
        event Action HookAttemptPerformed;

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

        // False while gameplay input is suspended by pause/game-over. UI input is handled
        // separately by Unity's EventSystem and remains available.
        bool IsInputEnabled { get; }

        // The active physical-input route. Lobby/settings UI can switch this later without
        // needing to know how the service handles the individual devices.
        TinyFishingInputState InputState { get; }

        // Applies the config selected for the active game mode. This keeps gyro/touch
        // sensitivity and casting thresholds in sync with the fishing rules.
        void Configure(TinyFishingConfig fishingConfig);

        void SetInputState(TinyFishingInputState state);

        // Recenters the tilt baseline to the phone's current resting position.
        void Recalibrate();

        // Clears transient aim/pointer input without changing the gyro tilt baseline.
        void ResetAim();

        // Suspends or resumes all gameplay input. Disabling preserves the current aim so
        // rod/camera visuals can freeze in place; enabling recalibrates the current device
        // attitude as the new neutral pose.
        void SetInputEnabled(bool enabled);
    }
}
