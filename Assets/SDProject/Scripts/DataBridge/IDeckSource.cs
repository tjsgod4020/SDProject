using System.Collections.Generic;
using SDProject.Data;

namespace SDProject.DataBridge
{
    /// <summary>DeckRuntime이 초기 덱을 가져오기 위해 호출하는 어댑터 인터페이스</summary>
    public interface IDeckSource
    {
        IReadOnlyList<CardData> GetInitialDeck();
        // (선택) 디버그용 프로퍼티는 없어도 됩니다.
        // string DebugDeckName { get; }
        // int    DebugDeckCount { get; }
    }
}