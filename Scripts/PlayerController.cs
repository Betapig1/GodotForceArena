using Godot;
using System;
using System.Threading.Tasks;
public partial class PlayerController : CharacterBody3D
{
	//Network manager
	NetworkManager networkManager;
	//Player movement and interaction handling
	[Export]
	public int Speed { get; set; } = 14;
	[Export]
	public int FallAcceleration { get; set; } = 75;
	[Export]
	public int JumpVelocity { get; set; } = 300;
	private Vector3 _targetVelocity = Vector3.Zero;
	private Node3D neck;
	private Camera3D camera;
	//Input buffer handling
	private InputEvent inputBuffer;
	private InputEvent currentInput;

	//Attack and collision handling
	private const float RayLength = 1000.0f;
	public bool hasBeenHit = false;
	public Vector3 hitVector;
	[Export]
	private ObjectController heldObject;


	//UI elements
	[Export]
	private Label lifeUI;
	[Export]
	private CanvasLayer ui;
	[Export]
	Label win;
	[Export]
	Label port;

	//Death handling
	private int lives = 3;
	private int playerMax = 0;
	private int playersDead = 0;
	private bool isDead = false;

	//Win handling
	private bool won = false;

	//MISCELLANEOUS METHODS

	public override void _Ready()
	{
		//Sets up UI and cameras
		if (!IsMultiplayerAuthority()) return;
		neck = GetNode<Node3D>("Neck");
		camera = GetNode<Camera3D>("Neck/Camera3D");
		camera.Current = true;
		Camera3D deadCam = GetNode<Camera3D>("../Node3D/DeadCam");
		deadCam.SetProcess(false);
		Label deadText = (Label)deadCam.GetChild(0);
		deadText.Hide();
		lifeUI.Text = "Lives: " + lives;
		networkManager = GetNode<NetworkManager>("/root/GameScene/NetworkManager");
		port.Text = "Port: " + networkManager.port.ToString();

	}



	public override void _EnterTree()
	{
		SetMultiplayerAuthority(Int32.Parse(Name));
	}

	//LOCAL RAN METHODS

	/// <summary>
	///	When player falls off the edge, they lose a life, and if they aren't at 0 lives, are respawned, otherwise they are removed from the game 
	/// and are considered dead
	///	</summary>
	public void LoseLife()
	{
		if (!IsMultiplayerAuthority()) return;

		lives--;
		if (lives != 0)
		{
			Vector3 spawn = new Vector3(0, 10, 0);
			Position = spawn;
		}
		if (lives == 0)
		{


			Camera3D deadCam = GetNode<Camera3D>("../Node3D/DeadCam");
			Input.MouseMode = Input.MouseModeEnum.Visible;
			//Reenables process as the script attatched allows the player to close their connection
			deadCam.SetProcess(true);
			Label deadText = (Label)deadCam.GetChild(0);
			deadText.Show();
			//Changes current camera to give player a spectator view
			deadCam.Current = true;
			camera.Current = false;
			isDead = true;
			//Kills player, informs both server and client
			if (!Multiplayer.IsServer())
			{
				RpcId(1, nameof(RequestAddDeath), GetPath());
			}
			else
			{
				AddDeath(GetPath());
			}
			Input.MouseMode = Input.MouseModeEnum.Visible;
			GlobalKillUI();
			return;

		}


		lifeUI.Text = "Lives: " + lives;

	}





	/// <summary>
	///	Kills all UI on a players instance, ensuring that the UI from a remote player does not show on a local players camera
	///	</summary>
	public void KillUI()
	{
		ui.QueueFree();
		Camera3D deadCam = GetNode<Camera3D>("../Node3D/DeadCam");
		deadCam.Current = true;

	}


	/// <summary>
	///	Loops through all players in local instance of the game, and calls KillUI on all of them, to make sure the ui does not persists after
	/// death
	///	</summary>
	private void GlobalKillUI()
	{
		var players = GetTree().GetNodesInGroup("players");
		for (int x = 0; x < players.Count; x++)
		{
			PlayerController playerLocal = (PlayerController)players[x];

			playerLocal.KillUI();
		}
		Spectator spectator = (Spectator)GetTree().GetNodesInGroup("Spectator")[0];
		spectator.isDead = true;


	}

	//RPC CLIENT RUN METHODS


	/// <summary>
	///	Call made on server from client, requesting the server to attatch an object to the client so that replication remains consistent
	/// for all players
	///	</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void Win(string movementPath)
	{
		won = true;
		GetNode<PlayerController>(movementPath).win.Show();
		RpcId(Int32.Parse(GetNode<PlayerController>(movementPath).Name), nameof(ConfirmWin));


	}

