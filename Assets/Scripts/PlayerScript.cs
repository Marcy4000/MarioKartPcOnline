using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class PlayerScript : MonoBehaviour, IPunObservable
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
    private float RealSpeed; //not the applied speed
    public bool canMove;
    public bool star= false;
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

    [Header("Sounds")]
    public AudioSource[] soundEffects;

    //Values that will be synced over network
    Vector3 latestPos;
    Quaternion latestRot;
    //Lag compensation
    float currentTime = 0;
    double currentPacketTime = 0;
    double lastPacketTime = 0;
    Vector3 positionAtLastPacket = Vector3.zero;
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
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            //Lag compensation
            double timeToReachGoal = currentPacketTime - lastPacketTime;
            currentTime += Time.deltaTime;

            //Update remote player
            transform.position = Vector3.Lerp(positionAtLastPacket, latestPos, (float)(currentTime / timeToReachGoal));
            transform.rotation = Quaternion.Lerp(rotationAtLastPacket, latestRot, (float)(currentTime / timeToReachGoal));
            return;
        }

        if (!canMove)
        {
            if (Cutscene.instance.isPlaying) return;
            if (alreadyDown) return;
            if (Input.GetKeyDown(KeyCode.W) || Input.GetButton("Fire2"))
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

        if (Input.GetKeyDown(KeyCode.Space) && touchingGround || Input.GetButtonDown("Drift") && touchingGround)
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
        if ((driftLeft || driftRight) && !GLIDER_FLY)
        {
            rb.AddForce(-transform.up * outwardsDriftForce * Time.deltaTime, ForceMode.Acceleration);
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) || Input.GetButton("Fire2"))
        {
            CurrentSpeed = Mathf.Lerp(CurrentSpeed, MaxSpeed, Time.deltaTime * 0.5f); //speed
            if (!soundEffects[1].isPlaying)
            {
                soundEffects[0].Stop();
                soundEffects[1].Play();
            }
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) || Input.GetButton("Fire1"))
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

        if (!GLIDER_FLY)
        {
            Vector3 vel = transform.forward * CurrentSpeed;
            vel.y = rb.velocity.y; //gravity
            rb.velocity = vel;
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
        steerDirection = Input.GetAxisRaw("Horizontal"); // -1, 0, 1
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
        else //nothing
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
        else
        {
            transform.rotation = Quaternion.SlerpUnclamped(transform.rotation, Quaternion.Euler(0, transform.eulerAngles.y, transform.eulerAngles.z), 2 * Time.deltaTime);
        }

        steerDirVect = new Vector3(transform.eulerAngles.x, transform.eulerAngles.y + steerAmount, transform.eulerAngles.z);
        transform.eulerAngles = Vector3.Lerp(transform.eulerAngles, steerDirVect, 3 * Time.deltaTime);

    }

    private void groundNormalRotation()
    {
        RaycastHit hit;
        Debug.DrawRay(rayPoint.position, -transform.up * 1.2f);
        if (Physics.Raycast(rayPoint.position, -transform.up, out hit, 1.2f, whatIsGround))
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
        if (driftLeft || driftRight) 
        {
            if (Input.GetKey(KeyCode.Space) && touchingGround && CurrentSpeed > 35 && Input.GetAxis("Horizontal") != 0 || Input.GetButton("Drift") && touchingGround && CurrentSpeed > 35 && Input.GetAxis("Horizontal") != 0)
            {
                driftTime += Time.deltaTime;

                //particle effects (sparks)
                if (driftTime >= 1.5 && driftTime < 4)
                {
                    if (!soundEffects[2].isPlaying)
                    {
                        soundEffects[2].Play();
                        soundEffects[3].Stop();
                        soundEffects[4].Stop();
                    }
                    for (int i = 0; i < leftDrift.childCount; i++)
                    {
                        ParticleSystem DriftPS = rightDrift.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //right wheel particles
                        ParticleSystem.MainModule PSMAIN = DriftPS.main;

                        ParticleSystem DriftPS2 = leftDrift.GetChild(i).gameObject.GetComponent<ParticleSystem>(); //left wheel particles
                        ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;

                        PSMAIN.startColor = drift1;
                        PSMAIN2.startColor = drift1;

                        if (!DriftPS.isPlaying && !DriftPS2.isPlaying)
                        {
                            DriftPS.Play();
                            DriftPS2.Play();
                        }
                    }
                }
                if (driftTime >= 4 && driftTime < 6)
                {
                    if (!soundEffects[3].isPlaying)
                    {
                        soundEffects[3].Play();
                        soundEffects[4].Stop();
                        soundEffects[2].Stop();
                    }
                    //drift color particles
                    for (int i = 0; i < leftDrift.childCount; i++)
                    {
                        ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                        ParticleSystem.MainModule PSMAIN = DriftPS.main;
                        ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                        ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;
                        PSMAIN.startColor = drift2;
                        PSMAIN2.startColor = drift2;
                    }

                }
                if (driftTime >= 6)
                {
                    if (!soundEffects[4].isPlaying)
                    {
                        soundEffects[4].Play();
                        soundEffects[3].Stop();
                        soundEffects[2].Stop();
                    }
                    for (int i = 0; i < leftDrift.childCount; i++)
                    {
                        ParticleSystem DriftPS = rightDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                        ParticleSystem.MainModule PSMAIN = DriftPS.main;
                        ParticleSystem DriftPS2 = leftDrift.transform.GetChild(i).gameObject.GetComponent<ParticleSystem>();
                        ParticleSystem.MainModule PSMAIN2 = DriftPS2.main;
                        PSMAIN.startColor = drift3;
                        PSMAIN2.startColor = drift3;
                    }
                }
            }
        }

        if (!GlobalData.UseController)
        {
            if (!Input.GetKey(KeyCode.Space) || RealSpeed < 35)
            {
                driftLeft = false;
                driftRight = false;
                isSliding = false;

                //give a boost
                if (driftTime > 1.5 && driftTime < 4)
                {
                    BoostTime = 0.75f;
                    soundEffects[2].Stop();
                    soundEffects[3].Stop();
                    soundEffects[4].Stop();
                    soundEffects[5].Play();
                }
                if (driftTime >= 4 && driftTime < 7)
                {
                    BoostTime = 1.5f;
                    soundEffects[2].Stop();
                    soundEffects[3].Stop();
                    soundEffects[4].Stop();
                    soundEffects[5].Play();

                }
                if (driftTime >= 6)
                {
                    BoostTime = 2.5f;
                    soundEffects[4].Stop();
                    soundEffects[3].Stop();
                    soundEffects[2].Stop();
                    soundEffects[5].Play();
                }

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
        }
        else
        {
            if (!Input.GetButton("Drift") || RealSpeed < 35)
            {
                driftLeft = false;
                driftRight = false;
                isSliding = false;

                //give a boost
                if (driftTime > 1.5 && driftTime < 4)
                {
                    BoostTime = 0.75f;
                    soundEffects[4].Stop();
                    soundEffects[3].Stop();
                    soundEffects[2].Stop();
                    soundEffects[5].Play();
                }
                if (driftTime >= 4 && driftTime < 7)
                {
                    BoostTime = 1.5f;
                    soundEffects[4].Stop();
                    soundEffects[3].Stop();
                    soundEffects[2].Stop();
                    soundEffects[5].Play();

                }
                if (driftTime >= 6)
                {
                    BoostTime = 2.5f;
                    soundEffects[4].Stop();
                    soundEffects[3].Stop();
                    soundEffects[2].Stop();
                    soundEffects[5].Play();
                }

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
        if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
        {
            frontLeftTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 155, 0), 5 * Time.deltaTime);
            frontRightTire.localEulerAngles = Vector3.Lerp(frontLeftTire.localEulerAngles, new Vector3(0, 155, 0), 5 * Time.deltaTime);
        }
        else if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
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

        foreach (var item in things)
        {
            if (item.Index == KartLap.mainKart.CheckpointIndex)
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

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            //Network player, receive data
            latestPos = (Vector3)stream.ReceiveNext();
            latestRot = (Quaternion)stream.ReceiveNext();

            //Lag compensation
            currentTime = 0.0f;
            lastPacketTime = currentPacketTime;
            currentPacketTime = info.SentServerTime;
            positionAtLastPacket = transform.position;
            rotationAtLastPacket = transform.rotation;
        }
    }
}
