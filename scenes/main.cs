using Godot;
using static Utils;
using static ZMatrixUtils;
using System.Collections.Generic;
using System;
using Atom = AtomClass.atom;
using Bond = BondClass.bond;
using PTControl = periodic_table_ui_control;
using HUD = hud;
using Classes;
using System.IO;

public partial class main : Node
{

	// Elements
	private List<AtomBase> elements;
	private PTControl ptControl;
	private Control currentButton = null;


	// Atoms & Bonds
	private List<Atom> atomList = new List<Atom>();
	private AtomBase currentElement = null;
	private Atom currentAtom = null;
	private PackedScene atomScene;
	private Atom dragging = null;
	private Atom addingAtom = null;

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
	private FileDialog saveFileDialog;
	private FileDialog uploadFileDialog;
	private bool uploading = false;
	public static Control overlay;

	// Spring System
	private SpringSystem springSystem;

	// Preview
	private Atom previewAtom = null;
	private Bond previewBond = null;

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
		currentElement = elements[1];

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

		// Save File Dialog
		saveFileDialog = GetNode<FileDialog>("SaveFileDialog");
		saveFileDialog.Connect("file_selected", Callable.From<string>(OnSaveFileSelected));
		saveFileDialog.Connect("canceled", Callable.From(OnCancel));
		saveFileDialog.Filters = new string[] { "*.txt" };

		uploadFileDialog = GetNode<FileDialog>("UploadFileDialog");
		uploadFileDialog.Connect("file_selected", Callable.From<string>(OnUploadFileSelected));
		uploadFileDialog.Connect("canceled", Callable.From(OnCancel));
		uploadFileDialog.Filters = new string[] { "*.txt" };

		// Current Directory
		string currentDir = GetCurrentDirectory();
		saveFileDialog.RootSubfolder = currentDir;

