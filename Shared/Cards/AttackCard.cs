namespace Shared.Cards;

public class AttackCard(Character target) : Card(CardType.Attack, $"{nameof(CardType.Attack)} {target.Name}")
{
    public Character Target { get; set; } = target;

    //public AttackCard() { }

    //public override bool Equals(object? obj)
    //{
    //    if (!base.Equals(obj)) return false;
    //    if (!(obj is AttackCard other)) return false;
    //    return other.Target.Equals(Target);
    //}

    //public override int GetHashCode()
    //{
    //    return base.GetHashCode();
    //}

}