using Godot;
using System.Threading.Tasks;


public partial class NetworkManager : Node3D
{
	//Networking variables
	public int port;
	ENetMultiplayerPeer eNetPeer;
	bool isConnected = false;
	bool hostConnections = false;
	bool clientConnections = false;
	PacketPeerUdp socket = new PacketPeerUdp();
	bool sendPacket = false;
	//Miscellaneous
	PackedScene player;
	[Export]
	CanvasLayer mainMenu;

	//MISCELLANEOUS METHODS

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		//Generates random port from 2500 options
		port = (int)(GD.Randf() * 2500);
		port += 51000;
		eNetPeer = new ENetMultiplayerPeer();
		player = ResourceLoader.Load<PackedScene>("res://Player.tscn");


	}



	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{



	}

	//NETWORKING METHODS

	/// <summary>
	///	Checks if signals have already been attatched to methods, if not attatch them, and tries to connect to game with 5 second timeout
	///	</summary>
	public void CreateClient(string ip, int localPort)
	{
		if (!clientConnections)
		{
			Multiplayer.ServerDisconnected += Cleanup;
			Multiplayer.ConnectionFailed += Cleanup;
			Multiplayer.ConnectedToServer += Connected;
			clientConnections = true;
		}
		eNetPeer.CreateClient(ip, localPort);
		Multiplayer.MultiplayerPeer = eNetPeer;
		TimeoutConnection();



	}

	/// <summary>
	///	Stops timeout when the user connects, and adds them to the game
	///	</summary>
	private void Connected()
	{
		isConnected = true;
		var instance = ResourceLoader.Load<PackedScene>("res://Scenes/GameScene.tscn").Instantiate();
		AddChild(instance);
	}

	/// <summary>
	///	Checks if signals have already been attatched to methods, if not attatch them, and creates a server and starts advertising it to 
	/// port 50000
	///	</summary>
	public void CreateServer()
	{
		eNetPeer.CreateServer(port);
		Multiplayer.MultiplayerPeer = eNetPeer;
		var instance = ResourceLoader.Load<PackedScene>("res://Scenes/GameScene.tscn").Instantiate();

		AddChild(instance);
		CreatePlayer(Multiplayer.GetUniqueId());
		if (!hostConnections)
		{
			Multiplayer.ServerDisconnected += Cleanup;
			Multiplayer.PeerConnected += CreatePlayer;
			Multiplayer.PeerDisconnected += PlayerLeft;
			hostConnections = true;
		}


		socket.SetDestAddress("255.255.255.255", 50000);
		socket.SetBroadcastEnabled(true);
		sendPacket = true;
		SendData(750);
	}
	/// <summary>
	///	Creates player and adds them to the scene
	///	</summary>
	private void CreatePlayer(long peerId)
	{
		var loadedPlayer = player.Instantiate();
		CharacterBody3D playerObject = (CharacterBody3D)loadedPlayer;
		playerObject.Name = peerId.ToString();

		AddChild(loadedPlayer);

	}
	/// <summary>
	///	Cleans up the scene for server or client disconnection
	///	</summary>
	private void Cleanup()
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		if (Multiplayer.HasMultiplayerPeer())
		{
			Multiplayer.MultiplayerPeer.Close();
		}
		mainMenu.Show();
		GetTree().GetNodesInGroup("Game")[0].QueueFree();
		sendPacket = false;
		((MainMenu)mainMenu).ClearLobbies();

		var players = GetTree().GetNodesInGroup("players");
		for (int x = 0; x < players.Count; x++)
		{
			players[x].QueueFree();
		}
		isConnected = false;

		//Do cleanup here
	}
	/// <summary>
	///	Removes the player from the scene in the event a client disconnects
	///	</summary>
	private void PlayerLeft(long peerId)
	{
		var players = GetTree().GetNodesInGroup("players");
		for (int x = 0; x < players.Count; x++)
		{
			if (players[x].Name == peerId.ToString())
			{
				players[x].QueueFree();
				return;
			}
		}
	}

	//TIME DELAY TASK METHODS

	/// <summary>
	///	Broadcast method to advertise the game lobby
	///	</summary>
	async Task SendData(int delay)
	{
		await Task.Delay(delay);
		if (!sendPacket) return;
		string message = port.ToString(); // Customize your broadcast message as needed

		socket.PutPacket(message.ToAsciiBuffer());

		await SendData(delay);
	}
	/// <summary>
	///	Timeout method
	///	</summary>
	async Task TimeoutConnection()
	{
		await Task.Delay(5000);
		if (isConnected) return;
		Multiplayer.MultiplayerPeer.Close();


	}
}



