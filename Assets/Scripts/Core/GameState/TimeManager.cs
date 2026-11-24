using System.Collections.Generic;
using UnityEngine;

// Pausa do gameplay (lida pelo GameplayGate). Etapas 1+2 (revisão final, achado 4): vários sistemas
// pausam ao mesmo tempo (câmera de encontro de boss 1x por minuto, menu de pausa, tela de cartas).
// Cada um pausa com o próprio dono (PauseBy/ResumeBy) e o jogo só volta quando TODOS liberarem —
// antes era um bool só e o primeiro Resume() despausava por baixo do menu/cartas.
// Pause()/Resume() sem dono continuam valendo (Bag, popup do baú) como um dono "legado".
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    private static readonly object LegacyOwner = new object();
    private readonly HashSet<object> owners = new();

    public bool IsPaused => owners.Count > 0;

    private void Awake() => Instance = this;

    public void Pause() => PauseBy(LegacyOwner);
    public void Resume() => ResumeBy(LegacyOwner);

    public void PauseBy(object owner)
    {
        if (owner != null) owners.Add(owner);
    }

    public void ResumeBy(object owner)
    {
        if (owner != null) owners.Remove(owner);
    }

    // Saída da run (menu): nenhuma pausa sobrevive.
    public void ResumeAll() => owners.Clear();
}
