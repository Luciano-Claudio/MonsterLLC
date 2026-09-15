using UnityEngine;

public enum PatrolPhase { Idle, Walking }

// Só cuida da alternância aleatória idle/walk da patrulha — não sabe nada sobre combate.
// A proteção de "não cortar o idle no meio" não é responsabilidade desta classe: ela já
// é garantida pelo Exit Time da transição Idle -> Walk/IdleCombat no próprio Animator
// (rápido, do tamanho do clipe). Antes, essa classe também adiava a entrada em combate
// até a fase atual terminar — só que o timer da fase é bem mais longo (até
// maxWalkDuration, ex.: 4s) e não tem relação nenhuma com a duração real do clipe, o que
// deixava o monstro "ignorando" o jogador por tempo demais. Combate agora é decidido
// direto no EnemyController, sem passar por aqui.
public class PatrolAI
{
    private readonly float minIdleDuration, maxIdleDuration;
    private readonly float minWalkDuration, maxWalkDuration;
    private readonly System.Random rng;

    public PatrolPhase CurrentPhase { get; private set; }
    public Vector2 WalkTarget { get; private set; }

    private float phaseTimer;

    public PatrolAI(float minIdle, float maxIdle, float minWalk, float maxWalk, System.Random rng = null)
    {
        minIdleDuration = minIdle;
        maxIdleDuration = maxIdle;
        minWalkDuration = minWalk;
        maxWalkDuration = maxWalk;
        this.rng = rng ?? new System.Random();
        CurrentPhase = PatrolPhase.Idle;
        phaseTimer = NextDuration(minIdleDuration, maxIdleDuration);
    }

    public void Tick(float deltaTime, System.Func<Vector2> pickRandomWalkTarget)
    {
        phaseTimer -= deltaTime;
        if (phaseTimer > 0f) return;

        if (CurrentPhase == PatrolPhase.Idle)
        {
            CurrentPhase = PatrolPhase.Walking;
            WalkTarget = pickRandomWalkTarget();
            phaseTimer = NextDuration(minWalkDuration, maxWalkDuration);
        }
        else
        {
            CurrentPhase = PatrolPhase.Idle;
            phaseTimer = NextDuration(minIdleDuration, maxIdleDuration);
        }
    }

    private float NextDuration(float min, float max) => min + (float)rng.NextDouble() * (max - min);
}
