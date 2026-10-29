
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScrollbarInitializer : MonoBehaviour
{
    [SerializeField] private Scrollbar scrollbar;

    private void OnEnable()
    {
        StartCoroutine(InitializeScrollbar());
    }

    private IEnumerator InitializeScrollbar()
    {
        // Aguarda a Unity atualizar o layout da UI
        yield return new WaitForEndOfFrame();

        if (scrollbar == null) yield break;

        // Força o evento On Value Changed
        scrollbar.value = 1f;
        scrollbar.size = 0f;
    }
}
