using System;

namespace ShelfRush.Levels
{
    /// <summary>
    /// Runtime-прогресс уровня по целям (plain C#, НЕ MonoBehaviour и НЕ ScriptableObject —
    /// состояние, а не данные). Хранит текущее количество размещённых товаров по каждой
    /// <see cref="LevelObjective"/> и флаги завершения. Создаётся <see cref="LevelManager"/>
    /// на старте уровня на основе <see cref="LevelData"/> (TargetCount берётся из данных).
    ///
    /// Progress: 0/10 … 10/10 (для Level 1). Хранится только счётчик, остальное — из LevelData.
    /// </summary>
    public sealed class LevelProgress
    {
        private readonly LevelObjective[] _objectives;
        private readonly int[] _placed;
        private readonly bool[] _complete;
        private int _totalPlaced;

        public LevelProgress(LevelData level)
        {
            _objectives = level != null ? level.Objectives : Array.Empty<LevelObjective>();
            _placed = new int[_objectives.Length];
            _complete = new bool[_objectives.Length];
            _totalPlaced = 0;
        }

        /// <summary>Число целей уровня.</summary>
        public int Count => _objectives.Length;

        /// <summary>Сколько всего единиц размещено по всем целям.</summary>
        public int TotalPlaced => _totalPlaced;

        /// <summary>Суммарная цель уровня (сумма TargetCount всех целей).</summary>
        public int TotalTarget
        {
            get
            {
                var total = 0;
                for (var i = 0; i < _objectives.Length; i++)
                {
                    if (_objectives[i] != null) total += _objectives[i].TargetCount;
                }
                return total;
            }
        }

        /// <summary>Все цели уровня достигнуты (0 &lt; TotalPlaced == TotalTarget).</summary>
        public bool IsComplete => Count > 0 && TotalPlaced >= TotalTarget;

        /// <summary>Целевое количество для цели (0, если индекс вне диапазона).</summary>
        public int GetTarget(int index) =>
            index >= 0 && index < _objectives.Length && _objectives[index] != null
                ? _objectives[index].TargetCount
                : 0;

        /// <summary>Текущее размещённое количество для цели.</summary>
        public int GetCurrent(int index) =>
            index >= 0 && index < _placed.Length ? _placed[index] : 0;

        /// <summary>Завершена ли цель (Progress == Target).</summary>
        public bool IsCompleted(int index) =>
            index >= 0 && index < _complete.Length && _complete[index];

        /// <summary>Нормированный прогресс цели 0..1.</summary>
        public float GetRatio(int index)
        {
            var target = GetTarget(index);
            return target <= 0 ? 0f : Math.Clamp(GetCurrent(index) / (float)target, 0f, 1f);
        }

        /// <summary>
        /// Зафиксировать размещение одной единицы по цели. Возвращает true, если цель при
        /// этом ТОЛЬКО ЧТО завершилась (пересекла TargetCount). После завершения игнорируется.
        /// </summary>
        public bool Advance(int index)
        {
            if (index < 0 || index >= _objectives.Length || _complete[index]) return false;

            var target = GetTarget(index);
            if (_placed[index] < target)
            {
                _placed[index]++;
                _totalPlaced++;
                if (_placed[index] >= target)
                {
                    _complete[index] = true;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Сбросить прогресс (например, на старте уровня).</summary>
        public void Reset()
        {
            Array.Clear(_placed, 0, _placed.Length);
            Array.Clear(_complete, 0, _complete.Length);
            _totalPlaced = 0;
        }
    }
}