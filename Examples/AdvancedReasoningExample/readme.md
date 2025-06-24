# Advanced Reasoning Example

This example demonstrates the advanced reasoning capabilities of InstructSharp using specialized reasoning models from different providers.

## Features

- **Mathematical Problem Solving**: Tests complex percentage and calculation problems
- **Reasoning Models**: Uses specialized models designed for logical reasoning:
  - OpenAI's o1-preview (chain-of-thought reasoning)
  - Claude 3.7 Sonnet (latest reasoning capabilities)
  - DeepSeek R1 (specialized reasoning model)
- **Structured Output**: Returns detailed reasoning analysis including:
  - Step-by-step calculations
  - Confidence scores
  - Problem classification
  - Alternative approaches

## Models Used

- **ChatGPT**: `o1-preview` - OpenAI's latest reasoning model
- **Claude**: `claude-3-7-sonnet-latest` - Anthropic's latest reasoning model
- **DeepSeek**: `DeepSeek-R1-0528` - Specialized reasoning model

## Usage

1. Replace `"YOUR-API-KEY-HERE"` with your actual API keys
2. Run the example to see how different models approach the same mathematical reasoning problem
3. Compare the reasoning quality and approaches across different providers

## Expected Output

The example will show:
- How each model breaks down the problem
- The final calculated answer
- Confidence levels in the reasoning
- Different approaches to solving the same problem

This example is particularly useful for evaluating which reasoning models work best for your specific use case. 