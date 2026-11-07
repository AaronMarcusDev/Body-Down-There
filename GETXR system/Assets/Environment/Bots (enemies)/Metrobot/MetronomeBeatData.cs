using UnityEngine;

/// <summary>
/// a beat of 4 is split into 8th notes
/// Slot:  0   1   2   3   4   5   6   7
/// Tel:   1   &   2   &   3   &   4   &
/// slot 0 is always the bell, the others are a soft tick
/// </summary>
[CreateAssetMenu(fileName = "BeatPattern", menuName = "Metronome/Beat Pattern")]
public class MetronomeBeatData : ScriptableObject
{
    public const int SlotsPerBar = 8;

    [SerializeField] private string label = "Straight";

    [Tooltip("checked = a sound at this note")]
    [SerializeField] private bool[] slots = new bool[SlotsPerBar];

    public string Label => label;

    /// True if at this moment a tick is played (a bell does not count)
    public bool IsTick(int slot) => slot > 0 && slot < slots.Length && slots[slot];

    private void OnValidate()
    {
        if (slots == null || slots.Length != SlotsPerBar)
            System.Array.Resize(ref slots, SlotsPerBar);

        slots[0] = true; // bell is always on at 0
    }
}