	//RPC SERVER RUN METHODS


	/// <summary>
	///	Server side call for attatching an object, to a player for moving and throwing, also calls back to the client to let
	/// them know that it went through to the server, and to block the players ability to pick up items
	///	</summary>
	private void SetPlayer(string pointPath, string cubePath, string movementPath)
	{

		var point = GetNode<Node3D>(pointPath);
		var cubeObject = GetNode<ObjectController>(cubePath);
		var PlayerController = GetNode<PlayerController>(movementPath);
		PlayerController.heldObject = cubeObject;
		cubeObject.SetPlayer(point);
		RpcId(Multiplayer.GetRemoteSenderId(), nameof(ConfirmSetPlayer), cubePath);
	}
	/// <summary>
	///	Server side call for thorwing an object, also calls back to the client to let them know it went through on the server
	/// and to free up the players ability to grab items
	///	</summary>
	private void ThrowObject(string movementPath, bool isServer)
	{
		var PlayerController = GetNode<PlayerController>(movementPath);
		PlayerController.heldObject.ThrowObject();
		if (isServer)
		{
			heldObject = null;
			return;
		}
		RpcId(Multiplayer.GetRemoteSenderId(), nameof(ConfirmThrowObject));

	}



	/// <summary>
	///	Full call on server, recording death, and checking for a win. If there is a winning player, make the according calls
	///	</summary>
	private void AddDeath(string movementPath)
	{
		var players = GetTree().GetNodesInGroup("players");

		GetNode<PlayerController>(movementPath).isDead = true;
		playersDead++;
		playerMax = players.Count;

		if (playersDead == playerMax - 1)
		{
			for (int x = 0; x < playerMax; x++)
			{
				PlayerController player = (PlayerController)players[x];
				if (!player.isDead)
				{
					player.Win(player.GetPath());
					RpcId(Int32.Parse(player.Name), nameof(Win), player.GetPath());




					return;
				}
			}
		}
	}



	//RPC REQUEST METHODS

	/// <summary>
	///	Call made on server from client, requesting the server to attatch an object to the client so that replication remains consistent
	/// for all players
	///	</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestSetPlayer(string pointPath, string cubePath, string movementPath)
	{

		SetPlayer(pointPath, cubePath, movementPath);

	}


	/// <summary>
	///	Call made on server from client, requesting the server record a player death, and update all the value accordingly
	///	</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestAddDeath(string movementPath)
	{

		AddDeath(movementPath);

	}

	/// <summary>
	///	Call made on server from client, requesting the server to throw their held object, and allow them to pick up objects again
	///	</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestThrowObject(string movementPath)
	{

		ThrowObject(movementPath, false);


	}


	//RPC CONFIRMATION METHODS

	/// <summary>
	///	Confirmation call from the server to the requesting client, so the client is able to make all the proper changes to recognize
	/// a grabbed object
	///	</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ConfirmSetPlayer(string cubePath)
	{
		var cubeObject = GetNode<ObjectController>(cubePath);

		heldObject = cubeObject;

	}

