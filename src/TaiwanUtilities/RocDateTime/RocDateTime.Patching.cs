namespace TaiwanUtilities;

// 這是用來處理 JavaScriptSerializer 在序列化時的補丁
// JavaScriptSerializer 在當時的設計(尤其是序列化時)不夠周全

#if NET472_OR_GREATER
 
using System.ComponentModel; 
using System.Reflection;
using System.Text; 


using HarmonyLib;
using System.Web.Script.Serialization;

partial struct RocDateTime
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    private static void PatchJavaScriptSerializer()
    {
        var serializeCustomObject = typeof(JavaScriptSerializer)
            .GetMethod("SerializeCustomObject", BindingFlags.Instance | BindingFlags.NonPublic);

        var harmony = new Harmony("utilities.taiwan");

        harmony.Patch(
            original: serializeCustomObject,
            prefix: typeof(RocDateTime).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic));

    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    private static bool Prefix(object o, StringBuilder sb)
    {

        if (o is RocDateTime rd)
        {
            sb.Append('"');
            sb.Append(rd.ToString());
            sb.Append('"');
            return false;
        }

        return true;
    }
}

#endif