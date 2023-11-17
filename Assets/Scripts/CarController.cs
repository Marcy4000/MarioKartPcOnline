using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using Photon.Pun;

public class CarController : MonoBehaviourPun, IPunObservable
{
    public Character[] characters;
    public int selectedCharacter;

    public Rigidbody theRB;
    public static CarController Instance { get; private set; }

    public float forwardAccel, revesreAccel, maxSpeed, turnStrenght, gravityForce = 10f, dragOnGround = 3f;

    private float speedInput, turnInput;

    public bool grounded { get; private set; }
    [SerializeField] bool autoDisable;

    [SerializeField] public LayerMask whatIsGround;
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
    private bool isDrifting;
    [SerializeField] private ParticleSystem[] driftingParticles;

    public PhotonView pv { get; private set; }
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private AudioClip starTheme;
    private float turnStrBackup;
    private Coroutine driftingCorutine;

    private bool isReady = false;
    public bool isPlayer;

    public bool antiGravity = false;

    //Values that will be synced over network
    Vector3 latestPos, latestVel;
    Quaternion latestRot;
    //Lag compensation
    float currentTime = 0;
    double currentPacketTime = 0;
    double lastPacketTime = 0;
    Vector3 positionAtLastPacket = Vector3.zero;
    Vector3 velocityAtLastPacket = Vector3.zero;
    Quaternion rotationAtLastPacket = Quaternion.identity;

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
        botTurnSpeed = Random.Range(1.4f, 1.8f);
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
            isReady = true;
            yield break;
        }
        theRB.transform.parent = null;

        if (GlobalData.SelectedStage == 13)
        {
            antiGravity = true;
        }

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
        if (antiGravity)
        {
            theRB.useGravity = false;
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
            //Lag compensation
            double timeToReachGoal = currentPacketTime - lastPacketTime;
            currentTime += Time.deltaTime;

            //Update remote player
            rotationAtLastPacket = GlobalData.FixQuaternion(rotationAtLastPacket);
            latestRot = GlobalData.FixQuaternion(latestRot);

            var time = (float)(currentTime / timeToReachGoal);
            //time = Mathf.Clamp01(time);
            time = Mathf.Clamp01((float)(time + Time.deltaTime / timeToReachGoal));
            transform.position = Vector3.Lerp(positionAtLastPacket, latestPos, time);
            transform.rotation = Quaternion.Lerp(rotationAtLastPacket, latestRot, time);
            return;
        }

        if (!canMove)
        {
            return;
        }


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
        /*Quaternion newRotation = Quaternion.FromToRotation(transform.forward, checkpoint.forward) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, botTurnSpeed);*/
        Vector3 lookPos = checkpoint.position - transform.position;
        lookPos += new Vector3(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
        //lookPos.y = 0;
        Quaternion rotation = Quaternion.LookRotation(lookPos, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * botTurnSpeed);
        lastValue = kartLap.CheckpointIndex;

        //transform.position = new Vector3(theRB.transform.position.x, theRB.transform.position.y - kartOffset, theRB.transform.position.z);
        transform.position = theRB.position - (transform.up * kartOffset);
    }

    private IEnumerator Drifting()
    {
        yield return new WaitForSeconds(2.5f);
        foreach (var item in driftingParticles)
        {
            item.Play();
        }
    }

    private void FixedUpdate()
    {
        if (!canMove)
        {
            return;
        }

        if (!grounded)
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, 0), 2 * Time.deltaTime);
        }

        RaycastHit hit;

        Debug.DrawRay(groundRayPoint.position, -transform.up * groundRayLenght);
        if (Physics.Raycast(groundRayPoint.position, -transform.up, out hit, groundRayLenght, whatIsGround))
        {
            Quaternion newRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, newRotation, yourRotationSpeedVariable);
            if (hit.collider.CompareTag("Boost Pad"))
            {
                theRB.AddForce(transform.forward * 12000f);
            }
            theRB.drag = dragOnGround;
            theRB.AddForce(1000f * forwardAccel * transform.forward);
        }
        else
        {
            theRB.drag = 0.1f;
            //theRB.AddForce(100f * -gravityForce * Vector3.up);
        }

        if (antiGravity)
        {
            theRB.AddForce(-transform.up * 75f * theRB.mass);
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

    [PunRPC]
    void BotGetHitRPC(bool b)
    {
        if (!photonView.IsMine)
        {
            return;
        }
        GetHit();
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(SerializationHelper.SerializeVector3(transform.position));
            stream.SendNext(SerializationHelper.SerializeQuaternion(transform.rotation));
        }
        else if (stream.IsReading)
        {
            //Network player, receive data
            latestPos = SerializationHelper.DeserializeVector3((byte[])stream.ReceiveNext());
            latestRot = SerializationHelper.DeserializeQuaternion((byte[])stream.ReceiveNext());

            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            positionAtLastPacket = transform.position;
            rotationAtLastPacket = transform.rotation;
        }
    }
}
