namespace Common.Cards;

public class AttackCard(string target) : Card(CardType.Attack, $"{nameof(CardType.Attack)}")
{
    // TODO: Try to change back to PlayerId
    public string Target { get; set; } = target;
}