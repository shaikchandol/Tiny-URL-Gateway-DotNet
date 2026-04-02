namespace TinyUrl.Application.Interfaces;

public interface IShortCodeGenerator
{
    string Generate(int length = 7);
}
