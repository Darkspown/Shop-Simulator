using DG.Tweening;
using UnityEngine;

namespace ShelfRush.Player.View
{
    /// <summary>
    /// Анимации игрока (MonoBehaviour-вью). Единственная ответственность — визуальные
    /// состояния: idle / walk / carry / interaction. DOTween используется ТОЛЬКО для
    /// визуальных эффектов (пульс при взаимодействии, покачивание при ходьбе) и НЕ
    /// участвует в физике/движении.
    ///
    /// Если на модели есть Unity Animator с bool-параметрами Idle/Walk/Carry/Interact —
    /// выставляются они. Если Animator нет — работают DOTween-эффекты (и этого достаточно
    /// для проверки без сцены). И то, и другое необязательно.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [Header("Animator (optional)")]
        [Tooltip("Unity Animator с bool-параметрами Idle/Walk/Carry/Interact. Может отсутствовать.")]
        [SerializeField] private Animator animator;

        [Header("DOTween visual effects (optional)")]
        [Tooltip("Пульс-эффект (scale-множитель) при взаимодействии. 0 — выключено.")]
        [SerializeField] private float interactPulse = 1.08f;

        [Tooltip("Child-объект для покачивания при ходьбе. Если пусто — покачивание выключено.")]
        [SerializeField] private Transform bobTarget;

        [Tooltip("Амплитуда покачивания при ходьбе (units).")]
        [SerializeField] private float bobAmplitude = 0.02f;

        [Tooltip("Длительность одного полупериода покачивания (сек).")]
        [SerializeField] private float bobDuration = 0.4f;

        private Vector3 _baseScale;
        private float _bobBaseY;
        private Tween _bobTween;
        private Tween _pulseTween;
        private bool _walking;
        private bool _carrying;
        private bool _interacting;

        public bool IsWalking => _walking;
        public bool IsCarrying => _carrying;
        public bool IsInteracting => _interacting;

        private void Awake()
        {
            DOTween.Init();
            if (animator == null) animator = GetComponentInChildren<Animator>();
            _baseScale = transform.localScale;
            if (bobTarget != null) _bobBaseY = bobTarget.localPosition.y;
        }

        private void OnDisable()
        {
            KillTweens();
        }

        /// <summary>Сбросить в idle (не двигается).</summary>
        public void SetIdle()
        {
            _walking = false;
            Apply();
            StopBob();
        }

        /// <summary>Переключить состояние ходьбы.</summary>
        public void SetWalking(bool walking)
        {
            if (_walking == walking) return;
            _walking = walking;
            Apply();
            if (walking) StartBob();
            else StopBob();
        }

        /// <summary>Переключить состояние «несёт товары».</summary>
        public void SetCarrying(bool carrying)
        {
            if (_carrying == carrying) return;
            _carrying = carrying;
            Apply();
        }

        /// <summary>Начать/закончить анимацию взаимодействия (+ DOTween-пульс).</summary>
        public void SetInteraction(bool interacting)
        {
            if (_interacting == interacting) return;
            _interacting = interacting;
            Apply();
            if (interacting) PlayPulse();
        }

        private void Apply()
        {
            if (animator == null) return;
            animator.SetBool("Idle", !_walking);
            animator.SetBool("Walk", _walking);
            animator.SetBool("Carry", _carrying);
            animator.SetBool("Interact", _interacting);
        }

        private void StartBob()
        {
            if (bobTarget == null || bobAmplitude <= 0f || _bobTween != null) return;
            _bobTween = bobTarget
                .DOLocalMoveY(_bobBaseY + bobAmplitude, bobDuration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        private void StopBob()
        {
            if (_bobTween != null)
            {
                _bobTween.Kill();
                _bobTween = null;
            }
            if (bobTarget != null) bobTarget.localPosition = new Vector3(bobTarget.localPosition.x, _bobBaseY, bobTarget.localPosition.z);
        }

        private void PlayPulse()
        {
            if (interactPulse <= 0f) return;
            transform.DOKill();
            _pulseTween = transform
                .DOScale(_baseScale * interactPulse, 0.1f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject)
                .OnComplete(() =>
                {
                    _pulseTween = transform.DOScale(_baseScale, 0.18f).SetEase(Ease.InQuad).SetLink(gameObject);
                });
        }

        private void KillTweens()
        {
            _bobTween?.Kill();
            _bobTween = null;
            _pulseTween?.Kill();
            _pulseTween = null;
            transform.DOKill();
            if (bobTarget != null) bobTarget.DOKill();
        }
    }
}