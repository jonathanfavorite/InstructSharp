namespace InstructSharp.Clients.Gemini;

public static class GeminiImageParameters
{
    public static class AspectRatios
    {
        public const string Square1x1 = "1:1";
        public const string Portrait2x3 = "2:3";
        public const string Landscape3x2 = "3:2";
        public const string Landscape4x3 = "4:3";
        public const string Portrait4x5 = "4:5";
        public const string Landscape5x4 = "5:4";
        public const string Portrait3x4 = "3:4";
        public const string Landscape16x9 = "16:9";
        public const string Portrait9x16 = "9:16";
        public const string Landscape21x9 = "21:9";
    }

    public static class ImageSizes
    {
        public const string Size1K = "1K";
        public const string Size2K = "2K";
        public const string Size4K = "4K";
    }

    public static class ResponseModalities
    {
        public const string Text = "Text";
        public const string Image = "Image";
    }
}
