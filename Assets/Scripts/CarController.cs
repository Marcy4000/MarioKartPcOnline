using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Photon.Pun;

public class CarController : MonoBehaviourPun
{
    public Character[] characters;
    public int selectedCharacter;

    public Rigidbody theRB;
    public static CarController Instance { get; private set; }

    public float forwardAccel, revesreAccel, maxSpeed, turnStrenght, gravityForce = 10f, dragOnGround = 3f;

    private float speedInput, turnInput;

    public bool grounded { get; private set; }
    [SerializeField] bool autoDisable;

    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private float groundRayLenght;
    [SerializeField] private Transform groundRayPoint;
    [SerializeField] private float yourRotationSpeedVariable;
    [SerializeField] private float kartOffset = 1.14f;
    [SerializeField] private Animator[] tires;

    [SerializeField] private AudioSource idle, moving;

    [Space]
    [SerializeField] private Transform leftFrontWheel, rightFrontWheel;

    public bool botDrive = false;
    [SerializeField] private KartLap kartLap;
    [SerializeField] private Animator animator;
    private Transform checkpoint;
    private int lastValue;
    bool canMove = false;
    public float botTurnSpeed;
    public bool star { get; private set; }
    public bool bulletBil;
    private bool isDrifting, miniTurbo;
    [SerializeField] private ParticleSystem[] driftingParticles;

    public PhotonView pv { get; private set; }
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private AudioClip starTheme;
    private float turnStrBackup;
    private Coroutine driftingCorutine;

    private bool isReady = false;
    public bool isPlayer;

    private void OnEnable()
    {
        Countdown.Instance.OnCountdownEnded += CountdownEnded;
    }

    private void OnDisable()
    {
        Countdown.Instance.OnCountdownEnded -= CountdownEnded;
    }

    private void CountdownEnded()
    {
        canMove = true;
    }

    void Start()
    {
        StartCoroutine(NewStart());
    }

    IEnumerator NewStart()
    {
        while (!GlobalData.HasSceneLoaded)
        {
            yield return null;
        }

        pv = GetComponent<PhotonView>();
        turnStrBackup = turnStrenght;
        botTurnSpeed = Random.Range(0.05f, 0.075f);
        if (autoDisable)
        {
            enabled = false;
            yield break;
        }
        if (!pv.IsMine)
        {
            theRB.constraints = RigidbodyConstraints.FreezeAll;
            lastValue = kartLap.CheckpointIndex;
            checkpoint = GetNextCheckPoint();
            //SetCharacter((int)pv.Owner.CustomProperties["character"]);
            yield break;
        }
        theRB.transform.parent = null;

        if (!botDrive)
        {
            Instance = this;
            CinemachineVirtualCamera vCam = FindObjectOfType<CinemachineVirtualCamera>();
            vCam.Follow = transform;
            vCam.LookAt = transform;
            SetCharacter(GlobalData.SelectedCharacter);
        }
        else
        {
            int botChar = Random.Range(0, characters.Length);
            SetCharacter(botChar);
            pv.RPC("SyncCharacter", RpcTarget.Others, botChar, pv.ViewID);
        }
        lastValue = kartLap.CheckpointIndex;
        checkpoint = GetNextCheckPoint();
        idle.Play();
        moving.Stop();

        isReady = true;
    }

    public void GetHit()
    {
        if (star || bulletBil)
            return;
        theRB.velocity = Vector3.zero;
        theRB.angularVelocity = Vector3.zero;
        theRB.AddForce(transform.up * 4200f, ForceMode.Impulse);
    }

    public void SetCharacter(int character)
    {
        selectedCharacter = character;
        skinnedMeshRenderer.material = characters[character].kartMaterial;
        foreach (var model in characters)
        {
            model.model.SetActive(false);
        }
        characters[character].model.SetActive(true);
        animator = characters[character].modelAnimator;
    }

    [PunRPC]//old stuff
    public void SyncCharacter(int character, int viewid)
    {
        if (photonView.ViewID == viewid)
        {
            SetCharacter(character);
        }
    }

