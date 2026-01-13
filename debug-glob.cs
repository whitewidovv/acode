using System;
using Acode.Domain.Security.PathProtection;

var matcher = new GlobMatcher(caseSensitive: true);

Console.WriteLine("Test 1: Starting exact match test...");
try
{
    var result = matcher.Matches("test", "test");
    Console.WriteLine($"Result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Exception: {ex.Message}");
}

Console.WriteLine("Test completed");
