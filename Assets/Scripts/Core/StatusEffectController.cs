using System.Collections.Generic;
using UnityEngine;

// Efeitos Nocivos de dano-ao-longo-do-tempo (GDD Seção 33) — componente genérico,
// compartilhado entre herói e monstro (os 2 únicos IDamageable do jogo), em vez de duplicar
// a mesma lógica nos dois. Fire e Bleeding são praticamente idênticos mecanicamente (dano
// por tick, só muda a animação) — um StatusEffectType novo cobre qualquer efeito futuro do
// mesmo tipo, sem classe nova. Trapped (Seção 33 também) continua à parte, é incapacitação
// via HeroController.SetTrapped(), não dano ao longo do tempo.
//
// Toda entidade que pode receber um Efeito Nocivo tem um filho chamado "Status" com um
// Animator próprio — os nomes dos estados nesse Animator precisam bater exatamente com os
// valores do enum, mais um estado `Empty` pra quando não tem efeito ativo. Sem Blend Tree,
// sem transições — é só Animator.Play() direto pelo nome.
//
// Lista completa preparada com todas as animações já desenhadas (mesmo as que ainda não
// têm mecânica nem uso confirmado no projeto) — só nomeação pronta pra quando cada uma for
// implementada de verdade. HOJE só existe mecânica de dano-ao-longo-do-tempo
// (ApplyStatusEffect/Tick abaixo), que serve pra Fire/Bleeding (confirmados) e serviria
// também pra Poison/Sickness se algum dia forem implementados como dano contínuo. Os
// outros são categorias diferentes que ainda não têm código nenhum: Heal é o oposto de
// dano (cura, não dano negativo), Ice/Sleep/Stun/Petrification são incapacitação (mesma
// família do Trapped, não dano), Fear muda comportamento (foge em vez de atacar), Nature
// sem definição ainda. Confirmados pro MVP: Fire, Bleeding, Ice, Fear, Heal — o resto é
// só reserva de nome até decidir se entra no jogo.
public enum StatusEffectType
{
    Fire,
    Bleeding,
    Fear,
    Heal,
    Ice,
    Nature,
    Petrification,
    Poison,
    Shock,
    Sickness,
    Sleep,
    Stun
}

public class StatusEffectController : MonoBehaviour
{
    [SerializeField] private Animator statusAnimator; // Animator do filho "Status"
    [SerializeField] private float tickInterval = 1f; // 🔢 cadência genérica de todo efeito

    // Imunidades por tipo (ex.: Fire Elemental é imune a Fire) — configurado por monstro no
    // Inspector. Bloqueia só o EFEITO (DoT contínuo); dano de impacto direto (ex.: a
    // explosão em si da Ultimate do Mage) não passa por aqui, continua acertando normal —
    // quem decide se o dano de área/superfície também é bloqueado é a fonte do efeito (ex.:
    // MageFireball consulta IsImmuneTo antes de aplicar o tick do rastro no chão).
    [SerializeField] private StatusEffectType[] immunities;

    public bool IsImmuneTo(StatusEffectType type) => System.Array.IndexOf(immunities, type) >= 0;

    private class StatusEffectInstance
    {
        public StatusEffectType type;
        public float remaining;
        public float damagePerSecond;
        public AttackCooldown tick;
    }

    private IDamageable target;
    private readonly List<StatusEffectInstance> activeEffects = new List<StatusEffectInstance>();
    private string currentVisualState = "Empty"; // evita Play() repetido todo tick reiniciando o clipe do zero

    private void Awake()
    {
        target = GetComponent<IDamageable>();
    }

    // Chamado por quem aplica o efeito (ex.: MageFireball, no tick da área de fogo no chão).
    // Reaplicar o mesmo tipo só reseta a duração/dano da instância existente — não empilha
    // múltiplas instâncias do mesmo efeito na mesma entidade.
    public void ApplyStatusEffect(StatusEffectType type, float duration, float damagePerSecond)
    {
        if (IsImmuneTo(type)) return;

        var existing = activeEffects.Find(e => e.type == type);
        if (existing != null)
        {
            existing.remaining = duration;
            existing.damagePerSecond = damagePerSecond;
            return;
        }

        activeEffects.Add(new StatusEffectInstance
        {
            type = type,
            remaining = duration,
            damagePerSecond = damagePerSecond,
            tick = new AttackCooldown(tickInterval)
        });
        UpdateVisual();
    }

    // Chamado pelo Update() do herói/monstro dono (mesmo local de sempre — já respeita
    // GameplayGate/Floor Sleep de graça, porque só é chamado quando o dono já passou por
    // esses gates). Corre via TakeDamage() (mesmo caminho de dano de qualquer outra fonte),
    // então já herda aggro instantâneo, flash de dano e checagem de morte — e continuar
    // chamando isso depois da entidade morrer não faz nada de mal, TakeDamage() já ignora
    // dano em cima de quem já está morto.
    public void Tick(float deltaTime)
    {
        if (activeEffects.Count == 0) return;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            effect.remaining -= deltaTime;
            if (effect.remaining <= 0f)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            effect.tick.Tick(deltaTime);
            if (effect.tick.TryConsume() && target != null) target.TakeDamage(effect.damagePerSecond * tickInterval);
        }

        UpdateVisual();
    }

    // Sem stack visual — se tiver mais de 1 efeito ativo ao mesmo tempo, mostra só o
    // primeiro da lista (ordem de aplicação). Não existe caso de uso ainda que precise de
    // mostrar os 2 ao mesmo tempo. Só chama Play() quando o estado realmente muda — chamar
    // todo tick reiniciaria o clipe do zero a cada segundo, mesmo sem mudar nada.
    private void UpdateVisual()
    {
        if (statusAnimator == null) return;

        string stateName = activeEffects.Count > 0 ? activeEffects[0].type.ToString() : "Empty";
        if (stateName == currentVisualState) return;
        currentVisualState = stateName;
        statusAnimator.Play(stateName);
    }
}
