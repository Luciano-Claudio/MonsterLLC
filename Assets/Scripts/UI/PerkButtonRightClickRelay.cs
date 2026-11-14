using UnityEngine;
using UnityEngine.EventSystems;

// O Button de verdade (uGUI Selectable) só entrega onClick pro clique ESQUERDO — nunca chama
// nada no botão direito. Em vez de reimplementar o Button inteiro, este componente é anexado
// no MESMO GameObject do Button (PerkButtonUI.Awake(), não precisa anexar manualmente no
// prefab) só pra escutar o clique direito e repassar pro pai — mesmo critério de
// GroundCrackHitRelay (Unity entrega o evento pro componente que está no objeto raycastado,
// nunca sobe pro pai sozinho se o próprio objeto já tem alguém escutando a mesma interface).
public class PerkButtonRightClickRelay : MonoBehaviour, IPointerClickHandler
{
    public PerkButtonUI Owner;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        Owner?.OnRightClick();
    }
}
