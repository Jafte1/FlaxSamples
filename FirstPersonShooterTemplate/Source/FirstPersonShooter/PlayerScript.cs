using System;
using FlaxEngine;
using FlaxEngine.GUI;

struct Cmd { 
    public float horizontal;
    public float vertical;
    public float up;
}

public class PlayerScript : Script
{
    public CharacterController PlayerController;
    public Actor CameraTarget;
    public Camera Camera;

    public UIControl myUI;
    public float uispeed = 0f;
    private Label myLabel;

    public Model SphereModel;

    public float CameraSmoothing = 20.0f;

    private Cmd _cmd;
    public bool CanJump = true;
    public bool UseMouse = true;
    public float JumpForce = 800;

    public float Friction = 6.0f;
    private float playerFriction = 0.0f;
    public float GroundAccelerate = 5000;
    public float AirAccelerate = 10000;
    public float MaxVelocityGround = 400;
    public float MaxVelocityAir = 200;

    public float runDeacceleration = 10.0f;

    private Vector3 _velocity;
    private bool _jump;
    private float _pitch;
    private float _yaw;
    private float _horizontal;
    private float _vertical;

    private bool wishJump = false;

    private float mouseSens = 1; // > 0

    /// <summary>
    /// Adds the movement and rotation to the camera (as input).
    /// </summary>
    /// <param name="horizontal">The horizontal input.</param>
    /// <param name="vertical">The vertical input.</param>
    /// <param name="pitch">The pitch rotation input.</param>
    /// <param name="yaw">The yaw rotation input.</param>
    /// 

    public override void OnStart() {
        _cmd.vertical = 0;
        _cmd.horizontal = 0;
        _cmd.up = 0;
        myLabel = myUI.Get<Label>();
    }
    public void AddMovementRotation(float horizontal, float vertical, float pitch, float yaw)
    {
        _pitch += pitch;
        _yaw += yaw;
        _horizontal += horizontal;
        _vertical += vertical;
    }

    public override void OnUpdate()
    {
        if (UseMouse)
        {
            // Cursor
            Screen.CursorVisible = false;
            Screen.CursorLock = CursorLockMode.Locked;

            // Mouse
            Vector2 mouseDelta = new Vector2(Input.GetAxis("Mouse X")*mouseSens, Input.GetAxis("Mouse Y")*mouseSens);
            _pitch = Mathf.Clamp(_pitch + mouseDelta.Y, -88, 88);
            _yaw += mouseDelta.X;
        }

        // Jump
        CheckJump();
        /*
        if (CanJump && Input.GetAction("Jump")) {
            _jump = true;
        }
        */
        myLabel.Text = uispeed.ToString();
        // Shoot
        if (Input.GetAction("Fire"))
        {
            var ball = new RigidBody
            {
                Name = "Bullet",
                StaticFlags = StaticFlags.None,
                UseCCD = true,
            };
            var ballModel = new StaticModel
            {
                Model = SphereModel,
                Parent = ball,
                StaticFlags = StaticFlags.None
            };
            var ballCollider = new SphereCollider
            {
                Parent = ball,
                StaticFlags = StaticFlags.None
            };
            ball.Transform = new Transform(
                CameraTarget.Position + Horizontal(CameraTarget.Direction) * 70.0f,
                Quaternion.Identity,
                new Vector3(0.1f));
            Level.SpawnActor(ball);
            ball.LinearVelocity = CameraTarget.Direction * 600.0f;
            Destroy(ball, 5.0f);
        }
    }

    private Vector3 Horizontal(Vector3 v)
    {
        return new Vector3(v.X, 0, v.Z);
    }

    private void CheckJump() {

        if (Input.GetAction("Jump") && !wishJump)
            wishJump = true;
        if (Input.GetAction("Jump"))
            wishJump = false;
    }
    private void PM_Friction(Vector3 playerVelocity, float t) {
        Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.Z = 0.0f;
        speed = vec.Length;
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (PlayerController.IsGrounded) {
            control = speed < runDeacceleration ? runDeacceleration : speed;
            drop = control * Friction * Time.DeltaTime * t;
        }

        newspeed = speed - drop;
        playerFriction = newspeed;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        playerVelocity.X *= newspeed;
        playerVelocity.Y *= newspeed;
    }

