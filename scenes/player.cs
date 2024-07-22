using Godot;
using System;

public partial class player : CharacterBody3D
{

	public Node3D head;
	public Camera3D cam;
	public RayCast3D raycast;

	public const float Speed = 15.0f;
	public const float JumpVelocity = 4.5f;
	public const float Sensitivity = 0.01f;

	public const float gravity = 10.0f;

	public override void _Ready()
	{
		head = GetNode<Node3D>("Head");
		if (head == null)
		{
			GD.PrintErr("Head node not found!");
			return;
		}
		
		cam = GetNode<Camera3D>("Head/Camera");
		if (cam == null)
		{
			GD.PrintErr("Camera node not found!");
			return;
		}

		raycast = GetNode<RayCast3D>("Head/Camera/RayCast");
		if (cam == null)
		{
			GD.PrintErr("Raycast node not found!");
			return;
		}

		// raycast.TargetPosition = new Vector3(0, 0, rayDistance);

		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			if (head != null && cam != null)
			{
				InputEventMouseMotion mouseMotion = @event as InputEventMouseMotion;
				head.RotateY(-mouseMotion.Relative.X * Sensitivity);
				cam.RotateX(-mouseMotion.Relative.Y * Sensitivity);

				Vector3 cameraRot = cam.Rotation;
				cameraRot.X = Mathf.Clamp(cameraRot.X, Mathf.DegToRad(-80f), Mathf.DegToRad(80f));
				cam.Rotation = cameraRot;
			}
			else
			{
				GD.PrintErr("Head or Camera node is not initialized!");
			}
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;

		// Add the gravity.
		if (!IsOnFloor())
			velocity.Y -= gravity * (float)delta;

		// Handle Jump.
		if (Input.IsActionJustPressed("ui_accept") && IsOnFloor())
			velocity.Y = JumpVelocity;

		// Get the input direction and handle the movement/deceleration.
		Vector2 inputDir = Input.GetVector("left", "right", "up", "down");
		Vector3 forward = cam.GlobalTransform.Basis.Z.Normalized();
		Vector3 right = cam.GlobalTransform.Basis.X.Normalized();
		forward.Y = 0;
		right.Y = 0;
		forward = forward.Normalized();
		right = right.Normalized();

		Vector3 direction = (forward * inputDir.Y + right * inputDir.X).Normalized();
		if (direction != Vector3.Zero)
		{
			velocity.X = direction.X * Speed;
			velocity.Z = direction.Z * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
	}
}
