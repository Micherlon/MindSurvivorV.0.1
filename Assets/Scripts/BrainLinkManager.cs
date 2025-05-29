using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

/// <summary>
/// Cross-platform manager for BrainLink / ThinkGear headsets.
/// Keeps the Bluetooth connection alive between scenes
/// and raises high-level C# events for the game layer.
/// </summary>
public sealed class BrainLinkManager : MonoBehaviour
{
    //--------------------------------------------------------------------
    //  Public-facing single-instance access
    //--------------------------------------------------------------------
    public static BrainLinkManager Instance { get; private set; }

    //--------------------------------------------------------------------
    //  Events
    //--------------------------------------------------------------------
    public event Action<List<string>> OnDeviceListUpdated;
    public event Action<Dictionary<string, int>> OnDataReceived;
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<bool> OnBluetoothStateChanged; // true = enabled

    //--------------------------------------------------------------------
    //  Configuration
    //--------------------------------------------------------------------
    [Tooltip("If true the first device in the list is connected automatically.")]
    public bool autoConnectFirstDevice = true;

    //--------------------------------------------------------------------
    //  Private fields
    //--------------------------------------------------------------------
    private readonly List<string> _deviceList = new List<string>();
    private readonly Dictionary<string, int> _data = new Dictionary<string, int>();
    private AndroidJavaClass _unity;      // Android bridge
    private bool _isScanning;

    //--------------------------------------------------------------------
    //  Native iOS bindings
    //--------------------------------------------------------------------
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void iOSStartScan();
    [DllImport("__Internal")] private static extern void iOSStopScan();
    [DllImport("__Internal")] private static extern void iOSSelectDevice(int index);
    [DllImport("__Internal")] private static extern void iOSDisconnect();
#endif

    //--------------------------------------------------------------------
    //  Unity lifecycle
    //--------------------------------------------------------------------
    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

#if UNITY_ANDROID && !UNITY_EDITOR
        _unity = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
#endif
        // Kick off scanning immediately if Bluetooth is already enabled.
        if (IsBluetoothEnabled())
            StartScan();
    }

    //--------------------------------------------------------------------
    //  Public API ----------------------------------------------------------------
    //--------------------------------------------------------------------
    public void StartScan()
    {
        if (_isScanning) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        _unity.GetStatic<AndroidJavaObject>("currentActivity")?.Call("StartScan");
#endif
#if UNITY_IOS && !UNITY_EDITOR
        iOSStartScan();
#endif
        _isScanning = true;
    }

    public void StopScan()
    {
        if (!_isScanning) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        _unity.GetStatic<AndroidJavaObject>("currentActivity")?.Call("StopScan");
#endif
#if UNITY_IOS && !UNITY_EDITOR
        iOSStopScan();
#endif
        _deviceList.Clear();
        _isScanning = false;
    }

    public void ConnectToDevice(int index)
    {
        if (index < 0 || index >= _deviceList.Count) return;

#if UNITY_ANDROID && !UNITY_EDITOR
        _unity.GetStatic<AndroidJavaObject>("currentActivity")?.Call("OnClickScanDevice", index.ToString());
#endif
#if UNITY_IOS && !UNITY_EDITOR
        iOSSelectDevice(index);
#endif
        // The native plugin will trigger onServicesDiscovered → OnConnected event.
    }

    public void Disconnect()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _unity.GetStatic<AndroidJavaObject>("currentActivity")?.Call("DisconnectBLE");
#endif
#if UNITY_IOS && !UNITY_EDITOR
        iOSDisconnect();
#endif
    }

    /// <summary>Query native layer. For Android we do it via a small Java helper.</summary>
    public bool IsBluetoothEnabled()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        var currentActivity = _unity.GetStatic<AndroidJavaObject>("currentActivity");
        return currentActivity != null && currentActivity.Call<bool>("IsBluetoothEnabled");
#else
        return true; // iOS + Editor fallback
#endif
    }

    //--------------------------------------------------------------------
    //  Native callbacks (names must match Java / Objective-C side)
    //--------------------------------------------------------------------
    // Called every time a device is discovered or when the list changes.
    private void onScanResult(string devicesCsv)
    {
        _deviceList.Clear();
        foreach (string address in devicesCsv.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            _deviceList.Add(address.Trim());

        OnDeviceListUpdated?.Invoke(_deviceList);

        // Optional auto-connect
        if (autoConnectFirstDevice && _deviceList.Count > 0)
            ConnectToDevice(0);
    }

    // Bluetooth turned off
    private void onBlueToothClose(string _)
    {
        StopScan();
        OnBluetoothStateChanged?.Invoke(false);
    }

    // Bluetooth turned on
    private void onBlueToothOpen(string _)
    {
        OnBluetoothStateChanged?.Invoke(true);
        StartScan(); // resume or begin scanning
    }

    private void onServicesDiscovered(string _) => OnConnected?.Invoke();
    private void onDisconnected(string _) => OnDisconnected?.Invoke();
    private void onFailToConnect(string _) => OnDisconnected?.Invoke();  // Treat the same

    // Raw data callback
    private void DataParse(string raw)
    {
        if (!raw.EndsWith("\n")) raw += "\n";
        foreach (string line in raw.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = line.Split(':');
            if (parts.Length < 2) continue;

            string label = parts[^2].Trim();
            if (!int.TryParse(parts[^1].Trim(), out int value)) continue;

            _data[label] = value;
        }
        OnDataReceived?.Invoke(_data);
    }
}
