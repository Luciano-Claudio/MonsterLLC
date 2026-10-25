public static class DemandCalculator
{
    // GDD Secao 39: "Demanda do Dia = 40 x 2^(Dia - 1)". long (nao int) - por volta do Dia 28
    // esse valor ja passa de 2,1 bilhoes, acima do teto do int32.
    public static long GetDemand(int day) => (long)(40 * System.Math.Pow(2, day - 1));
}
