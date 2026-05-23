using Sandbox;
using Sandbox.Mapping;

public sealed class RedButton : Component, Component.IPressable, Component.ITriggerListener
{
	[Property] TextRenderer textRenderer { get; set; }
	[Property] GameObject ButtonMover { get; set; }
	[Property] Vector3 TriggerPressOffset { get; set; } = new Vector3( 0, 0, -50f );
	[Property] float TriggerPressSpeed { get; set; } = 100f;
	[Property] float PressDuration { get; set; } = 0.2f;

	int pressCount = 0;
	bool _isOnTrigger = false;
	TimeUntil _pressReleaseAt = 0;
	Vector3 _restPosition;

	protected override void OnStart()
	{
		_restPosition = ButtonMover.LocalPosition;
	}

	protected override void OnUpdate()
	{
		bool isPressed = _isOnTrigger || _pressReleaseAt > 0;
		var target = isPressed ? _restPosition + TriggerPressOffset : _restPosition;
		ButtonMover.LocalPosition = Vector3.Lerp( ButtonMover.LocalPosition, target, Time.Delta * TriggerPressSpeed );

		if ( ButtonMover.LocalPosition.DistanceSquared( target ) < 0.01f )
		{
			ButtonMover.LocalPosition = target;
		}
	}

	private void onButtonPressed()
	{
		Log.Info( "onButtonPressed!" );
		pressCount++;
		textRenderer.Text = $"#{pressCount}";
	}

	/* ------------------------------------------------------------
	   Trigger collider — pressure-plate style activation
	   ------------------------------------------------------------ */

	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Has( "buttonIgnore" ) ) return;
		if ( !_isOnTrigger ) onButtonPressed();
		_isOnTrigger = true;
	}

	public void OnTriggerExit( Collider other )
	{
		if ( other.Tags.Has( "buttonIgnore" ) ) return;
		_isOnTrigger = false;
	}

	/* ------------------------------------------------------------
	   IPressable — handles E-key press interactions
	   ------------------------------------------------------------ */

	[Button]
	public bool Press( IPressable.Event @event )
	{
		Log.Info( $"Press! Source: {@event.Source}" );
		_pressReleaseAt = PressDuration;
		onButtonPressed();
		return true;
	}

	public void Release( IPressable.Event @event )
	{
		Log.Info( $"Release! Source: {@event.Source}" );
	}

	public void Hover( IPressable.Event @event )
	{
		Log.Info( $"Hovering at Red Button! {@event}" );
	}

	public void Look( IPressable.Event @event )
	{
	}

	public void Blur( IPressable.Event @event )
	{
		Log.Info( $"Blurred Red Button!" );
	}
}
