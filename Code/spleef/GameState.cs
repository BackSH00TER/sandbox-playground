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

    /// <summary>Current phase of the match — used by Tile/Platform/etc. to gate their behaviour.</summary>
    public Phase CurrentPhase { get; private set; } = Phase.Countdown;

    /// <summary>Counts down from CountdownSeconds; goes negative for the "GO!" display window before flipping to Playing.</summary>
    private TimeUntil _phaseEnd;

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

            int seconds = (int)MathF.Ceiling( (float)_phaseEnd );
            return seconds > 0 ? seconds.ToString() : "GO!";
        }
    }

    protected override void OnEnabled()
    {
        Current = this;
        StartCountdown();
    }

    protected override void OnDisabled()
    {
        if ( Current == this )
            Current = null;
    }

    public void StartCountdown()
    {
        CurrentPhase = Phase.Countdown;
        _phaseEnd = CountdownSeconds;
        SetPlayersFrozen( true );
    }

    protected override void OnUpdate()
    {
        if ( CurrentPhase == Phase.Countdown && (float)_phaseEnd <= -GoDisplayDuration )
        {
            CurrentPhase = Phase.Playing;
            SetPlayersFrozen( false );
        }
    }

    /// <summary>Toggle UseInputControls on every PlayerController in the scene so players can't walk but can still look around.</summary>
    private void SetPlayersFrozen( bool frozen )
    {
        foreach ( var pc in Scene.GetAllComponents<PlayerController>() )
        {
            pc.UseInputControls = !frozen;
        }
    }
}
