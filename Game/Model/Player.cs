using Game.Events;
using Shared.Cards;

namespace Game.Model
{
    public class Player(string id, Character character)
    {
        public event EventHandler? AttributeChanged;
        public event EventHandler? CardChanged;

        public string Id { get; } = id;
        public string Name => Character.Name;
        public Character Character { get; } = character;

        public Card? SelectedCard { get; private set; }
        public bool Alive { get; private set; } = true;
        public bool Winner { get; private set; }

        public int Coins
        {
            get => _coins;
            set { _coins = value; AttributeChanged?.Invoke(this, EventArgs.Empty); }
        }

        public int Shots
        {
            get => _shots;
            set { _shots = value; AttributeChanged?.Invoke(this, EventArgs.Empty); }
        }

        public int Bullets
        {
            get => _bullets;
            set { _bullets = value; AttributeChanged?.Invoke(this, EventArgs.Empty); }
        }

        private int _coins;
        private int _shots;
        private int _bullets;
        private bool _locked;

        private readonly Dictionary<int, bool> _results = new();

        public void SetSelectedCard(Card? card)
        {
            if (_locked) return;
            if (card == null && SelectedCard != null
                || card != null && SelectedCard == null
                || card != null && !card.Equals(SelectedCard))
            {
                SelectedCard = card;
                CardChanged?.Invoke(this, new PlayerEvent(this));
            }
        }

        public void ResetCards()
        {
            SelectedCard = null;
        }

        public virtual void NewRound(object? sender, EventArgs e)
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
            _coins = 0;
            _shots = 0;
            _bullets = 0;
            _locked = false;
            _results.Clear();
        }
    }
}