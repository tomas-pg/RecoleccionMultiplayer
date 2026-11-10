using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


public class SpawnerObjetos : NetworkBehaviour
{
    [Header("Prefabs (con NetworkObject)")]
    [SerializeField] private NetworkObject prefabComun;
    [SerializeField] private NetworkObject prefabEspecial;
    [SerializeField] private NetworkObject prefabNegativo;

    [Header("Probabilidades (deben sumar 100)")]
    [SerializeField] private int probComun = 60;
    [SerializeField] private int probEspecial = 20;
  

    [Header("Área de aparición")]
    [SerializeField] private Vector2 limitesX = new Vector2(-13f, 13f);
    [SerializeField] private Vector2 limitesZ = new Vector2(-13f, 13f);
    [SerializeField] private float alturaRaycast = 10f;
    [SerializeField] private float separacionDelSuelo = 0.8f;
    [SerializeField] private LayerMask capaSuelo = ~0;

    [Header("Ritmo")]
    [SerializeField] private float intervalo = 1.5f;
    [SerializeField] private int maxObjetosEnEscena = 15;

    private float temporizador;
    private readonly List<NetworkObject> activos = new List<NetworkObject>();
    private bool generando = false;

    public void Iniciar()
    {
        if (!IsServer) return;
        generando = true;
        temporizador = 0f;
    }

    public void Detener()
    {
        generando = false;
    }

    
    public void LimpiarObjetos()
    {
        if (!IsServer) return;

        foreach (var o in activos)
        {
            if (o != null && o.IsSpawned)
                o.Despawn(true);
        }
        activos.Clear();
    }

    
    public void Olvidar(NetworkObject o)
    {
        activos.Remove(o);
    }

    private void Update()
    {
        if (!IsServer || !generando) return;

        temporizador += Time.deltaTime;
        if (temporizador < intervalo) return;
        temporizador = 0f;

        activos.RemoveAll(o => o == null || !o.IsSpawned);
        if (activos.Count >= maxObjetosEnEscena) return;

        GenerarUno();
    }

    private void GenerarUno()
    {
        
        for (int i = 0; i < 10; i++)
        {
            float x = Random.Range(limitesX.x, limitesX.y);
            float z = Random.Range(limitesZ.x, limitesZ.y);
            Vector3 origen = new Vector3(x, alturaRaycast, z);

            if (Physics.Raycast(origen, Vector3.down, out RaycastHit hit,
                                alturaRaycast + 5f, capaSuelo,
                                QueryTriggerInteraction.Ignore))
            {
                
                if (hit.collider.GetComponent<PlayerController>() != null) continue;

                Vector3 posicion = hit.point + Vector3.up * separacionDelSuelo;
                NetworkObject prefab = ElegirPrefab();

                NetworkObject instancia = Instantiate(prefab, posicion, Quaternion.identity);
                instancia.Spawn(true);
                activos.Add(instancia);
                return;
            }
        }
    }

    private NetworkObject ElegirPrefab()
    {
        int dado = Random.Range(0, 100);
        if (dado < probComun) return prefabComun;
        if (dado < probComun + probEspecial) return prefabEspecial;
        return prefabNegativo;
    }
}


