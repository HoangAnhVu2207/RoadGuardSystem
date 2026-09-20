using System.Security.Cryptography;

namespace RoadGuardSystem.Services.Identity;

public sealed class TemporaryPasswordGenerator
{
    private const string Upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
    private const string Lower = "abcdefghijkmnopqrstuvwxyz";
    private const string Digits = "23456789";
    private const string Symbols = "!@#$%";
    private const string All = Upper + Lower + Digits + Symbols;

    public static string Generate(int length = 20)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 4);

        var characters = new List<char>
        {
            Pick(Upper),
            Pick(Lower),
            Pick(Digits),
            Pick(Symbols)
        };
        while (characters.Count < length)
        {
            characters.Add(Pick(All));
        }

        for (var index = characters.Count - 1; index > 0; index--)
        {
            var swapIndex = RandomNumberGenerator.GetInt32(index + 1);
            (characters[index], characters[swapIndex]) = (characters[swapIndex], characters[index]);
        }

        return new string(characters.ToArray());
    }

    private static char Pick(string alphabet) => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
}
