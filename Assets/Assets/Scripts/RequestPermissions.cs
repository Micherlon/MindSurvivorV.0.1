using UnityEngine;
using UnityEngine.Android;

public class RequestPermissions : MonoBehaviour
{
    void Start()
    {
        // Para Android 12+:
        // BLUETOOTH_SCAN e BLUETOOTH_CONNECT
        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
        {
            Permission.RequestUserPermission("android.permission.BLUETOOTH_SCAN");
        }
        if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
        {
            Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
        }

        // Para versões anteriores ou fallback
        if (!Permission.HasUserAuthorizedPermission(Permission.FineLocation))
        {
            Permission.RequestUserPermission(Permission.FineLocation);
        }
    }
}
