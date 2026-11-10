using Unity.Netcode;
using UnityEngine;


public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] puntosSpawn;

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        if (nm == null || !nm.IsServer) return;

       
        foreach (ulong id in nm.ConnectedClientsIds)
            SpawnearJugador(id);

       
        nm.OnClientConnectedCallback += SpawnearJugador;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= SpawnearJugador;
    }

    private void SpawnearJugador(ulong clientId)
    {
        var nm = NetworkManager.Singleton;

   
        if (nm.ConnectedClients.TryGetValue(clientId, out var cliente) &&
            cliente.PlayerObject != null) return;

        
        int indice = (int)(clientId % 4);

        Transform punto = puntosSpawn[indice % puntosSpawn.Length];

        NetworkObject obj = Instantiate(playerPrefab, punto.position, Quaternion.identity);
        obj.SpawnAsPlayerObject(clientId, true);

       
        var pc = obj.GetComponent<PlayerController>();
        pc.indiceJugador.Value = indice % 4;
    }
}