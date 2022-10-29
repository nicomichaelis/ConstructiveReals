using System;
using System.Text;

namespace ConstructiveReals;

internal static class StringBuilderUtils
{
    [ThreadStatic]
    private static StringBuilder? _stringbuilder;

    public static string GetAndRelease(StringBuilder builder)
    {
        string result = builder.ToString();
        if (builder.Capacity <= 4096)
        {
            _stringbuilder = builder;
            _stringbuilder.Clear();
        }
        return result;
    }

    public static StringBuilder AquireBuider()
    {
        var result = _stringbuilder ?? new StringBuilder();
        _stringbuilder = null;
        return result;
    }

}