    public override void OnFixedUpdate()
    {
        // Update camera
        var camTrans = Camera.Transform;
        var camFactor = Mathf.Saturate(CameraSmoothing * Time.DeltaTime);
        CameraTarget.LocalOrientation = Quaternion.Lerp(CameraTarget.LocalOrientation, Quaternion.Euler(_pitch, _yaw, 0), camFactor);
        //CameraTarget.LocalOrientation = Quaternion.Euler(pitch, yaw, 0);
        camTrans.Translation = Vector3.Lerp(camTrans.Translation, CameraTarget.Position, camFactor);
        camTrans.Orientation = CameraTarget.Orientation;
        Camera.Transform = camTrans;

        var inputH = Input.GetAxis("Horizontal") + _horizontal;
        var inputV = Input.GetAxis("Vertical") + _vertical;
        _cmd.horizontal = Input.GetAxis("Horizontal") + _horizontal;
        _cmd.vertical = Input.GetAxis("Vertical") + _vertical;
        _horizontal = 0;
        _vertical = 0;

       // var velocity = new Vector3(inputH, 0.0f, inputV);
        var velocity = new Vector3(_cmd.horizontal,0f,_cmd.vertical);
        velocity.Normalize();
        velocity = CameraTarget.Transform.TransformDirection(velocity);

        //Jump

        //Friction

        if (PlayerController.IsGrounded)
        {
            velocity = MoveGround(velocity.Normalized, Horizontal(_velocity));
           // velocity.Y = -Mathf.Abs(Physics.Gravity.Y * 0.5f);
        }
        else
        {
            velocity = MoveAir(velocity.Normalized, Horizontal(_velocity));
            velocity.Y = _velocity.Y;
        }

        // Fix direction
        if (velocity.Length < 0.05f)
            velocity = Vector3.Zero;

        /*
        if (_jump && PlayerController.IsGrounded)
            velocity.Y = JumpForce;

        _jump = false;
        */

        // Apply gravity
        velocity.Y += -Mathf.Abs(Physics.Gravity.Y * 2.5f) * Time.DeltaTime;

        // Check if player is not blocked by something above head
        if ((PlayerController.Flags & CharacterController.CollisionFlags.Above) != 0)
        {
            if (velocity.Y > 0)
            {
                // Player head hit something above, zero the gravity acceleration
                velocity.Y = 0;
            }
        }

        // Move
        PlayerController.Move(velocity * Time.DeltaTime);
        uispeed = Mathf.Abs(PlayerController.Velocity.Length);
        _velocity = velocity;
    }

    // accelDir: normalized direction that the player has requested to move (taking into account the movement keys and look direction)
    // prevVelocity: The current velocity of the player, before any additional calculations
    // accelerate: The server-defined player acceleration value
    // maxVelocity: The server-defined maximum player velocity (this is not strictly adhered to due to strafejumping)
    private Vector3 Accelerate(Vector3 accelDir, Vector3 prevVelocity, float accel, float maxVelocity,float wishspeed)
    {
        float addspeed, accelspeed, currentspeed;

        currentspeed = Vector3.Dot(prevVelocity, accelDir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0) {
            return Vector3.Zero;
        }
        accelspeed = accel * Time.DeltaTime * wishspeed;
        if (accelspeed > addspeed) {
            accelspeed = addspeed;
        }
  
        return prevVelocity + accelspeed * accelDir;
    }

    private Vector3 MoveGround(Vector3 accelDir, Vector3 prevVelocity)
    {
        // Apply Friction
        float speed = prevVelocity.Length;
        if (Math.Abs(speed) > 0.01f) // To avoid divide by zero errors
        {
            float drop = speed * Friction * Time.DeltaTime;
            prevVelocity *= Mathf.Max(speed - drop, 0) / speed; // Scale the velocity based on friction
        }
        float wishspeed = 400f;
        // GroundAccelerate and MaxVelocityGround are server-defined movement variables
        return Accelerate(accelDir, prevVelocity, GroundAccelerate, MaxVelocityGround,wishspeed);
    }

    private Vector3 MoveAir(Vector3 accelDir, Vector3 prevVelocity)
    {
        float wishspeed = 200f;
        // air_accelerate and max_velocity_air are server-defined movement variables
        return Accelerate(accelDir, prevVelocity, AirAccelerate, MaxVelocityAir,wishspeed);
    }

    public override void OnDebugDraw()
    {
        var trans = PlayerController.Transform;
        DebugDraw.DrawWireTube(trans.Translation, trans.Orientation * Quaternion.Euler(90, 0, 0), PlayerController.Radius, PlayerController.Height, Color.Blue);
    }
}
