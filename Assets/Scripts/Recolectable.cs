using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class Recolectable : NetworkBehaviour
{
    [SerializeField] private int valor = 1;      
    [SerializeField] private float giroPorSegundo = 90f;

    private bool recogidoEnServidor = false;

    private void Update()
    {
        transform.Rotate(0f, giroPorSegundo * Time.deltaTime, 0f, Space.World);
    }

    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsSpawned) return;

        var jugador = other.GetComponent<PlayerController>();
        if (jugador == null || !jugador.IsOwner) return;

        RecogerServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void RecogerServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsSpawned || recogidoEnServidor) return;

        if (GameManager.Instance != null && !GameManager.Instance.PartidaActiva) return;

        ulong quien = rpcParams.Receive.SenderClientId;
        var nm = NetworkManager.Singleton;
        if (!nm.ConnectedClients.TryGetValue(quien, out var cliente) ||
            cliente.PlayerObject == null) return;

        var jugador = cliente.PlayerObject.GetComponent<PlayerController>();
        if (jugador == null) return;

        recogidoEnServidor = true;
        jugador.puntaje.Value += valor;

        if (GameManager.Instance != null)
            GameManager.Instance.NotificarObjetoRecogido(this);

        NetworkObject.Despawn(true);
    }

    public int Valor => valor;
}