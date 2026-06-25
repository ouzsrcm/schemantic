using Schemantic.Interpreters;

namespace Schemantic.Tests;

/// <summary>
/// Unit tests for <see cref="InterpreterFactory"/>.
/// </summary>
public class InterpreterFactoryTests
{
    [Theory]
    [InlineData("ollama")]
    [InlineData("OLLAMA")]
    [InlineData("openai")]
    public void Create_returns_interpreter_for_known_providers(string provider)
    {
        using var http = new HttpClient();
        var options = new LlmOptions { Provider = provider };

        var interpreter = InterpreterFactory.Create(options, http);

        Assert.NotNull(interpreter);
        Assert.StartsWith("Llm(", interpreter.Name);
    }

    [Fact]
    public void Create_throws_for_unknown_provider()
    {
        using var http = new HttpClient();
        var options = new LlmOptions { Provider = "does-not-exist" };

        Assert.Throws<ArgumentException>(() => InterpreterFactory.Create(options, http));
    }

    [Fact]
    public void Create_throws_when_options_or_http_null()
    {
        using var http = new HttpClient();

        Assert.Throws<ArgumentNullException>(() => InterpreterFactory.Create(null!, http));
        Assert.Throws<ArgumentNullException>(() => InterpreterFactory.Create(new LlmOptions(), null!));
    }
}
