using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System;
using Atom = AtomClass.atom;
using Bond = BondClass.bond;
using PTControl = periodic_table_ui_control;
using HUD = hud;
using System.IO;

public partial class main : Node
{

	// Elements
	private List<Atom> elements;
	private PTControl ptControl;
	private Control currentButton = null;


	// Atoms & Bonds
	private List<Atom> atomList = new List<Atom>();
	private Atom currentElement = null;
	private Atom currentAtom = null;
	private PackedScene atomScene;
	private Atom dragging = new Atom();

	private PackedScene bondScene;
	private List<Bond> bondsList = new List<Bond>();

	private MolecularSurface molecularSurface;

	// Player attributes
	private Camera3D camera;
	private RayCast3D rayCast;

	//Hud
	private HUD hud;

	// Misc.
	private bool _isFullScreen = false;
	private CanvasLayer cursorScene;
	private ErrorLabel errorLabel;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

		// Load atomscene
		atomScene = GD.Load<PackedScene>("res://scenes/atom.tscn");

		// Load bondScene
		bondScene = GD.Load<PackedScene>("res://scenes/bond.tscn");

		// Get player camera
		camera = GetNode<Camera3D>("Player/Head/Camera");
		if (camera == null)
		{
			GD.PrintErr("Camera node not found!");
			return;
		}

		// Load Cursor
		cursorScene = GetNode<CanvasLayer>("Cursor");

		// Initialize Raycast
		rayCast = GetNode<RayCast3D>("Player/Head/Camera/RayCast");
		if (rayCast == null)
		{
			GD.PrintErr("rayCast node not found!");
			return;
		}

		// Error label
		errorLabel = GetNode<ErrorLabel>("ErrorLabel");

		// Get the periodic table from JSON
		elements = LoadElementsFromJSON();
		currentElement = elements[3];

		// HUD
		hud = GetNode<HUD>("HUD");
		UpdateHud();

		// Periodic table
		ptControl = GetNode<PTControl>("PeriodicTableUI/SubViewport/Control");
		ptControl.Setup(elements);

