// IDs dos 90+1 perks bespoke do Ranger (GDD Seções 12/13/14) — número no nome da constante é o
// número ORIGINAL do GDD dentro de cada árvore (nunca a posição na linha/UI). Mesmo critério de
// BarbarianPerkIds.cs/WorldPerkIds.cs.
public static class RangerPerkIds
{
    public const string Central = "central_awakening_ranger";

    // Esquerda — Ataque Primário (Seção 12)
    public const string EsquerdaMiraAfiada = "ranger_esquerda_01";
    public const string EsquerdaPrecisaoMortal = "ranger_esquerda_02";
    public const string EsquerdaTiroCerteiro = "ranger_esquerda_03";
    public const string EsquerdaChuvaMortal = "ranger_esquerda_04";
    public const string EsquerdaDisparoSupremo = "ranger_esquerda_05";
    public const string EsquerdaFlechaVeloz = "ranger_esquerda_06";
    public const string EsquerdaVentoCortante = "ranger_esquerda_07";
    public const string EsquerdaVelocidadeSonica = "ranger_esquerda_08";
    public const string EsquerdaAlcanceEstendido = "ranger_esquerda_09";
    public const string EsquerdaVistaDeAguia = "ranger_esquerda_10";
    public const string EsquerdaHorizonteInfinito = "ranger_esquerda_11";
    public const string EsquerdaImpactoPerfurante = "ranger_esquerda_12";
    public const string EsquerdaForcaDoDisparo = "ranger_esquerda_13";
    public const string EsquerdaOndaDeChoque = "ranger_esquerda_14";
    public const string EsquerdaCarcazCheio = "ranger_esquerda_15";
    public const string EsquerdaChuvaDeFlechas = "ranger_esquerda_16";
    public const string EsquerdaTempestadeDeSetas = "ranger_esquerda_17";
    public const string EsquerdaMaosRapidas = "ranger_esquerda_18";
    public const string EsquerdaReflexoDeCacador = "ranger_esquerda_19";
    public const string EsquerdaInstintoAgil = "ranger_esquerda_20";
    public const string EsquerdaDisparoRapido = "ranger_esquerda_21";
    public const string EsquerdaFluidezDeCombate = "ranger_esquerda_22";
    public const string EsquerdaPrecisaoVeloz = "ranger_esquerda_23";
    public const string EsquerdaGolpeVital = "ranger_esquerda_24";
    public const string EsquerdaOlhoDeFalcao = "ranger_esquerda_25";
    public const string EsquerdaAtaqueFurtivo = "ranger_esquerda_26";
    public const string EsquerdaTiroADistancia = "ranger_esquerda_27";
    public const string EsquerdaFlechasSangrentas = "ranger_esquerda_28";
    public const string EsquerdaFlechaBumerangue = "ranger_esquerda_29";
    public const string EsquerdaEnxamePerfurante = "ranger_esquerda_30";

