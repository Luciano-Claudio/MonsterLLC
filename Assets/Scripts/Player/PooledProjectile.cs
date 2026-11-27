using UnityEngine;

// Marca de qual prefab este objeto veio (chave da fila). Sem marca = criado à parte: destruído no fim, como antes.
public class PooledProjectile : MonoBehaviour
{
    public GameObject Prefab { get; set; }
}
