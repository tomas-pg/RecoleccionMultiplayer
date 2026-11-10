using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class HUDManager : MonoBehaviour
{
    [Header("Textos")]
    [SerializeField] private TMP_Text textoTiempo;
    [SerializeField] private TMP_Text textoPuntajes;
    [SerializeField] private TMP_Text textoEstado;

    [Header("Panel final")]
    [SerializeField] private GameObject panelFinal;
    [SerializeField] private TMP_Text textoGanador;
    [SerializeField] private TMP_Text textoResumen;
    [SerializeField] private GameObject botonJugarDeNuevo;
    [SerializeField] private GameObject textoEsperandoHost;

    [Header("Botón del host")]
    [SerializeField] private GameObject botonComenzar;

    [Header("Escenas")]
    [SerializeField] private string nombreEscenaMenu = "Menu";

    private static readonly string[] colores =
        { "#4FA3FF", "#FF5A5A", "#5CD65C", "#FFD84D" };

    private float proximaActualizacion;

    private void Update()
    {
        if (Time.unscaledTime < proximaActualizacion) return;
        proximaActualizacion = Time.unscaledTime + 0.1f;

        var gm = GameManager.Instance;
        if (gm == null || !gm.IsSpawned) return;

        ActualizarTiempo(gm);
        ActualizarPuntajes();
        ActualizarEstado(gm);
    }

    private void ActualizarTiempo(GameManager gm)
    {
        int s = Mathf.CeilToInt(gm.tiempoRestante.Value);
        s = Mathf.Max(0, s);
        textoTiempo.text = $"{s / 60:00}:{s % 60:00}";
    }

    private void ActualizarPuntajes()
    {
        var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                        .OrderBy(p => p.indiceJugador.Value)
                        .ToArray();

        var sb = new StringBuilder();
        foreach (var p in jugadores)
        {
            int idx = Mathf.Clamp(p.indiceJugador.Value, 0, colores.Length - 1);
            string etiqueta = p.IsOwner ? " (Vos)" : "";
            sb.AppendLine(
                $"<color={colores[idx]}>Jugador {p.indiceJugador.Value + 1}{etiqueta}: {p.puntaje.Value}</color>");
        }
        textoPuntajes.text = sb.ToString();
    }

    private void ActualizarEstado(GameManager gm)
    {
        switch (gm.EstadoActual)
        {
            case GameManager.Estado.Esperando:
                OcultarFinal();
                bool alcanza = gm.CantidadJugadores >= gm.MinJugadores;

                if (gm.IsServer)
                {
                    textoEstado.text = alcanza
                        ? "Todo listo. ¡Comenzá la partida!"
                        : $"Esperando jugadores... ({gm.CantidadJugadores}/{gm.MinJugadores})";
                    botonComenzar.SetActive(alcanza);
                }
                else
                {
                    textoEstado.text = "Esperando a que el host comience...";
                    botonComenzar.SetActive(false);
                }
                break;

            case GameManager.Estado.Jugando:
                OcultarFinal();
                textoEstado.text = "";
                botonComenzar.SetActive(false);
                break;

            case GameManager.Estado.Terminada:
                textoEstado.text = "";
                botonComenzar.SetActive(false);
                MostrarFinal(gm);
                break;
        }
    }



    private void MostrarFinal(GameManager gm)
    {
        if (panelFinal == null) return;
        panelFinal.SetActive(true);

        if (gm.hayEmpate.Value)
        {
            textoGanador.text = "¡EMPATE!";
        }
        else
        {
            int idx = Mathf.Clamp(gm.indiceGanador.Value, 0, colores.Length - 1);
            textoGanador.text =
                $"¡GANADOR!\n<color={colores[idx]}>JUGADOR {gm.indiceGanador.Value + 1}</color>";
        }

        if (textoResumen != null)
        {
            var jugadores = FindObjectsByType<PlayerController>(FindObjectsSortMode.None)
                            .OrderBy(p => p.indiceJugador.Value)
                            .ToArray();

            var sb = new StringBuilder();
            foreach (var p in jugadores)
                sb.AppendLine($"Jugador {p.indiceJugador.Value + 1}: {p.puntaje.Value} pts");
            textoResumen.text = sb.ToString();
        }

        bool esHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        if (botonJugarDeNuevo != null) botonJugarDeNuevo.SetActive(esHost);
        if (textoEsperandoHost != null) textoEsperandoHost.SetActive(!esHost);
    }

    private void OcultarFinal()
    {
        if (panelFinal != null && panelFinal.activeSelf)
            panelFinal.SetActive(false);
    }


    public void BotonComenzar()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.ComenzarPartida();
    }


    public void BotonJugarDeNuevo()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.JugarDeNuevo();
    }

   
    public void BotonVolverAlMenu()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        SceneManager.LoadScene(nombreEscenaMenu);
    }
}
