using TinyUrl.Application.Interfaces;

namespace TinyUrl.Infrastructure.Services;

public class Base62ShortCodeGenerator : IShortCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private static readonly Random _random = new();

    public string Generate(int length = 7)
    {
        return new string(Enumerable.Range(0, length)
            .Select(_ => Alphabet[_random.Next(Alphabet.Length)])
            .ToArray());
    }
}
