using System.Collections.Generic;
using UnityEngine;

namespace SDProject.Data
{
    [CreateAssetMenu(menuName = "SDProject/Deck List", fileName = "DeckList")]
    public class DeckList : ScriptableObject
    {
        // �ʱ� �� ���� ����Ʈ(Inspector���� CardData���� �巡���ؼ� ä�켼��)
        public List<CardData> cards = new();
    }
}