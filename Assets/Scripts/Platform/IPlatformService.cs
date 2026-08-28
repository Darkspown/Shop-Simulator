using System;

namespace ShelfRush.Platform
{
    /// <summary>
    /// Единая обёртка платформы (десктоп/WebGL/Яндекс-игры). Игровая логика зависит
    /// только от этого интерфейса и не знает про конкретный SDK (YG2 и т.п.).
    /// </summary>
    public interface IPlatformService : Core.IGameService
    {
        /// <summary>Событие изменения паузы со стороны платформы (true = пауза).</summary>
        event Action<bool> PauseToggled;

        bool IsPaused { get; }

        string Language { get; }

        /// <summary>Сигнал платформе, что игра готова к показу (GameReady).</summary>
        void SetReady();

        /// <summary>Запросить паузу/возобновление у самой игры.</summary>
        void SetPause(bool paused);

        /// <summary>Показать interstitial-рекламу. onClosed(true) — если закрыта штатно.</summary>
        void ShowInterstitial(Action<bool> onClosed);

        /// <summary>Показать rewarded-рекламу. onReward(true) — если награда выдана.</summary>
        void ShowRewarded(Action<bool> onReward);

        /// <summary>Запросить сброс save на платформу (Яндекс cloud).</summary>
        void RequestSave();
    }
}