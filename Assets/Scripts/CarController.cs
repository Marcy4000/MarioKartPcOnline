using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class CarController : NetworkBehaviour
{
    public KartCharacter[] characters;
    public NetworkVariable<int> selectedCharacter = new NetworkVariable<int>();
    [SerializeField] private Transform characterSpawnParent;

    public Rigidbody theRB;
    public static CarController Instance { get; private set; }

    public float forwardAccel, revesreAccel, maxSpeed, turnStrenght, gravityForce = 10f, dragOnGround = 3f;

    private NetworkVariable<float> speedInput = new NetworkVariable<float>();
    private NetworkVariable<float> turnInput = new NetworkVariable<float>();

    public NetworkVariable<bool> grounded = new NetworkVariable<bool>();
    [SerializeField] bool autoDisable;

    [SerializeField] public LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float kartOffset = 1.14f;
    [SerializeField] private Animator[] tires;

    [SerializeField] private AudioSource idle, moving;

    [Space]
    [SerializeField] private Transform leftFrontWheel, rightFrontWheel;

    public NetworkVariable<bool> botDrive = new NetworkVariable<bool>();
    [SerializeField] private KartLap kartLap;
    [SerializeField] private Animator animator;
    private Transform checkpoint;
    private int lastValue;
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>();
    public float botTurnSpeed;
    public NetworkVariable<bool> star = new NetworkVariable<bool>();
    public NetworkVariable<bool> bulletBil = new NetworkVariable<bool>();
    private NetworkVariable<bool> isDrifting = new NetworkVariable<bool>();
    [SerializeField] private ParticleSystem[] driftingParticles;

    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private AudioClip starTheme;
    private float turnStrBackup;
    private Coroutine driftingCoroutine;

    private NetworkVariable<bool> isReady = new NetworkVariable<bool>();
    public NetworkVariable<bool> isPlayer = new NetworkVariable<bool>();

    public NetworkVariable<bool> antiGravity = new NetworkVariable<bool>();

    private StandardInput controls;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        controls = new StandardInput();
        controls.Enable();

        theRB = GetComponent<Rigidbody>();
        turnStrBackup = turnStrenght;

        StartCoroutine(NewStart());
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner) return;
        controls.Disable();
    }

    private IEnumerator NewStart()
    {
        yield return new WaitUntil(() => GlobalData.HasSceneLoaded);

        GameEvent.current.onCountdownEnded += CountdownEnded;
        botDrive.Value = !isPlayer.Value;

        if (botDrive.Value)
        {
            checkpoint = CheckpointManager.instance.checkpoints[0];
            canMove.Value = true;
        }

        isReady.Value = true;
    }

    [ServerRpc]
    public void GetHitServerRpc()
    {
        GetHitClientRpc();
    }

    [ClientRpc]
    private void GetHitClientRpc()
    {
        StartCoroutine(BotHitCoroutine());
    }

    private IEnumerator BotHitCoroutine()
    {
        canMove.Value = false;
        yield return new WaitForSeconds(1f);
        canMove.Value = true;
    }

    [ServerRpc]
    public void SetCharacterServerRpc(int character)
    {
        selectedCharacter.Value = character;
        SetCharacterClientRpc(character);
    }

    [ClientRpc]
    private void SetCharacterClientRpc(int character)
    {
        if (characterSpawnParent.childCount > 0)
        {
            Destroy(characterSpawnParent.GetChild(0).gameObject);
        }

        Instantiate(characters[character].characterModel, characterSpawnParent.position, characterSpawnParent.rotation, characterSpawnParent);
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isReady.Value) return;

        if (grounded.Value)
        {
            theRB.drag = dragOnGround;

            if (Mathf.Abs(speedInput.Value) > 0)
            {
                theRB.AddForce(transform.forward * speedInput.Value);
            }
        }
        else
        {
            theRB.drag = 0.1f;
            theRB.AddForce(Vector3.up * -gravityForce * 100);
        }

        if (botDrive.Value)
        {
            BotUpdate();
        }
    }

    private void BotUpdate()
    {
        if (!canMove.Value) return;

        Vector3 targetDirection = (checkpoint.position - transform.position).normalized;
        float angle = Vector3.SignedAngle(transform.forward, targetDirection, Vector3.up);

        turnInput.Value = Mathf.Clamp(angle / 90f, -1f, 1f);
        speedInput.Value = forwardAccel;

        transform.rotation = Quaternion.Lerp(transform.rotation,
            Quaternion.LookRotation(targetDirection, transform.up),
            Time.deltaTime * botTurnSpeed);
    }

    private void CountdownEnded()
    {
        canMove.Value = true;
    }

    [ServerRpc]
    public void EnterStarModeServerRpc()
    {
        star.Value = true;
        StartCoroutine(StarMan());
    }

    private IEnumerator StarMan()
    {
        AudioSource.PlayClipAtPoint(starTheme, transform.position);
        yield return new WaitForSeconds(10);
        star.Value = false;
    }
}
