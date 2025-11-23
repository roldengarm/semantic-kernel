// Copyright (c) Microsoft. All rights reserved.

using System.Linq;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.Google;

namespace ChatCompletion;

/// <summary>
/// This example demonstrates how to access the model's reasoning process using IncludeThoughts.
/// <para>
/// IncludeThoughts is only supported in Google AI Gemini 2.5+ models and allows you to see
/// the model's internal reasoning steps that lead to its final response.
/// See: https://developers.googleblog.com/en/start-building-with-gemini-25-flash/#:~:text=thinking%20budgets
/// </para>
/// </summary>
public sealed class Google_GeminiChatCompletionWithThoughts(ITestOutputHelper output)
    : BaseTest(output)
{
  [Fact]
  public async Task GoogleAIChatCompletionWithAccessToThoughts()
  {
    Console.WriteLine(
        "============= Google AI - Gemini 2.5 Chat Completion with Thought Access ============="
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

    var chatHistory = new ChatHistory(
        "You are a math tutor helping students understand problem-solving steps."
    );
    var chat = kernel.GetRequiredService<IChatCompletionService>();

    // Configure settings to include thoughts in the response
    var executionSettings = new GeminiPromptExecutionSettings
    {
      ThinkingConfig = new()
      {
        ThinkingBudget = 1000, // Allow up to 1000 tokens for reasoning
        IncludeThoughts = true // Include the model's reasoning process in response
      }
    };

    // Ask the model to solve a complex problem
    chatHistory.AddUserMessage(
        "Solve this step by step: If a train travels 120 miles in 2 hours, and then increases its speed by 25% for the next 3 hours, how far did it travel in total?"
    );

    Console.WriteLine($"User: {chatHistory.Last().Content}");
    Console.WriteLine("------------------------");

    // Get response with reasoning included
    var reply = await chat.GetChatMessageContentAsync(chatHistory, executionSettings);
    chatHistory.Add(reply);

    // Display the model's reasoning process
    var reasoningContents = reply.Items.OfType<ReasoningContent>().ToList();
    if (reasoningContents.Any())
    {
      Console.WriteLine("🧠 Model's Reasoning Process:");
      foreach (var reasoning in reasoningContents)
      {
        Console.WriteLine($"   💭 {reasoning.InnerContent}");
      }
      Console.WriteLine();
    }

    // Display the final answer
    Console.WriteLine($"Assistant: {reply.Content}");
    Console.WriteLine("------------------------");

    // Follow up question to see more reasoning
    chatHistory.AddUserMessage(
        "Can you explain why you increased the speed by 25% instead of adding 25 mph?"
    );

    Console.WriteLine($"User: {chatHistory.Last().Content}");
    Console.WriteLine("------------------------");

    var followUpReply = await chat.GetChatMessageContentAsync(chatHistory, executionSettings);
    chatHistory.Add(followUpReply);

    // Show reasoning for the follow-up
    var followUpReasoning = followUpReply.Items.OfType<ReasoningContent>().ToList();
    if (followUpReasoning.Any())
    {
      Console.WriteLine("🧠 Follow-up Reasoning:");
      foreach (var reasoning in followUpReasoning)
      {
        Console.WriteLine($"   💭 {reasoning.InnerContent}");
      }
      Console.WriteLine();
    }

    Console.WriteLine($"Assistant: {followUpReply.Content}");
    Console.WriteLine("------------------------");
  }

  [Fact]
  public async Task StreamingChatWithThoughts()
  {
    Console.WriteLine("============= Google AI - Streaming Chat with Thoughts =============");

    Assert.NotNull(TestConfiguration.GoogleAI.ApiKey);
    string geminiModelId = "gemini-2.5-pro-exp-03-25";

    Kernel kernel = Kernel
        .CreateBuilder()
        .AddGoogleAIGeminiChatCompletion(
            modelId: geminiModelId,
            apiKey: TestConfiguration.GoogleAI.ApiKey
        )
        .Build();

    var chatHistory = new ChatHistory("You are a creative writing assistant.");
    var chat = kernel.GetRequiredService<IChatCompletionService>();

    var executionSettings = new GeminiPromptExecutionSettings
    {
      ThinkingConfig = new() { ThinkingBudget = 800, IncludeThoughts = true }
    };

    chatHistory.AddUserMessage(
        "Write a short story about a robot discovering emotions. Show me your creative process."
    );

    Console.WriteLine($"User: {chatHistory.Last().Content}");
    Console.WriteLine("------------------------");
    Console.WriteLine("Streaming Response:");

    var streamingResponse = new List<string>();
    var thoughtParts = new List<string>();

    await foreach (
        var streamChunk in chat.GetStreamingChatMessageContentsAsync(
            chatHistory,
            executionSettings
        )
    )
    {
      // Collect reasoning content
      foreach (var reasoning in streamChunk.Items.OfType<StreamingReasoningContent>())
      {
        thoughtParts.Add(reasoning.InnerContent ?? string.Empty);
        Console.Write($"💭[{reasoning.InnerContent}] ");
      }

      // Collect regular content
      if (!string.IsNullOrEmpty(streamChunk.Content))
      {
        streamingResponse.Add(streamChunk.Content);
        Console.Write(streamChunk.Content);
      }
    }

    Console.WriteLine();
    Console.WriteLine("------------------------");

    if (thoughtParts.Any())
    {
      Console.WriteLine("🧠 Complete Reasoning Process:");
      foreach (var thought in thoughtParts.Where(t => !string.IsNullOrWhiteSpace(t)))
      {
        Console.WriteLine($"   💭 {thought}");
      }
      Console.WriteLine("------------------------");
    }

    Console.WriteLine($"Final Story:\n{string.Join("", streamingResponse)}");
  }
}
