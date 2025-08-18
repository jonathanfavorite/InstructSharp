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
    public static readonly string GPT4oMini = "gpt-4o-mini";
    public static readonly string GPT4oLatest = "gpt-4o-latest";

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
    
    // o4-mini reasoning series
    public static readonly string O4Mini = "o4-mini";
    public static readonly string O4MiniHigh = "o4-mini-high";

    public static readonly string GPT5Nano = "gpt-5-nano";
    public static readonly string GPT5Mini = "gpt-5-mini";
    public static readonly string GPT5 = "gpt-5";
}

