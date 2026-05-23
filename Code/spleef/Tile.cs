using System;
using Sandbox;

public sealed class Tile : Component, Component.ITriggerListener
{
	/// <summary>Seconds between a player stepping on the tile and it breaking away.</summary>
	[Property] public float BreakDelay { get; set; } = 1.0f;

	/// <summary>Seconds after breaking before the tile GameObject is destroyed.</summary>
	[Property] public float FallDuration { get; set; } = 2.0f;

	/// <summary>Maximum wobble roll angle (degrees) just before the tile breaks.</summary>
	[Property] public float WobbleAngle { get; set; } = 5.0f;

	/// <summary>How fast the wobble oscillates (radians/sec fed into Sin).</summary>
	[Property] public float WobbleSpeed { get; set; } = 25.0f;

	/// <summary>Mass applied to the rigidbody when the tile breaks, so gravity has something to act on.</summary>
	[Property] public float FallMass { get; set; } = 100f;

	/// <summary>Rigidbody used to make the tile physically fall when it breaks.</summary>
	[Property] public Rigidbody Rigidbody { get; set; }

	/// <summary>Non-trigger collider that the player stands on. Switched to a trigger on break so the player falls through.</summary>
	[Property] public Collider SolidCollider { get; set; }

	/// <summary>Trigger collider that detects the player stepping onto the tile.</summary>
	[Property] public Collider TriggerCollider { get; set; }

	/// <summary>True once a player has stepped on the tile and the break timer has started.</summary>
	private bool _triggered = false;

	/// <summary>True once BreakTile() has run and the tile is physically falling.</summary>
	private bool _falling = false;

	/// <summary>Counts down from BreakDelay; when it hits 0 the tile breaks.</summary>
	private TimeUntil _breakAt;

	/// <summary>Counts down from FallDuration after breaking; when it hits 0 the tile is destroyed.</summary>
	private TimeUntil _destroyAt;

	/// <summary>The tile's original world rotation, used as the wobble pivot.</summary>
	private Rotation _restRotation;

	protected override void OnStart()
	{
		_restRotation = WorldRotation;

		if ( Rigidbody.IsValid() )
		{
			// Disable physics simulation so the tile stays put until it breaks.
			Rigidbody.MotionEnabled = false;
			// Set the mass override so when we enable physics on break, gravity actually makes it fall instead of just floating in place.
			Rigidbody.MassOverride = FallMass;
		}
	}

	protected override void OnUpdate()
	{
		if ( _triggered && !_falling )
		{
			float remaining = MathX.Clamp( (float)_breakAt / BreakDelay, 0f, 1f );
			float intensity = 1f - remaining;
			float angle = MathF.Sin( Time.Now * WobbleSpeed ) * WobbleAngle * intensity;
			WorldRotation = _restRotation * Rotation.FromRoll( angle );

			if ( _breakAt <= 0 )
				BreakTile();
		}
		else if ( _falling && _destroyAt <= 0 )
		{
			GameObject.Destroy();
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( _triggered ) return;
		if ( !other.Tags.Has( "player" ) ) return;

		_triggered = true;
		_breakAt = BreakDelay;
	}

	public void OnTriggerExit( Collider other )
	{
	}

	public void BreakTile()
	{
		if ( _falling ) return;
		_falling = true;

		// Convert the solid collider to a trigger so the player falls through, but keep
		// it enabled so the rigidbody still has a physics shape. FallMass below gives the
		// (now massless) trigger shape mass so gravity actually applies.
		if ( SolidCollider.IsValid() )
			SolidCollider.IsTrigger = true;

		// Disable the player-detect trigger now that the tile has broken — no need to
		// keep firing trigger events as falling debris.
		if ( TriggerCollider.IsValid() )
			TriggerCollider.Enabled = false;

		if ( Rigidbody.IsValid() )
		{
			Rigidbody.MotionEnabled = true;
		}

		_destroyAt = FallDuration;
	}
}
