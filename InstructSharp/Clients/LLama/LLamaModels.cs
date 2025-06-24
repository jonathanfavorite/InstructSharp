using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstructSharp.Clients.LLama;
public static class LlamaModels
{
    // Llama 1 (Original) series
    public static readonly string Llama1_7B = "meta-llama/Llama-1-7B";
    public static readonly string Llama1_13B = "meta-llama/Llama-1-13B";
    public static readonly string Llama1_30B = "meta-llama/Llama-1-30B";
    public static readonly string Llama1_65B = "meta-llama/Llama-1-65B";

    // Llama 2 series – foundation models
    public static readonly string Llama2_7B = "meta-llama/Llama-2-7B";
    public static readonly string Llama2_13B = "meta-llama/Llama-2-13B";
    public static readonly string Llama2_70B = "meta-llama/Llama-2-70B";

    // Llama 2 series – chat models
    public static readonly string Llama2_7B_Chat = "meta-llama/Llama-2-7B-chat";
    public static readonly string Llama2_13B_Chat = "meta-llama/Llama-2-13B-chat";
    public static readonly string Llama2_70B_Chat = "meta-llama/Llama-2-70B-chat";

    // Llama 2 series – Hugging Face wrappers
    public static readonly string Llama2_7B_HF = "meta-llama/Llama-2-7b-hf";
    public static readonly string Llama2_7B_Chat_HF = "meta-llama/Llama-2-7b-chat-hf";
    public static readonly string Llama2_13B_HF = "meta-llama/Llama-2-13b-hf";
    public static readonly string Llama2_13B_Chat_HF = "meta-llama/Llama-2-13b-chat-hf";
    public static readonly string Llama2_70B_HF = "meta-llama/Llama-2-70b-hf";
    public static readonly string Llama2_70B_Chat_HF = "meta-llama/Llama-2-70b-chat-hf";

    // Llama 3 series – instruction-tuned models
    public static readonly string Llama3_1_8B_Instruct = "meta-llama/Llama-3.1-8B-Instruct";
    public static readonly string Llama3_2_1B_Instruct = "meta-llama/Llama-3.2-1B-Instruct";
    public static readonly string Llama3_2_3B_Instruct = "meta-llama/Llama-3.2-3B-Instruct";
    public static readonly string Llama3_3_70B_Instruct = "meta-llama/Llama-3.3-70B-Instruct";

    // Llama 4 series – base models
    public static readonly string Llama4_Scout_17B_16E = "meta-llama/Llama-4-Scout-17B-16E";
    public static readonly string Llama4_Maverick_17B_128E = "meta-llama/Llama-4-Maverick-17B-128E";

    // Llama 4 series – instruction-tuned models
    public static readonly string Llama4_Scout_17B_16E_Instruct = "meta-llama/Llama-4-Scout-17B-16E-Instruct";
    public static readonly string Llama4_Maverick_17B_128E_Instruct_FP8 = "meta-llama/Llama-4-Maverick-17B-128E-Instruct-FP8";
}
