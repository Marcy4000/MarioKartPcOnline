using UnityEngine;
using Unity.Netcode;
using System.Collections;
using UnityEngine.InputSystem;
using Unity.Netcode.Components;

public class PlayerScript : NetworkBehaviour
{
    private Rigidbody rb;
    
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask slowGround;
    private float CurrentSpeed = 0;
    public float KartMaxSpeed;
    private float MaxSpeed;
    public float boostSpeed;
    public NetworkVariable<float> RealSpeed = new NetworkVariable<float>();
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>();
    public NetworkVariable<bool> star = new NetworkVariable<bool>();
    public NetworkVariable<bool> BulletBill = new NetworkVariable<bool>();
    public Transform rayPoint;
    public Transform holder;
    private Transform checkpoint;
    public bool GLIDER_FLY;

    public Animator gliderAnim;
    public Animator kartAnimator { get; private set; }

    [Header("Tires")]
    public Transform frontLeftTire;
    public Transform frontRightTire;
    public Transform backLeftTire;
    public Transform backRightTire;

    //drift and steering stuffz
    private NetworkVariable<float> steerDirection = new NetworkVariable<float>();
    private float driftTime;
    bool driftLeft = false;
    bool driftRight = false;
    float outwardsDriftForce = 50000f;

    public NetworkVariable<bool> isSliding = new NetworkVariable<bool>();

    private bool touchingGround;

    [Header("Particles Drift Sparks")]
    public Transform leftDrift;
    public Transform rightDrift;
    public Color drift1;
    public Color drift2;
    public Color drift3;

    public float BoostTime = 0;

    public Transform boostFire;
    public Transform boostExplosion;
    private bool alreadyDown = false;
    public NetworkVariable<bool> antiGravity = new NetworkVariable<bool>();
    float gravity = 0;

    [Header("Sounds")]
    public AudioSource[] soundEffects;

    private StandardInput controls;
    private NetworkAnimator networkAnimator;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        
        controls = new StandardInput();
        controls.Enable();
        controls.KartInput.Move.performed += OnMove;
        controls.KartInput.Move.canceled += OnMove;
        controls.KartInput.Turn.performed += OnTurn;
        controls.KartInput.Turn.canceled += OnTurn;
        
        rb = GetComponent<Rigidbody>();
        kartAnimator = GetComponent<Animator>();
        networkAnimator = GetComponent<NetworkAnimator>();
        
        MaxSpeed = KartMaxSpeed;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;

        controls.KartInput.Move.performed -= OnMove;
        controls.KartInput.Move.canceled -= OnMove;
        controls.KartInput.Turn.performed -= OnTurn;
        controls.KartInput.Turn.canceled -= OnTurn;
        controls.Disable();
    }

    private void OnMove(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        float value = context.ReadValue<float>();
        HandleMoveServerRpc(value);
    }

    private void OnTurn(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        float value = context.ReadValue<float>();
        HandleTurnServerRpc(value);
    }

    [ServerRpc]
    private void HandleMoveServerRpc(float value)
    {
        CurrentSpeed = Mathf.Lerp(CurrentSpeed, value * MaxSpeed, Time.deltaTime * 2f);
        RealSpeed.Value = CurrentSpeed;
    }

    [ServerRpc]
    private void HandleTurnServerRpc(float value)
    {
        steerDirection.Value = value;
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        if (canMove.Value)
        {
            move();
            steer();
        }

        // Drift checks and physics
        if (touchingGround)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation, Time.deltaTime * 8f);
        }
    }

    private void move()
    {
        if (!GLIDER_FLY && !antiGravity.Value)
        {
            Vector3 vel = transform.forward * CurrentSpeed;
            vel.y = rb.velocity.y;
            rb.velocity = vel;
        }
        else if (!GLIDER_FLY && antiGravity.Value)
        {
            Vector3 vel = transform.forward * CurrentSpeed;
            rb.velocity = vel;
        }
    }

    private void steer()
    {
        if (isSliding.Value)
        {
            float steerInput = steerDirection.Value;
            
            if (driftRight)
                steerInput = 1;
            else if (driftLeft)
                steerInput = -1;
                
            transform.Rotate(Vector3.up * steerInput * 2.5f, Space.World);

            if (driftRight)
                rb.AddForce(transform.right * outwardsDriftForce * Time.deltaTime);
            else if (driftLeft)
                rb.AddForce(-transform.right * outwardsDriftForce * Time.deltaTime);

            return;
        }

        transform.Rotate(Vector3.up * steerDirection.Value * 2.5f, Space.World);
    }

    [ServerRpc]
    public void GetHitServerRpc(bool spin)
    {
        GetHitClientRpc(spin);
    }

    [ClientRpc]
    private void GetHitClientRpc(bool spin)
    {
        StartCoroutine(GetHitAnimation(spin));
    }

    private IEnumerator GetHitAnimation(bool spin)
    {
        canMove.Value = false;
        if (spin)
        {
            for (float t = 0; t < 1; t += Time.deltaTime)
            {
                transform.Rotate(Vector3.up * 360 * Time.deltaTime, Space.World);
                yield return null;
            }
        }
        yield return new WaitForSeconds(0.5f);
        canMove.Value = true;
    }

    [ServerRpc]
    public void StopDriftServerRpc()
    {
        isSliding.Value = false;
        driftLeft = false;
        driftRight = false;
    }
}
