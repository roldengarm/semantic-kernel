// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace ChatCompletion;

/// <summary>
/// These examples demonstrate different ways of using chat completion with Google AI API.
/// <para>
/// Currently thinking budget and IncludeThoughts are only supported in Google AI Gemini 2.5+ models.
/// ThinkingBudget controls how many tokens the model can use for reasoning.
/// IncludeThoughts allows access to the model's reasoning process in the response.
/// See: https://developers.googleblog.com/en/start-building-with-gemini-25-flash/#:~:text=thinking%20budgets
/// </para>
/// </summary>
public sealed class Google_GeminiChatCompletionWithThinkingBudget(ITestOutputHelper output)
    : BaseTest(output)
{
    [Fact]
    public async Task GoogleAIChatCompletionUsingThinkingBudget()
    {
        Console.WriteLine(
            "============= Google AI - Gemini 2.5 Chat Completion with Thinking Budget & Thoughts ============="
        );

        Assert.NotNull(TestConfiguration.GoogleAI.ApiKey);
        string geminiModelId = "gemini-2.5-pro-exp-03-25";

        Kernel kernel = Kernel
            .CreateBuilder()
            .AddGoogleAIGeminiChatCompletion(
                modelId: geminiModelId,
                apiKey: TestConfiguration.GoogleAI.ApiKey
            )
            .Build();

        var chatHistory = new ChatHistory("You are an expert in the tool shop.");
        var chat = kernel.GetRequiredService<IChatCompletionService>();
        var executionSettings = new GeminiPromptExecutionSettings
        {
            // This parameter gives the model how much tokens it can use during the thinking process.
            // IncludeThoughts allows you to access the model's reasoning process in the response.
            ThinkingConfig = new() { ThinkingBudget = 2000, IncludeThoughts = true }
        };

        // First user message
        chatHistory.AddUserMessage("Hi, I'm looking for new power tools, any suggestion?");
        await MessageOutputAsync(chatHistory);

        // First assistant message
        var reply = await chat.GetChatMessageContentAsync(chatHistory, executionSettings);
        chatHistory.Add(reply);
        await MessageOutputAsync(chatHistory);

        // Demonstrate accessing thoughts when IncludeThoughts is enabled
        if (reply.Items.OfType<ReasoningContent>().Any())
        {
            Console.WriteLine("🧠 Model's Reasoning Process:");
            foreach (var reasoning in reply.Items.OfType<ReasoningContent>())
            {
                Console.WriteLine($"Thought: {reasoning.InnerContent}");
            }
            Console.WriteLine("------------------------");
        }
    }

    /// <summary>
    /// Outputs the last message of the chat history
    /// </summary>
    private Task MessageOutputAsync(ChatHistory chatHistory)
    {
        var message = chatHistory.Last();

        Console.WriteLine($"{message.Role}: {message.Content}");
        Console.WriteLine("------------------------");

        return Task.CompletedTask;
    }
}
