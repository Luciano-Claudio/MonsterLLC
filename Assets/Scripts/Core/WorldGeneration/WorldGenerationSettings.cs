using UnityEngine;

// Primeiro ScriptableObject do projeto — decisão deliberada (docs/new/Mudanca_Estrutural_
// Geracao_Procedural..., Seção 23: "WorldGenerationSettingsSO") pra manter a configuração do
// mundo editável como asset, sem exigir recompilar código pra ajustar seed/tamanho de
// chunk/raio. Valores default são ilustrativos, não balanceamento final (mesmo documento,
// Seção 26: "não fixar sem teste").
[CreateAssetMenu(fileName = "WorldGenerationSettings", menuName = "MonsterLLC/World Generation Settings")]
public class WorldGenerationSettings : ScriptableObject
{
    [Tooltip("Seed determinística do mundo desta run. Sprint P10 passa a ler isto do RunState em vez do asset (GDD Seção 15/29).")]
    public int worldSeed = 0;

    [Tooltip("Tamanho de cada chunk, em unidades de mundo. Ilustrativo (32x32 ou 64x64) — medir em protótipo antes de fechar.")]
    public int chunkSize = 64;

    [Tooltip("Quantos chunks de raio ficam ativos ao redor do alvo de streaming (normalmente o jogador).")]
    public int streamingRadiusInChunks = 2;

    [Tooltip("Margem extra de chunks consultados (mas não necessariamente ativados) pra resolver vizinhança/bordas sem depender de quem carregou primeiro (halo).")]
    public int haloInChunks = 1;
}
