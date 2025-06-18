namespace InstructSharp.Core;
public class LLMImageRequest
{
    public string Url { get; set; }
    public bool IsBase64 => Url.StartsWith("data:image/") && Url.Contains(";base64,");
    public string Base64FileExtension => IsBase64 ? Url.Split(';')[0].Split('/')[1] : Url.Split('.').LastOrDefault() ?? "png";
    public int DetailRequired { get; set; } = 1; // 0 is auto, 1 is low, 2 is medium, 3 is high
    public LLMImageRequest(string url, int detailRequired = 1)
    {
        Url = url;
        DetailRequired = detailRequired;
    }
}
