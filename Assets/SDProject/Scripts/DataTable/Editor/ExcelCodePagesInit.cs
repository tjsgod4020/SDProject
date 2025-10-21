#if UNITY_EDITOR
using System.Text;

namespace SD.DataTable.Editor
{
    [UnityEditor.InitializeOnLoad]
    internal static class ExcelCodePagesInit
    {
        static ExcelCodePagesInit()
        {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}
#endif
