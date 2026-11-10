using UnityEngine;


public class NetworkManagerSingleton : MonoBehaviour
{
    private static NetworkManagerSingleton instancia;

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
    }
}