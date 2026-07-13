using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class MenuConveyor : MonoBehaviour
{
    [Header("Conveyor Settings")]
    [Tooltip("Список спрайтов ресурсов, которые будут двигаться (руда, слитки, пластины)")]
    public Sprite[] resourceSprites;
    
    [Tooltip("Скорость движения (пикселей в секунду в режиме UI)")]
    public float speed = 100f;
    
    [Tooltip("Интервал появления новых предметов (в секундах)")]
    public float spawnInterval = 1.5f;
    
    [Tooltip("Масштаб предметов (увеличьте, чтобы 16x16 иконки выглядели крупно)")]
    public float itemScaleMultiplier = 3f;

    [Tooltip("Направление движения влево. Если false, предметы едут вправо")]
    public bool moveLeft = false;

    [Tooltip("Порядок сортировки (Sorting Order) для предметов. Меньшие значения рисуются позади, большие — впереди")]
    public int sortingOrder = 0;

    [Header("Эффекты физики движения")]
    [Tooltip("Включить покачивание и подпрыгивание ресурсов на роликах конвейера?")]
    public bool enablePhysics = true;

    [Tooltip("Частота бугорков (роликов) конвейера. Чем выше, тем чаще прыгают")]
    public float bounceFrequency = 0.05f;

    [Tooltip("Высота подпрыгивания на роликах")]
    public float bounceAmount = 4f;

    [Tooltip("Частота покачивания влево-вправо при движении")]
    public float wiggleFrequency = 0.03f;

    [Tooltip("Максимальный угол наклона предмета при покачивании")]
    public float maxWiggleAngle = 8f;

    [Header("Spawn Points")]
    [Tooltip("Точка спавна предметов (должна быть за экраном с одной стороны)")]
    public RectTransform spawnPoint;
    
    [Tooltip("Точка удаления предметов (должна быть за экраном с другой стороны)")]
    public RectTransform destroyPoint;

    private float spawnTimer;
    private List<RectTransform> activeItems = new List<RectTransform>();
    private RectTransform actualSpawnPoint;
    private RectTransform actualDestroyPoint;

    // Кольцевой буфер для хранения 5 последних спавнов, чтобы избежать частых повторов
    private int[] lastIndices = new int[5] { -1, -1, -1, -1, -1 };
    private int lastIndicesPointer = 0;
    private int[] allowedIndicesPool;

    void Start()
    {
        // Автоматически определяем точки старта и финиша в зависимости от направления
        actualSpawnPoint = moveLeft ? destroyPoint : spawnPoint;
        actualDestroyPoint = moveLeft ? spawnPoint : destroyPoint;
    }

    void Update()
    {
        // Спавн новых предметов по таймеру
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            if (actualSpawnPoint != null)
            {
                SpawnResourceAt(actualSpawnPoint.localPosition);
            }
            spawnTimer = 0;
        }

        // Движение существующих предметов
        float dirMultiplier = moveLeft ? -1f : 1f;
        float endX = actualDestroyPoint != null ? actualDestroyPoint.localPosition.x : 0f;
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            RectTransform item = activeItems[i];
            if (item == null)
            {
                activeItems.RemoveAt(i);
                continue;
            }

            // Вычисляем новую позицию по горизонтали
            Vector3 pos = item.localPosition;
            pos.x += dirMultiplier * speed * Time.deltaTime;

            // Эффект езды по роликам конвейера (покачивание и прыжки)
            if (enablePhysics && actualSpawnPoint != null)
            {
                // Подпрыгивание вверх-вниз на неровностях конвейера (используем Abs от синуса для эффекта прыжков)
                pos.y = actualSpawnPoint.localPosition.y + Mathf.Abs(Mathf.Sin(pos.x * bounceFrequency)) * bounceAmount;
                
                // Легкое покачивание (наклон) влево-вправо
                float zRot = Mathf.Sin(pos.x * wiggleFrequency) * maxWiggleAngle;
                item.localRotation = Quaternion.Euler(0f, 0f, zRot);
            }
            else if (actualSpawnPoint != null)
            {
                pos.y = actualSpawnPoint.localPosition.y;
                item.localRotation = Quaternion.identity;
            }

            item.localPosition = pos;

            // Проверяем выход за границу экрана
            bool isOutOfBounds = moveLeft ? (item.localPosition.x < endX) : (item.localPosition.x > endX);
            if (isOutOfBounds)
            {
                activeItems.RemoveAt(i);
                Destroy(item.gameObject);
            }
        }
    }

    void SpawnResourceAt(Vector3 position)
    {
        if (resourceSprites == null || resourceSprites.Length == 0) return;

        // Инициализируем или обновляем пул индексов без выделения мусора в памяти
        if (allowedIndicesPool == null || allowedIndicesPool.Length != resourceSprites.Length)
        {
            allowedIndicesPool = new int[resourceSprites.Length];
        }

        // Вычисляем допустимый лимит исключений (не более N-1, чтобы не уйти в бесконечный цикл при малом числе ресурсов)
        int exclusionLimit = Mathf.Min(5, resourceSprites.Length - 1);
        int allowedCount = 0;

        // Собираем индексы, которые не спавнились в последних 5 шагах
        for (int i = 0; i < resourceSprites.Length; i++)
        {
            bool isExcluded = false;
            for (int j = 0; j < exclusionLimit; j++)
            {
                if (lastIndices[j] == i)
                {
                    isExcluded = true;
                    break;
                }
            }
            if (!isExcluded)
            {
                allowedIndicesPool[allowedCount] = i;
                allowedCount++;
            }
        }

        // Выбираем случайный индекс из разрешенных
        int selectedIndex = 0;
        if (allowedCount > 0)
        {
            int randomAllowedIndex = Random.Range(0, allowedCount);
            selectedIndex = allowedIndicesPool[randomAllowedIndex];
        }
        else
        {
            selectedIndex = Random.Range(0, resourceSprites.Length);
        }

        // Записываем индекс в историю
        if (exclusionLimit > 0)
        {
            lastIndices[lastIndicesPointer] = selectedIndex;
            lastIndicesPointer = (lastIndicesPointer + 1) % 5;
        }

        // Создаем игровой объект UI Image
        GameObject go = new GameObject("ConveyorItem", typeof(Image));
        go.transform.SetParent(transform, false);

        // Настраиваем слой отрисовки через оверрайд сортировки Canvas
        Canvas itemCanvas = go.AddComponent<Canvas>();
        itemCanvas.overrideSorting = true;
        itemCanvas.sortingOrder = sortingOrder;

        Image img = go.GetComponent<Image>();
        img.sprite = resourceSprites[selectedIndex];
        
        // Устанавливаем оригинальный размер пикселей
        img.SetNativeSize();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.localPosition = position;
        
        // Увеличиваем размер предмета под пиксельный стиль меню
        rt.localScale = Vector3.one * itemScaleMultiplier;

        activeItems.Add(rt);
    }
}
