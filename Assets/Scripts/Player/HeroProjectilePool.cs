using System.Collections.Generic;
using UnityEngine;

// Projéteis reaproveitados dos heróis (pedido do usuário, 2026-11-27): o projétil é criado na 1ª vez que
// precisa; quando termina, é desligado e guardado (em vez de destruído). Só cria um novo quando estão em
// uso mais projéteis daquele prefab do que existem guardados — a fila só cresce. A física não muda.
//
// "Reusado == novo": todo script de projétil implementa IPooledProjectile.ResetForReuse(), que volta cada
// campo ao valor que um Instantiate novo teria (o Instantiate da Unity só copia campos serializados — os
// privados nascem no padrão). Escala volta à do prefab; o Animator volta ao estado inicial ao religar.
public interface IPooledProjectile
{
    void ResetForReuse();
}

public static class HeroProjectilePool
{
    private static readonly Dictionary<GameObject, Stack<GameObject>> free = new();

    // Domain reload desligado: os estáticos sobrevivem entre Plays (a cena antiga já destruiu os objetos).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Clear() => free.Clear();

    // Quantos estão guardados (desligados) para este prefab — usado pelos testes.
    public static int FreeCount(GameObject prefab)
    {
        if (prefab == null || !free.TryGetValue(prefab, out var stack)) return 0;
        int n = 0;
        foreach (var o in stack) if (o != null) n++;
        return n;
    }

    // Igual a Instantiate(prefab, position, Quaternion.identity, parent), mas reaproveita um guardado.
    public static GameObject Spawn(GameObject prefab, Vector3 position, Transform parent)
    {
        if (prefab == null) return null;
        if (!free.TryGetValue(prefab, out var stack)) free[prefab] = stack = new Stack<GameObject>();

        GameObject obj = null;
        while (stack.Count > 0 && obj == null) obj = stack.Pop(); // ignora algum destruído por fora (troca de cena)
        if (obj == null)
        {
            obj = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            obj.AddComponent<PooledProjectile>().Prefab = prefab;
            return obj;
        }

        var t = obj.transform;
        if (t.parent != parent) t.SetParent(parent, false);
        t.SetPositionAndRotation(position, Quaternion.identity);
        t.localScale = prefab.transform.localScale;
        foreach (var p in obj.GetComponents<IPooledProjectile>()) p.ResetForReuse();
        obj.SetActive(true);
        // Religar o objeto já volta o Animator ao estado inicial (com os parâmetros zerados) quando
        // keepAnimatorStateOnDisable está desligado (padrão). Rebind só se alguém ligar isso — custa ~30 us cada.
        var animator = obj.GetComponent<Animator>();
        if (animator != null && animator.keepAnimatorStateOnDisable) animator.Rebind();
        return obj;
    }

    // Cópia de um projétil em voo (estilhaço, ricochete, fragmento): sai da fila do mesmo prefab. Um projétil
    // criado à parte (sem marca) continua sendo clonado como antes.
    public static GameObject SpawnLike(GameObject source, Vector3 position, Transform parent)
    {
        var mark = source.GetComponent<PooledProjectile>();
        if (mark != null && mark.Prefab != null) return Spawn(mark.Prefab, position, parent);
        return Object.Instantiate(source, position, Quaternion.identity, parent);
    }

    // No lugar de Destroy(gameObject): desliga e guarda. Sem marca, destrói como antes.
    public static void Release(GameObject obj)
    {
        if (obj == null) return;
        var mark = obj.GetComponent<PooledProjectile>();
        if (mark == null || mark.Prefab == null) { Object.Destroy(obj); return; }
        if (!obj.activeSelf) return; // já guardado (2 fins no mesmo quadro)
        obj.SetActive(false);
        if (!free.TryGetValue(mark.Prefab, out var stack)) free[mark.Prefab] = stack = new Stack<GameObject>();
        stack.Push(obj);
    }
}
