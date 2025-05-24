namespace TaiwanUtilities.Internal;

// 這是用來處理 JavaScriptSerializer 在序列化時的補丁
// JavaScriptSerializer 在當時的設計(尤其是序列化時)不夠周全

#if NET472_OR_GREATER

using System.ComponentModel;
using System.Reflection;
using System.Text; 
using HarmonyLib;
using System.Web.Script.Serialization; 
using System.Runtime.CompilerServices; 

internal static class JavaScriptSerializerPatcher
{
    //
    [ModuleInitializer]
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static void Patch()
    {
       // 確保只執行一次
       var serializeCustomObject = typeof(JavaScriptSerializer)
           .GetMethod("SerializeCustomObject", BindingFlags.Instance | BindingFlags.NonPublic);

        var harmony = new Harmony("utilities.taiwan");

        harmony.Patch(
            original: serializeCustomObject,
            prefix: typeof(JavaScriptSerializerPatcher).GetMethod(nameof(TryParse), BindingFlags.Static | BindingFlags.NonPublic));

    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    private static bool TryParse(object o, StringBuilder sb)
    {

        if (o is ChineseNumeric cn)
        {
            sb.Append('"');
            sb.Append(cn.ToString());
            sb.Append('"');
            return false;
        }

        if (o is RocDateTime rd)
        {
            sb.Append('"');
            sb.Append(rd.ToString());
            sb.Append('"');
            return false;
        }

        if(o is PostalAddress pa) 
        {
            sb.Append('"');
            sb.Append(pa.ToString());
            sb.Append('"');
            return false;
        }

        return true;
    }
}

#endif