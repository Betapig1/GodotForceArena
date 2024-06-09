using Godot;

public partial class JoinGame : PanelContainer
{
	//Network Manager
	private NetworkManager manager;
	//UI Elements
	private CanvasLayer menu;
	[Export]
	private VBoxContainer vBox;
	[Export]
	private Label ipAddressLabel;
	[Export]
	private Label PortLabel;
	//Network data
	string ip;
	int port;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		manager = GetTree().Root.GetNode<NetworkManager>("/root/GameScene/NetworkManager");
		menu = GetTree().Root.GetNode<CanvasLayer>("/root/GameScene/MainCanvas");
	}


	/// <summary>
	///	Set data for when the user clicks join
	/// </summary>
	public void SetAddress(string givenIp, int givenPort)
	{
		ip = givenIp;
		ipAddressLabel.Text = givenIp;
		port = givenPort;
		PortLabel.Text = givenPort.ToString();
	}

	/// <summary>
	///	Have player join game as a new client and hide the menu
	/// </summary>
	private void _on_join_pressed()
	{
		manager.CreateClient(ip, port);
		menu.Hide();
	}

}