    private void Update()
    {
        if (!isReady)
        {
            return;
        }

        if (!pv.IsMine)
        {
            return;
        }

        if (!canMove)
        {
            return;
        }

        if (!botDrive)
        {
            speedInput = 0;
            if (!GlobalData.UseController)
            {
                if (Input.GetAxis("Vertical") > 0)
                {
                    speedInput = Input.GetAxis("Vertical") * forwardAccel * 1000f;
                }
                else if (Input.GetAxis("Vertical") < 0)
                {
                    speedInput = Input.GetAxis("Vertical") * revesreAccel * 1000f;
                }
            }
            else
            {
                if (Input.GetAxis("MoveCar") > 0)
                {
                    speedInput = Input.GetAxis("MoveCar") * forwardAccel * 1000f;
                }
                else if (Input.GetAxis("MoveCar") < 0)
                {
                    speedInput = Input.GetAxis("MoveCar") * revesreAccel * 1000f;
                }
            }

            if (Input.GetKeyDown(KeyCode.Space) && grounded)
            {
                theRB.AddForce(Vector3.up * 350f, ForceMode.Impulse);
                isDrifting = true;
                if (driftingCorutine != null)
                {
                    StopCoroutine(driftingCorutine);
                }
                driftingCorutine = StartCoroutine(Drifting());
            }

            if (Input.GetKeyUp(KeyCode.Space))
            {
                isDrifting = false;
                if (miniTurbo)
                {
                    theRB.AddForce(transform.forward * 3000f, ForceMode.Impulse);
                    miniTurbo = false;
                    foreach (var item in driftingParticles)
                    {
                        item.Stop();
                    }
                }
                else
                {
                    if (driftingCorutine != null)
                    {
                        StopCoroutine(driftingCorutine);
                    }
                    foreach (var item in driftingParticles)
                    {
                        item.Stop();
                    }
                }
            }

            turnInput = Input.GetAxis("Horizontal");

            leftFrontWheel.transform.localRotation = Quaternion.Euler(leftFrontWheel.localRotation.eulerAngles.x, (turnInput * 30) - 180, leftFrontWheel.localRotation.eulerAngles.z);
            rightFrontWheel.transform.localRotation = Quaternion.Euler(rightFrontWheel.localRotation.eulerAngles.x, (turnInput * 30) - 180, rightFrontWheel.localRotation.eulerAngles.z);

            if (!GlobalData.UseController)
            {
                foreach (var tire in tires)
                {
                    tire.SetFloat("Blend", Input.GetAxis("Vertical"));
                }
            }
            else
            {
                foreach (var tire in tires)
                {
                    tire.SetFloat("Blend", Input.GetAxis("MoveCar"));
                }
            }



            if (grounded)
            {
                if (isDrifting)
                {
                    turnStrenght = turnStrBackup;
                }
                else
                {
                    turnStrenght = turnStrBackup / 1.5f;
                }
            }
            else
            {
                turnStrenght = turnStrBackup / 2.5f;
            }

            if (!GlobalData.UseController)
            {
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, turnInput * turnStrenght * Input.GetAxis("Vertical") * Time.deltaTime, 0));
            }
            else
            {
                transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles + new Vector3(0f, turnInput * turnStrenght * Input.GetAxis("MoveCar") * Time.deltaTime, 0));
            }

            if (Mathf.Abs(speedInput) > 0 && !moving.isPlaying)
            {
                idle.Stop();
                moving.Play();
            }
            else if (Mathf.Abs(speedInput) <= 0 && !idle.isPlaying)
            {
                idle.Play();
                moving.Stop();
            }
            animator.SetFloat("Direction", Input.GetAxis("Horizontal"));
        }
        else
        {
            if (!moving.isPlaying)
            {
                idle.Stop();
                moving.Play();
            }
            if (lastValue != kartLap.CheckpointIndex)
            {
                checkpoint = GetNextCheckPoint();
            }
            foreach (var tire in tires)
            {
                tire.SetFloat("Blend", 1);
            }
            Quaternion newRotation = Quaternion.FromToRotation(transform.forward, checkpoint.forward) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, botTurnSpeed);
            lastValue = kartLap.CheckpointIndex;
        }
        transform.position = new Vector3(theRB.transform.position.x, theRB.transform.position.y - kartOffset, theRB.transform.position.z);
    }

    private IEnumerator Drifting()
    {
        yield return new WaitForSeconds(2.5f);
        foreach (var item in driftingParticles)
        {
            item.Play();
        }
        miniTurbo = true;
    }

    private void FixedUpdate()
    {
        if (!canMove)
        {
            return;
        }

        grounded = false;
        RaycastHit hit;

        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLenght, whatIsGround))
        {
            grounded = true;

            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, yourRotationSpeedVariable);
            if (hit.collider.CompareTag("Boost Pad"))
            {
                theRB.AddForce(transform.forward * 12000f);
            }
        }

        if (!botDrive)
        {
            if (grounded)
            {
                theRB.drag = dragOnGround;
                if (Mathf.Abs(speedInput) > 0)
                {
                    theRB.AddForce(transform.forward * speedInput);
                }
            }
            else
            {
                theRB.drag = 0.1f;
                //theRB.AddForce(100f * -gravityForce * Vector3.up);
            }
        }
        else
        {
            if (grounded)
            {
                theRB.drag = dragOnGround;
                theRB.AddForce(1000f * forwardAccel * transform.forward);
            }
            else
            {
                theRB.drag = 0.1f;
                //theRB.AddForce(100f * -gravityForce * Vector3.up);
            }
        }

        if (theRB.velocity.magnitude > maxSpeed)
        {
            theRB.velocity = theRB.velocity.normalized * maxSpeed;
        }
    }

    public void EnterStarmode()
    {
        StartCoroutine(StarMan());
    }

    private IEnumerator StarMan()
    {
        star = true;
        forwardAccel = forwardAccel * 1.3f;
        MusicManager.instance.Stop();
        MusicManager.instance.SetAudioClip(starTheme);
        MusicManager.instance.Play();
        yield return new WaitForSeconds(8f);
        star = false;
        forwardAccel = forwardAccel / 1.3f;
        MusicManager.instance.Stop();
        MusicManager.instance.ResetAudioClip();
        MusicManager.instance.Play();
    }

    private Transform GetCurrentCheckPoint()
    {
        LapCheckPoint[] things = FindObjectsOfType<LapCheckPoint>();

        foreach (var item in things)
        {
            if (item.Index == kartLap.CheckpointIndex)
            {
                return item.transform;
            }
        }

        foreach (var item in things)
        {
            if (item.Index == 1)
            {
                return item.transform;
            }
        }

        return things[0].transform;
    }

    private Transform GetNextCheckPoint()
    {
        LapCheckPoint[] things = FindObjectsOfType<LapCheckPoint>();

        foreach (var item in things)
        {
            if (item.Index == kartLap.CheckpointIndex)
            {
                return item.next;
            }
        }

        foreach (var item in things)
        {
            if (item.Index == 1)
            {
                return item.transform;
            }
        }

        return things[0].transform;
    }
}
