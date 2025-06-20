namespace InstructSharp.Helpers;
internal static class LLMRequestImageHelper
{
    public static string StripBase64Prefix(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        const string marker = "base64,";
        int idx = input.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return input;

        return input.Substring(idx + marker.Length);
    }
}
