
using System;
using UnityEngine;

[Serializable]
public struct CardData
{
    public string Id;         // Unique Id
    public string Name;       // Display Name
    public int Cost;          // Mana/Energy cost
    public string Rarity;     // Common/Rare/Epic...
    public string Tags;       // CSV tags (for quick demo)
}