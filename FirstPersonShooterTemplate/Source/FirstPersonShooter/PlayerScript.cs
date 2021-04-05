using System;
using FlaxEngine;
using FlaxEngine.GUI;

struct Cmd { 
    public float horizontal;
    public float vertical;
    public float up;
}
struct Pml {
    public Vector3 horizontal;
    public Vector3 vertical;
    public Vector3 up;

}
public class PlayerScript : Script
{
    public CharacterController PlayerController;
    public Actor CameraTarget;
    public Camera Camera;

    public UIControl myUI;
    public float uispeed = 0f;
    private Label myLabel;
    private Label tester1;

    public Model SphereModel;

    public float CameraSmoothing = 20.0f;

    private Cmd _cmd;
    private Pml _pml;
    public bool CanJump = true;
    public bool UseMouse = true;
    public float JumpForce = 800;

    public float runDeacceleration = 10.0f;

    private Vector3 _velocity;
    private bool _jump;
    private float _pitch;
    private float _yaw;
    private float _horizontal;
    private float _vertical;
    private Vector3 moveDirectionNorm = Vector3.Zero;

    private bool wishJump = false;

    // movement parameters
    private float CM_stopspeed = 100.0f;
    private float CM_duckScale = 0.25f;
    private float CM_swimScale = 0.50f;

    private float CM_accelerate = 10.0f;
    private float CM_airaccelerate = 1.0f;
    private float CM_wateraccelerate = 4.0f;
    private float CM_flyaccelerate = 8.0f;

    private float CM_friction = 6.0f;
    private float CM_waterfriction = 1.0f;
    private float CM_flightfriction = 3.0f;
    private float CM_spectatorfriction = 5.0f;

    private float CM_groundspeed = 400;
    private float CM_airspeed = 400;

    private float playerFriction = 0.0f;

    int c_pmove = 0;

    public float uit = 0;

    private float mouseSens = 1; // > 0
    private bool walking = true;
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

        _pml.vertical = Vector3.Zero;
        _pml.horizontal = Vector3.Zero;
        _pml.up = Vector3.Zero;

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

