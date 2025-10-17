using System.Collections.Generic;
using SDProject.Data;

namespace SDProject.DataBridge
{
    /// <summary>DeckRuntime�� �ʱ� ���� �������� ���� ȣ���ϴ� ����� �������̽�</summary>
    public interface IDeckSource
    {
        IReadOnlyList<CardData> GetInitialDeck();
        // (����) ����׿� ������Ƽ�� ��� �˴ϴ�.
        // string DebugDeckName { get; }
        // int    DebugDeckCount { get; }
    }
}