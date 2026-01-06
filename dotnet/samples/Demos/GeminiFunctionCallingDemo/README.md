# Gemini Function Calling Demo

This console application demonstrates function calling with Google's Gemini API using Semantic Kernel.

## Purpose

This demo reproduces and tests the reported issue where:

- Plugins with `[KernelFunction]` attributes are registered
- `ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions` is set
- But the Gemini API request does not include the `tools` field, and functions are not called

## Setup

1. Set your Google AI API key as an environment variable:

   ```bash
   export GOOGLE_API_KEY='your-api-key-here'
   ```

2. Run the application:
   ```bash
   dotnet run
   ```

## What to Expect

If working correctly:

- The application should show function calls being made (EchoPlugin.Echo and WeatherPlugin.GetWeather)
- The chat history should include tool call and tool result messages
- The Gemini model should use the tools to answer the questions

If the bug exists:

- No function calls will be detected
- The model will try to answer without using the tools
- The chat history will only contain user and assistant messages

## Plugins Included

1. **EchoPlugin**: Simple plugin that echoes input text back
2. **WeatherPlugin**: Returns mock weather information for a location

Both plugins are registered with the kernel and should be available to the Gemini model.
