// Quick test script
using Acode.Domain.Security.PathProtection;

var matcher = new GlobMatcher(caseSensitive: true);

Console.WriteLine("Test 1: Exact match");
Console.WriteLine($"Result: {matcher.Matches("~/.ssh/id_rsa", "~/.ssh/id_rsa")}"); // Should be true

Console.WriteLine("\nTest 2: Exact no match");
Console.WriteLine($"Result: {matcher.Matches("~/.ssh/id_rsa", "~/.ssh/id_ed25519")}"); // Should be false

Console.WriteLine("\nDone");
