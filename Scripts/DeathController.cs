using Godot;

public partial class DeathController : Area3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		BodyEntered += HandleDeath;
	}


	/// <summary>
	///	When the player falls off the edge and collides with one of the 3d areas, set the velocity to 0, remove the has been hit attribute,
	/// and have them lose a life
	/// </summary>
	private void HandleDeath(Node3D body)
	{
		if (body is not PlayerController) return;
		PlayerController player = (PlayerController)body;
		CharacterBody3D playerBody = (CharacterBody3D)body;
		playerBody.Velocity = new Vector3(0, 0, 0);
		player.hasBeenHit = false;
		player.LoseLife();
	}


}