	/// <summary>
	///	Confirmation call from the server to the requesting client, so the client is able to make all the proper changes to recognize
	/// a thrown object
	///	</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void ConfirmThrowObject()
	{


		heldObject = null;

	}
	/// <summary> 
	///	Confirmation call from the server to the winning client, informing them of the win, and stopping movement of the player
	/// telling them they've won, and instructing them on how to leave
	///	</summary>
	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable, CallLocal = true)]
	private void ConfirmWin()
	{

		Input.MouseMode = Input.MouseModeEnum.Visible;
		won = true;

	}


	//TIME DELAY TASK METHODS

	/// <summary>
	///	Clears the input after a given amount of miliseconds, allows responsive buffering that doesn't feel drawn out
	///	</summary>
	async Task ClearInput(int delay)
	{
		await Task.Delay(delay);
		inputBuffer = null;
	}
	/// <summary>
	///	Resets the player being affected by being hit by an object after so many miliseconds
	///	</summary>
	public async Task ResetHit(int delay)
	{
		await Task.Delay(delay);
		hasBeenHit = false;

	}


	//INPUT AND PLAYER CONTROL METHODS


	public override void _Input(InputEvent @event)
	{
		//If the player has won disallow movement
		if (won) return;
		//Saves the current input globally, to potentially if there is a need to buffer missed inputs, to allow
		//smoother gameplay
		currentInput = @event;

		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.Pressed && eventMouseButton.ButtonIndex == MouseButton.Left)
		{

			if (!IsMultiplayerAuthority()) return;

			//If there was a click, and the player is holding an object, request the server to throw the object
			//Unless the server is the player throwing, then the server can just call directly
			if (heldObject != null)
			{


				if (Multiplayer.IsServer())
				{
					ThrowObject(GetPath(), true);
				}
				else
				{
					RpcId(1, nameof(RequestThrowObject), GetPath());

				}
				return;
			}
			//Math and values to generate ray, to pick up blocks with gravity un
			var space_state = GetWorld3D().DirectSpaceState;
			var from = camera.ProjectRayOrigin(eventMouseButton.Position);
			var to = from + camera.ProjectRayNormal(eventMouseButton.Position) * RayLength;

			var query = PhysicsRayQueryParameters3D.Create(from, to);
			var result = space_state.IntersectRay(query);
			if (!result.ContainsKey("collider_id")) return;
			GodotObject cast = InstanceFromId((ulong)result["collider_id"]);


			//checks to see if the object found with the raycast contains an object controller
			//if it doesn't we can't grab it, so return
			if (cast is not ObjectController) return;
			ObjectController cubeObject = (ObjectController)cast;
			//Otherwise, talk to the server to pick up the item
			if (Multiplayer.IsServer())
			{
				SetPlayer(GetNode<Node3D>("Neck/Camera3D/ObjectLock").GetPath(), cubeObject.GetPath(), GetPath());
			}
			else
			{
				RpcId(1, nameof(RequestSetPlayer), GetNode<Node3D>("Neck/Camera3D/ObjectLock").GetPath(), cubeObject.GetPath(), GetPath());

			}

		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{



		if (!IsMultiplayerAuthority()) return;
		//if the user has won the game, allow the to disconnect and return to the main menu, by hitting escape
		if (@event.IsActionPressed("ui_cancel") && won)
		{
			Multiplayer.MultiplayerPeer.Close();
		}
		if (won || isDead) return;

		//Allows user to go in and out of focus of the game
		if (@event is InputEventMouseButton)
		{
			Input.MouseMode = Input.MouseModeEnum.Captured;
		}
		else if (@event.IsActionPressed("ui_cancel"))
		{
			Input.MouseMode = Input.MouseModeEnum.Visible;
		}

		//If they are in forcus, allow them to move the camera
		if (Input.MouseMode == Input.MouseModeEnum.Captured)
		{
			if (@event is InputEventMouseMotion)
			{
				InputEventMouseMotion motion = (InputEventMouseMotion)@event;

				neck.RotateY(-motion.Relative.X * .01f);

				camera.RotateX(-motion.Relative.Y * .01f);
				Vector3 tempRotate = camera.Rotation;

				tempRotate.X = (float)Mathf.Clamp(tempRotate.X, -1.5, 1.5);

				camera.Rotation = tempRotate;
			}
		}
	}

	/// <summary>
	///	Goes through buffered input, if it exists. An input becomes buffered if it is attempted but is unable to be completed due to game rules
	/// If the input is now able to be completed, then run the input, and clear the input buffer
	///	</summary>
	private void CheckBuffer()
	{
		bool usedBuffer = false;
		if (inputBuffer.IsActionPressed("jump") && IsOnFloor())
		{

			Jump();
			usedBuffer = true;
		}

		if (usedBuffer)
		{
			inputBuffer = null;
		}



	}

	public override void _PhysicsProcess(double delta)
	{
		//If the player has won don't allow them to move
		if (won) return;

		if (!IsMultiplayerAuthority()) return;
		//If they are dead, don't allow them to move
		if (lives == 0) return;
		//Check input buffer to see if buffered inputs are valid
		if (inputBuffer != null)
		{
			CheckBuffer();
		}
		var input_dir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
		var direction = neck.Transform.Basis * new Vector3(input_dir.X, 0, input_dir.Y);

		if (Input.IsActionJustPressed("jump"))
		{
			//Check if player is able to jump, if not, buffer the input
			if (IsOnFloor())
			{
				Jump();
			}
			else
			{
				inputBuffer = currentInput;
				ClearInput(200);
			}
		}



		// Ground velocity
		_targetVelocity.X = direction.X * Speed;
		_targetVelocity.Z = direction.Z * Speed;

		// Vertical velocity
		if (!IsOnFloor()) // If in the air, fall towards the floor. Literally gravity
		{
			_targetVelocity.Y -= FallAcceleration * (float)delta;
		}
		// Moving the character
		if (hasBeenHit)
		{
			_targetVelocity += hitVector * (float)delta;
			hitVector -= hitVector * (float)delta;
		}
		Velocity = _targetVelocity;
		MoveAndSlide();
	}
	/// <summary>
	///	Method to make the player jump
	///	</summary>
	public void Jump()
	{
		_targetVelocity.Y = JumpVelocity;

	}
}
