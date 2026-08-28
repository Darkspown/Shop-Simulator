using System.Collections.Generic;

namespace ShelfRush.Economy
{
    /// <summary>
    /// Хранилище балансов валют (чистая data-модель). Событие Changed — единственный способ
    /// узнать об изменении; публикацию в EventBus делает EconomyService.
    /// </summary>
    public sealed class Wallet
    {
        private readonly Dictionary<CurrencyType, int> _balances = new Dictionary<CurrencyType, int>();
        private readonly List<System.Action<CurrencyType, int, int>> _changedHandlers = new List<System.Action<CurrencyType, int, int>>();

        public IReadOnlyDictionary<CurrencyType, int> Balances => _balances;

        /// <summary>handlers(currency, newBalance, delta)</summary>
        public event System.Action<CurrencyType, int, int> Changed
        {
            add => _changedHandlers.Add(value);
            remove => _changedHandlers.Remove(value);
        }

        public int GetBalance(CurrencyType currency)
        {
            return _balances.TryGetValue(currency, out var balance) ? balance : 0;
        }

        public void SetBalance(CurrencyType currency, int amount)
        {
            var previous = GetBalance(currency);
            _balances[currency] = amount;
            Notify(currency, amount, amount - previous);
        }

        public void Add(CurrencyType currency, int amount)
        {
            if (amount == 0) return;
            var newBalance = GetBalance(currency) + amount;
            _balances[currency] = newBalance;
            Notify(currency, newBalance, amount);
        }

        public bool TrySpend(CurrencyType currency, int amount)
        {
            var balance = GetBalance(currency);
            if (amount <= 0 || balance < amount) return false;
            _balances[currency] = balance - amount;
            Notify(currency, balance - amount, -amount);
            return true;
        }

        public void Clear() => _balances.Clear();

        private void Notify(CurrencyType currency, int newBalance, int delta)
        {
            for (var i = 0; i < _changedHandlers.Count; i++) _changedHandlers[i](currency, newBalance, delta);
        }
    }
}