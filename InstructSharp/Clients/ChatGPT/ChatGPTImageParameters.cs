namespace InstructSharp.Clients.ChatGPT;

public static class ChatGPTImageParameters
{
    public static class Sizes
    {
        public const string Square256 = "256x256";
        public const string Square512 = "512x512";
        public const string Square1024 = "1024x1024";
        public const string Portrait1024x1536 = "1024x1536";
        public const string Landscape1536x1024 = "1536x1024";
        public const string Square2048 = "2048x2048";
        public const string Landscape2048x1152 = "2048x1152";
        public const string Landscape3840x2160 = "3840x2160";
        public const string Portrait2160x3840 = "2160x3840";
        public const string Portrait1024x1792 = "1024x1792";
        public const string Landscape1792x1024 = "1792x1024";
        public const string Auto = "auto";
    }

    public static class Quality
    {
        public const string Auto = "auto";
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
        public const string Standard = "standard";
        public const string Hd = "hd";
    }

    public static class Styles
    {
        public const string Natural = "natural";
        public const string Vivid = "vivid";
    }

    public static class Backgrounds
    {
        public const string Transparent = "transparent";
        public const string Opaque = "opaque";
        public const string Auto = "auto";
    }

    public static class OutputFormats
    {
        public const string Png = "png";
        public const string Jpeg = "jpeg";
        public const string Webp = "webp";
    }

    public static class ResponseFormats
    {
        public const string Url = "url";
        public const string Base64Json = "b64_json";
    }

    public static class Moderation
    {
        public const string Auto = "auto";
        public const string Low = "low";
    }

    public static class InputFidelity
    {
        public const string High = "high";
        public const string Low = "low";
    }
}
