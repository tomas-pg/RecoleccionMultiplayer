using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 6f;
    [SerializeField] private float alturaSalto = 1.4f;
    [SerializeField] private float gravedad = -20f;
    [SerializeField] private float velocidadGiro = 12f;

    [Header("Visual")]
    [SerializeField] private Renderer rendererCuerpo;
    [SerializeField] private Material materialAzul;
    [SerializeField] private Material materialRojo;
    [SerializeField] private Material materialVerde;
    [SerializeField] private Material materialAmarillo;

    private CharacterController cc;
    private Vector3 velocidadVertical;

    
    public NetworkVariable<int> indiceJugador = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );


    public NetworkVariable<int> puntaje = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

 
    public bool puedeMoverse = true;

    private void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    public override void OnNetworkSpawn()
    {
        indiceJugador.OnValueChanged += AlCambiarIndice;
        AplicarColor(indiceJugador.Value);

        if (IsOwner)
        {
          
        }

        if (IsServer)
        {
    
            if (GameManager.Instance != null)
                GameManager.Instance.RegistrarJugador(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        indiceJugador.OnValueChanged -= AlCambiarIndice;

        if (IsServer && GameManager.Instance != null)
            GameManager.Instance.QuitarJugador(this);
    }

    private void AlCambiarIndice(int anterior, int nuevo)
    {
        AplicarColor(nuevo);
    }

    private void AplicarColor(int indice)
    {
        if (rendererCuerpo == null) return;

        Material m = materialAzul;
        switch (indice)
        {
            case 0: m = materialAzul; break;
            case 1: m = materialRojo; break;
            case 2: m = materialVerde; break;
            case 3: m = materialAmarillo; break;
        }
        if (m != null) rendererCuerpo.material = m;
    }

    private void Update()
    {
 
        if (!IsOwner) return;

        bool enSuelo = cc.isGrounded;
        if (enSuelo && velocidadVertical.y < 0)
            velocidadVertical.y = -2f;

        Vector3 mover = Vector3.zero;

        if (puedeMoverse)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            mover = new Vector3(h, 0f, v).normalized;

            
            if (mover.sqrMagnitude > 0.01f)
            {
                Quaternion destino = Quaternion.LookRotation(mover);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, destino, Time.deltaTime * velocidadGiro);
            }

           
            if (enSuelo && Input.GetButtonDown("Jump"))
            {
                velocidadVertical.y = Mathf.Sqrt(alturaSalto * -2f * gravedad);
            }
        }

     
        velocidadVertical.y += gravedad * Time.deltaTime;

        Vector3 movimientoFinal = mover * velocidad + velocidadVertical;
        cc.Move(movimientoFinal * Time.deltaTime);
    }

    
    [ClientRpc]
    public void TeletransportarClientRpc(Vector3 posicion, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;

        cc.enabled = false;
        transform.position = posicion;
        velocidadVertical = Vector3.zero;
        cc.enabled = true;
    }

    [ClientRpc]
    public void SetPuedeMoverseClientRpc(bool valor, ClientRpcParams rpcParams = default)
    {
        if (!IsOwner) return;
        puedeMoverse = valor;
    }
}