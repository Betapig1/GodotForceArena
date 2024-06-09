using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


public partial class MainMenu : CanvasLayer
{
	//Network Manager
	public NetworkManager networkManager;
	//UI Elements
	PanelContainer mainMenu;
	[Export]
	public VBoxContainer vBox;
	[Export]
	public TextEdit IP;
	[Export]
	public TextEdit Port;
	//Networking and Game set up
	PackedScene gameOption = ResourceLoader.Load<PackedScene>("res://Scenes/GameOption.tscn");
	bool listening = false;
	UdpServer scanner = new UdpServer();
	List<string> ips = new List<string>();
	List<int> ports = new List<int>();



	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{


		mainMenu = GetNode<PanelContainer>("MainMenu");
		networkManager = GetNode<NetworkManager>("/root/GameScene/NetworkManager");
		//Starts listening system for LAN lobbies
		RefreshLobbies(1000);

	}



	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{


		if (listening)
		{
			scanner.Poll();
			while (scanner.IsConnectionAvailable())
			{
				GD.Print("New connection");
				PacketPeerUdp peer = scanner.TakeConnection();



				byte[] packet = peer.GetPacket();


				// Checks if packet with lobby information has already been recorded, if so, return
				if (ips.Contains(peer.GetPacketIP()))
				{
					peer.Close();
					return;
				}

				ips.Add(peer.GetPacketIP());
				ports.Add(Int32.Parse(packet.GetStringFromUtf8()));

				// Optionally close the peer connection or keep it open for further communication
				peer.Close();
			}
		}
	}




	private void _on_listen_pressed()
	{
		//Listens on port 50000 for UDP packets
		scanner.Listen(50000);
		listening = true;
	}

	private void _on_host_pressed()
	{
		networkManager.CreateServer();
		HideMenu();
	}

	/// <summary>
	///	Clears all the lobby entries
	/// </summary>
	public void ClearLobbies()
	{
		var games = vBox.GetChildren();
		for (int x = 0; x < games.Count; x++)
		{
			games[x].QueueFree();
		}
	}

	/// <summary>
	///	Using saved list data from last cycle, clear listen lobbies and put in new lobby list
	/// </summary>
	async Task RefreshLobbies(int delay)
	{
		await Task.Delay(delay);

		ClearLobbies();

		for (int y = 0; y < ips.Count; y++)
		{
			var madeOption = gameOption.Instantiate();
			JoinGame temp = (JoinGame)madeOption;

			temp.SetAddress(ips[y], ports[y]);
			vBox.AddChild(madeOption);


		}
		ips = new List<string>();
		ports = new List<int>();
		await RefreshLobbies(delay);
	}
	/// <summary>
	///	Join from inputted IP address and port
	/// </summary>
	private void _on_join_pressed()
	{
		networkManager.CreateClient(IP.Text, Int32.Parse(Port.Text));
		IP.Text = "";
		Port.Text = "";
		HideMenu();
	}

	/// <summary>
	///	Hides menu when user enters game
	/// </summary>
	private void HideMenu()
	{
		var parentMenu = (CanvasLayer)mainMenu.GetParent();
		parentMenu.Hide();
	}



}














