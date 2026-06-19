using System.Collections.Generic;
using UnityEngine;

public enum LogChannel
{
    Storage,
    Crafting,
    Conveyors,
    Building,
    Saving,
    General
}

public class GameLogger : MonoBehaviour
{
    public static GameLogger Instance { get; private set; }

    [Header("Global Settings")]
    [Tooltip("If disabled, no logs will be printed from GameLogger.")]
    public bool globalLoggingEnabled = true;

    [System.Serializable]
    public class ChannelSetting
    {
        public LogChannel channel;
        public bool enabled = true;
    }

    [Header("Channels")]
    [Tooltip("Enable or disable specific log categories.")]
    public List<ChannelSetting> channels = new List<ChannelSetting>();

    private Dictionary<LogChannel, bool> channelDict = new Dictionary<LogChannel, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Pre-populate dictionary for fast lookups
        foreach (var c in channels)
        {
            channelDict[c.channel] = c.enabled;
        }

        // Add any missing channels by default
        foreach (LogChannel channel in System.Enum.GetValues(typeof(LogChannel)))
        {
            if (!channelDict.ContainsKey(channel))
            {
                channelDict[channel] = true;
                channels.Add(new ChannelSetting { channel = channel, enabled = true });
            }
        }
    }

    public static void Log(LogChannel channel, string message, Object context = null)
    {
        if (Instance == null || !Instance.globalLoggingEnabled) return;

        if (Instance.channelDict.TryGetValue(channel, out bool isEnabled) && isEnabled)
        {
            Debug.Log($"<color=cyan>[{channel}]</color> {message}", context);
        }
    }

    public static void LogWarning(LogChannel channel, string message, Object context = null)
    {
        if (Instance == null || !Instance.globalLoggingEnabled) return;

        if (Instance.channelDict.TryGetValue(channel, out bool isEnabled) && isEnabled)
        {
            Debug.LogWarning($"[{channel}] {message}", context);
        }
    }

    public static void LogError(LogChannel channel, string message, Object context = null)
    {
        // Errors should probably bypass filters, but we can respect them
        if (Instance == null || !Instance.globalLoggingEnabled) return;

        if (Instance.channelDict.TryGetValue(channel, out bool isEnabled) && isEnabled)
        {
            Debug.LogError($"[{channel}] {message}", context);
        }
    }
}
