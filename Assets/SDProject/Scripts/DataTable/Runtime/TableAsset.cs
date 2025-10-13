using UnityEngine;

namespace SDProject.DataTable
{
    public abstract class TableAsset : ScriptableObject
    {
        [TextArea][SerializeField] private string debugNote;
        public virtual string DebugNote => debugNote;

        /// Apply raw text (CSV/JSON/etc) into internal structures.
        public abstract void Apply(string rawText);
    }
}
