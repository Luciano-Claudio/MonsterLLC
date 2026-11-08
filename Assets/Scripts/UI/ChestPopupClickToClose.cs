using UnityEngine;
using UnityEngine.EventSystems;

// PLACEHOLDER (decisão explícita do usuário, 2026-11-08 — remover quando Perks/Talentos
// existirem de verdade): vive no próprio Chest_Popup (precisa estar no mesmo GameObject da
// Image clicável pra receber o evento) e fecha o painel com 1 clique em qualquer canto. O
// comportamento real vai ser só fechar quando o player escolher 1 das 6 opções (perk/talento),
// contemplando a escolha — isto aqui é só pra poder testar a pausa/abertura antes disso existir.
public class ChestPopupClickToClose : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData) => ChestPopupUI.Instance.Close();
}
