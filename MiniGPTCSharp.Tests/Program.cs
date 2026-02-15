using MiniGPTCSharp.Tests.Golden;

try
{
    GoldenGenerationTests.RunAll();
    Console.WriteLine("All tests passed.");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
