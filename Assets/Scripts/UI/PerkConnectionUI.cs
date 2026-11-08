using UnityEngine;
using UnityEngine.UI;

// 1 conector visual entre linhas de uma árvore de Perk (GDD Deadline 8.1, Seção 6.2/8-38, R9) —
// cada Connection_N tem sua PRÓPRIA sprite de bloqueado/liberado (pedido explícito do usuário:
// nenhuma sprite compartilhada aqui, diferente do botão de perk). Por isso este componente
// precisa já EXISTIR na cena (adicionado manualmente pelo usuário em cada um dos
// "{Tree}_Connection_N"), com os 2 campos preenchidos no Inspector — PerkTreeController só
// chama Refresh(), nunca AddComponent nisto.
public class PerkConnectionUI : MonoBehaviour
{
    [SerializeField] private Sprite lockedSprite;
    [SerializeField] private Sprite unlockedSprite;

    private Image image;

    private void Awake() => image = GetComponent<Image>();

    public void Refresh(bool unlocked)
    {
        if (image == null) return;
        var sprite = unlocked ? unlockedSprite : lockedSprite;
        if (sprite != null) image.sprite = sprite;
    }
}
