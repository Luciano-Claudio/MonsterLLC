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

    [Tooltip("Monstro comum mais longe que isso do herói é reposicionado no anel de spawn, na frente (D9.4) — nunca morte de verdade.")]
    public float enemyDespawnDistance = 30f;

    [Header("Mini bosses (2026-11-21 — docs/superpowers/specs/2026-11-21-mini-bosses-design.md)")]
    [Tooltip("Ex-bosses que viraram monstros comuns mais fortes. Nasce 1 sozinho a cada miniBossIntervalMin-Max segundos de tempo real do dia, sem aviso, sorteado entre os do tier que o dia já tem (sem repetir o último). Não entram na meta da horda.")]
    public List<EnemyDefinitionSO> miniBossRoster = new();
    public float miniBossIntervalMin = 30f; // 🔢
    public float miniBossIntervalMax = 40f; // 🔢

    [Header("Cerco (D9.4) — nasce fora da tela, numa elipse com a proporção da tela, por setores")]
    [Tooltip("Em quantos setores o anel é dividido. Cada monstro novo vai pro setor com menos monstros vivos.")]
    public int sectorCount = 12; // 🔢
    [Tooltip("Distância extra (unidades) da elipse além dos cantos da tela.")]
    public float offscreenMargin = 2f; // 🔢
    [Tooltip("Variação extra (unidades) pra fora da elipse, pra não nascer todo mundo na mesma linha.")]
    public float ringJitter = 1f; // 🔢

    [Header("Anti-kite (D9.4) — sempre ligado, com limite")]
    [Tooltip("Janela (s) da média de velocidade do herói usada pra saber pra onde ele foge.")]
    public float kiteWindowSeconds = 3f; // 🔢
    [Tooltip("Velocidade média mínima (u/s) pra contar como fuga consistente.")]
    public float kiteMinSpeed = 1.5f; // 🔢
    [Tooltip("Arco (graus) à frente da fuga cujos setores ganham peso.")]
    public float kiteArcDegrees = 90f; // 🔢
    [Tooltip("Peso dos setores da frente, contado como 'monstros a menos' na escolha do setor.")]
    public float kiteBias = 3f; // 🔢
    [Range(0f, 1f)]
    [Tooltip("Limite: fração máxima dos últimos spawns que pode ir pra frente da fuga. Passou disso, a escolha volta a ser só por contagem.")]
    public float kiteMaxFrontShare = 0.4f; // 🔢
    [Tooltip("Quantos spawns recentes entram na conta do limite acima.")]
    public int kiteShareWindowSpawns = 20; // 🔢

    [Tooltip("Tentativas de amostragem de posição válida (fora da Safe Zone + nó caminhável do GridGraph) antes de desistir por este ciclo — nunca trava em loop infinito (doc, Seção 11.6).")]
    public int maxSpawnPositionAttempts = 12;

    [Header("Distância física de obstáculo (só pro spawn, nunca muda o grafo A*)")]
    [Tooltip("Raio de sondagem física (Physics2D.OverlapCircle) em volta do ponto de spawn candidato — rejeita se colidir com qualquer coisa de Obstacle Layer Mask. Resolve monstro nascer colado demais numa borda (água/prop) sem usar Erosion Iterations no GridGraph, que afetaria o PATHING normal de todo mundo, não só o spawn. 0 = desligado.")]
    public float spawnObstacleClearanceRadius = 0.6f;

    [Tooltip("Layer(s) considerada(s) obstáculo pra checagem acima — mesma layer usada no Obstacle Layer Mask do GridGraph OpenWorld (ex.: \"Collision\").")]
    public LayerMask obstacleLayerMask;
}
