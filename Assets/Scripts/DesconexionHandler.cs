using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Va en la escena Game. Si el host se cae, el cliente vuelve al menú.
public class DesconexionHandler : MonoBehaviour
{
    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null) return;

        nm.OnClientDisconnectCallback += AlDesconectarse;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarse;
    }

    private void AlDesconectarse(ulong clientId)
    {
        var nm = NetworkManager.Singleton;

        
        if (nm.IsServer || clientId != nm.LocalClientId) return;

        nm.Shutdown();
        SceneManager.LoadScene("Menu");
    }
}