using System;
using Sandbox;

public sealed class GameState : Component
{
    public enum Phase
    {
        Countdown,
        Playing
    }

    /// <summary>How many seconds to count down from before the match starts.</summary>
    [Property] public float CountdownSeconds { get; set; } = 10f;

    /// <summary>How long to keep the "GO!" message on screen after the countdown hits 0.</summary>
    [Property] public float GoDisplayDuration { get; set; } = 1f;

    /// <summary>Current phase of the match — used by Tile/Platform/etc. to gate their behaviour. Synced from host to clients.</summary>
    /// <remarks>Defaults to Playing so a freshly-joined client doesn't briefly freeze its own player before the host's sync data arrives. The host explicitly flips this to Countdown in StartCountdown().</remarks>
    [Sync] public Phase CurrentPhase { get; private set; } = Phase.Playing;

    /// <summary>Seconds left on the countdown, written by the host each frame and read by clients for the HUD.</summary>
    [Sync] public float SecondsRemaining { get; private set; }

    /// <summary>Host-only countdown timer (negative values are the "GO!" display window).</summary>
    private TimeUntil _phaseEnd;

    /// <summary>Tracks the phase we last saw locally, so each client can detect the Countdown→Playing transition and unfreeze its player exactly once.</summary>
    private Phase _lastAppliedPhase = Phase.Playing;

    /// <summary>Scene-wide singleton so tiles/HUD can look us up without a hard reference.</summary>
    public static GameState Current { get; private set; }

    /// <summary>True once the countdown finished and gameplay should be live. Defaults to true if no GameState exists in the scene (so legacy scenes still work).</summary>
    public static bool IsPlaying => Current == null || Current.CurrentPhase == Phase.Playing;

    /// <summary>Text the HUD should show right now — "10".."1", "GO!", or empty once playing.</summary>
    public string DisplayText
    {
        get
        {
            if ( CurrentPhase != Phase.Countdown )
                return string.Empty;

            int seconds = (int)MathF.Ceiling( SecondsRemaining );
            return seconds > 0 ? seconds.ToString() : "GO!";
        }
    }

    protected override void OnEnabled()
    {
        Current = this;

        // Only the host drives the countdown; clients will receive phase/seconds via [Sync].
        if ( Networking.IsHost )
            StartCountdown();
    }

    protected override void OnDisabled()
    {
        if ( Current == this )
            Current = null;
    }

    public void StartCountdown()
    {
        if ( !Networking.IsHost ) return;

        CurrentPhase = Phase.Countdown;
        _phaseEnd = CountdownSeconds;
        SecondsRemaining = CountdownSeconds;
    }

    protected override void OnUpdate()
    {
        // Host drives the countdown timing and phase transitions.
        if ( Networking.IsHost && CurrentPhase == Phase.Countdown )
        {
            SecondsRemaining = MathF.Max( 0f, (float)_phaseEnd );

            if ( (float)_phaseEnd <= -GoDisplayDuration )
            {
                CurrentPhase = Phase.Playing;
                SecondsRemaining = 0f;
            }
        }

        // Only do freeze work while we're in Countdown, or for the one frame we transition out of it.
        if ( CurrentPhase == Phase.Countdown )
        {
            SetLocalPlayerFrozen( true );
            _lastAppliedPhase = Phase.Countdown;
        }
        else if ( _lastAppliedPhase == Phase.Countdown )
        {
            SetLocalPlayerFrozen( false );
            _lastAppliedPhase = CurrentPhase;
        }
    }

    /// <summary>Set UseInputControls on the locally-owned PlayerController. Look/camera is untouched so the player can still look around.</summary>
    private void SetLocalPlayerFrozen( bool frozen )
    {
        foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
        {
            // Skip players owned by other clients — we'd just be fighting their input state.
            if ( pc.Network.IsProxy ) continue;

            pc.UseInputControls = !frozen;
        }
    }
}
