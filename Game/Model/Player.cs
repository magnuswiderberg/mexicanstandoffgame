using Shared.Cards;

namespace Game.Model
{
    public class Player(string id, Character character)
    {
        public string Id { get; } = id;
        public string Name { get; set; } = character.Name;
        public Character Character { get; } = character;

        public Card? SelectedCard { get; private set; }
        public bool Alive { get; private set; } = true;
        public bool Winner { get; private set; }
        public bool Quit { get; private set; }

        public int Coins { get; set; }
        public int Shots { get; set; }
        public int Bullets { get; set; }

        public Func<Player, Task>? CardChanged { get; set; }

        private bool _locked;

        private readonly Dictionary<int, bool> _results = new();

        public async Task SetSelectedCardAsync(Card? card)
        {
            if (_locked) return;
            if (card == null && SelectedCard != null
                || card != null && SelectedCard == null
                || card != null && !card.Equals(SelectedCard))
            {
                SelectedCard = card;
                if (CardChanged != null) await CardChanged(this);
            }
        }

        public void ResetCards()
        {
            SelectedCard = null;
        }

        public virtual void NewRound()
        {
            ResetCards();
        }

        public void SetDead() { Alive = false; }
        public void SetWinner() { Winner = true; }
        public void SetLocked(bool locked) { _locked = locked; }

        public void SetResult(int round, bool success)
        {
            _results.Remove(round);
            _results.Add(round, success);
        }

        public IEnumerable<bool> SuccessTrend(int lastResults)
        {
            return _results.OrderByDescending(x => x.Key).Select(x => x.Value).Take(lastResults).Reverse();
        }

        //public override bool Equals(object obj)
        //{
        //    if (!(obj is Player other)) return false;
        //    return other.Character.Equals(Character);
        //}

        //public override int GetHashCode()
        //{
        //    return Character.GetHashCode();
        //}

        public override string ToString()
        {
            return $"{Name} [${Coins}|S={Shots}|B={Bullets}|({SelectedCard?.ToString() ?? "-"})]";
        }

        public void ResetAll()
        {
            ResetCards();
            Alive = true;
            Winner = false;
            Quit = false;
            Coins = 0;
            Shots = 0;
            Bullets = 0;
            _locked = false;
            _results.Clear();
        }
    }
}