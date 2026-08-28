namespace ShelfRush.Economy
{
    /// <summary>API экономики: доступ к балансу, списание и начисление.</summary>
    public interface IEconomyService : Core.IGameService
    {
        Wallet Wallet { get; }

        bool TrySpend(CurrencyType currency, int amount);

        void AddCurrency(CurrencyType currency, int amount);

        void SetCurrency(CurrencyType currency, int amount);

        void LoadInitial(EconomyConfig config);
    }
}