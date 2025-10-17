using System;

namespace SDProject.DataTable
{
    /// <summary>
    /// CardData.csv의 한 줄. 최소: Id, NameId, DescId, AP
    /// 헤더는 대소문자 무시.
    /// </summary>
    [Serializable]
    public class CardDataRow
    {
        public string Id;
        public string NameId;
        public string DescId;
        public int AP;
    }
}
