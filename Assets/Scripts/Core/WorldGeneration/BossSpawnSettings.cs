using System.Collections.Generic;
using UnityEngine;

// Config central do spawn de Boss no mundo aberto (Sprint P7, ver docs/new/Mudanca_
// Estrutural_Mapa_Unico_Spawn_Bosses_ATUALIZADO_V2.md, Seção 17, e docs/new/Plano_Spawn_
// Monstros_Mundo_Aberto.md). Paralelo a MonsterSpawnSettings, mas pros parâmetros próprios
// de Boss — substitui os valores hardcoded do BossSpawnManager antigo (ex.: intervalo fixo
// de 10s), que foi removido junto com o resto do sistema de Floor.
[CreateAssetMenu(fileName = "BossSpawnSettings", menuName = "MonsterLLC/World Generation/Boss Spawn Settings")]
public class BossSpawnSettings : ScriptableObject
{
    [Tooltip("Todo BossDefinitionSO disponível pro mundo aberto. Tier sem nenhuma entrada aqui faz o Boss Timer cair pro Tier1 quando sorteado (mesmo critério de fallback do monstro comum) — nunca fica sem spawnar nada.")]
    public List<BossDefinitionSO> roster = new();

    [Tooltip("Intervalo do Boss Timer global (doc: ~20-30s, substitui os 10s fixos do BossSpawnManager antigo). Reinicia a cada spawn — despawn por distância NÃO reinicia (ver bossReplacementCooldown).")]
    public float bossSpawnInterval = 30f;

    [Tooltip("Distância do jogador em que um Boss é removido sem loot/kill (Distance Despawn) e substituído — maior que o enemyDespawnDistance do monstro comum, de propósito (Boss é mais raro/importante, dá mais folga antes de desistir dele).")]
    public float bossDespawnDistance = 45f;

    [Tooltip("Anel de spawn/replacement ao redor do jogador pro Boss — mesmo conceito do Spawn Annulus do monstro comum, mas com seus próprios valores (Boss pode valer a pena nascer um pouco mais longe).")]
    public float bossReplacementSpawnMinDistance = 15f;
    public float bossReplacementSpawnMaxDistance = 25f;

    [Tooltip("Cooldown mínimo entre um replacement de Boss por distância e o próximo — proteção contra abuso (doc, Seção 17): sem isso, ficar andando pra lá e pra cá na borda do bossDespawnDistance poderia gerar replacement repetido rápido demais.")]
    public float bossReplacementCooldown = 10f;

    [Tooltip("Somado a WorldGenerationSettings.safeZoneRadius pra decidir a distância mínima de spawn de Boss — mesmo conceito do monstro comum.")]
    public float spawnSafetyPadding = 0f;

    [Tooltip("Tentativas de amostragem de posição válida (fora da Safe Zone + nó caminhável) antes de desistir por este ciclo.")]
    public int maxSpawnPositionAttempts = 20;

    [Header("Distância física de obstáculo (só pro spawn, nunca muda o grafo A*)")]
    [Tooltip("Mesmo mecanismo do monstro comum (WorldSpawnUtility) — raio de sondagem física pra não nascer colado numa borda.")]
    public float spawnObstacleClearanceRadius = 0.6f;
    public LayerMask obstacleLayerMask;

    [Header("Visão da câmera (doc de design, Seção 23/P9)")]
    [Range(0f, 1f)]
    [Tooltip("% dos spawns de boss (rolado individualmente, não por BossDefinitionSO) que PREFEREM nascer fora da visão atual da câmera — o resto pode nascer perto/à vista do jogador de propósito (decisão do usuário: dar um ar de perigo). Mesmo mecanismo do monstro comum, valor próprio e independente aqui.")]
    public float outsideCameraViewChance = 0.6f;
}
