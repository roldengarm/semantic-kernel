// Copyright (c) Microsoft. All rights reserved.

using System.ComponentModel;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace GeminiFunctionCallingDemo;

/// <summary>
/// Demo application to test Gemini function calling with streaming.
/// This reproduces the reported issue where plugins are not called.
/// </summary>
public static class Program
{
    public static async Task Main(string[] args)
    {
        // Get API key from environment variable or user secrets
        var apiKey = Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Please set GOOGLE_API_KEY environment variable");
            Console.WriteLine("Example: export GOOGLE_API_KEY='your-api-key'");
            return;
        }

        Console.WriteLine("=== Gemini Function Calling Demo ===\n");

        //var gemini2Model = "gemini-2.0-flash-exp";
        var gemini3Model1 = "gemini-3-pro-preview";
        var gemini3Model2 = "gemini-3-flash-preview";

        await RunAsync(gemini3Model1, apiKey).ConfigureAwait(false);
        await RunAsync(gemini3Model2, apiKey).ConfigureAwait(false);
    }

    private static async Task RunAsync(string modelId, string apiKey)
    {
        Console.WriteLine($"Model: {modelId}\n");

        // Create logging handler to capture HTTP requests/responses
        var loggingHandler = new HttpLoggingHandler();
        var httpClient = new HttpClient(loggingHandler);

        // Create kernel with Gemini chat completion
        var kernel = Kernel.CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                httpClient: httpClient)
            .Build();

        // Register the Echo plugin
        kernel.Plugins.AddFromType<EchoPlugin>();
        kernel.Plugins.AddFromType<WeatherPlugin>();

        Console.WriteLine($"Registered plugins: {string.Join(", ", kernel.Plugins.Select(p => p.Name))}\n");

        // Create settings with ToolCallBehavior
        var settings = new GeminiPromptExecutionSettings
        {
            MaxTokens = 1000,
            ToolCallBehavior = GeminiToolCallBehavior.AutoInvokeKernelFunctions,
            ThinkingConfig = new GeminiThinkingConfig { IncludeThoughts = true, ThinkingBudget = 1000 }
        };

        // Create chat history
        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage("You are a helpful assistant that uses available tools to answer questions.");
        chatHistory.AddUserMessage("Echo this message: Hello from Semantic Kernel!");
        chatHistory.AddUserMessage("What's the weather like in Seattle?");

        // Get chat completion service
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        Console.WriteLine("Sending request to Gemini...\n");
        Console.WriteLine("Chat History:");
        foreach (var message in chatHistory)
        {
            Console.WriteLine($"  [{message.Role}]: {message.Content}");
        }
        Console.WriteLine();

        // Stream the response
        Console.WriteLine("Response:");
        var responseBuilder = new System.Text.StringBuilder();
        var streamCount = 0;
        await foreach (var result in chatService.GetStreamingChatMessageContentsAsync(chatHistory, settings, kernel).ConfigureAwait(false))
        {
            streamCount++;
            Console.WriteLine($"\n[Stream chunk #{streamCount}]");

            var content = result.Content;
            if (!string.IsNullOrEmpty(content))
            {
                Console.Write($"Content: {content}");
                responseBuilder.Append(content);
            }

            // Check for thoughts
#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates
            var streaming = result.Items.OfType<StreamingReasoningContent>().SingleOrDefault();
            if (streaming != null)
            {
                Console.WriteLine($"\n[Thoughts]: {streaming.Text}");
            }
#pragma warning restore SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates

            // Check for function calls
            if (result is GeminiStreamingChatMessageContent geminiContent)
            {
                if (geminiContent.ToolCalls != null && geminiContent.ToolCalls.Any())
                {
                    Console.WriteLine("\n[Function calls detected in this chunk:]");
                    foreach (var toolCall in geminiContent.ToolCalls)
                    {
                        Console.WriteLine($"  - {toolCall.FullyQualifiedName}");
                        if (toolCall.Arguments != null)
                        {
                            Console.WriteLine($"    Arguments: {System.Text.Json.JsonSerializer.Serialize(toolCall.Arguments)}");
                        }
                    }
                    Console.WriteLine("[NOTE: Auto-invoke should call these functions after this stream ends]");
                }
            }
        }

        Console.WriteLine($"\n[Total stream chunks received: {streamCount}]");

        Console.WriteLine("\n\n=== Conversation Complete ===");
        Console.WriteLine($"\nFinal chat history count: {chatHistory.Count} messages");

        // Display final chat history
        Console.WriteLine("\nFinal Chat History:");
        for (int i = 0; i < chatHistory.Count; i++)
        {
            var message = chatHistory[i];
            Console.WriteLine($"  [{i + 1}] {message.Role}: {(message.Content?.Length > 100 ? message.Content.Substring(0, 100) + "..." : message.Content)}");

            if (message is GeminiChatMessageContent geminiMsg)
            {
                if (geminiMsg.ToolCalls != null && geminiMsg.ToolCalls.Any())
                {
                    Console.WriteLine($"      Tool Calls: {string.Join(", ", geminiMsg.ToolCalls.Select(tc => tc.FullyQualifiedName))}");
                }
                if (geminiMsg.CalledToolResult != null)
                {
                    Console.WriteLine($"      Tool Result: {geminiMsg.CalledToolResult.FullyQualifiedName}");
                }
            }
        }
    }
}

/// <summary>
/// Simple plugin that echoes input - always gets called if working correctly
/// </summary>
public class EchoPlugin
{
    [KernelFunction, Description("Echoes the input string back to the user.")]
    public string Echo([Description("The string to echo.")] string input)
    {
        Console.WriteLine($"\n[EchoPlugin.Echo called with: '{input}']");
        return $"Echo: {input}";
    }
}

/// <summary>
/// Weather plugin for testing
/// </summary>
public class WeatherPlugin
{
    [KernelFunction, Description("Gets the current weather for a location.")]
    public string GetWeather([Description("The city and state, e.g. 'Seattle, WA'")] string location)
    {
        Console.WriteLine($"\n[WeatherPlugin.GetWeather called with: '{location}']");
        return $"The weather in {location} is sunny and 71°F";
    }
}
