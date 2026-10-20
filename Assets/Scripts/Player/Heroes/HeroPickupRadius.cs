using UnityEngine;

// GameObject FILHO dedicado, criado via AddComponent pelo HeroController — nunca compartilha
// GameObject com o CapsuleCollider2D do corpo. Motivo: o Unity dispara OnTriggerStay2D sempre
// que PELO MENOS 1 dos 2 Collider2D envolvidos é trigger, sem distinguir QUAL collider do
// GameObject foi — se o trigger de pickup morasse no mesmo objeto do corpo (sólido, do tamanho
// do personagem), o corpo sozinho já causaria coleta, ignorando totalmente o raio configurado
// (bug real, achado em playtest: "raio extremamente pequeno" não tinha efeito nenhum). Sem
// Rigidbody2D próprio de propósito — usa o do pai (HeroController) pra física, mesmo critério
// já usado nos AttackHitbox_NE/SE/SW/NW do Barbarian.
public class HeroPickupRadius : MonoBehaviour
{
    private CircleCollider2D trigger;

    private void Awake()
    {
        trigger = gameObject.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
    }

    public void SetRadius(float radius) => trigger.radius = radius;

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!GameplayGate.IsActive) return;

        var lootDrop = other.GetComponent<LootDrop>();
        if (lootDrop != null) lootDrop.Collect();
    }
}
