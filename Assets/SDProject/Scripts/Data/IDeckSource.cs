using System.Collections.Generic;
using SDProject.Data;

namespace SDProject.DataBridge
{
    /// <summary>
    /// Read-only source for initial deck (e.g., from DeckList ScriptableObject).
    /// </summary>
    public interface IDeckSource
    {
        IReadOnlyList<CardData> GetInitialDeck();
    }
}
