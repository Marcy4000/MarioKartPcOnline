using Discord;
using UnityEngine;

public class Discord_Controller : MonoBehaviour
{
    public long applicationID;
    [Space]
    public string details = "Enjoying the PC online experience";
    public string state = "Browsing the menus...";
    [Space]
    public string largeImage = "maric_rast";
    public string largeText = "Made my AI";
    [Space]
    public string smallImage = "Current Character: ???";
    public string smallText = "mario";

    private long time;

    private static bool instanceExists;
    public static Discord_Controller instance;
    public Discord.Discord discord;

    void Awake()
    {
        // Transition the GameObject between scenes, destroy any duplicates
        if (!instanceExists)
        {
            instance = this;
            instanceExists = true;
            DontDestroyOnLoad(gameObject);
        }
        else if (FindObjectsOfType(GetType()).Length > 1)
        {
            Destroy(gameObject);
        }
    }

    public void UpdateStatusInfo(string _details, string _state, string _largeImage, string _largeText, string _smallImage, string _smallText)
    {
        details = _details;
        state = _state;
        largeImage = _largeImage;
        largeText = _largeText;
        smallImage = _smallImage;
        smallText = _smallText;
    }

    void Start()
    {
        // Log in with the Application ID
        discord = new Discord.Discord(applicationID, (System.UInt64)Discord.CreateFlags.NoRequireDiscord);

        time = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();

        UpdateStatus();
    }

    void Update()
    {
        // Destroy the GameObject if Discord isn't running
        try
        {
            discord.RunCallbacks();
        }
        catch
        {
            Destroy(gameObject);
        }
    }

    void LateUpdate()
    {
        UpdateStatus();
    }

    void UpdateStatus()
    {
        // Update Status every frame
        try
        {
            var activityManager = discord.GetActivityManager();
            var activity = new Discord.Activity
            {
                Details = details,
                State = state,
                Assets =
                {
                    LargeImage = largeImage,
                    LargeText = largeText,
                    SmallImage = smallImage,
                    SmallText = smallText
                },
                Timestamps =
                {
                    Start = time
                }
            };

            activityManager.UpdateActivity(activity, (res) =>
            {
                if (res != Discord.Result.Ok) Debug.LogWarning("Failed connecting to Discord!");
            });
        }
        catch
        {
            // If updating the status fails, Destroy the GameObject
            Destroy(gameObject);
        }
    }
}