using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Deck List", fileName = "DeckList")]
    public class DeckList : ScriptableObject
    {
        // �ʱ� �� ���� ����Ʈ (Inspector���� ī����� �巡���Ͽ� ä�켼��)
        public List<CardData> cards = new();
    }
}