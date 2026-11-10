using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Paneles")]
    [SerializeField] private GameObject panelPrincipal;
    [SerializeField] private GameObject panelCliente;

    [Header("UI")]
    [SerializeField] private TMP_InputField inputIP;
    [SerializeField] private TMP_Text textoEstado;
    [SerializeField] private TMP_Text textoIPLocal;

    [Header("Config")]
    [SerializeField] private ushort puerto = 7777;
    [SerializeField] private string escenaJuego = "Game";

    private UnityTransport transporte;

    private void Start()
    {
        var nm = NetworkManager.Singleton;
        transporte = nm.GetComponent<UnityTransport>();


        transporte.MaxConnectAttempts = 3;

        nm.OnClientDisconnectCallback += AlDesconectarse;

        textoIPLocal.text = "Tu IP local: " + ObtenerIPLocal();
        textoEstado.text = "";
        MostrarPrincipal();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarse;
    }



    public void BotonHost()
    {
       
        transporte.SetConnectionData("127.0.0.1", puerto, "0.0.0.0");

        if (NetworkManager.Singleton.StartHost())
        {
            NetworkManager.Singleton.SceneManager.LoadScene(escenaJuego, LoadSceneMode.Single);
        }
        else
        {
            textoEstado.text = "No se pudo iniciar el host (¿puerto ocupado?).";
        }
    }

    public void BotonIrACliente()
    {
        textoEstado.text = "";
        panelPrincipal.SetActive(false);
        panelCliente.SetActive(true);
    }

    public void BotonConectar()
    {
        string ip = inputIP.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            textoEstado.text = "Ingresá la IP del host.";
            return;
        }

        transporte.SetConnectionData(ip, puerto);

        if (NetworkManager.Singleton.StartClient())
            textoEstado.text = "Conectando...";
        else
            textoEstado.text = "No se pudo iniciar el cliente.";
    }

    public void BotonVolver()
    {
        if (NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();

        textoEstado.text = "";
        MostrarPrincipal();
    }

    public void BotonSalir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

  

    private void MostrarPrincipal()
    {
        panelPrincipal.SetActive(true);
        panelCliente.SetActive(false);
    }

    private void AlDesconectarse(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
       
        if (clientId != nm.LocalClientId || nm.IsServer) return;

        textoEstado.text = "No se pudo conectar. Revisá la IP y que el host esté activo.";
        nm.Shutdown();
    }

    private string ObtenerIPLocal()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                    return ip.ToString();
        }
        catch { }
        return "no disponible";
    }
}