using System.Collections.Generic;
using UnityEngine;

// Config central do spawn de monstro comum no mundo aberto (Fase A — sem boss, sem
// dia/noite real ainda, ver docs/new/Mudanca_Estrutural_Mapa_Unico_Spawn_Bosses_
// ATUALIZADO_V2.md, Seções 2/11/14/16). Paralelo a WorldGenerationSettings (terreno) e
// PopulationConfig/FloorPopulationManager (modelo de crise/escalação reaproveitado, agora
// global em vez de por-Floor).
[CreateAssetMenu(fileName = "MonsterSpawnSettings", menuName = "MonsterLLC/World Generation/Monster Spawn Settings")]
public class MonsterSpawnSettings : ScriptableObject
{
    [Tooltip("Todo EnemyDefinitionSO disponível pro mundo aberto, qualquer tier (1-10). Tier sem nenhuma entrada aqui simplesmente nunca spawna, mesmo com peso > 0 na tabela de dia (ver WorldPopulationManager.SpawnOne).")]
    public List<EnemyDefinitionSO> roster = new();

    // 3 campos soltos, não a classe PopulationConfig (FloorPopulationManager) — esse script
    // vive no assembly "Core" (Assets/Scripts/Core/Core.asmdef), que não referencia o
    // assembly padrão onde PopulationConfig mora; duplicar 3 ints é mais simples que mover
    // arquivo/assembly só por causa disso.
    [Header("População global (substitui FloorPopulationManager por-Floor)")]
    public int populationMinimum = 3;
    public int populationTarget = 8;
    public int populationMaximum = 12;

    [Tooltip("Cooldown do repovoamento normal (1 por vez) — igual FloorPopulationManager, ignorado nos preenchimentos instantâneos (entrada/crise).")]
    public float respawnInterval = 3f;

    [Tooltip("Quantas crises seguidas (caiu até o mínimo) até o patamar min/target/max escalar permanentemente — mesmo modelo do FloorPopulationManager, agora 1 população só pro mundo inteiro.")]
    public int crisisStreakToEscalate = 3;

    [Header("Distância (doc de design, Seção 2.1.3 / 11)")]
    [Tooltip("Somado a WorldGenerationSettings.safeZoneRadius pra decidir a distância mínima de spawn: MinimumSpawnDistanceFromCenter = safeZoneRadius + spawnSafetyPadding.")]
    public float spawnSafetyPadding = 0f;

    [Tooltip("Monstro comum mais longe que isso do jogador é removido sem loot/kill/energia (Distance Despawn, nunca morte de verdade) e substituído por 1 novo dentro do anel abaixo.")]
    public float enemyDespawnDistance = 40f;

    [Tooltip("Anel de spawn/replacement ao redor do jogador — Spawn Annulus (doc, Seção 11.5/14).")]
    public float replacementSpawnMinDistance = 15f;
    public float replacementSpawnMaxDistance = 30f;

    [Tooltip("Tentativas de amostragem de posição válida (fora da Safe Zone + nó caminhável do GridGraph) antes de desistir por este ciclo — nunca trava em loop infinito (doc, Seção 11.6).")]
    public int maxSpawnPositionAttempts = 12;

    [Header("Distância física de obstáculo (só pro spawn, nunca muda o grafo A*)")]
    [Tooltip("Raio de sondagem física (Physics2D.OverlapCircle) em volta do ponto de spawn candidato — rejeita se colidir com qualquer coisa de Obstacle Layer Mask. Resolve monstro nascer colado demais numa borda (água/prop) sem usar Erosion Iterations no GridGraph, que afetaria o PATHING normal de todo mundo, não só o spawn. 0 = desligado.")]
    public float spawnObstacleClearanceRadius = 0.6f;

    [Tooltip("Layer(s) considerada(s) obstáculo pra checagem acima — mesma layer usada no Obstacle Layer Mask do GridGraph OpenWorld (ex.: \"Collision\").")]
    public LayerMask obstacleLayerMask;

    [Header("Visão da câmera (doc de design, Seção 23/P9)")]
    [Range(0f, 1f)]
    [Tooltip("% dos spawns (rolado individualmente por monstro, não por EnemyDefinitionSO) que PREFEREM nascer fora da visão atual da câmera — o resto pode nascer perto/à vista do jogador de propósito (decisão do usuário: dar um ar de perigo). Nunca sobrepõe a Safe Zone (ver WorldSpawnUtility.TryGetSpawnPosition) — é só uma preferência de posição dentro do Spawn Annulus.")]
    public float outsideCameraViewChance = 0.6f;
}
