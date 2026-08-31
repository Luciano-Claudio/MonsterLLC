using System;

public static class GameEvents
{
    public static event Action OnEnemyKilled;
    public static void EnemyKilled() => OnEnemyKilled?.Invoke();

    // Mais eventos entram aqui conforme os sistemas nascerem.
    // Nenhum outro script deve declarar um event solto — tudo passa por aqui.
}
