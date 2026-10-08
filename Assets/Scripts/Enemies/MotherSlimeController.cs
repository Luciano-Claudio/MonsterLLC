using UnityEngine;

// Mother Slime Green/Blue (Bestiário, Andar 1 — Bosses). Mesma arquitetura do Slime comum
// (SlimeEnemyController: só dano de contato, hasAttackAnimation=false no Inspector, pula e
// causa dano ao encostar, sem attack dedicado "pra sempre" — exceção permanente do jogo).
// A única adição: ao morrer, nascem 3 Slimes normais em POSIÇÕES FIXAS (3 Transforms filhos
// deste próprio GameObject, configurados no prefab — GDD é explícito que não são
// aleatórios). 1 script serve os dois bosses — childSlimePrefab decide a cor.
public class MotherSlimeController : SlimeEnemyController
{
    [SerializeField] private GameObject childSlimePrefab; // Slime_Green.prefab ou Slime_Blue.prefab, conforme o boss
    [SerializeField] private Transform[] childSpawnPoints; // 3 pontos fixos, filhos deste GameObject (GDD)

    private bool childrenSpawned; // própria trava — AnimationDieEndEvent() pode ser chamado 2x (evento real + timeout de segurança da base), e childSpawnPoints deixam de existir depois do Destroy() da base

    public override void AnimationDieEndEvent()
    {
        if (!childrenSpawned)
        {
            childrenSpawned = true;
            SpawnChildren();
        }
        base.AnimationDieEndEvent(); // dieHandled (privado, na base) já evita destruir/dropar loot 2x
    }

    private void SpawnChildren()
    {
        if (childSlimePrefab == null) return;
        foreach (var point in childSpawnPoints)
        {
            if (point == null) continue;

            var childObj = Instantiate(childSlimePrefab, point.position, Quaternion.identity);
            var childController = childObj.GetComponent<EnemyController>();
            if (childController == null) continue;

            // Mesmo Floor da mãe (ela já tinha isso certo — sem isso, os filhotes nasceriam
            // sem ownerFloor, sem restrição de graph e fora do gate de FloorActivationCheck).
            childController.ownerFloor = ownerFloor;

            // Decisão do usuário: os 3 Slimes nascem já com o jogador como alvo, igual ao
            // boss que acabou de morrer — sem fase de patrulha/idle esperando
            // observationRadius detectar (mesmo critério de "agressão imediata" do
            // BossSpawnManager, reaproveitado aqui).
            childController.ForceImmediateAggro();
        }
    }
}
