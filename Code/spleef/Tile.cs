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

	/// <summary>How fast the white-flash pulses as the tile is about to break (Hz-ish).</summary>
	[Property] public float FlashSpeed { get; set; } = 12f;

	/// <summary>How far down the visual model dips while a player is standing on it (local units).</summary>
	[Property] public float DepressDepth { get; set; } = 3f;

	/// <summary>How quickly the model lerps toward its depressed/rest position.</summary>
	[Property] public float DepressSpeed { get; set; } = 25f;

	/// <summary>Rigidbody used to make the tile physically fall when it breaks.</summary>
	[Property] public Rigidbody Rigidbody { get; set; }

	/// <summary>Non-trigger collider that the player stands on. Switched to a trigger on break so the player falls through.</summary>
	[Property] public Collider SolidCollider { get; set; }

	/// <summary>Trigger collider that detects the player stepping onto the tile.</summary>
	[Property] public Collider TriggerCollider { get; set; }

	/// <summary>Model renderer used for the white-flash effect and depression bob. Should be on a child GameObject so we can move it without moving the collider.</summary>
	[Property] public ModelRenderer Model { get; set; }

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

	/// <summary>The Model GameObject's resting local position, used as the depression pivot.</summary>
	private Vector3 _modelRestPosition;

	/// <summary>Number of player colliders currently overlapping the trigger — used to drive depression.</summary>
	private int _playersOnTile = 0;

	/// <summary>The Model's base tint at startup, used as the "non-flashing" colour.</summary>
	private Color _baseTint = Color.White;

	protected override void OnStart()
	{
		_restRotation = WorldRotation;

		if ( Model.IsValid() )
		{
			_modelRestPosition = Model.LocalPosition;
			_baseTint = Model.Tint;
		}

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

			// White flash that pulses faster/brighter as the tile is about to break.
			if ( Model.IsValid() )
			{
				float pulse = (MathF.Sin( Time.Now * FlashSpeed ) + 1f) * 0.5f; // 0..1
				float flashAmount = pulse * intensity;
				Model.Tint = Color.Lerp( _baseTint, Color.White, flashAmount );
			}

			if ( _breakAt <= 0 )
				BreakTile();
		}
		else if ( _falling && _destroyAt <= 0 )
		{
			GameObject.Destroy();
		}

		// Depression bob — only the visual Model moves, the colliders stay put.
		if ( Model.IsValid() && !_falling )
		{
			var target = _playersOnTile > 0
				? _modelRestPosition + Vector3.Down * DepressDepth
				: _modelRestPosition;

			Model.LocalPosition = Vector3.Lerp( Model.LocalPosition, target, Time.Delta * DepressSpeed );
		}
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( !other.Tags.Has( "player" ) ) return;

		_playersOnTile++;

		if ( !_triggered )
		{
			_triggered = true;
			_breakAt = BreakDelay;
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( !other.Tags.Has( "player" ) ) return;

		_playersOnTile = Math.Max( 0, _playersOnTile - 1 );
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
