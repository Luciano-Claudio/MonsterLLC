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
    Stun,

    // Sprint 23 (Cleric, Ultimate/Oração) — primeiro Efeito que é DoT E incapacitação ao
    // mesmo tempo (as categorias acima sempre foram uma coisa OU outra). EnemyController
    // consulta IsEffectActive(WordOfPain) direto pra decidir se paralisa — sem flag própria,
    // a incapacitação dura exatamente o tempo que o efeito durar. Também é "superior" a
    // qualquer outro Efeito ativo na prioridade visual (ver UpdateVisual).
    WordOfPain,

    // Sprint 30 (Blood Mage, Shift/Extract Blood) — primeiro uso de FlashStatus fora do Heal:
    // toca 1x só quando o sangue do monstro é extraído, sem dano/duração própria (o dano real
    // é aplicado direto por BloodMage, fora daqui). Prioridade visual abaixo só de WordOfPain
    // (ver guarda em FlashStatus) — cancela Fire/Bleeding/etc. na tela, mas nunca escapa a
    // paralisia da Oração.
    Drain,

    // D9.1 (Druid Floresta, Ultimate) — mesmo padrão do Drain: toca 1x (FlashStatus) quando as
    // folhas atingem o monstro; o efeito real é o Poison aplicado junto, que assume o visual
    // em loop quando o flash termina. SEMPRE no fim do enum (valores salvos em prefab são int).
    LeavesSpell
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
    [SerializeField] private StatusEffectType[] immunities = new StatusEffectType[0]; // nunca nulo (AddComponent em runtime/teste não passa pela serialização)

    public bool IsImmuneTo(StatusEffectType type) => immunities != null && System.Array.IndexOf(immunities, type) >= 0;

    // Consultado por quem precisa saber "esse efeito está rolando agora" sem ser dono dele
    // (ex.: EnemyController checando WordOfPain pra decidir paralisia — ver comentário no
    // enum).
    // Laço simples, sem lambda: o Exists(e => e.type == type) alocava um closure por chamada, e
    // isto roda por monstro por frame (IsIncapacitated no EnemyController + UpdateVisual) — achado
    // da revisão final do D9.1, antes do teste de densidade da horda.
    // Catálogo de cartas — algum efeito que dá dano (DOT) ativo? (Withering Eye, Elemental Supremacy...)
    public bool HasAnyDot
    {
        get
        {
            for (int i = 0; i < activeEffects.Count; i++)
                if (DealsDamage(activeEffects[i].damagePerSecond)) return true;
            return false;
        }
    }

    public bool IsEffectActive(StatusEffectType type)
    {
        for (int i = 0; i < activeEffects.Count; i++)
            if (activeEffects[i].type == type) return true;
        return false;
    }

    // Efeito com dano 0 (ex.: Stun) não chama TakeDamage — senão cada tick mostrava "0" na tela,
    // disparava a reação de dano do monstro e contava como tick de DoT (revisão final do D9.1).
    public static bool DealsDamage(float damagePerSecond) => damagePerSecond > 0f;

    // Incapacitação total (IA/ataque/movimento travam) — EnemyController consulta isto. D9.1:
    // Stun (Ultimate do Druid Feral) entra na mesma mecânica da paralisia da Oração do Cleric.
    public bool IsIncapacitated => IsEffectActive(StatusEffectType.WordOfPain) || IsEffectActive(StatusEffectType.Stun);

    // Catálogo de cartas — cada aplicação é uma pilha (DotStackRules). Sem Blight Crucible o limite é 1,
    // que é exatamente o comportamento antigo (reaplicar renova duração e dano).
    private IDamageable target;
    private readonly List<StatusStack> activeEffects = new List<StatusStack>();
    private long applyOrder;

    // Inimigos marcam isto (EnemyController.Awake): os DOTs neles recebem os bônus das cartas do herói
    // (pilhas, duração, dano, crítico do tick). DOT no próprio herói não muda.
    public bool UseHeroDotModifiers { get; set; }

    // Verdadeiro só durante o TakeDamage de um tick — o EnemyController usa pra saber se morreu por DOT.
    public bool IsTicking { get; private set; }

    public int StackCount(StatusEffectType type)
    {
        int n = 0;
        for (int i = 0; i < activeEffects.Count; i++) if (activeEffects[i].type == type) n++;
        return n;
    }

    // A pilha mais forte (maior dps) de cada tipo de DOT ativo — Plague Herald repassa estas.
    public void CollectStrongestDots(List<StatusStack> into)
    {
        into.Clear();
        for (int i = 0; i < activeEffects.Count; i++)
        {
            var s = activeEffects[i];
            if (!DealsDamage(s.damagePerSecond)) continue;
            int found = -1;
            for (int j = 0; j < into.Count; j++) if (into[j].type == s.type) { found = j; break; }
            if (found < 0) into.Add(s);
            else if (s.damagePerSecond > into[found].damagePerSecond) into[found] = s;
        }
    }
    private string currentVisualState = "Empty"; // evita Play() repetido todo tick reiniciando o clipe do zero

    // Sprint 23 (Cleric, Shift/Reza) — "flash" visual de 1x só, não é um Efeito com duração
    // (não entra em activeEffects, não causa dano, não é removível/imune). Usado hoje só pelo
    // Heal: mostra por cima de qualquer Efeito ativo por um tempo fixo, depois volta sozinho
    // pro que estava tocando antes (ex.: Fire).
    private bool isFlashing;
    private float flashRemaining;

    private void Awake()
    {
        target = GetComponent<IDamageable>();
    }

    // Toca type imediatamente, por cima de qualquer Efeito ativo, por duration segundos — ao
    // fim, UpdateVisual() volta a rodar normalmente (WordOfPain > primeiro da lista > Empty).
    // Não usa ApplyStatusEffect/activeEffects de propósito: isso é só visual, instantâneo, sem
    // dano, sem duração acumulável — quem quiser dano real por cima ainda usa ApplyStatusEffect.
    // Exceção: WordOfPain nunca é escondido por um flash (Sprint 30, Drain do Blood Mage) —
    // é incapacitação, não só visual, então continua acima de qualquer flash de 1x só.
    public void FlashStatus(StatusEffectType type, float duration)
    {
        if (IsEffectActive(StatusEffectType.WordOfPain)) return;

        isFlashing = true;
        flashRemaining = duration;

        if (statusAnimator == null) return;
        currentVisualState = type.ToString();
        statusAnimator.Play(currentVisualState);
    }

    // Chamado na morte do herói/monstro (HeroController.OnDeath() / EnemyController.Die()) —
    // sem isso, um Efeito Nocivo ativo no instante da morte fica "congelado" (Update() já
    // para de chamar Tick() quando isDead/morto, então o efeito nunca decrementa nem
    // termina) e retoma sozinho depois do respawn, como se nada tivesse acontecido (bug
    // real: herói respawnava ainda queimando). Reseta o visual pra "Empty" na hora, mesmo
    // critério de "não faz sentido renascer com o status antigo ainda ativo".
    public void ClearAllEffects()
    {
        activeEffects.Clear();
        isFlashing = false;
        UpdateVisual();
    }

    // Chamado por quem aplica o efeito (ex.: MageFireball, no tick da área de fogo no chão).
    // Reaplicar o mesmo tipo só reseta a duração/dano da instância existente — não empilha
    // múltiplas instâncias do mesmo efeito na mesma entidade.
    // Etapa 4 — dps do efeito ativo (0 se não estiver ativo). Leitura pra testes/UI.
    public float GetDamagePerSecond(StatusEffectType type)
    {
        float best = 0f;
        for (int i = 0; i < activeEffects.Count; i++)
            if (activeEffects[i].type == type && activeEffects[i].damagePerSecond > best) best = activeEffects[i].damagePerSecond;
        return best;
    }

    public void ApplyStatusEffect(StatusEffectType type, float duration, float damagePerSecond)
    {
        if (IsImmuneTo(type)) return;

        bool heroDot = UseHeroDotModifiers && DealsDamage(damagePerSecond);
        int maxStacks = heroDot ? DotModifiers.MaxStacks : 1;
        float finalDuration = heroDot ? duration * DotModifiers.DurationMultiplier : duration;
        float finalDps = heroDot ? damagePerSecond * DotModifiers.DamageMultiplier : damagePerSecond;
        DotStackRules.Apply(activeEffects, type, finalDuration, duration, finalDps, maxStacks, ref applyOrder, tickInterval);
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
        if (isFlashing)
        {
            flashRemaining -= deltaTime;
            if (flashRemaining <= 0f) isFlashing = false; // próximo UpdateVisual() abaixo já volta pro estado real
        }

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            if (i >= activeEffects.Count) continue; // a morte no tick anterior pode ter limpado a lista
            var effect = activeEffects[i];
            effect.remaining -= deltaTime;
            if (effect.remaining <= 0f)
            {
                activeEffects.RemoveAt(i);
                continue;
            }

            effect.tick.Tick(deltaTime);
            // isDirectPlayerHit: false — DoT nunca aciona o Game Feel de hit direto
            // (HitFeelController), só o hit de verdade aciona.
            if (effect.tick.TryConsume() && target != null && DealsDamage(effect.damagePerSecond))
            {
                float damage = effect.damagePerSecond * tickInterval;
                bool crit = UseHeroDotModifiers && DotModifiers.CritChance > 0f && UnityEngine.Random.value < DotModifiers.CritChance;
                if (crit) damage *= DotModifiers.CritMultiplier; // Cursed Diadem
                var type = effect.type;
                IsTicking = true;
                target.TakeDamage(damage, isDirectPlayerHit: false);
                IsTicking = false;
                if (UseHeroDotModifiers) GameEvents.DotTicked(this, type, damage, crit);
            }
        }

        // Enquanto o flash estiver tocando, ele manda no visual sozinho (FlashStatus já deu o
        // Play) — UpdateVisual() só volta a decidir o ícone de verdade quando ele acabar.
        if (!isFlashing) UpdateVisual();
    }

    // Sem stack visual — se tiver mais de 1 efeito ativo ao mesmo tempo, mostra só 1. WordOfPain
    // é "superior" a qualquer outro (decisão explícita do usuário — Oração do Cleric precisa
    // ficar visualmente óbvia mesmo num monstro já pegando Fire/Bleeding), checado antes de
    // cair no critério padrão (primeiro da lista, ordem de aplicação). Só chama Play() quando
    // o estado realmente muda — chamar todo tick reiniciaria o clipe do zero a cada segundo,
    // mesmo sem mudar nada.
    private void UpdateVisual()
    {
        if (statusAnimator == null) return;

        string stateName;
        if (IsEffectActive(StatusEffectType.WordOfPain)) stateName = StatusEffectType.WordOfPain.ToString();
        // Stun logo abaixo da Oração (D9.1): incapacitação é o feedback mais importante — um
        // monstro envenenado e stunado mostra o Stun, não o Poison.
        else if (IsEffectActive(StatusEffectType.Stun)) stateName = StatusEffectType.Stun.ToString();
        else stateName = activeEffects.Count > 0 ? activeEffects[0].type.ToString() : "Empty";

        if (stateName == currentVisualState) return;
        currentVisualState = stateName;
        statusAnimator.Play(stateName);
    }
}
