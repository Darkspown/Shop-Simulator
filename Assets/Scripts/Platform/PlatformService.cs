using System;
using ShelfRush.Core;
using UnityEngine;

namespace ShelfRush.Platform
{
    /// <summary>
    /// Реализация платформенной обёртки (база). На десктопе/мобильных:
    ///  - пауза при потере фокуса окна (Application.focusChanged);
    ///  - язык из системного языка;
    ///  - реклама/сохранение — нейтральные заглушки.
    ///  Яндекс-интеграция (YG2) реализуется отдельным классом без изменения контракта.
    /// </summary>
    public sealed class PlatformService : IPlatformService
    {
        private IEventBus _events;
        private bool _isPaused;

        public event Action<bool> PauseToggled;

        public bool IsPaused => _isPaused;

        public string Language
        {
            get
            {
                switch (Application.systemLanguage)
                {
                    case SystemLanguage.Russian:
                    case SystemLanguage.Ukrainian:
                    case SystemLanguage.Belarusian:
                        return "ru";
                    default:
                        return "en";
                }
            }
        }

        public void Initialize(ServiceLocator services)
        {
            _events = services.Get<IEventBus>();
            Application.focusChanged += OnApplicationFocus;
        }

        public void Dispose()
        {
            Application.focusChanged -= OnApplicationFocus;
            _events = null;
        }

        public void SetReady()
        {
            // Заглушка: YandexGame.GameReadyAPIG() подключается в Yandex-реализации.
        }

        public void SetPause(bool paused)
        {
            if (_isPaused == paused) return;
            _isPaused = paused;
            PauseToggled?.Invoke(paused);
            _events?.Publish(new GamePauseRequestedEvent(paused));
        }

        public void ShowInterstitial(Action<bool> onClosed)
        {
            // Заглушка рекламы; onClosed(true) — штатное завершение.
            onClosed?.Invoke(true);
        }

        public void ShowRewarded(Action<bool> onReward)
        {
            // Заглушка rewarded-рекламы; выдача награды по контракту.
            onReward?.Invoke(true);
        }

        public void RequestSave()
        {
            // На десктопе save уже в PlayerPrefs; облачный сброс — в Yandex-реализации.
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) SetPause(true);
        }
    }
}