using System.Collections.Generic;
using UnityEngine;
using SDProject.Data;
using SDProject.Core.Messaging;
using System;

namespace SDProject.Combat
{
    /// <summary>
    /// ���� ����/���� å��. ���(=�������� ���� �ĺ�)�� OnUsed�� ����.
    /// </summary>
    public class HandRuntime : MonoBehaviour
    {
        [SerializeField] private List<CardData> _cards = new();
        public IReadOnlyList<CardData> Cards => _cards;
        public int Count => _cards.Count;

        /// <summary>ī�� �� ���� '����'�� �˸� (���� ���� �̵��� ���� ��Ʈ�ѷ� å��).</summary>
        public event Action<CardData> OnUsed;

        public int AddCards(List<CardData> add, int maxHand)
        {
            if (add == null || add.Count == 0) return 0;
            int canAdd = Mathf.Max(0, maxHand - _cards.Count);
            int take = Mathf.Min(canAdd, add.Count);
            if (take <= 0) return 0;

            for (int i = 0; i < take; i++)
                _cards.Add(add[i]);

            GameEvents.RaiseHandChanged(_cards.Count);
            return take;
        }

        public bool Remove(CardData c)
        {
            bool ok = _cards.Remove(c);
            if (ok) GameEvents.RaiseHandChanged(_cards.Count);
            return ok;
        }

        /// <summary>��� ��ư �ڵ鷯�� ȣ��. ���ο��� ���� �� OnUsed�� �˸�.</summary>
        public void Use(CardData c)
        {
            if (Remove(c))
                OnUsed?.Invoke(c);
        }

        public void Clear()
        {
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
        }

        public List<CardData> TakeAll()
        {
            var all = new List<CardData>(_cards);
            _cards.Clear();
            GameEvents.RaiseHandChanged(_cards.Count);
            return all;
        }
    }
}