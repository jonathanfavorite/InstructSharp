using InstructSharp.Clients.ChatGPT;

namespace InstructSharp.Utils;

public static class ChatGPTImageGenerationRequestExtensions
{
    public static ChatGPTImageGenerationRequest AddImageFile(this ChatGPTImageGenerationRequest request, string filePath, int detailRequired = 1)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Images.Add(ImageRequest.FromFile(filePath, detailRequired));
        return request;
    }

    public static ChatGPTImageGenerationRequest AddImageFiles(this ChatGPTImageGenerationRequest request, IEnumerable<string> filePaths, int detailRequired = 1)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Images.AddRange(ImageRequest.FromFiles(filePaths, detailRequired));
        return request;
    }

    public static async Task<ChatGPTImageGenerationRequest> AddImageFileAsync(this ChatGPTImageGenerationRequest request, string filePath, int detailRequired = 1, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Images.Add(await ImageRequest.FromFileAsync(filePath, detailRequired, cancellationToken));
        return request;
    }

    public static async Task<ChatGPTImageGenerationRequest> AddImageFilesAsync(this ChatGPTImageGenerationRequest request, IEnumerable<string> filePaths, int detailRequired = 1, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        request.Images.AddRange(await ImageRequest.FromFilesAsync(filePaths, detailRequired, cancellationToken));
        return request;
    }
}
