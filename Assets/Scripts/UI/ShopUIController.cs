using UnityEngine;

// Liga GameState.Shop à visibilidade real do painel "Shop" (pai de Perk_Painel/Resume_Painel/
// Talent_Painel) — achado real (usuário testando morte com o Cleric): o estado virava
// GameState.Shop certinho (ShopHandler até logava "Loja aberta"), mas o painel continuava
// m_IsActive=0 pra sempre, porque nada em código nunca ligava ele. Ativa só em GameState.Shop,
// desativa em qualquer outro (ex.: ao voltar pra Gameplay).
public class ShopUIController : MonoBehaviour
{
    [SerializeField] private GameObject shopRoot; // GameObject "Shop" na cena

    private void OnEnable() => GameEvents.OnGameStateChanged += HandleStateChanged;
    private void OnDisable() => GameEvents.OnGameStateChanged -= HandleStateChanged;

    private void HandleStateChanged(GameState state)
    {
        if (shopRoot != null) shopRoot.SetActive(state == GameState.Shop);
    }
}
