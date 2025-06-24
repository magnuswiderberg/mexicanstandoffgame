namespace Common.Cards;

public class Card
{
    public CardType Type { get; set; }
    public string Name { get; set; } = null!;

    public Card() { }

    protected Card(CardType type, string name)
    {
        Type = type;
        Name = name;
    }

    public static Card Dodge { get; } = new(CardType.Dodge, nameof(CardType.Dodge));
    public static Card Load { get; } = new(CardType.Load, nameof(CardType.Load));
    public static Card Chest { get; } = new(CardType.Chest, nameof(CardType.Chest));

    public override string ToString()
    {
        return Name;
    }
}