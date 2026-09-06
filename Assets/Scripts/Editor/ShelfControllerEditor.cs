using UnityEditor;
using UnityEngine;

namespace ShelfRush.Shelves.Editor
{
    /// <summary>
    /// Редактор-инструмент для РУЧНОЙ настройки слотов полки (ShelfController.useManualSlots).
    /// Даёт в инспекторе ShelfController кнопки:
    ///  - «+ Добавить слот (child)» — создаёт дочерний объект с компонентом <see cref="ShelfSlot"/>
    ///    под <see cref="ShelfController.SlotsRoot"/>, который можно расставить вручную в редакторе;
    ///  - «Переименовать/синхро. порядок» — приводит имена слотов к `ShelfSlot_0..N` по порядку
    ///    иерархии (SiblingIndex), чтобы позиции и индексы совпадали с порядком в сцене.
    /// Сцена не меняется автоматически — кнопки вызываются пользователем вручную.
    /// </summary>
    [CustomEditor(typeof(ShelfController))]
    [CanEditMultipleObjects]
    public sealed class ShelfControllerEditor : UnityEditor.Editor
    {
        private static readonly string SlotNameBase = "ShelfSlot";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var controller = (ShelfController)target;
            if (controller == null) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Ручная настройка слотов", EditorStyles.boldLabel);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                if (GUILayout.Button("+ Добавить слот (child)"))
                {
                    AddSlot(controller);
                }

                if (GUILayout.Button("Синхрон. порядок / имена слотов"))
                {
                    RenameSlots(controller);
                }

                EditorGUILayout.HelpBox(
                    "Создайте слоты через кнопку, расставьте их вручную в сцене (перемещая объекты), " +
                    "затем используйте «Синхрон.» для фиксации порядка. Контроллер применяет их в Awake, " +
                    "если включён режим ручных слотов.",
                    MessageType.Info);
            }

            // Помечаем сцену грязной после правок, чтобы изменения сохранились.
            if (GUI.changed)
            {
                EditorUtility.SetDirty(controller);
            }
        }

        /// <summary>Создаёт дочерний объект ShelfSlot под корнем слотов.</summary>
        private static void AddSlot(ShelfController controller)
        {
            var root = controller.SlotsRoot;
            var go = new GameObject($"{SlotNameBase}_{root.childCount}");
            go.transform.SetParent(root, false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.AddComponent<ShelfSlot>();

            Undo.RegisterCreatedObjectUndo(go, "Add Shelf Slot");
            EditorUtility.SetDirty(controller);
            Selection.activeGameObject = go;
        }

        /// <summary>Приводит имена слотов к ShelfSlot_0..N по порядку иерархии.</summary>
        private static void RenameSlots(ShelfController controller)
        {
            var root = controller.SlotsRoot;
            var slots = root.GetComponentsInChildren<ShelfSlot>(true);
            if (slots == null || slots.Length == 0) return;

            // Стабильный арт-порядок: по SiblingIndex.
            System.Array.Sort(slots, (a, b) => a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex()));

            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].gameObject.name = $"{SlotNameBase}_{i}";
                EditorUtility.SetDirty(slots[i].gameObject);
            }
        }
    }
}