using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class PlayerScript : MonoBehaviour, IPunObservable, IKartController
{
    private Rigidbody rb;

    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] private LayerMask slowGround;
    public PhotonView photonView { get; private set; }
    private int lastValue;
    private float CurrentSpeed = 0;
    public float KartMaxSpeed;
    private float MaxSpeed;
    public float boostSpeed;
    public float RealSpeed { get; private set; } //not the applied speed
    public bool canMove;
    public bool star = false;
    public bool BulletBill = false;
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
    private float steerDirection;
    private float driftTime;

    bool driftLeft = false;
    bool driftRight = false;
    float outwardsDriftForce = 50000f;

    public bool isSliding = false;

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
    public bool antiGravity = false;
    float gravity = 0;

    [Header("Sounds")]
    public AudioSource[] soundEffects;

    [Header("Bot Settings")]
    public bool isBotControlled = false;
    public float botSkillLevel = 1.0f; // 0.5-1.5 for varying difficulty
    public bool canUseDrift = true;
    public bool canUseAdvancedMoves = true;
    [SerializeField] private Transform currentCheckpoint;
    private float botReactionTime;
    private float nextMoveDecision;
    private float botTurnSpeed = 1.5f;
    private KartLap kartLap;
    [SerializeField] private AudioClip starTheme;

    // Bot drift state
    private bool botDrifting = false;
    private float botDriftTimer = 0f;
    private float botDriftDuration = 0f;

    // IKartController implementation
    public bool IsBot => isBotControlled;
    public bool IsGrounded => touchingGround;
    public bool Star => star;
    bool IKartController.BulletBill => BulletBill;
    public LayerMask GroundMask => whatIsGround;
    public Rigidbody Rigidbody => rb;
    public PhotonView PhotonView => photonView;
    public Transform Transform => transform;

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
    private void AddBostAfterCountDownEnded()
    {
        BoostTime = 5f;
    }
    private void StallAfterCountDownEnded()
    {
        GetHit(true);
    }
    // Start is called before the first frame update
    void Start()
    {
        photonView = GetComponent<PhotonView>();
        rb = GetComponent<Rigidbody>();
        kartAnimator = holder.GetComponent<Animator>();
        kartLap = GetComponent<KartLap>();

        // Initialize bot settings
        botReactionTime = (2.0f - botSkillLevel) * 0.5f;
        botTurnSpeed = Random.Range(1.4f, 1.8f) * botSkillLevel;

        if (GlobalData.SelectedStage == 13)
        {
            antiGravity = true;
            rb.useGravity = false;
        }

        // Initialize bot checkpoint tracking
        if (isBotControlled)
        {
            lastValue = kartLap.CheckpointIndex;
            currentCheckpoint = GetNextCheckPoint();
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
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
            rb.velocity = Vector3.Lerp(velocityAtLastPacket, latestVel, time);
            return;
        }

        if (!canMove)
        {
            if (Cutscene.instance.isPlaying) return;
            if (alreadyDown) return;
            // Ignora input boost di partenza se è un bot
            if (!isBotControlled && (Input.GetKeyDown(KeyCode.W) || Input.GetButton("Fire2")))
            {
                alreadyDown = true;
                if (Countdown.Instance.canDoRocketBoost)
                {
                    Countdown.Instance.OnCountdownEnded += AddBostAfterCountDownEnded;
                }
                else
                {
                    Countdown.Instance.OnCountdownEnded += StallAfterCountDownEnded;
                }
            }
            return;
        }

        // Ignora input hop/derapata se è un bot
        if (!isBotControlled && ((Input.GetKeyDown(KeyCode.Space) && touchingGround) || (Input.GetButtonDown("Drift") && touchingGround)))
        {
            kartAnimator.SetTrigger("Hop");
            if (steerDirection > 0)
            {
                driftRight = true;
                driftLeft = false;
            }
            else if (steerDirection < 0)
            {
                driftRight = false;
                driftLeft = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (!canMove)
        {
            return;
        }

        move();
        RaycastHit hit;
        if (Physics.Raycast(rayPoint.position, -transform.up, out hit, 1.2f, whatIsGround))
        {
            if (hit.collider.CompareTag("Boost Pad"))
            {
                BoostTime = 1.2f;
            }
        }
        tireSteer();
        steer();
        groundNormalRotation();
        drift();
        boosts();
        if (BoostTime <= 0)
        {
            if (Physics.Raycast(rayPoint.position, -transform.up, out hit, 1.2f, whatIsGround))
            {
                if (hit.collider.gameObject.CompareTag("SlowGround"))
                {
                    MaxSpeed = KartMaxSpeed * 45 / 100;
                    if (CurrentSpeed > MaxSpeed)
                    {
                        CurrentSpeed = MaxSpeed;
                    }
                }
                else
                {
                    MaxSpeed = KartMaxSpeed;
                }
            }
        }
    }

    public void GetHit(bool spin)
    {
        if (star || BulletBill)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(GetHitAnimation(spin));
    }

    IEnumerator GetHitAnimation(bool spin)
    {
        stopDrift();
        canMove = false;
        CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.deltaTime * 1.5f);
        if (spin)
        {
            //kartAnimator.Play("HitSpin");
            kartAnimator.SetTrigger("Spin");
            yield return new WaitForSeconds(1.1f);
        }
        else
        {
            //kartAnimator.Play("HitFlip");
            kartAnimator.SetTrigger("Flip");
            yield return new WaitForSeconds(1.3f);
        }

        canMove = true;
    }

    public void stopDrift()
    {
        driftRight = false;
        driftLeft = false;
        RealSpeed = 0;
        CurrentSpeed = 0;
        //reset everything
        driftTime = 0;
        //stop particles
        for (int i = 0; i < 5; i++)
        {
            ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //right wheel particles
            ParticleSystem.MainModule PSMAIN = DriftPS.main;

            ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //left wheel particles
            ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;

            DriftPS.Stop();
            DriftPS2.Stop();
        }
    }

    private void move()
    {
        RealSpeed = transform.InverseTransformDirection(rb.velocity).z; //real velocity before setting the value. This can be useful if say you want to have hair moving on the player, but don't want it to move if you are accelerating into a wall, since checking velocity after it has been applied will always be the applied value, and not real
        if (BulletBill)
        {
            return;
        }

        bool accelerate = false;
        bool brake = false;

        if (isBotControlled)
        {
            // Bot AI movement logic
            accelerate = true; // Bots generally always accelerate

            // Update checkpoint tracking
            if (lastValue != kartLap.CheckpointIndex)
            {
                lastValue = kartLap.CheckpointIndex;
                currentCheckpoint = GetNextCheckPoint();
            }
        }
        else
        {
            // Player input
            accelerate = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetButton("Fire2");
            brake = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetButton("Fire1");
        }

        if (accelerate)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxSpeed, Time.deltaTime * 0.5f); //speed
            if (!soundEffects[1].isPlaying)
            {
                soundEffects[0].Stop();
                soundEffects[1].Play();
            }
        }
        else if (brake)
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, -MaxSpeed / 1.75f, 1f * Time.deltaTime);
            if (!soundEffects[1].isPlaying)
            {
                soundEffects[0].Stop();
                soundEffects[1].Play();
            }
        }
        else
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, 0, Time.deltaTime * 1.5f); //speed
            if (!soundEffects[0].isPlaying)
            {
                soundEffects[1].Stop();
                soundEffects[0].Play();
            }
        }

        if (!GLIDER_FLY && !antiGravity)
        {
            Vector3 vel = transform.forward * CurrentSpeed;
            vel.y = rb.velocity.y; //gravity
            rb.velocity = vel;
        }
        else if (!GLIDER_FLY && antiGravity)
        {
            Vector3 vel = transform.forward * CurrentSpeed;
            rb.velocity = vel;
            if (!Physics.Raycast(rayPoint.position, -transform.up, 0.75f, whatIsGround))
            {
                if (gravity < 75f)
                {
                    gravity += 5f;
                }
                rb.AddForce(-transform.up * gravity, ForceMode.VelocityChange);
            }
            else
            {
                gravity = 0;
            }
        }
        else
        {
            Vector3 vel = transform.forward * CurrentSpeed;
            vel.y = rb.velocity.y * 0.7f; //gravity with gliding
            rb.velocity = vel;
        }

    }

    private void steer()
    {
        if (BulletBill)
        {
            return;
        }

        // Get steering input (either from player or bot AI)
        if (isBotControlled)
        {
            steerDirection = GetBotSteerDirection();
        }
        else
        {
            steerDirection = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
        }

        Vector3 steerDirVect; //this is used for the final rotation of the kart for steering
        float steerAmount;

        if (driftLeft && !driftRight)
        {
            //steerDirection = Input.GetAxis("Horizontal") < 0 ? -1.5f : -0.5f;
            steerDirection = Mathf.Lerp(-1.5f, -0.3f, (Input.GetAxis("Horizontal") + 1) / 2);
            holder.localRotation = Quaternion.Lerp(holder.localRotation, Quaternion.Euler(0, -20f, 0), 8f * Time.deltaTime);


            if (isSliding && touchingGround)
            {
                rb.AddForce(transform.right * outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
            }
        }
        else if (driftRight && !driftLeft)
        {
            //steerDirection = Input.GetAxis("Horizontal") > 0 ? 1.5f : 0.5f;
            steerDirection = Mathf.Lerp(1.5f, 0.3f, (Input.GetAxis("Horizontal") - 1) / -2);
            holder.localRotation = Quaternion.Lerp(holder.localRotation, Quaternion.Euler(0, 20f, 0), 8f * Time.deltaTime);

            if (isSliding && touchingGround)
            {
                rb.AddForce(-transform.right * outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
            }
        }
        else
        {
            holder.localRotation = Quaternion.Lerp(holder.localRotation, Quaternion.Euler(0, 0f, 0), 8f * Time.deltaTime);
        }

        if ((driftLeft || driftRight) && !GLIDER_FLY)
        {
            rb.AddForce(-transform.up * outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
        }

        //since handling is supposed to be stronger when car is moving slower, we adjust steerAmount depending on the real speed of the kart, and then rotate the kart on its y axis with steerAmount
        steerAmount = RealSpeed > 30 ? RealSpeed / 3 * steerDirection : steerAmount = RealSpeed / 1.25f * steerDirection;

        //glider movements
        if (Input.GetKey(KeyCode.LeftArrow) && GLIDER_FLY || Input.GetKey(KeyCode.A) && GLIDER_FLY)  //left
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 40), 2 * Time.deltaTime);
        } // left 
        else if (Input.GetKey(KeyCode.RightArrow) && GLIDER_FLY || Input.GetKey(KeyCode.D) && GLIDER_FLY) //right
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, -40), 2 * Time.deltaTime);
        } //right
        else if (GLIDER_FLY) //nothing
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(transform.eulerAngles.x, transform.eulerAngles.y, 0), 2 * Time.deltaTime);
        } //nothing

        if (Input.GetKey(KeyCode.UpArrow) && GLIDER_FLY)
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(25, transform.eulerAngles.y, transform.eulerAngles.z), 2 * Time.deltaTime);

            rb.AddForce(Vector3.down * 8000 * Time.deltaTime, ForceMode.Acceleration);
        } //moving down
        else if (Input.GetKey(KeyCode.DownArrow) && GLIDER_FLY)
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(-25, transform.eulerAngles.y, transform.eulerAngles.z), 2 * Time.deltaTime);
            rb.AddForce(Vector3.up * 4000 * Time.deltaTime, ForceMode.Acceleration);

        } //rotating up - only use this if you have special triggers around the track which disable this functionality at some point, or the player will be able to just fly around the track the whole time
        else if (GLIDER_FLY)
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z), 2 * Time.deltaTime);
        }

        if (!touchingGround && !antiGravity)
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, 0), 2 * Time.deltaTime);
        }

        if (!antiGravity)
        {
            steerDirVect = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + steerAmount, transform.eulerAngles.z);
            transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, steerDirVect, 3 * Time.deltaTime);
        }
        else
        {
            transform.RotateAround(transform.position, transform.up, 3 * steerAmount * Time.deltaTime);
        }
    }

    private void groundNormalRotation()
    {
        RaycastHit hit;
        Debug.DrawRay(rayPoint.position, -transform.up * 2f);
        if (Physics.Raycast(rayPoint.position, -transform.up, out hit, 2f, whatIsGround))
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up * 2, hit.normal) * transform.rotation, 7.5f * Time.deltaTime);
            touchingGround = true;
        }
        else
        {
            touchingGround = false;
        }
    }

    private void drift()
    {
        // Handle bot drift timer
        if (isBotControlled && botDrifting)
        {
            botDriftTimer += Time.deltaTime;
            if (botDriftTimer >= botDriftDuration)
            {
                // End bot drift
                botDrifting = false;
                botDriftTimer = 0f;
                driftLeft = false;
                driftRight = false;
                isSliding = false;

                // Give boost and reset
                if (driftTime > 1.5)
                {
                    if (driftTime >= 6)
                        BoostTime = 2.5f;
                    else if (driftTime >= 4)
                        BoostTime = 1.5f;
                    else
                        BoostTime = 0.75f;

                    if (soundEffects.Length > 5) soundEffects[5].Play();
                }

                driftTime = 0;
                StopDriftParticles();
                return;
            }
        }

        // Check if we should be drifting
        bool shouldDrift = false;
        bool hasSteerInput = false;

        if (isBotControlled)
        {
            shouldDrift = botDrifting && touchingGround && CurrentSpeed > 35;
            hasSteerInput = botDrifting; // Bots always have "input" when drifting
        }
        else
        {
            bool driftInput = !GlobalData.UseController ?
                Input.GetKey(KeyCode.Space) : Input.GetButton("Drift");
            float horizontalInput = !GlobalData.UseController ?
                Input.GetAxis("Horizontal") : Input.GetAxis("Horizontal");

            shouldDrift = driftInput && touchingGround && CurrentSpeed > 35 && horizontalInput != 0;
            hasSteerInput = horizontalInput != 0;
        }

        if (driftLeft || driftRight)
        {
            if (shouldDrift && hasSteerInput)
            {
                driftTime += Time.deltaTime;
                isSliding = true;

                //particle effects (sparks)
                if (driftTime >= 1.5 && driftTime < 4)
                {
                    if (soundEffects.Length > 2 && !soundEffects[2].isPlaying)
                    {
                        soundEffects[2].Play();
                        if (soundEffects.Length > 3) soundEffects[3].Stop();
                        if (soundEffects.Length > 4) soundEffects[4].Stop();
                    }
                    UpdateDriftParticles(drift1);
                }
                else if (driftTime >= 4 && driftTime < 6)
                {
                    if (soundEffects.Length > 3 && !soundEffects[3].isPlaying)
                    {
                        soundEffects[3].Play();
                        if (soundEffects.Length > 2) soundEffects[2].Stop();
                        if (soundEffects.Length > 4) soundEffects[4].Stop();
                    }
                    UpdateDriftParticles(drift2);
                }
                else if (driftTime >= 6)
                {
                    if (soundEffects.Length > 4 && !soundEffects[4].isPlaying)
                    {
                        soundEffects[4].Play();
                        if (soundEffects.Length > 2) soundEffects[2].Stop();
                        if (soundEffects.Length > 3) soundEffects[3].Stop();
                    }
                    UpdateDriftParticles(drift3);
                }
            }
        }

        // Check if we should stop drifting
        bool shouldStopDrift = false;

        if (isBotControlled)
        {
            shouldStopDrift = !botDrifting || RealSpeed < 35;
        }
        else
        {
            bool driftInput = !GlobalData.UseController ?
                Input.GetKey(KeyCode.Space) : Input.GetButton("Drift");
            shouldStopDrift = !driftInput || RealSpeed < 35;
        }

        if (shouldStopDrift)
        {
            driftLeft = false;
            driftRight = false;
            isSliding = false;

            //give a boost
            if (driftTime > 1.5 && driftTime < 4)
            {
                BoostTime = 0.75f;
                if (soundEffects.Length > 5) soundEffects[5].Play();
            }
            else if (driftTime >= 4 && driftTime < 7)
            {
                BoostTime = 1.5f;
                if (soundEffects.Length > 5) soundEffects[5].Play();
            }
            else if (driftTime >= 6)
            {
                BoostTime = 2.5f;
                if (soundEffects.Length > 5) soundEffects[5].Play();
            }

            // Stop all drift sounds
            if (soundEffects.Length > 2) soundEffects[2].Stop();
            if (soundEffects.Length > 3) soundEffects[3].Stop();
            if (soundEffects.Length > 4) soundEffects[4].Stop();

            //reset everything
            driftTime = 0;
            StopDriftParticles();
        }
    }

    private void UpdateDriftParticles(Color color)
    {
        for (int i = 0; i < leftDrift.childCount; i++)
        {
            ParticleSystem DriftPS = rightDrift.GetChild(i).gameObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule PSMAIN = DriftPS.main;

            ParticleSystem DriftPS2 = leftDrift.GetChild(i).gameObject.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;

            PSMAIN.startColor = color;
            PSMAIN2.startColor = color;

            if (!DriftPS.isPlaying && !DriftPS2.isPlaying)
            {
                DriftPS.Play();
                DriftPS2.Play();
            }
        }
    }

    private void StopDriftParticles()
    {
        for (int i = 0; i < 5; i++)
        {
            if (i < rightDrift.childCount)
            {
                ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                DriftPS.Stop();
            }

            if (i < leftDrift.childCount)
            {
                ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                DriftPS2.Stop();
            }
        }
    }

    private void boosts()
    {
        BoostTime -= Time.deltaTime;
        if (BoostTime > 0)
        {
            for (int i = 0; i < boostFire.childCount; i++)
            {
                if (!boostFire.GetChild(i).GetComponent<ParticleSystem>().isPlaying)
                {
                    boostFire.GetChild(i).GetComponent<ParticleSystem>().Play();
                }
            }
            MaxSpeed = boostSpeed;

            CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxSpeed, 1 * Time.deltaTime);
        }
        else
        {
            for (int i = 0; i < boostFire.childCount; i++)
            {
                boostFire.GetChild(i).GetComponent<ParticleSystem>().Stop();
            }
            BulletBill = false;
            MaxSpeed = boostSpeed - 20;
        }
    }

    private void tireSteer()
    {
        if (BulletBill) { return; }
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) || steerDirection < -0.2f)
        {
            frontLeftTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 155, 0), 5 * Time.deltaTime);
            frontRightTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 155, 0), 5 * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) || steerDirection > 0.2f)
        {
            frontLeftTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 205, 0), 5 * Time.deltaTime);
            frontRightTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 205, 0), 5 * Time.deltaTime);
        }
        else
        {
            frontLeftTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 180, 0), 5 * Time.deltaTime);
            frontRightTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 180, 0), 5 * Time.deltaTime);
        }

        //tire spinning
        if (CurrentSpeed > 30)
        {
            frontLeftTire.GetChild(0).Rotate(-90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
            frontRightTire.GetChild(0).Rotate(-90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
            backLeftTire.Rotate(90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
            backRightTire.Rotate(90 * Time.deltaTime * CurrentSpeed * 0.5f, 0, 0);
        }
        else
        {
            frontLeftTire.GetChild(0).Rotate(-90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
            frontRightTire.GetChild(0).Rotate(-90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
            backLeftTire.Rotate(90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
            backRightTire.Rotate(90 * Time.deltaTime * RealSpeed * 0.5f, 0, 0);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("GliderPad"))
        {
            GLIDER_FLY = true;
            gliderAnim.SetBool("GliderOpen", true);
            gliderAnim.SetBool("GliderClose", false);
        }
        if (other.gameObject.tag == "GetHitCollider")
        {
            this.GetHit(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (LayerMask.LayerToName(collision.gameObject.layer) == "Ground" || LayerMask.LayerToName(collision.gameObject.layer) == "OffRoad")
        {
            GLIDER_FLY = false;
            gliderAnim.SetBool("GliderOpen", false);
            gliderAnim.SetBool("GliderClose", true);
        }

        if (collision.gameObject.CompareTag("Mushroom"))
        {
            rb.AddForce(transform.up * 2500f);
        }
    }

    [PunRPC]
    void PlayerGetHitRPC(bool b)
    {
        if (!photonView.IsMine)
        {
            return;
        }
        GetHit(b);
    }

    private Transform GetNextCheckPoint()
    {
        LapCheckPoint[] things = FindObjectsOfType<LapCheckPoint>();

        // Use this kart's checkpoint index
        int currentCheckpointIndex = kartLap?.CheckpointIndex ?? 0;

        foreach (var item in things)
        {
            if (item.Index == currentCheckpointIndex + 1)
            {
                return item.transform;
            }
        }

        // If no next checkpoint found, look for checkpoint 1 (start of next lap)
        foreach (var item in things)
        {
            if (item.Index == 1)
            {
                return item.transform;
            }
        }

        // Fallback to first checkpoint found
        return things.Length > 0 ? things[0].transform : null;
    }

    private float GetBotSteerDirection()
    {
        if (currentCheckpoint == null)
        {
            currentCheckpoint = GetNextCheckPoint();
            return 0f;
        }

        // Calculate direction to checkpoint
        Vector3 directionToCheckpoint = (currentCheckpoint.position - transform.position).normalized;
        Vector3 forward = transform.forward;

        // Use cross product to determine turn direction
        Vector3 cross = Vector3.Cross(forward, directionToCheckpoint);
        float steer = cross.y;

        // Apply bot skill level - less skilled bots have more erratic steering
        steer = Mathf.Clamp(steer * botTurnSpeed, -1f, 1f);

        // Add some randomness based on skill level (reduced when drifting)
        float randomnessMultiplier = botDrifting ? 0.1f : 1f; // Less randomness when drifting
        float randomness = (1.0f - botSkillLevel) * 0.3f * randomnessMultiplier;
        steer += Random.Range(-randomness, randomness);

        // Wall avoidance - check for walls ahead
        RaycastHit wallHit;
        Vector3 wallCheckDirection = transform.forward + transform.right * steer;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, wallCheckDirection, out wallHit, 8f))
        {
            if (wallHit.collider.CompareTag("Wall"))
            {
                // Avoid wall by steering away
                Vector3 avoidDirection = Vector3.Reflect(wallCheckDirection, wallHit.normal);
                float avoidSteer = Vector3.Dot(transform.right, avoidDirection.normalized);
                steer = Mathf.Lerp(steer, avoidSteer, 0.8f);

                // Stop drifting if heading toward wall
                if (botDrifting && Vector3.Dot(transform.forward, wallHit.normal) < -0.5f)
                {
                    botDrifting = false;
                    botDriftTimer = 0f;
                    driftLeft = false;
                    driftRight = false;
                    isSliding = false;
                }
            }
        }

        // Bot drift logic - only drift on safe turns
        if (canUseDrift && canUseAdvancedMoves && touchingGround && CurrentSpeed > 35 && !botDrifting)
        {
            float turnAngle = Vector3.Angle(forward, directionToCheckpoint);
            float distanceToCheckpoint = Vector3.Distance(transform.position, currentCheckpoint.position);

            // Only drift if turn is significant, we have enough distance, and no walls detected
            bool shouldStartDrift = turnAngle > 60f && // Increased angle threshold
                                  distanceToCheckpoint > 15f && // Ensure enough distance
                                  Random.value < (botSkillLevel * 0.5f) && // Reduced drift frequency
                                  !Physics.Raycast(transform.position + Vector3.up * 0.5f, wallCheckDirection, 10f); // No walls ahead

            if (shouldStartDrift && !driftLeft && !driftRight && Mathf.Abs(steer) > 0.4f)
            {
                // Start drift based on turn direction
                botDrifting = true;
                botDriftTimer = 0f;
                botDriftDuration = Random.Range(1.5f * botSkillLevel, 3f * botSkillLevel); // Shorter drift duration

                if (steer > 0.4f)
                {
                    driftRight = true;
                    driftLeft = false;
                    // Attiva animazione hop solo se NON è un bot
                    if (!isBotControlled)
                        kartAnimator.SetTrigger("Hop");
                }
                else if (steer < -0.4f)
                {
                    driftLeft = true;
                    driftRight = false;
                    if (!isBotControlled)
                        kartAnimator.SetTrigger("Hop");
                }
            }
        }

        return Mathf.Clamp(steer, -1f, 1f);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(SerializationHelper.SerializeVector3(transform.position));
            stream.SendNext(SerializationHelper.SerializeQuaternion(transform.rotation));
            if (rb != null)
            {
                stream.SendNext(SerializationHelper.SerializeVector3(rb.velocity));
            }
            else
            {
                stream.SendNext(SerializationHelper.SerializeVector3(Vector3.zero));
            }
        }
        else if (stream.IsReading)
        {
            //Network player, receive data
            latestPos = SerializationHelper.DeserializeVector3((byte[])stream.ReceiveNext());
            latestRot = SerializationHelper.DeserializeQuaternion((byte[])stream.ReceiveNext());
            latestVel = SerializationHelper.DeserializeVector3((byte[])stream.ReceiveNext());

            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            positionAtLastPacket = transform.position;
            rotationAtLastPacket = transform.rotation;
            if (rb != null)
            {
                velocityAtLastPacket = rb.velocity;
            }
            else
            {
                velocityAtLastPacket = Vector3.zero;
            }
        }
    }

    public void EnterStarMode()
    {
        StartCoroutine(StarMode());
    }

    public void SwitchToBot()
    {
        isBotControlled = true;
        // Initialize bot AI components
        if (kartLap == null) kartLap = GetComponent<KartLap>();
        currentCheckpoint = GetNextCheckPoint();
        botReactionTime = (2.0f - botSkillLevel) * 0.5f;
        botTurnSpeed = Random.Range(1.4f, 1.8f) * botSkillLevel;
    }

    public void SwitchToPlayer()
    {
        isBotControlled = false;
    }

    private IEnumerator StarMode()
    {
        star = true;
        kartAnimator.SetBool("Star", true);
        BoostTime = 8f;
        if (starTheme != null)
        {
            MusicManager.instance.Stop();
            MusicManager.instance.SetAudioClip(starTheme);
            MusicManager.instance.Play();
        }
        yield return new WaitForSeconds(8f);
        star = false;
        kartAnimator.SetBool("Star", false);
        if (starTheme != null)
        {
            MusicManager.instance.Stop();
            MusicManager.instance.ResetAudioClip();
            MusicManager.instance.Play();
        }
    }
}
