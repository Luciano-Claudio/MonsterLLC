using UnityEngine;
using UnityEngine.UI;

// Troca de aba dentro do painel "Settings" (Header: Sound/Display/Controls) — mesma estrutura
// usada em Main_Menu e _TestScene (Settings/Header/<aba> = botão da aba, Settings/<aba> =
// painel de conteúdo irmão, mesmo nome). Por isso o componente vive direto no GameObject
// "Settings" e acha tudo por Find relativo, sem nenhum campo pra arrastar no Inspector —
// só precisa ser adicionado ao GameObject "Settings" nas duas cenas.
// Language_Dropdown fica de fora de propósito (GDD: comportamento ainda não definido).
public class SettingsTabsUI : MonoBehaviour
{
    private GameObject soundContent;
    private GameObject displayContent;
    private GameObject controlsContent;

    private void Awake()
    {
        soundContent = transform.Find("Sound").gameObject;
        displayContent = transform.Find("Display").gameObject;
        controlsContent = transform.Find("Controls").gameObject;

        WireTab("Header/Sound", ShowSound);
        WireTab("Header/Display", ShowDisplay);
        WireTab("Header/Controls", ShowControls);
    }

    // Toda vez que o painel Settings é reaberto (SetActive(true) em algum lugar), volta pra aba
    // Sound — mesma regra do InventoryPanelController pra Inventory/Stats: nunca lembra a última
    // aba usada.
    private void OnEnable() => ShowSound();

    private void WireTab(string path, UnityEngine.Events.UnityAction action) =>
        transform.Find(path).GetComponent<Button>().onClick.AddListener(action);

    private void ShowSound()
    {
        soundContent.SetActive(true);
        displayContent.SetActive(false);
        controlsContent.SetActive(false);
    }

    private void ShowDisplay()
    {
        soundContent.SetActive(false);
        displayContent.SetActive(true);
        controlsContent.SetActive(false);
    }

    private void ShowControls()
    {
        soundContent.SetActive(false);
        displayContent.SetActive(false);
        controlsContent.SetActive(true);
    }
}
