using System.Text;

namespace Game.Model;

public class UniqueIdentifier
{
    public string Id { get; private set; } = null!;

    public static UniqueIdentifier Create(Func<string, bool> idExists)
    {
        ArgumentNullException.ThrowIfNull(idExists);

        const string validChars = "abcdefghjklmnpqrstuvxyz0123456789";

        // We want as short ids as possible
        var length = 3;
        var id = Randomize(validChars, length);
        var count = 0;
        while (idExists(id))
        {
            count++;
            if (count >= (validChars.Length ^ length)) length++;
            id = Randomize(validChars, length);

            if (count > 100000)
            {
                throw new InvalidOperationException("Could not create a unique game id. Most likely a programmers error.");
            }
        }

        return new UniqueIdentifier { Id = id };
    }

    //private static readonly Random RandomGenerator = new Random();

    private static string Randomize(string chars, int length)
    {
        var result = new StringBuilder();
        for (var i = 0; i < length; i++)
        {
            result.Append(chars[Random.Shared.Next(chars.Length)]);
        }

        return result.ToString();
    }
}