		// Overlay
		overlay = GetNode<Control>("DisableOverlay");
		overlay.Visible = false;
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
					if (currentAtom == null || currentAtom != atom)
					{
						if (currentAtom != null)
						{
							currentAtom.Highlight(); // Unhighlight the previous atom
						}
						currentAtom = atom;
						currentAtom.Highlight(); // Highlight the new atom
					}
					break;
				}
			}
		}
		else
		{
			// Removing Highlighting atom and buttons
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

		// Moving atoms and molecule
		if (dragging != null)
		{
			Vector2 mousePos = GetViewport().GetMousePosition();
			Vector3 newPosition = camera.ProjectRayOrigin(mousePos) + camera.ProjectRayNormal(mousePos) * 2.5f;

			if (Input.IsKeyPressed((Key.Shift)))
			{
				MoveMolecule(dragging, newPosition);
			}
			else
			{
				dragging.SetPosition(newPosition);
				UpdateBonds(dragging);
			}
		}

		// Previewing added atom
		if (addingAtom != null)
		{
			UpdatePreview(GetViewport().GetMousePosition());
		}
		else
		{
			ClearPreview();
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
			// Handle right click for dragging atoms
			if (mouseEvent.ButtonIndex == MouseButton.Right)
			{
				if (mouseEvent.Pressed)
				{
					if (currentAtom != null)
					{
						dragging = currentAtom;
					}
				}
				else
				{
					if (dragging != null)
					{
						dragging = null;
					}
				}
			}

			else if (mouseEvent.ButtonIndex == MouseButton.Left)
			{
				if (mouseEvent.Pressed)
				{
					if (currentButton != null)
					{
						UpdateCurrentElement(currentButton);
					}

					if (currentAtom != null)
					{
						addingAtom = currentAtom;
					}
				}
				else
				{
					if (currentButton == null)
					{
						if (atomList.Count == 0 || addingAtom != null || uploading)
						{
							AddAtom(mouseEvent.GlobalPosition, addingAtom);
						}
						else
						{
							errorLabel.ShowText("Error, Molecule already placed in scene!");
						}
					}

					addingAtom = null;
					ClearPreview();
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
			// GenerateMolecularSurface();
		}

		if (Input.IsActionJustPressed("toggle_surface_visibility"))
		{
			molecularSurface.Visible = !molecularSurface.Visible;
		}

		if (Input.IsActionJustPressed("optimize"))
		{
			RunSpringSystemOptimization();
		}

		if (Input.IsActionJustPressed("undo_addition"))
		{
			UndoAtomAddition();
		}

		if (Input.IsActionJustPressed("save_molecule"))
		{
			if (atomList.Count == 0)
				errorLabel.ShowText("Error, Can't save empty scene!");
			else
				ShowFileDialog(saveFileDialog);
		}

		if (Input.IsActionJustPressed("upload_molecule"))
		{
			if (atomList.Count != 0)
				errorLabel.ShowText("Error, Can't load molecule. Scene isn't empty!");
			else
				ShowFileDialog(uploadFileDialog);
		}
	}

	private void UndoAtomAddition()
	{
		if (atomList.Count == 0)
		{
			errorLabel.ShowText("No atoms to undo!");
			return;
		}

		// Get the last atom in the list
		Atom lastAtom = atomList[atomList.Count - 1];

		// Find and remove any bonds connected to the last atom
		List<Bond> bondsToRemove = new List<Bond>();
		foreach (Bond bond in bondsList)
		{
			if (bond.ConnectsTo(lastAtom.atomBase))
			{
				bondsToRemove.Add(bond);
			}
		}

		foreach (Bond bond in bondsToRemove)
		{
			bond.QueueFree();
			bondsList.Remove(bond);
		}

		// Remove the last atom
		lastAtom.QueueFree();
		atomList.RemoveAt(atomList.Count - 1);

		// Reset currentAtom and addingAtom if they were the last atom
		if (currentAtom == lastAtom)
		{
			currentAtom = null;
		}
		if (addingAtom == lastAtom)
		{
			addingAtom = null;
		}

		// Update the HUD and any other necessary UI elements
		UpdateHud();
	}

	private void AddAtom(Vector2 clickPos, Atom currAtom = null)
	{
		ClearPreview();

		// Instansiate scene
		var godotAtom = (Atom)atomScene.Instantiate();

		// Copy the element data
		godotAtom.atomBase.CopyData(currentElement);

		// Setup mesh specifics
		godotAtom.SetRadius();

		Vector3 atomColorVec = ConvertToGodotVector3(currentElement.AtomColor);
		godotAtom.atomColor = new Color(atomColorVec.X, atomColorVec.Y, atomColorVec.Z);

		// Add atom to tree
		AddChild(godotAtom);

		// Set sphere position
		var from = camera.ProjectRayOrigin(clickPos);
		var to = from + camera.ProjectRayNormal(clickPos) * 2.5f;

		godotAtom.SetPosition(to);

		atomList.Add(godotAtom);

		if (currAtom != null)
		{
			var bond = (Bond)bondScene.Instantiate();

			bond.CreateBond(currAtom.atomBase, godotAtom.atomBase);

			bondsList.Add(bond);
			AddChild(bond);
		}
	}

	private void RefreshCursor()
	{
		cursorScene._Ready();
	}

	private void UpdateHud()
	{
		Vector3 atomColorVec = ConvertToGodotVector3(currentElement.AtomColor);

		hud.UpdateChemicalFormula(atomList);
		hud.UpdateElementColor(atomColorVec);
		hud.UpdateElementName(currentElement.ElementName);
	}

	private AtomBase FindElementByAtomicNumber(int atomicNumber)
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

		// Reset currentAtom and addingAtom
		currentAtom = null;
		addingAtom = null;
		dragging = null;

		// Update the HUD and any other necessary UI elements
		UpdateHud();
	}

	private void GenerateMolecularSurface()
	{
		molecularSurface.GenerateSurface(atomList);
	}

	private void UpdateBonds(Atom movedAtom)
	{
		foreach (var bond in bondsList)
		{
			if (bond.bondBase.Atom1 == movedAtom.atomBase || bond.bondBase.Atom2 == movedAtom.atomBase)
			{
				bond.UpdateBond();
			}
		}
	}

	private void MoveMolecule(Atom rootAtom, Vector3 newPosition)
	{
		Vector3 delta = newPosition - rootAtom.Position;

		foreach (var atom in atomList)
		{
			if (IsConnected(rootAtom, atom))
			{
				atom.SetPosition(atom.Position + delta);
			}
		}

		UpdateAllBonds();
	}

	private bool IsConnected(Atom rootAtom, Atom targetAtom)
	{
		var visited = new HashSet<Atom>();
		var stack = new Stack<Atom>();

		stack.Push(rootAtom);

		while (stack.Count > 0)
		{
			var current = stack.Pop();

			if (current == targetAtom)
			{
				return true;
			}

			visited.Add(current);

			foreach (var bond in bondsList)
			{
				Atom neighbor = null;

				if (bond.bondBase.Atom1 == current.atomBase)
				{
					neighbor = atomList.Find(a => a.atomBase == bond.bondBase.Atom2);
				}
				else if (bond.bondBase.Atom2 == current.atomBase)
				{
					neighbor = atomList.Find(a => a.atomBase == bond.bondBase.Atom1);
				}

				if (neighbor != null && !visited.Contains(neighbor))
				{
					stack.Push(neighbor);
				}
			}
		}

		return false;
	}

	private void UpdateAllBonds()
	{
		foreach (var bond in bondsList)
		{
			bond.UpdateBond();
		}
	}

	private void UpdatePreview(Vector2 mousePos)
	{
		Vector3 newPosition = camera.ProjectRayOrigin(mousePos) + camera.ProjectRayNormal(mousePos) * 2.5f;

		if (previewAtom == null)
		{
			previewAtom = (Atom)atomScene.Instantiate();
			previewAtom.atomBase.CopyData(currentElement);
			previewAtom.SetRadius();
			AddChild(previewAtom);
			previewAtom._Ready(); // Ensure the atom is properly initialized
			previewAtom.SetPreviewMode(true);
		}

		if (previewAtom != null)
		{
			previewAtom.SetPosition(newPosition);
		}

		if (addingAtom != null && previewBond == null)
		{
			previewBond = (Bond)bondScene.Instantiate();
			previewBond.CreateBond(addingAtom.atomBase, previewAtom.atomBase);
			AddChild(previewBond);
			previewBond._Ready(); // Ensure the bond is properly initialized
			previewBond.SetPreviewMode(true);
		}

		if (previewBond != null)
		{
			previewBond.UpdateBond();
		}
	}

	private void ClearPreview()
	{
		if (previewAtom != null)
		{
			previewAtom.QueueFree();
			previewAtom = null;
		}

		if (previewBond != null)
		{
			previewBond.QueueFree();
			previewBond = null;
		}
	}

	private void InitializeSpringSystem()
	{
		List<AtomBase> atomBases = new List<AtomBase>();
		List<BondBase> bondBases = new List<BondBase>();

		foreach (var atom in atomList)
		{
			atomBases.Add(atom.atomBase);
		}

		foreach (var bond in bondsList)
		{
			bondBases.Add(bond.bondBase);
		}

		springSystem = new SpringSystem(atomBases, bondBases);
	}

	private void RunSpringSystemOptimization()
	{
		InitializeSpringSystem();  // Reinitialize to capture current atom positions
		springSystem.Optimize();

		// Update the Godot scene with the new positions
		for (int i = 0; i < atomList.Count; i++)
		{
			atomList[i].SetPosition(ConvertToGodotVector3(springSystem.atoms[i].Position));
		}

		foreach (var bond in bondsList)
		{
			bond.UpdateBond();
		}

		GD.Print("Optimization completed and scene updated.");
	}

	private void ShowFileDialog(FileDialog dialog)
	{
		Input.MouseMode = Input.MouseModeEnum.Visible;
		dialog.Visible = true;
		overlay.Visible = true;
	}

	private void OnSaveFileSelected(string path)
	{

		if (!path.EndsWith(".txt"))
		{
			path += ".txt"; // Default extension
		}

		string zMatrix = ConvertToZMatrix(atomList.ConvertAll(a => a.atomBase), bondsList.ConvertAll(b => b.bondBase));
		SaveZMatrixToFile(zMatrix, path);
		GD.Print($"Molecule successfully saved to {path}");
		Input.MouseMode = Input.MouseModeEnum.Captured;
		overlay.Visible = false;
	}

	private void OnCancel()
	{
		Input.MouseMode = Input.MouseModeEnum.Captured;
		saveFileDialog.Visible = false;
		uploadFileDialog.Visible = false;
		overlay.Visible = false;
	}

	private void UploadMoleculeZMatrix(string filePath)
	{
		string zMatrix = File.ReadAllText(filePath);

		CreateMoleculeFromZMatrix(zMatrix);
		uploading = false;
	}

	private void CreateMoleculeFromZMatrix(string zMatrix)
	{
		// Parse the Z-matrix to get atom and bond data
		var bases = ParseZMatrix(zMatrix);
		var atomBases = bases.Item1;
		var bondBases = bases.Item2;


		// Calculate the position where the first atom should be placed
		Vector3 rayOrigin = camera.GlobalTransform.Origin;
		Vector3 rayDirection = -camera.GlobalTransform.Basis.Z;
		float placementDistance = 2.5f;

		// Calculate the target position for the first atom
		Vector3 targetPosition = rayOrigin + rayDirection * placementDistance;

		// Original position of the first atom from the Z-matrix
		Vector3 firstAtomOriginalPosition = ConvertToGodotVector3(atomBases[0].Position);

		// Calculate the offset needed to move the first atom to the target position
		Vector3 offset = targetPosition - firstAtomOriginalPosition;

		// Dictionary to map AtomBase to the instantiated Atom nodes
		Dictionary<AtomBase, Atom> atomBaseToGodotAtom = new Dictionary<AtomBase, Atom>();

		// Create atoms and add them to the scene
		for (int i = 0; i < atomBases.Count; i++)
		{
			var atomBase = atomBases[i];
			var godotAtom = (Atom)atomScene.Instantiate();

			// Adjust the position by the calculated offset
			Vector3 adjustedPosition = ConvertToGodotVector3(atomBase.Position) + offset;

			godotAtom.atomBase.CopyData(atomBase);
			godotAtom.SetRadius();
			godotAtom.atomColor = new Color(atomBase.AtomColor.X, atomBase.AtomColor.Y, atomBase.AtomColor.Z);

			// Add the atom to the scene tree and the atom list
			AddChild(godotAtom);
			godotAtom.SetPosition(adjustedPosition);
			atomList.Add(godotAtom);

			// Map the atomBase to the instantiated Godot atom
			atomBaseToGodotAtom[atomBase] = godotAtom;
		}

		// Create bonds and add them to the scene
		foreach (var bondBase in bondBases)
		{
			var godotBond = (Bond)bondScene.Instantiate();

			// Get the Godot atoms corresponding to the bond's atom bases
			Atom atom1 = atomBaseToGodotAtom[bondBase.Atom1];
			Atom atom2 = atomBaseToGodotAtom[bondBase.Atom2];

			// Create the bond between the two atoms
			godotBond.CreateBond(atom1.atomBase, atom2.atomBase);

			// Add the bond to the scene tree and the bond list
			AddChild(godotBond);
			bondsList.Add(godotBond);

			// Update the bond to ensure it reflects the new positions of the atoms
			godotBond.UpdateBond();
		}

		// Update the HUD to reflect the new molecule
		UpdateHud();
	}

	private void OnUploadFileSelected(string path)
	{
		UploadMoleculeZMatrix(path);
		GD.Print($"Molecule successfully uploaded to {path}");
		Input.MouseMode = Input.MouseModeEnum.Captured;
		overlay.Visible = false;
	}
}
