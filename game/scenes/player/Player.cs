using Godot;
using System;
using System.Diagnostics;

// ADAPTED FROM https://web.archive.org/web/20230511075238/http://www.willdonnelly.net/blog/2021-05-16-godot-airstrafe-controller/

public partial class Player : CharacterBody3D
{
    [ExportGroup("Player Components")]
    [Export]
    public Camera3D playerCamera;
    [Export]
    public Camera3D gunmodelCamera;

    //DEBUGDEBUGDEBUG
    //[Export]
    //public Node3D gunmodel;

    [Export]
    public Control playerHUD;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    private float GRAVITY = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    //private const float GRAVITY = 15.34f; // Apparently this is what half life uses. Apparently.

    private float JUMP_IMPULSE;

    const float GROUND_ACCELERATION = 55f;
    const float GROUND_ACCELERATION_SPRINTING = 75f;

    const float AIR_ACCELERATION = 5f; // how long it takes to reach max speed apparently?

    const float MAX_ACCELERATION_GROUND = 4f;
    const float MAX_ACCELERATION_GROUND_SPRINTING = 6.5f;


    const float MAX_ACCELERATION_AIR = .6f;

    const float GROUNDFRICTION = .9f;

    const float TARGET_WALK_FOV = 63f;
    const float TARGET_SPRINT_FOV = 67f;

    // Camera shake stuff


    [ExportGroup("Player control stuff")]

    [Export]
    public float mouseSensitivity = .05f;

    private bool queueJump = false;

    private float currentSpeed;

    private float currentPOV;

    private float currentMaxAccelerationGround;
    private float currentAccelerationGround;

    private bool isStrafing = false;
    private bool isSprinting = false;
    private bool isCrouching = false;

    private bool isFocused = true;

    private Vector3 velocity;
    private Vector3 direction;

    private float playerPOVLerpSpeedDecel = 2.5f;
    private float playerFOVLerpSpeed = 4f;
    private float playerFOVZoomLerpSpeed = 8f;

    private float fovLerp = 1;

    public override void _Ready()
    {
        JUMP_IMPULSE = Mathf.Sqrt(3 * GRAVITY * .85f);

        isFocused = true;
        Input.MouseMode = Input.MouseModeEnum.Captured;

        base._Ready();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion && isFocused)
        {
            InputEventMouseMotion mouseMotion = (@event as InputEventMouseMotion);

            RotateY(-mouseMotion.Relative.X * mouseSensitivity / 15f);

            playerCamera.RotateX(-mouseMotion.Relative.Y * mouseSensitivity / 10f);

            playerCamera.Rotation = new Vector3(Mathf.Clamp(playerCamera.Rotation.X, Mathf.DegToRad(-89f), Mathf.DegToRad(89f)), playerCamera.Rotation.Y, playerCamera.Rotation.Z);

        }

        base._Input(@event);
    }

    public override void _Process(double delta)
    {
        gunmodelCamera.GlobalTransform = playerCamera.GlobalTransform;
        //gunmodelCamera.GlobalPosition += new Vector3(0, .1f, -.25f);

        base._Process(delta);
    }

    public override void _PhysicsProcess(double delta)
    {

        if (Input.IsActionPressed("sprint") && IsOnFloor())
        {
            isSprinting = true;

            currentMaxAccelerationGround = MAX_ACCELERATION_GROUND_SPRINTING;
            currentAccelerationGround = GROUND_ACCELERATION_SPRINTING;
        }
        else
        {
            isSprinting = false;

            currentMaxAccelerationGround = MAX_ACCELERATION_GROUND;
            currentAccelerationGround = GROUND_ACCELERATION;
        }

        if (velocity.Round() == Vector3.Zero) // Only sprinting if moving
            isSprinting = false;

        // Adds gravity.
        if (!IsOnFloor())
            velocity.Y -= GRAVITY * (float)delta;

        HandleMouseState();

        HandleJumping();

        HandleStrafing(delta);

        if (isSprinting)
        {
            currentPOV = Mathf.Lerp(currentPOV, TARGET_SPRINT_FOV, playerFOVLerpSpeed * (float)delta);
        }
        else
        {
            currentPOV = Mathf.Lerp(currentPOV, TARGET_WALK_FOV, playerPOVLerpSpeedDecel * (float)delta);
        }

        //playerCamera.Fov = currentPOV;

        Velocity = velocity;

        MoveAndSlide();

        //GD.Print(playerCamera.GlobalPosition + "- playerCamera");
        //GD.Print(gunmodelCamera.GlobalPosition + "- gunmodelCamera");
        //GD.Print(gunmodel.GlobalPosition + "- gunmodel");

    }

    public void HandleJumping()
    {
        // Handles Jump.
        if (Input.IsActionJustPressed("ui_accept"))
        {
            if (IsOnFloor())
            {
                velocity.Y = JUMP_IMPULSE;
            }
        }
    }

    public void HandleStrafing(double delta)
    {
        Basis basis = GlobalTransform.Basis;
        Vector3 strafeDir = Vector3.Zero;

        // No fucking clue what a basis is but it works so god bless
        if (Input.IsActionPressed("ui_up"))
            strafeDir -= basis.Z;
        if (Input.IsActionPressed("ui_down"))
            strafeDir += basis.Z;
        if (Input.IsActionPressed("ui_left"))
            strafeDir -= basis.X;
        if (Input.IsActionPressed("ui_right"))
            strafeDir += basis.X;

        if(strafeDir == Vector3.Zero)
            isStrafing = false;
        else
            isStrafing = true;

        strafeDir = strafeDir.Normalized();

        // Figure out which strafe force and speed limits apply
        float strafeAcceleration = IsOnFloor() ? (isSprinting ? GROUND_ACCELERATION_SPRINTING : GROUND_ACCELERATION) : AIR_ACCELERATION;
        float speedLimit = IsOnFloor() ? currentMaxAccelerationGround : MAX_ACCELERATION_AIR;

        // Project current velocity onto the strafe direction, and compute a capped
        // acceleration such that *projected* speed will remain within the limit.
        float currentSpeed = strafeDir.Dot(velocity);
        float acceleration = strafeAcceleration * (float)delta;
        acceleration = Mathf.Max(0, Mathf.Min(acceleration, speedLimit - currentSpeed));

        // Resets velocity if on ceiling
        if (IsOnCeiling())
            velocity.Y = 0f;

        velocity += strafeDir * acceleration;

        // Adds friction when on ground
        if (IsOnFloor())
            velocity *= GROUNDFRICTION;
         
    }

    public void HandleMouseState()
    {
        // Toggles mouse capturing 
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            isFocused = !isFocused;

            if (isFocused)
                Input.MouseMode = Input.MouseModeEnum.Captured;
            else
                Input.MouseMode = Input.MouseModeEnum.Visible;
        }
    }
}