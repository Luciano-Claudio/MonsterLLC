using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Liga GameState.Shop à visibilidade real do painel "Shop" (pai de Perk_Painel/Resume_Painel/
// Talent_Painel/Popup_Confirme) — achado real (usuário testando morte com o Cleric): o estado
// virava GameState.Shop certinho (ShopHandler até logava "Loja aberta"), mas o painel
// continuava m_IsActive=0 pra sempre, porque nada em código nunca ligava ele. Ativa só em
// GameState.Shop, desativa em qualquer outro (ex.: ao voltar pra Gameplay).
//
// GDD Deadline 8.1, Seção 5 (extensão pedida pelo usuário) — 3 coisas novas, todas disparadas
// daqui porque é o único ponto que já sabe exatamente quando a Loja abre:
// 1) Os 2 botões "Start_Day" (Perk_Painel/Resume_Painel) mostram o número real do próximo dia
//    (dia+1 se sobreviveu, dia atual se morreu).
// 2) Clicar "Start_Day" NUNCA avança direto — sempre abre Popup_Confirme (Yes/No) primeiro.
//    É só uma "cortina de fumaça" pro jogador passar mais tempo na Loja enquanto o mapa termina
//    de carregar em background (pedido explícito do usuário).
// 3) Se sobreviveu, dispara a pré-pintura do mapa do próximo dia em BACKGROUND, paceada (nunca
//    trava FPS), assim que a Loja abre — ver ChunkTerrainGenerator.BeginBackgroundRegeneration.
//    Se morreu, o mapa não muda, nada a pré-gerar.
public class ShopUIController : MonoBehaviour
{
    [SerializeField] private GameObject shopRoot; // GameObject "Shop" na cena

    private GameObject confirmPopup; // "Popup_Confirme", filho de shopRoot

    // ShopHandler só existe em runtime, num GameObject que vem de outra cena (Main_Menu) via
    // DontDestroyOnLoad — não dá pra arrastar essa referência num arquivo de cena onde o objeto
    // nem existe ainda. Por isso liga por código aqui; o lookup de ShopHandler.Instance só
    // acontece no clique (dentro dos métodos abaixo), nunca no Awake, então a ordem de
    // carregamento entre os 2 nunca importa.
    private void Awake()
    {
        if (shopRoot == null) return;

        confirmPopup = shopRoot.transform.Find("Popup_Confirme")?.gameObject;

        foreach (var button in shopRoot.GetComponentsInChildren<Button>(true))
        {
            if (button.name == "Start_Day")
            {
                button.onClick.AddListener(OpenConfirmPopup);
            }
            else if (confirmPopup != null && button.transform.IsChildOf(confirmPopup.transform))
            {
                if (button.name == "Yes") button.onClick.AddListener(ConfirmYes);
                else if (button.name == "No") button.onClick.AddListener(ConfirmNo);
            }
        }
    }

    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (shopRoot != null) shopRoot.SetActive(state == GameState.Shop);
        if (state != GameState.Shop) return;

        confirmPopup?.SetActive(false); // reseta qualquer estado preso de uma visita anterior
        UpdateStartDayTexts();

        if (DayResolver.PlayerSurvivedLastDay && ChunkTerrainGenerator.Instance != null)
            ChunkTerrainGenerator.Instance.BeginBackgroundRegeneration(MainMenuUI.CurrentRun.day + 1);
    }

    // Os 2 botões "Start Day X" e a pergunta do Popup_Confirme mostram o MESMO número X.
    private void UpdateStartDayTexts()
    {
        int nextDay = DayResolver.PlayerSurvivedLastDay ? MainMenuUI.CurrentRun.day + 1 : MainMenuUI.CurrentRun.day;

        foreach (var button in shopRoot.GetComponentsInChildren<Button>(true))
        {
            if (button.name != "Start_Day") continue;
            var label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null) label.text = $"Start Day {nextDay}";
        }

        var questionLabel = confirmPopup?.transform.Find("Text (TMP)")?.GetComponent<TMP_Text>();
        if (questionLabel != null) questionLabel.text = $"Start Day {nextDay}?";
    }

    private void OpenConfirmPopup() => confirmPopup?.SetActive(true);

    private void ConfirmYes()
    {
        confirmPopup?.SetActive(false);
        ShopHandler.Instance?.ProceedFromShop();
    }

    // Só fecha o popup e volta pra Loja — a pré-pintura em background (se estiver rodando)
    // continua no mesmo ritmo, sem acelerar nada (pedido explícito do usuário).
    private void ConfirmNo() => confirmPopup?.SetActive(false);
}
