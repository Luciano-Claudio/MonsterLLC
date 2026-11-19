// O que pode encurtar um teleporte/dash de mira livre (Mage, Assassin — TeleportUtility):
// só collider SÓLIDO de cenário (água, parede, árvore, prop). Nunca trigger, nunca o próprio
// herói e nunca monstro — bug real (2026-11-19): o teleporte parava no corpo dos monstros
// (CapsuleCollider2D sólido na layer "Enemy"); a trava existe pra não pousar dentro de
// cenário, e o herói deve passar por dentro da horda.
public static class TeleportBlockRule
{
    public static bool Blocks(int colliderLayer, bool isTrigger, bool isCaster, int enemyLayer)
    {
        if (isTrigger || isCaster) return false;
        return colliderLayer != enemyLayer;
    }
}
