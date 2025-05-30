using UnityEngine;
using UnityEngine.Android;

/// <summary>
/// Requests the runtime Bluetooth permissions required on Android 12+.
/// Place this on a GameObject in your first scene (or just keep it in a prefab).
/// </summary>
public sealed class BluetoothPermissions : MonoBehaviour
{
    private void Start()
    {
        const string BLUETOOTH_SCAN = "android.permission.BLUETOOTH_SCAN";
        const string BLUETOOTH_CONNECT = "android.permission.BLUETOOTH_CONNECT";

        if (!Permission.HasUserAuthorizedPermission(BLUETOOTH_SCAN))
            Permission.RequestUserPermission(BLUETOOTH_SCAN);

        if (!Permission.HasUserAuthorizedPermission(BLUETOOTH_CONNECT))
            Permission.RequestUserPermission(BLUETOOTH_CONNECT);

        // Fallback for < Android 12 where location is still required
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
            Permission.RequestUserPermission(Permission.FineLocation);
    }
}
