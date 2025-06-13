**InstructSharp**
*Effortless, provider-agnostic LLM integration with structured JSON outputs*

---

## Installation

```bash
dotnet add package InstructSharp
```

Or via Package Manager:

```powershell
Install-Package InstructSharp
```

---

## Supported Providers

* **OpenAI ChatGPT** (`ChatGPTClient`)
* **Anthropic Claude** (`ClaudeClient`)
* **Google Gemini** (`GeminiClient`)
* **X.AI Grok** (`GrokClient`)
* **DeepSeek** (`DeepSeekClient`)
* **Meta LLaMA (via DeepInfra)** (`LLamaClient`)

Each client implements the same `QueryAsync<T>` pattern and returns a `LLMResponse<T>`.

---

## Quick Start

1. **Define your output schema**

   ```csharp
   class QuestionAnswer
   {
       public string Question { get; set; }
       public string Answer   { get; set; }
   }
   ```

2. **Instantiate a client**

   ```csharp
   // ChatGPT example
   var chat = new ChatGPTClient("YOUR_OPENAI_API_KEY");
   ```

3. **Build a request**

   ```csharp
   var req = new ChatGPTRequest
   {
       Model       = ChatGPTModels.GPT41,
       Instruction = "Talk like a pirate.",
       Input       = "Are semicolons optional in JavaScript?"
   };
   ```

4. **Send and receive structured output**

   ```csharp
   var res = await chat.QueryAsync<QuestionAnswer>(req);

   Console.WriteLine($"Q: {res.Result.Question}");
   Console.WriteLine($"A: {res.Result.Answer}");
   ```

5. **Plain‐text fallback**

   ```csharp
   // For unstructured responses:
   var textRes = await chat.QueryAsync<string>(req);
   Console.WriteLine(textRes.Result);
   ```

---

## Full Demo

```csharp
using InstructSharp.Clients.ChatGPT;
using InstructSharp.Core;
using System.Text.Json;

// ...

var client = new ChatGPTClient("sk-...");
var request = new ChatGPTRequest {
    Model       = ChatGPTModels.GPT41,
    Instruction = "Talk like a pirate.",
    Input       = "What is 2 + 2?"
};

var response = await client.QueryAsync<QuestionAnswer>(request);

Console.WriteLine(JsonSerializer.Serialize(response.Result));
// → { "Question":"What is 2 + 2?","Answer":"Arr, 'tis four, matey!" }
```

Repeat the same steps for any other provider:

```csharp
var claude = new ClaudeClient("YOUR_CLAUDE_KEY");
var gemini = new GeminiClient("YOUR_GOOGLE_KEY", GeminiModels.Flash25);
// …and so on…
```

---

## Advanced

* **Custom types**: any POCO can be used as `T`.
* **Temperature**, **max\_tokens**, etc. available via each provider’s request class.
* **Error handling**: exceptions thrown on non-2xx responses. Inspect `response.Usage` and `response.AdditionalData` for extra info.

---

## Contributing

1. Fork the repo
2. Implement your feature / fix
3. Send a PR
