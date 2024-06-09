using Godot;


public partial class ObjectController : RigidBody3D
{

	[Export]
	public Node3D gravityPoint;
	bool isClicked = false;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}


	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		//If an object is selected pull it towards the gravity point like the gravity gun from half life
		if (isClicked)
		{
			Vector3 direction = Position - gravityPoint.GlobalPosition;


			Position -= direction * (float)delta * 15;
			Rotation = gravityPoint.GlobalRotation;
		}
	}
	/// <summary>
	///	Turn off rigidbody PlayerController and disable gravity while held
	///	</summary>
	public void SetPlayer(Node3D point)
	{
		gravityPoint = point;
		this.AxisLockLinearX = true;
		this.AxisLockLinearY = true;
		this.AxisLockLinearZ = true;
		this.GravityScale = 0;

		isClicked = true;
	}

	/// <summary>
	///	Add force, reapply PlayerController, and readd gravity
	///	</summary>
	public void ThrowObject()
	{
		isClicked = false;
		this.AxisLockLinearX = false;
		this.AxisLockLinearY = false;
		this.AxisLockLinearZ = false;
		this.GravityScale = 1;
		GetNode<RigidBody3D>(GetPath()).ApplyForce(Transform.Basis.Z * -30);
	}

	/// <summary>
	///	When a cube hits a body, check if its been hit already within the invincibility delay, if not, hit and push the player
	///	</summary>
	private void _on_body_entered(Node body)
	{

		if (body is CharacterBody3D)
		{
			var movementScript = (PlayerController)body;
			if (movementScript.hasBeenHit) return;
			movementScript.Jump();

			movementScript.hasBeenHit = true;
			Vector3 temp = LinearVelocity;
			temp.Y = 0;
			movementScript.hitVector = temp * 50;
			movementScript.ResetHit(2500);
		}



	}
}