    // Direita — Shift/Survival (Seção 13)
    public const string DireitaRegeneracaoFurtiva = "ranger_direita_01";
    public const string DireitaCuraSilenciosa = "ranger_direita_02";
    public const string DireitaRenovacaoNaSombra = "ranger_direita_03";
    public const string DireitaVidaOculta = "ranger_direita_04";
    public const string DireitaPulsoVital = "ranger_direita_05";
    public const string DireitaFluxoConstante = "ranger_direita_06";
    public const string DireitaRitmoDaSombra = "ranger_direita_07";
    public const string DireitaReflexoFurtivo = "ranger_direita_08";
    public const string DireitaInstintoSilencioso = "ranger_direita_09";
    public const string DireitaSombraVeloz = "ranger_direita_10";
    public const string DireitaDissipacaoRapida = "ranger_direita_11";
    public const string DireitaDisfarcePerfeito = "ranger_direita_12";
    public const string DireitaEmboscadaPerfeita = "ranger_direita_13";
    public const string DireitaReflexosDeEmergencia = "ranger_direita_14";
    public const string DireitaRetornoSombrio = "ranger_direita_15";
    public const string DireitaVeuProlongado = "ranger_direita_16";
    public const string DireitaSombraVingativa = "ranger_direita_17";
    public const string DireitaInstintoDeSobrevivencia = "ranger_direita_18";
    public const string DireitaFolegoSombrio = "ranger_direita_19";
    public const string DireitaReflexosNasSombras = "ranger_direita_20";
    public const string DireitaInstintoPredador = "ranger_direita_21";
    public const string DireitaUltimaLinhaDeDefesa = "ranger_direita_22";
    public const string DireitaArmadilhaCamuflada = "ranger_direita_23";
    public const string DireitaInstintoCacador = "ranger_direita_24";
    // #25-30: "topo da árvore" — remove a necessidade do disfarce (Veterano de Guerra) e os
    // perks que só fazem sentido nesse modo novo (visível/vulnerável). Ver RecomputeDireita()
    // em Ranger.cs e o comentário de UseSecondaryAbility() pro porquê desses 5 exigirem um
    // caminho de código próprio, não só leitura de rank.
    public const string DireitaVeteranoDeGuerra = "ranger_direita_25";
    public const string DireitaPosturaDeCombate = "ranger_direita_26";
    public const string DireitaFuriaCurativa = "ranger_direita_27";
    public const string DireitaVigorDeBatalha = "ranger_direita_28";
    public const string DireitaReflexosDeVeterano = "ranger_direita_29";
    public const string DireitaImortalEmCombate = "ranger_direita_30"; // pré-requisito: DireitaVeteranoDeGuerra >= 1 (ver PerkDefinitionSO.prerequisitePerkId)

    // Cima — Ultimate (Seção 14)
    public const string CimaLaminaCerteira = "ranger_cima_01";
    public const string CimaCorteProfundo = "ranger_cima_02";
    public const string CimaFuriaDasLaminas = "ranger_cima_03";
    public const string CimaTempestadeDeAco = "ranger_cima_04";
    public const string CimaLaminaSuprema = "ranger_cima_05";
    public const string CimaFacasFincadas = "ranger_cima_06";
    public const string CimaLaminasCravadas = "ranger_cima_07";
    public const string CimaArmadilhaMortal = "ranger_cima_08";
    public const string CimaSangramentoIntenso = "ranger_cima_09";
    public const string CimaHemorragia = "ranger_cima_10";
    public const string CimaFeridaFatal = "ranger_cima_11";
    public const string CimaVooVeloz = "ranger_cima_12";
    public const string CimaLaminaRelampago = "ranger_cima_13";
    public const string CimaVelocidadeMortal = "ranger_cima_14";
    public const string CimaAlcanceDaLamina = "ranger_cima_15";
    public const string CimaLongoVoo = "ranger_cima_16";
    public const string CimaHorizonteDeAco = "ranger_cima_17";
    public const string CimaImpactoDaLamina = "ranger_cima_18";
    public const string CimaForcaCortante = "ranger_cima_19";
    public const string CimaArmadilhaDuradoura = "ranger_cima_20";
    public const string CimaLaminaPersistente = "ranger_cima_21";
    public const string CimaCampoMinado = "ranger_cima_22";
    public const string CimaAreaAmpliada = "ranger_cima_23";
    public const string CimaZonaDePerigo = "ranger_cima_24";
    public const string CimaVenenoProlongado = "ranger_cima_25";
    public const string CimaCanalizacaoRapida = "ranger_cima_26";
    public const string CimaInstintoDeCaca = "ranger_cima_27";
    public const string CimaGolpeCriticoSombrio = "ranger_cima_28";
    public const string CimaSegundaRajada = "ranger_cima_29";
    public const string CimaChuvaDeLaminas = "ranger_cima_30";
}