        //myLabel.Text = uispeed.ToString();
        myLabel.Text = uit.ToString();

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
                CameraTarget.Position + (CameraTarget.Direction.Z) * 70.0f,
                Quaternion.Identity,
                new Vector3(0.1f));
            Level.SpawnActor(ball);
            ball.LinearVelocity = CameraTarget.Direction * 600.0f;
            Destroy(ball, 5.0f);
        }
    }

    private float PM_CmdScale(Cmd cmd, float maxspeed = 0f) {
        float max = 0;
        float total = 0;
        float scale = 0;

        //max = abs(cmd->forwardmove);
        max = Mathf.Abs(cmd.vertical);
        if (Mathf.Abs(cmd.horizontal) > max) {
            max = Mathf.Abs(cmd.horizontal);
        }

        if (Mathf.Abs(cmd.up) > max) {
            max = Mathf.Abs(cmd.up);
        }
        if (max.Equals(null)) {
            return 0;
        }

        total = Mathf.Sqrt(cmd.vertical * cmd.vertical
            + cmd.horizontal * cmd.horizontal + cmd.up * cmd.up);
        if (total == 0) {
            return 0f;
        }
        scale = maxspeed * max / (127.0f * total);
        
        return scale;
    }
    private void CheckJump() {

        if (Input.GetAction("Jump") && !wishJump)
            wishJump = true;
        if (Input.GetAction("Jump"))
            wishJump = false;
    }
    private Vector3 PM_Friction(Vector3 playerVelocity) {
        Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
        float speed;
        float newspeed;
        float control;
        float drop;

       if (walking) { 
            vec.Y = 0.0f; 
        }
        speed = vec.Length;
        if (speed < 1) {
            playerVelocity.X = 0;
            playerVelocity.Y = 0; // underwater sinking?
            return playerVelocity;
        }
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (PlayerController.IsGrounded) {
            control = speed < CM_stopspeed ? CM_stopspeed : speed;
            drop += control * CM_friction * Time.DeltaTime;
        }

        newspeed = speed - drop;
        playerFriction = newspeed;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        playerVelocity.X *= newspeed;
        playerVelocity.Y *= newspeed;
        playerVelocity.Z *= newspeed;
        return playerVelocity;
    }

    private Vector3 AddFriction(Vector3 prevVelocity) {
        float speed = prevVelocity.Length;
        if (Math.Abs(speed) > 0.01f) // To avoid divide by zero errors
        {
            float drop = speed * CM_friction * Time.DeltaTime;
            prevVelocity *= Mathf.Max(speed - drop, 0) / speed; // Scale the velocity based on friction
        }
        return prevVelocity;
    }

    public override void OnFixedUpdate()
    {
        var velocity = Vector3.Zero;
        // Update camera
        var camTrans = Camera.Transform;
        var camFactor = Mathf.Saturate(CameraSmoothing * Time.DeltaTime);
        CameraTarget.LocalOrientation = Quaternion.Lerp(CameraTarget.LocalOrientation, Quaternion.Euler(_pitch, _yaw, 0), camFactor);
        //CameraTarget.LocalOrientation = Quaternion.Euler(pitch, yaw, 0);
        camTrans.Translation = Vector3.Lerp(camTrans.Translation, CameraTarget.Position, camFactor);
        camTrans.Orientation = CameraTarget.Orientation;
        Camera.Transform = camTrans;

        _cmd.horizontal = Input.GetAxis("Horizontal") + _horizontal;
        _cmd.vertical = Input.GetAxis("Vertical") + _vertical;
        _horizontal = 0;
        _vertical = 0;

        //Jump

        if (PlayerController.IsGrounded)
        {
            velocity = PM_GroundMove( _velocity);
            //velocity = GroundMove(_velocity);

        }
        else
        {
            velocity = PM_AirMove( _velocity);
            //velocity = AirMove(_velocity);
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
        //velocity.Y += -Mathf.Abs(Physics.Gravity.Y * 2.5f) * Time.DeltaTime;

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

    private Vector3 Accelerate(Vector3 accelDir, Vector3 prevVelocity, float accel,float wishspeed)
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
    private Vector3 PM_AirMove(Vector3 prevVelocity ) {
        int i;
        Vector3 wishvel = Vector3.Zero;
        float fmove = 0; 
        float smove = 0;
        Vector3 wishdir = Vector3.Zero;
        float wishspeed = 0;
        float scale = 0;
        Cmd cmd;

        prevVelocity = PM_Friction(prevVelocity);

        //fmove = pm->cmd.forwardmove;
        //smove = pm->cmd.rightmove;

        fmove = _cmd.vertical;
        smove = _cmd.horizontal;

        cmd = _cmd;
        scale = PM_CmdScale(cmd,CM_airspeed);

        // set the movementDir so clients can rotate the legs for strafing
        PM_SetMovementDir();

        // project moves down to flat plane
        _pml.vertical.Y = 0;
        _pml.horizontal.Y = 0;
        //VectorNormalize(pml.forward);
        //VectorNormalize(pml.right);
        _pml.vertical.Normalize();
        _pml.horizontal.Normalize();
        for (i = 0; i < 2; i++) {
            wishvel[i] = _pml.vertical[i] * fmove + _pml.horizontal[i] * smove;
        }
        wishvel[2] = 0;

        //VectorCopy(wishvel, wishdir);
        wishdir.X = wishvel.Y;
        wishdir.Y = wishvel.Y;
        wishdir.Z = wishvel.Z;
        //wishspeed = VectorNormalize(wishdir);
        wishspeed = wishdir.Length;
        wishspeed *= scale;

        // not on ground, so little effect on velocity
        prevVelocity = Accelerate(wishdir,prevVelocity, wishspeed, CM_airaccelerate);

        // we may have a ground plane that is very steep, even
        // though we don't have a groundentity
        // slide along the steep plane
        /*
        if (pml.groundPlane) {
            PM_ClipVelocity(pm->ps->velocity, pml.groundTrace.plane.normal,
                pm->ps->velocity, OVERCLIP);
        }
        */
        /*
#if false
	//ZOID:  If we are on the grapple, try stair-stepping
	//this allows a player to use the grapple to pull himself
	//over a ledge
	if (pm->ps->pm_flags & PMF_GRAPPLE_PULL)
		PM_StepSlideMove ( qtrue );
	else
		PM_SlideMove ( qtrue );
#endif
        */
        PM_StepSlideMove(true);
        prevVelocity.Y += -Mathf.Abs(Physics.Gravity.Y) * Time.DeltaTime;
        return prevVelocity;
    }

    private void PM_StepSlideMove(bool qbool) {


    }
    private void PM_SetMovementDir() {

    }
    private Vector3 PM_GroundMove( Vector3 prevVelocity)
    {
        //uit = 1;
        int i = 0;
        Vector3 wishvel = Vector3.Zero;
        float fmove = 0;
        float smove = 0;
        Vector3 wishdir = Vector3.Zero;
        float wishspeed = 0;
        float scale =0 ;
        Cmd cmd;
        float accelerate;
        float vel;

        // Apply Friction
        prevVelocity = AddFriction(prevVelocity);
        //prevVelocity = PM_Friction(prevVelocity);


        fmove = _cmd.vertical;
        smove = _cmd.horizontal;

        cmd = _cmd;
        scale = PM_CmdScale(cmd,CM_groundspeed);
        //uit = scale;
        wishdir = new Vector3(cmd.horizontal,0,cmd.vertical);
       
        // project moves down to flat plane
        _pml.vertical.Y = 0;
        _pml.horizontal.Y = 0;
        uit = _pml.vertical.X;
        //PM_ClipVelocity(pml.forward, pml.groundTrace.plane.normal, pml.forward, OVERCLIP);
        //PM_ClipVelocity(pml.right, pml.groundTrace.plane.normal, pml.right, OVERCLIP);

        _pml.vertical.Normalize();
        _pml.horizontal.Normalize();

        for (i = 0; i < 2; i++) {
            wishvel[i] = _pml.vertical[i] * fmove + _pml.horizontal[i] * smove;
        }
        wishvel[2] = 0;

        wishdir.X = wishvel.Y;
        wishdir.Y = wishvel.Y;
        wishdir.Z = wishvel.Z;

        wishspeed = wishdir.Normalized.Length;
        wishspeed *= scale;
        //uit = wishspeed;

        /*
        // clamp the speed lower if ducking
        if (pm->ps->pm_flags & PMF_DUCKED) {
            if (wishspeed > pm->ps->speed * pm_duckScale) {
                wishspeed = pm->ps->speed * pm_duckScale;
            }
        }

        // clamp the speed lower if wading or walking on the bottom
        if (pm->waterlevel) {
            float waterScale;

            waterScale = pm->waterlevel / 3.0;
            waterScale = 1.0 - (1.0 - pm_swimScale) * waterScale;
            if (wishspeed > pm->ps->speed * waterScale) {
                wishspeed = pm->ps->speed * waterScale;
            }
        }

        // when a player gets hit, they temporarily lose
        // full control, which allows them to be moved a bit
        if ((pml.groundTrace.surfaceFlags & SURF_SLICK) || pm->ps->pm_flags & PMF_TIME_KNOCKBACK) {
            accelerate = pm_airaccelerate;
        } else {
            accelerate = pm_accelerate;
        }
        */

        prevVelocity = Accelerate(wishdir, prevVelocity, CM_accelerate, wishspeed);

        //Com_Printf("velocity = %1.1f %1.1f %1.1f\n", pm->ps->velocity[0], pm->ps->velocity[1], pm->ps->velocity[2]);
        //Com_Printf("velocity1 = %1.1f\n", VectorLength(pm->ps->velocity));

        //if ((pml.groundTrace.surfaceFlags & SURF_SLICK) || pm->ps->pm_flags & PMF_TIME_KNOCKBACK) {
        //    pm->ps->velocity[2] -= pm->ps->gravity * pml.frametime;
        //} else {
            // don't reset the z velocity for slopes
            //		pm->ps->velocity[2] = 0;
        //}

        //vel = VectorLength(pm->ps->velocity);

        // slide along the ground plane
        //PM_ClipVelocity(pm->ps->velocity, pml.groundTrace.plane.normal,
        //    pm->ps->velocity, OVERCLIP);

        // don't decrease velocity when going up or down a slope
        //VectorNormalize(pm->ps->velocity);
        //VectorScale(pm->ps->velocity, vel, pm->ps->velocity);

        // don't do anything if standing still
        //if (!pm->ps->velocity[0] && !pm->ps->velocity[1]) {
        //    return;
        //}

        PM_StepSlideMove(false);

        //Com_Printf("velocity2 = %1.1f\n", VectorLength(pm->ps->velocity));

        return prevVelocity;
    }

    private Vector3 GroundMove(Vector3 prevVelocity) {
        uit = 1f;
        Vector3 wishdir;

        // Do not apply friction if the player is queueing up the next jump
        
        prevVelocity =    PM_Friction(prevVelocity);

        //SetMovementDir();

        wishdir = new Vector3(_cmd.horizontal, 0, _cmd.vertical);
        //wishdir = this.Transform.TransformDirection(wishdir);
        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        var wishspeed = wishdir.Length;
        wishspeed *= CM_groundspeed;

        prevVelocity =Accelerate(wishdir,prevVelocity, CM_accelerate, wishspeed);

        // Reset the gravity velocity
        /*
        prevVelocity.Y = -Physics.Gravity.Y * Time.DeltaTime;

        if (wishJump) {
            prevVelocity.Y = JumpForce;
            wishJump = false;
        }
        */
        return prevVelocity;
    }
    
    private Vector3 AirMove(Vector3 prevVelocity) {

        Vector3 wishdir;
        float wishvel = CM_airaccelerate;
        float accel;

        wishdir = new Vector3(_cmd.horizontal, 0, _cmd.vertical);
        wishdir = Transform.TransformDirection(wishdir);

        float wishspeed = wishdir.Length;
        wishspeed *= CM_airspeed;

        wishdir.Normalize();
        moveDirectionNorm = wishdir;

        // CPM: Aircontrol
        float wishspeed2 = wishspeed;
        if (Vector3.Dot(prevVelocity, wishdir) < 0)
            accel = CM_stopspeed;
        else
            accel = CM_airaccelerate;
        // If the player is ONLY strafing left or right
        //if (_cmd.vertical == 0 && _cmd.horizontal != 0) {
         //   if (wishspeed > sideStrafeSpeed)
         //       wishspeed = sideStrafeSpeed;
         //   accel = sideStrafeAcceleration;
        //}

        prevVelocity = Accelerate(wishdir,prevVelocity, accel, wishspeed);
        //if (airControl > 0)
        //   AirControl(wishdir, wishspeed2);
        // !CPM: Aircontrol

        // Apply gravity
        prevVelocity.Y += -Mathf.Abs(Physics.Gravity.Y * 2.5f) * Time.DeltaTime;
        return prevVelocity;
    }
    public override void OnDebugDraw()
    {
        var trans = PlayerController.Transform;
        DebugDraw.DrawWireTube(trans.Translation, trans.Orientation * Quaternion.Euler(90, 0, 0), PlayerController.Radius, PlayerController.Height, Color.Blue);
    }
}
