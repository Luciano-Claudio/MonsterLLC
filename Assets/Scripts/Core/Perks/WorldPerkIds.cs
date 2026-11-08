// IDs estáveis dos 30 perks da área "Mundo" (GDD Deadline 8.1, Seção 8) — usados tanto no
// PerkDefinitionSO.perkId de cada asset quanto em WorldPerkEffects.Recompute() pra ler o rank
// certo via PerkState.GetRank(id). Nunca mudar um valor aqui depois de publicado (o save
// guarda só a string, em RunState.perkRanks).
public static class WorldPerkIds
{
    public const string InstintoCacador = "world_instinto_cacador";
    public const string ButimGuerra = "world_butim_guerra";
    public const string FaroSaque = "world_faro_saque";
    public const string Atracao = "world_atracao";
    public const string CampoAtracao = "world_campo_atracao";
    public const string HorizonteColeta = "world_horizonte_coleta";
    public const string ImaVivo = "world_ima_vivo";
    public const string NegocianteNato = "world_negociante_nato";
    public const string MercadorAstuto = "world_mercador_astuto";
    public const string RedeContatos = "world_rede_contatos";
    public const string TesourariaReal = "world_tesouraria_real";
    public const string BauExtra = "world_bau_extra";
    public const string BauDuplo = "world_bau_duplo";
    public const string MaisEspacoBag = "world_mais_espaco_bag";
    public const string StackMaior = "world_stack_maior";
    public const string RerollTalento = "world_reroll_talento";
    public const string Vigor = "world_vigor";
    public const string Fortitude = "world_fortitude";
    public const string PulsoVital = "world_pulso_vital";
    public const string VitalidadeSuprema = "world_vitalidade_suprema";
    public const string PeleGrossa = "world_pele_grossa";
    public const string Couraca = "world_couraca";
    public const string CascoFerro = "world_casco_ferro";
    public const string Recuperacao = "world_recuperacao";
    public const string MetabolismoAcelerado = "world_metabolismo_acelerado";
    public const string FolegoEterno = "world_folego_eterno";
    public const string PassoLeve = "world_passo_leve";
    public const string VentoCostas = "world_vento_costas";
    public const string PesLigeiros = "world_pes_ligeiros";
    public const string SegundaChance = "world_segunda_chance";
}
