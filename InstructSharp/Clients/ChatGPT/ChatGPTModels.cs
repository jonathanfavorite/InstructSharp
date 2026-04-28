using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.ChatGPT;
public static class ChatGPTModels
{
    // GPT-3.5 Turbo variants
    public static readonly string GPT35Turbo = "gpt-3.5-turbo";
    public static readonly string GPT35Turbo0301 = "gpt-3.5-turbo-0301";
    public static readonly string GPT35Turbo0613 = "gpt-3.5-turbo-0613";
    public static readonly string GPT35Turbo16k = "gpt-3.5-turbo-16k";
    public static readonly string GPT35Turbo16k0613 = "gpt-3.5-turbo-16k-0613";

    // GPT-4 variants
    public static readonly string GPT4 = "gpt-4";
    public static readonly string GPT40314 = "gpt-4-0314";
    public static readonly string GPT40613 = "gpt-4-0613";
    public static readonly string GPT432K = "gpt-4-32k";
    public static readonly string GPT432K0314 = "gpt-4-32k-0314";
    public static readonly string GPT432K0613 = "gpt-4-32k-0613";

    // GPT-4o (Omni) series
    public static readonly string GPT4o = "gpt-4o";
    public static readonly string GPT4o20240513 = "gpt-4o-2024-05-13";
    public static readonly string GPT4o20240806 = "gpt-4o-2024-08-06";
    public static readonly string GPT4o20241120 = "gpt-4o-2024-11-20";
    public static readonly string GPT4oMini = "gpt-4o-mini";
    public static readonly string GPT4oLatest = "gpt-4o-latest";
    public static readonly string GPT4oSearchPreview = "gpt-4o-search-preview";
    public static readonly string GPT4oMiniSearchPreview = "gpt-4o-mini-search-preview";
    public static readonly string GPTImage1 = "gpt-image-1";
    public static readonly string GPTImage2 = "gpt-image-2";
    public static readonly string GPTImage15 = "gpt-image-1.5";
    public static readonly string GPTImage1Mini = "gpt-image-1-mini";
    public static readonly string GPT4oAudioPreview = "gpt-4o-audio-preview";
    public static readonly string GPT4oMiniAudioPreview = "gpt-4o-mini-audio-preview";
    public static readonly string GPT4oRealtimePreview = "gpt-4o-realtime-preview";
    public static readonly string GPT4oMiniRealtimePreview = "gpt-4o-mini-realtime-preview";

    // GPT-4.1 series
    public static readonly string GPT41 = "gpt-4.1";
    public static readonly string GPT41Mini = "gpt-4.1-mini";
    public static readonly string GPT41Nano = "gpt-4.1-nano";

    // GPT-4.5 series
    public static readonly string GPT45 = "gpt-4.5";
    public static readonly string GPT45Preview = "gpt-4.5-preview";

    // o1 (chain-of-thought) series
    public static readonly string O1Preview = "o1-preview";
    public static readonly string O1 = "o1";
    public static readonly string O1Mini = "o1-mini";
    public static readonly string O1Pro = "o1-pro";

    // o3 series
    public static readonly string O3 = "o3";
    public static readonly string O3Mini = "o3-mini";
    public static readonly string O3MiniHigh = "o3-mini-high";
    public static readonly string O3Pro = "o3-pro";
    public static readonly string O3DeepResearch = "o3-deep-research";
    
    // o4-mini reasoning series
    public static readonly string O4Mini = "o4-mini";
    public static readonly string O4MiniHigh = "o4-mini-high";
    public static readonly string O4MiniDeepResearch = "o4-mini-deep-research";

    // GPT-5.2 series
    public static readonly string GPT52 = "gpt-5.2";

    // GPT-5.1 series
    public static readonly string GPT51 = "gpt-5.1";
    public static readonly string GPT51Codex = "gpt-5.1-codex";
    public static readonly string GPT51CodexMax = "gpt-5.1-codex-max";
    public static readonly string GPT51CodexMini = "gpt-5.1-codex-mini";
    public static readonly string GPT51ChatLatest = "gpt-5.1-chat-latest";
    public static readonly string CodexMiniLatest = "codex-mini-latest";

    // GPT-5 series
    public static readonly string GPT5Pro = "gpt-5-pro";
    public static readonly string GPT5Codex = "gpt-5-codex";
    public static readonly string GPT5ChatLatest = "gpt-5-chat-latest";
    public static readonly string GPT5SearchApi = "gpt-5-search-api";

    // Audio/realtime
    public static readonly string GPTAudio = "gpt-audio";
    public static readonly string GPTAudioMini = "gpt-audio-mini";
    public static readonly string GPTRealtime = "gpt-realtime";
    public static readonly string GPTRealtimeMini = "gpt-realtime-mini";

    // Specialized models
    public static readonly string ComputerUsePreview = "computer-use-preview";

    public static readonly string GPT5Nano = "gpt-5-nano";
    public static readonly string GPT5Mini = "gpt-5-mini";
    public static readonly string GPT5 = "gpt-5";
}

