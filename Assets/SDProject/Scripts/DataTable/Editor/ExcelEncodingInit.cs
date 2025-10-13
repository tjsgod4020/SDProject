#if UNITY_EDITOR
using System.Text;
using UnityEditor;

namespace SDProject.DataTable.Editor
{
    [InitializeOnLoad]
    public static class ExcelEncodingInit
    {
        static ExcelEncodingInit()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
#endif