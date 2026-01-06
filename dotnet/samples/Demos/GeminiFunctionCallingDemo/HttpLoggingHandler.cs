// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace GeminiFunctionCallingDemo;

/// <summary>
/// HTTP message handler that logs requests and responses for debugging.
/// </summary>
public class HttpLoggingHandler : DelegatingHandler
{
  /// <summary>
  /// Initializes a new instance of the <see cref="HttpLoggingHandler"/> class.
  /// </summary>
  public HttpLoggingHandler() : base(new HttpClientHandler())
  {
  }

  /// <inheritdoc/>
  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    // Log request
    Console.WriteLine("\n========== HTTP REQUEST ==========");
    Console.WriteLine($"Method: {request.Method}");
    Console.WriteLine($"URI: {request.RequestUri}");
    Console.WriteLine("Headers:");
    foreach (var header in request.Headers)
    {
      if (header.Key.Equals("x-goog-api-key", StringComparison.OrdinalIgnoreCase) ||
          header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
      {
        Console.WriteLine($"  {header.Key}: [REDACTED]");
      }
      else
      {
        Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
      }
    }

    if (request.Content != null)
    {
      var requestBody = await request.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      Console.WriteLine("\nRequest Body:");
      try
      {
        var jsonDoc = JsonDocument.Parse(requestBody);
        Console.WriteLine(JsonSerializer.Serialize(jsonDoc, new JsonSerializerOptions { WriteIndented = true }));
      }
      catch
      {
        Console.WriteLine(requestBody);
      }
    }

    Console.WriteLine("==================================\n");

    // Send request
    var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

    // Log response
    Console.WriteLine("\n========== HTTP RESPONSE ==========");
    Console.WriteLine($"Status: {(int)response.StatusCode} {response.StatusCode}");
    Console.WriteLine("Headers:");
    foreach (var header in response.Headers)
    {
      Console.WriteLine($"  {header.Key}: {string.Join(", ", header.Value)}");
    }

    if (response.Content != null)
    {
      var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
      Console.WriteLine("\nResponse Body (first 2000 chars):");
      var preview = responseBody.Length > 2000 ? responseBody.Substring(0, 2000) + "..." : responseBody;
      Console.WriteLine(preview);
    }

    Console.WriteLine("===================================\n");

    return response;
  }
}
