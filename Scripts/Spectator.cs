using Godot;

public partial class Spectator : Camera3D
{
	public bool isDead = false;

	/// <summary>
	///	If the player is dead, allow them to leave
	///	</summary>
	public override void _UnhandledInput(InputEvent @event)
	{


		if (@event.IsActionPressed("ui_cancel") && isDead)
		{
			Multiplayer.MultiplayerPeer.Close();
		}



	}

}
