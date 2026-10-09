using UnityEngine;

// Ultimate do Paladin (GDD Seção 17.7) — até 8 espadas orbitando o Paladin. Quantidade
// escalável por upgrade futuro (2 → 4 → 8, nunca número ímpar). GameObject filho dedicado,
// sempre presente no prefab com os 8 slots já posicionados/rotacionados ao redor do círculo
// (nunca instanciados) — Activate() só liga o subconjunto correspondente ao bladeCount
// recebido (2 = N/S, 4 = + E/W, 8 = + as 4 diagonais).
//
// Nenhum valor de balanceamento é serializado aqui de propósito — bladeCount e
// orbitSpeedDegreesPerSecond são decididos pelo Paladin.cs (o controlador) e chegam como
// parâmetro em Activate(). Isso mantém upgrades futuros concentrados num único lugar
// (Paladin.cs), em vez de espalhados entre o herói e cada filho/prefab que ele instancia ou
// aciona. Este componente só guarda wiring (os 8 slots) e a mecânica de órbita em si.
//
// A órbita em si é só a rotação do PRÓPRIO Transform deste GameObject (Update() abaixo) — como
// as 8 espadas são filhas dele, giram juntas automaticamente, sem precisar recalcular posição
// de cada uma por trigonometria a cada frame. Gira desde o momento em que as espadas começam a
// surgir (Start) até o fim da animação de desaparecer (End), não só durante o Cycle.
public class PaladinOrbitingBlades : MonoBehaviour
{
    [Header("8 slots fixos ao redor do círculo — N/S sempre ativos, E/W a partir de 4, diagonais a partir de 8")]
    [SerializeField] private PaladinOrbitingBlade bladeN;
    [SerializeField] private PaladinOrbitingBlade bladeNE;
    [SerializeField] private PaladinOrbitingBlade bladeE;
    [SerializeField] private PaladinOrbitingBlade bladeSE;
    [SerializeField] private PaladinOrbitingBlade bladeS;
    [SerializeField] private PaladinOrbitingBlade bladeSW;
    [SerializeField] private PaladinOrbitingBlade bladeW;
    [SerializeField] private PaladinOrbitingBlade bladeNW;

    private enum State { Inactive, Active, Ending }
    private State state = State.Inactive;

    private float remainingDuration;
    private float damagePerHit;
    private int bladeCount;
    private float orbitSpeedDegreesPerSecond;
    private readonly System.Collections.Generic.List<PaladinOrbitingBlade> activeBlades = new();
    private int pendingEndCount;

    private void Awake()
    {
        // Cada espada precisa saber em quem repassar o próprio hit/fim do End.
        if (bladeN != null) bladeN.Init(this);
        if (bladeNE != null) bladeNE.Init(this);
        if (bladeE != null) bladeE.Init(this);
        if (bladeSE != null) bladeSE.Init(this);
        if (bladeS != null) bladeS.Init(this);
        if (bladeSW != null) bladeSW.Init(this);
        if (bladeW != null) bladeW.Init(this);
        if (bladeNW != null) bladeNW.Init(this);
    }

    // bladeCount/orbitSpeed vêm do Paladin.cs a cada chamada — ver comentário no topo do arquivo.
    public void Activate(float duration, float damage, int activeBladeCount, float speedDegreesPerSecond)
    {
        remainingDuration = duration;
        damagePerHit = damage;
        bladeCount = activeBladeCount;
        orbitSpeedDegreesPerSecond = speedDegreesPerSecond;
        state = State.Active;
        transform.rotation = Quaternion.identity;

        activeBlades.Clear();
        activeBlades.Add(bladeN);
        activeBlades.Add(bladeS);
        if (bladeCount >= 4)
        {
            activeBlades.Add(bladeE);
            activeBlades.Add(bladeW);
        }
        if (bladeCount >= 8)
        {
            activeBlades.Add(bladeNE);
            activeBlades.Add(bladeNW);
            activeBlades.Add(bladeSE);
            activeBlades.Add(bladeSW);
        }

        foreach (var blade in activeBlades)
            if (blade != null) blade.PlayStart();
    }

    private void Update()
    {
        if (!GameplayGate.IsActive || state == State.Inactive) return;

        // Gira do início do Start até o fim do End — não só durante o Cycle (pedido do usuário).
        transform.Rotate(0f, 0f, orbitSpeedDegreesPerSecond * Time.deltaTime);

        if (state == State.Active)
        {
            remainingDuration -= Time.deltaTime;
            if (remainingDuration <= 0f) EndOrbit();
        }
    }

    private void EndOrbit()
    {
        state = State.Ending;
        pendingEndCount = activeBlades.Count;
        foreach (var blade in activeBlades)
            if (blade != null) blade.PlayEnd();
    }

    // Chamado por cada espada (PaladinOrbitingBlade) quando o próprio clipe de End termina —
    // só para a órbita quando a ÚLTIMA espada ativa tiver terminado de desaparecer.
    public void NotifyBladeEndFinished()
    {
        pendingEndCount--;
        if (pendingEndCount <= 0)
        {
            state = State.Inactive;
            transform.rotation = Quaternion.identity;
        }
    }

    // Chamado pela espada (PaladinOrbitingBlade) que detectou o hit — dano sem dedup entre
    // voltas (GDD Seção 0, item 3: cada volta que passar por cima de um monstro conta como hit novo).
    public void NotifyHit(Collider2D other)
    {
        if (!other.CompareTag("Enemy")) return;
        var enemy = other.GetComponent<EnemyController>();
        if (enemy == null) return;
        enemy.TakeDamage(damagePerHit);
        // Sem knockback — Seção 0, item 4.
    }
}
