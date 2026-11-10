using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    public enum Estado { Esperando = 0, Jugando = 1, Terminada = 2 }

    [Header("Config")]
    [SerializeField] private float duracionPartida = 90f;
    [SerializeField] private int minJugadores = 2;
    [SerializeField] private SpawnerObjetos spawner;
    [SerializeField] private Transform[] puntosSpawn;

    public NetworkVariable<int> estado = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> tiempoRestante = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> indiceGanador = new NetworkVariable<int>(
        -1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> hayEmpate = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly List<PlayerController> jugadores = new List<PlayerController>();
    private float tiempoServidor;

    public int MinJugadores => minJugadores;
    public int CantidadJugadores => jugadores.Count;
    public Estado EstadoActual => (Estado)estado.Value;
    public bool PartidaActiva => estado.Value == (int)Estado.Jugando;

    private void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this) Instance = null;
        base.OnDestroy();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        tiempoServidor = duracionPartida;
        tiempoRestante.Value = duracionPartida;
        estado.Value = (int)Estado.Esperando;

        NetworkManager.OnClientDisconnectCallback += AlDesconectarseCliente;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarseCliente;
    }

    private void AlDesconectarseCliente(ulong clientId)
    {
        jugadores.RemoveAll(j => j == null);

        if (EstadoActual == Estado.Jugando && jugadores.Count < minJugadores)
            FinalizarPartida();
    }


    public void RegistrarJugador(PlayerController p)
    {
        if (!jugadores.Contains(p)) jugadores.Add(p);
    }

    public void QuitarJugador(PlayerController p)
    {
        jugadores.Remove(p);
    }

    public void NotificarObjetoRecogido(Recolectable r)
    {
        if (spawner != null)
            spawner.Olvidar(r.NetworkObject);
    }



    public void ComenzarPartida()
    {
        if (!IsServer) return;
        if (EstadoActual != Estado.Esperando) return;
        if (jugadores.Count < minJugadores) return;

        foreach (var j in jugadores)
            if (j != null) j.puntaje.Value = 0;

        tiempoServidor = duracionPartida;
        tiempoRestante.Value = duracionPartida;

        if (spawner != null)
        {
            spawner.LimpiarObjetos();
            spawner.Iniciar();
        }

        CongelarJugadores(false);
        estado.Value = (int)Estado.Jugando;
    }

    private void Update()
    {
        if (!IsServer || !IsSpawned) return;
        if (EstadoActual != Estado.Jugando) return;

        tiempoServidor -= Time.deltaTime;

        float redondeado = Mathf.Max(0f, Mathf.Ceil(tiempoServidor));
        if (!Mathf.Approximately(redondeado, tiempoRestante.Value))
            tiempoRestante.Value = redondeado;

        if (tiempoServidor <= 0f)
            FinalizarPartida();
    }

    private void FinalizarPartida()
    {
        tiempoRestante.Value = 0f;

        if (spawner != null)
        {
            spawner.Detener();
            spawner.LimpiarObjetos();
        }

        CalcularGanador();
        CongelarJugadores(true);
        estado.Value = (int)Estado.Terminada;
    }

    private void CalcularGanador()
    {
        int mejor = int.MinValue;
        int ganador = -1;
        bool empate = false;

        foreach (var j in jugadores)
        {
            if (j == null) continue;

            int pts = j.puntaje.Value;
            if (pts > mejor)
            {
                mejor = pts;
                ganador = j.indiceJugador.Value;
                empate = false;
            }
            else if (pts == mejor)
            {
                empate = true;
            }
        }

        hayEmpate.Value = empate;
        indiceGanador.Value = empate ? -1 : ganador;
    }


    public void JugarDeNuevo()
    {
        if (!IsServer) return;
        if (EstadoActual != Estado.Terminada) return;

        if (spawner != null)
        {
            spawner.Detener();
            spawner.LimpiarObjetos();
        }

        int i = 0;
        foreach (var j in jugadores)
        {
            if (j == null) continue;

            j.puntaje.Value = 0;

            var p = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { j.OwnerClientId }
                }
            };
            j.TeletransportarClientRpc(ObtenerPuntoSpawn(i), p);
            j.SetPuedeMoverseClientRpc(true, p);
            i++;
        }

        indiceGanador.Value = -1;
        hayEmpate.Value = false;
        tiempoServidor = duracionPartida;
        tiempoRestante.Value = duracionPartida;
        estado.Value = (int)Estado.Esperando;
    }

    private Vector3 ObtenerPuntoSpawn(int indice)
    {
        if (puntosSpawn != null && puntosSpawn.Length > 0)
            return puntosSpawn[indice % puntosSpawn.Length].position;
        return new Vector3(0f, 1f, 0f);
    }

    private void CongelarJugadores(bool congelar)
    {
        foreach (var j in jugadores)
        {
            if (j == null) continue;

            var p = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { j.OwnerClientId }
                }
            };
            j.SetPuedeMoverseClientRpc(!congelar, p);
        }
    }
}