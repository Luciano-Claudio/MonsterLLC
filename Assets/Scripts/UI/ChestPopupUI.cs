using UnityEngine;

// Chest_Popup (perks/talentos de recompensa — GDD Deadline 8.1, Seção 8). Por enquanto só a
// plumbing de pausar/abrir/fechar; preencher os 3 perks + 3 talentos sorteados (e respeitar
// "perk só de linha já desbloqueada") fica pra quando Perks/Talentos existirem de verdade
// (decisão do usuário, 2026-11-08).
//
// Vive em algum objeto sempre ativo acima do Chest_Popup na hierarquia (ex.: "Canvas") e acha
// o Chest_Popup por busca recursiva — mesmo padrão de PauseMenuUI (zero campo pra arrastar),
// mas sem depender de ele ser filho DIRETO (na prática fica dentro de "UI_On_Game", um nível
// mais pra dentro — transform.Find comum só acha filho direto). Precisa estar num objeto
// sempre ativo (não no próprio Chest_Popup, que começa desligado) porque Awake() só dispara na
// 1ª vez que o objeto liga, e isso criaria uma dependência circular (precisaria de Instance pra
// religar, mas Instance só existiria depois de religar).
public class ChestPopupUI : MonoBehaviour
{
    public static ChestPopupUI Instance { get; private set; }

    private GameObject popupPanel;
    private GameObject pendingCollectedObject; // destruído em Close() — ver ChestRewardSystem.GrantReward

    private void Awake()
    {
        Instance = this;

        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Chest_Popup") continue;
            popupPanel = t.gameObject;
            break;
        }

        if (popupPanel == null)
        {
            Debug.LogError("[ChestPopupUI] Não achei nenhum objeto \"Chest_Popup\" dentro da hierarquia deste componente.");
            return;
        }

        popupPanel.SetActive(false);
    }

    public void Show(GameObject collectedObject)
    {
        if (popupPanel == null) return;
        pendingCollectedObject = collectedObject;
        TimeManager.Instance.Pause();
        popupPanel.SetActive(true);
    }

    // PLACEHOLDER (decisão explícita do usuário, 2026-11-08, remover quando Perks/Talentos
    // existirem): fecha com 1 clique em QUALQUER canto do painel — chamado por
    // ChestPopupClickToClose (componente separado, vive no próprio Chest_Popup). O
    // comportamento real vai ser só fechar quando o player escolher 1 das 6 opções (e aí sim
    // destruir o baú — "só existe até o player coletá-lo").
    public void Close()
    {
        if (popupPanel == null) return;
        popupPanel.SetActive(false);
        TimeManager.Instance.Resume();

        if (pendingCollectedObject != null) Object.Destroy(pendingCollectedObject);
        pendingCollectedObject = null;
    }
}
