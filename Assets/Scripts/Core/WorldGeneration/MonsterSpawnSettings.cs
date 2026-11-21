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

    [Header("Horda (D9.4 — docs/superpowers/specs/2026-11-20-d9-4-horda-cerco-design.md)")]
    [Tooltip("Meta de monstros comuns vivos por (dia da run, tempo REAL do dia): começo e teto por dia, forma da subida, picos e limite absoluto. O tempo real não pausa com boss vivo — quem demora pra matar boss enfrenta mais monstros.")]
    public HordeCurveSettings horde = new HordeCurveSettings();

    [Tooltip("Quantos monstros podem nascer por segundo enquanto os vivos estão abaixo da meta (fluxo contínuo, espalhado pelos frames — nunca em rajada). Acima da velocidade de kill, o jogador forte nunca espera.")]
    public float spawnsPerSecond = 10f; // 🔢

    [Tooltip("Só no Dia 1: segundos de tempo real em que monstros que atiram de longe (EnemyDefinitionSO.isRanged) não nascem.")]
    public float rangedUnlockSeconds = 30f; // 🔢

    [Header("Distância (doc de design, Seção 2.1.3 / 11)")]
    [Tooltip("Somado a WorldGenerationSettings.safeZoneRadius pra decidir a distância mínima de spawn: MinimumSpawnDistanceFromCenter = safeZoneRadius + spawnSafetyPadding.")]
    public float spawnSafetyPadding = 0f;

    [Tooltip("Monstro comum mais longe que isso do jogador é removido sem loot/kill/energia (Distance Despawn, nunca morte de verdade) e substituído por 1 novo dentro do anel abaixo.")]
    public float enemyDespawnDistance = 30f;

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
