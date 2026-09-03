public static class DemandCalculator
{
    // GDD Secao 39: "Demanda do Dia = 40 x 2^(Dia - 1)".
    public static int GetDemand(int day) => (int)(40 * System.Math.Pow(2, day - 1));
}
