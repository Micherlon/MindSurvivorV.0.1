using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Minimal sample HUD that shows live Attention, Meditation,
/// Signal Quality and a single Connect / Retry button.
/// </summary>
public sealed class BrainLinkHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button connectButton;
    [SerializeField] private Text statusLabel;

    [Space]
    [SerializeField] private Slider attentionBar;
    [SerializeField] private Slider meditationBar;
    [SerializeField] private Slider signalBar;

    private void Start()
    {
        connectButton.onClick.AddListener(() =>
        {
            statusLabel.text = "Scanning...";
            BrainLinkManager.Instance.StartScan();
        });

        // Subscribe to manager events
        var mgr = BrainLinkManager.Instance;
        mgr.OnBluetoothStateChanged += HandleBtState;
        mgr.OnDeviceListUpdated += HandleDeviceList;
        mgr.OnConnected += HandleConnected;
        mgr.OnDisconnected += HandleDisconnected;
        mgr.OnDataReceived += UpdateGauges;

        // Immediate feedback
        HandleBtState(mgr.IsBluetoothEnabled());
    }

    private void HandleBtState(bool enabled)
    {
        statusLabel.text = enabled ? "Bluetooth ready" : "Please enable Bluetooth";
        connectButton.interactable = enabled;
    }

    private void HandleDeviceList(List<string> list)
    {
        statusLabel.text = list.Count == 0 ? "No headset found" : "Connecting...";
    }

    private void HandleConnected()
    {
        statusLabel.text = "CONNECTED";
    }

    private void HandleDisconnected()
    {
        statusLabel.text = "DISCONNECTED – press Connect";
        attentionBar.value = 0;
        meditationBar.value = 0;
        signalBar.value = 0;
    }

    private void UpdateGauges(Dictionary<string, int> data)
    {
        if (data.TryGetValue("Attention", out int att)) attentionBar.value = att;
        if (data.TryGetValue("Meditation", out int med)) meditationBar.value = med;
        if (data.TryGetValue("PoorSignalLevel", out int sig)) signalBar.value = 100 - sig;
    }
}