		// Add surface
		molecularSurface = new MolecularSurface();
		AddChild(molecularSurface);
		GD.Print($"Added MolecularSurface to main at position {molecularSurface.GlobalPosition}");
	}

	// Method to change the resolution
	public void FullScreen()
	{
		if (_isFullScreen)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}
		else
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}

		_isFullScreen = !_isFullScreen;
		RefreshCursor();
	}

	public override void _PhysicsProcess(double delta)
	{

		UpdateHud();

		// Update the raycast position from the camera
		if (rayCast.IsColliding())
		{
			var collider = rayCast.GetCollider();

			// periodic table mouse events
			if (collider is Node3D gui && gui.Name == "PeriodicTableUI")
			{
				try
				{

					SubViewport subViewport = (SubViewport)gui.GetChild(1);
					Control control = (Control)subViewport.GetChild(0);
					var buttons = control.GetChild(0).GetChildren();

					Vector3 collisionPoint = rayCast.GetCollisionPoint();
					Vector3 localPoint = gui.ToLocal(collisionPoint);

					// Get the size of the StaticBody3D (assuming it's a box shape)
					Vector3 guiSize = ((BoxShape3D)((StaticBody3D)gui).GetNode<CollisionShape3D>("CollisionShape3D").Shape).Size;

					// Convert 3D local point to 2D viewport coordinate
					Vector2 viewportPoint = new Vector2(
						(localPoint.X / guiSize.X + 0.5f) * subViewport.Size.X,
						(1 - (localPoint.Y / guiSize.Y + 0.5f)) * subViewport.Size.Y
					);

					bool buttonFound = false;
					foreach (var buttonNode in buttons)
					{
						if (buttonNode is Button button)
						{
							Rect2 buttonRect = button.GetGlobalRect();
							if (buttonRect.HasPoint(viewportPoint))
							{
								if (currentButton != button)
								{
									if (currentButton != null)
									{
										currentButton.ReleaseFocus();
									}
									button.GrabFocus();
									currentButton = button;
								}
								buttonFound = true;
								break;
							}
						}
					}
					if (!buttonFound && currentButton != null)
					{
						currentButton.ReleaseFocus();
						currentButton = null;
					}
				}
				catch (Exception e)
				{
					GD.PrintErr($"Error in _PhysicsProcess: {e.Message}\n{e.StackTrace}");
				}
			}

			// highlight hit atom
			foreach (Atom atom in atomList)
			{
				if (collider == atom.atomStaticBody)
				{
					// Apply hover effect
					if (currentAtom == null)
					{
						currentAtom = atom;
						currentAtom.Highlight();
					}
					break;
				}
			}

		}

		else
		{
			if (currentAtom != null)
			{
				currentAtom.Highlight();
				currentAtom = null;
			}

			if (currentButton != null)
			{
				currentButton.ReleaseFocus();
				currentButton = null;
			}
		}

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
		{
			if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					if (currentButton != null)
					{
						UpdateCurrentElement(currentButton);
					}

					if (currentAtom != null)
					{
						dragging = currentAtom;
					}
				}
				else
				{
					if (atomList.Count == 0)
					{
						AddAtom(mouseEvent.GlobalPosition);
					}
					else if (dragging != null)
					{
						AddAtom(mouseEvent.GlobalPosition, dragging);
					}
					else if (currentButton == null)
					{
						errorLabel.ShowText();
					}

					dragging = null;
				}
			}
		}

		if (Input.IsActionJustPressed("quit_game"))
		{
			GetTree().Quit();
		}

		if (Input.IsActionJustPressed("full_screen"))
		{
			FullScreen();
		}

		if (Input.IsActionJustPressed("clear"))
		{
			ClearScene();
		}

		if (Input.IsActionJustPressed("generate_surface"))
		{
			GenerateMolecularSurface();
		}

		if (Input.IsActionJustPressed("toggle_surface_visibility"))
		{
			molecularSurface.Visible = !molecularSurface.Visible;
		}
	}

	private void AddAtom(Vector2 clickPos, Atom currAtom = null)
	{

		var atom = (Atom)atomScene.Instantiate();

		atom.CopyData(currentElement);

		AddChild(atom);

		// Set sphere position
		var from = camera.ProjectRayOrigin(clickPos);
		var to = from + camera.ProjectRayNormal(clickPos) * 2.5f;

		atom.SetPosition(to);

		atomList.Add(atom);

		if (currAtom != null)
		{
			var bond = (Bond)bondScene.Instantiate();
			bond.Atom1 = currAtom;
			bond.Atom2 = atom;

			bondsList.Add(bond);
			AddChild(bond);
		}
	}

	private void RefreshCursor()
	{
		cursorScene._Ready();
	}

	private List<Atom> LoadElementsFromJSON()
	{

		var elements = new List<Atom>();
		string jsonText, filePath = "res://assets/periodic_table.json";

		using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read))
		{
			if (file == null)
			{
				GD.PrintErr("Failed to open periodic_table.json");
				return elements;
			}
			jsonText = file.GetAsText();
		}

		var jsonData = Json.ParseString(jsonText);

		if (jsonData.VariantType != Variant.Type.Nil)
		{
			var rootDictionary = jsonData.AsGodotDictionary();
			var elementsArray = rootDictionary["elements"].AsGodotArray();


			foreach (var element in elementsArray)
			{
				var elementData = element.AsGodotDictionary();
				var atomColorArray = elementData["atomColor"].AsGodotArray();
				var atomColor = new Color(
					(int)atomColorArray[0] / 255.0f,
					(int)atomColorArray[1] / 255.0f,
					(int)atomColorArray[2] / 255.0f
				);

				var atom = new Atom
				{
					ElementSymbol = (string)elementData["ElementSymbol"],
					ElementName = (string)elementData["ElementName"],
					AtomicNumber = (int)elementData["AtomicNumber"],
					Mass = (float)elementData["Mass"],
					CovalentRadius = (float)elementData["CovalentRadius"],
					VanDerWaalsRadius = (float)elementData["VanDerWaalsRadius"],
					atomColor = atomColor
				};
				elements.Add(atom);
			}
		}
		else
		{
			GD.PrintErr("Failed to parse elements.json");
		}

		return elements;
	}

	private void UpdateHud()
	{
		hud.UpdateChemicalFormula(atomList);
		hud.UpdateElementColor(currentElement.atomColor);
		hud.UpdateElementName(currentElement.ElementName);
	}

	private Atom FindElementByAtomicNumber(int atomicNumber)
	{
		return elements.Find(element => element.AtomicNumber == atomicNumber);
	}

	private void UpdateCurrentElement(Control button)
	{
		if (button is Button elementButton)
		{
			var label = elementButton.GetChild<Label>(0);
			if (label != null && int.TryParse(label.Text, out int atomicNumber))
			{
				var newElement = FindElementByAtomicNumber(atomicNumber);
				if (newElement != null)
				{
					currentElement = newElement;
					UpdateHud();
					GD.Print($"Current element updated to: {currentElement.ElementName}");
				}
			}
		}
	}

	private void ClearScene()
	{
		// Remove all atoms
		foreach (Atom atom in atomList)
		{
			atom.QueueFree();
		}
		atomList.Clear();

		// Remove all bonds
		foreach (Bond bond in bondsList)
		{
			bond.QueueFree();
		}
		bondsList.Clear();

		// Reset currentAtom and dragging
		currentAtom = null;
		dragging = null;

		// Update the HUD and any other necessary UI elements
		UpdateHud();
	}

	private void GenerateMolecularSurface()
	{
		molecularSurface.GenerateSurface(atomList);
	}
}
