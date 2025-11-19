namespace InstructSharp.Clients.ChatGPT;

public static class ChatGPTImageParameters
{
    public static class Sizes
    {
        public const string Square1024 = "1024x1024";
        public const string Portrait1024x1536 = "1024x1536";
        public const string Landscape1536x1024 = "1536x1024";
        public const string Auto = "auto";
    }

    public static class Quality
    {
        public const string Low = "low";
        public const string Medium = "medium";
        public const string High = "high";
    }

    public static class Styles
    {
        public const string Natural = "natural";
        public const string Vivid = "vivid";
    }

    public static class Backgrounds
    {
        public const string Transparent = "transparent";
        public const string White = "white";
    }

    public static class OutputFormats
    {
        public const string Png = "png";
        public const string Jpeg = "jpeg";
        public const string Webp = "webp";
    }
}
