namespace Common.Cards;

public class Character
{
    public string Name { get; }
    public int Id { get; }

    //public Character() { }

    private static readonly object LockObject = new();

    public static Character Random(List<Character> except)
    {
        lock (LockObject)
        {
            var available = All.Except(except).ToList();
            if (available.Count == 0) throw new ArgumentException("Can't target all", nameof(except));
            return available[System.Random.Shared.Next(available.Count)];
        }
    }

    public static Character? Get(int id)
    {
        lock (LockObject)
        {
            return All.FirstOrDefault(x => x.Id == id);
        }
    }

    private Character(int id, string name)
    {
        Id = id;
        Name = name;
    }

    private static readonly List<Character> All =
    [
        new(1, "Chico"),
        new(2, "Ben Wade"),
        new(3, "Sentenza"),
        new(4, "Dick Bannister"),
        new(5, "Henchman Boggs"),
        new(6, "Calvera"),
        new(7, "Vin Tanner"),
        new(8, "Teresh")
    ];


    //public override bool Equals(object obj)
    //{
    //    if (!(obj is Character other)) return false;
    //    return other.Id == Id;
    //}

    //public override int GetHashCode()
    //{
    //    return Id.GetHashCode();
    //}
}