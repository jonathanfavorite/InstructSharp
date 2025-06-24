using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.Gemini;
public static class GeminiModels
{
    // Gemini 1.0 models (discontinued)
    public static readonly string Gemini1Nano = "gemini-1.0-nano";
    public static readonly string Gemini1Pro = "gemini-1.0-pro";
    public static readonly string Gemini1Ultra = "gemini-1.0-ultra";

    // Gemini 1.5 models (discontinued)
    public static readonly string Gemini15Pro = "gemini-1.5-pro";
    public static readonly string Gemini15Flash = "gemini-1.5-flash";

    // Gemini 2.0 models
    public static readonly string Gemini20Flash = "gemini-2.0-flash";
    public static readonly string Gemini20FlashThinking = "gemini-2.0-flash-thinking";
    public static readonly string Gemini20FlashLitePreview = "gemini-2.0-flash-lite-preview";
    public static readonly string Gemini20Pro = "gemini-2.0-pro";

    // Gemini 2.5 models
    public static readonly string Gemini25Pro = "gemini-2.5-pro";
    public static readonly string Gemini25Flash = "gemini-2.5-flash";
    public static readonly string GeminiLive25Flash = "gemini-live-2.5-flash";
    public static readonly string Gemini25FlashPreview0520 = "gemini-2.5-flash-preview-05-20";
    public static readonly string Gemini25FlashPreview0417 = "gemini-2.5-flash-preview-04-17";
    public static readonly string Gemini25FlashLitePreview = "gemini-2.5-flash-lite-preview-06-17";
}