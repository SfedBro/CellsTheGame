using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class ConveyorAssembler : MonoBehaviour
{
    [Header("Ссылки на компоненты")]
    [Tooltip("Ссылка на основной скрипт конвейера MenuConveyor (он будет включен после сборки)")]
    public MenuConveyor menuConveyor;

    [Header("Настройки сегментов")]
    [Tooltip("Спрайт одного сегмента конвейера")]
    public Sprite segmentSprite;

    [Tooltip("Ширина одного сегмента в пикселях (с учетом масштаба, например, 48 или 64)")]
    public float segmentWidth = 48f;

    [Tooltip("Порядок сортировки (Sorting Order) для сегментов конвейера. Меньшие значения рисуются позади, большие — впереди")]
    public int sortingOrder = -2;

    [Header("Тайминги сборки")]
    [Tooltip("Интервал времени между спавном следующего сегмента конвейера при сборке")]
    public float segmentDelay = 0.15f;

    [Tooltip("Длительность приземления одного сегмента")]
    public float landDuration = 0.4f;

    [Header("Эффекты приземления")]
    [Tooltip("Высота, с которой «падает» сегмент при появлении")]
    public float landingHeightOffset = 150f;

    [Tooltip("Множитель масштаба при появлении (например, 2 означает, что он падает в 2 раза больше оригинального размера)")]
    public float landingScaleOffset = 2f;

    [Tooltip("Преаб частиц пыли, спавнящихся при ударе о землю")]
    public ParticleSystem landingParticlePrefab;

    [Tooltip("Множитель размера частиц пыли. Позволяет быстро увеличить или уменьшить эффект приземления")]
    public float particleScaleMultiplier = 1f;

    [Header("Настройки пыли по бокам")]
    [Tooltip("Спавнить два облака пыли по бокам (сверху и снизу конвейера) вместо одного по центру? Это предотвращает наложение пыли на сами плиты")]
    public bool doubleDustPuff = true;

    [Tooltip("Вертикальное смещение от центра конвейера для спавна пыли (вверх и вниз) в пикселях")]
    public float particleVerticalOffset = 20f;

    [Tooltip("Смещение по оси Z для частиц. В режиме Screen Space - Camera положительное значение (например, 1 или 10) физически отодвигает частицы дальше от камеры за плоскость UI")]
    public float particleZOffset = 1f;

    private List<RectTransform> activeSegments = new List<RectTransform>();
    private bool isMoving = false;
    private float segmentSpawnTimer = 0f;

    private RectTransform actualSpawnPoint;
    private RectTransform actualDestroyPoint;

    private void Start()
    {
        if (menuConveyor == null)
            menuConveyor = GetComponent<MenuConveyor>();

        if (menuConveyor != null)
        {
            // Автоматически определяем физические точки старта и финиша конвейера
            actualSpawnPoint = menuConveyor.moveLeft ? menuConveyor.destroyPoint : menuConveyor.spawnPoint;
            actualDestroyPoint = menuConveyor.moveLeft ? menuConveyor.spawnPoint : menuConveyor.destroyPoint;

            // Отключаем спавн ресурсов, пока конвейер не собрался
            menuConveyor.enabled = false;
            StartCoroutine(AssembleConveyorRoutine());
        }
        else
        {
            Debug.LogError("ConveyorAssembler: Ссылка на MenuConveyor не задана!");
        }
    }

    private void Update()
    {
        // После завершения сборки двигаем ленту конвейера бесконечно
        if (isMoving && menuConveyor != null)
        {
            float speed = menuConveyor.speed;
            float dirMultiplier = menuConveyor.moveLeft ? -1f : 1f;
            float endX = actualDestroyPoint != null ? actualDestroyPoint.localPosition.x : 0f;

            // 1. Движение существующих сегментов
            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                RectTransform rt = activeSegments[i];
                if (rt == null)
                {
                    activeSegments.RemoveAt(i);
                    continue;
                }

                rt.localPosition += Vector3.right * dirMultiplier * speed * Time.deltaTime;

                // Проверяем выход за границу экрана
                bool isOutOfBounds = menuConveyor.moveLeft ? (rt.localPosition.x < endX) : (rt.localPosition.x > endX);
                if (isOutOfBounds)
                {
                    activeSegments.RemoveAt(i);
                    Destroy(rt.gameObject);
                }
            }

            // 2. Бесшовный спавн новых сегментов
            float spawnInterval = segmentWidth / speed;
            segmentSpawnTimer += Time.deltaTime;
            
            if (segmentSpawnTimer >= spawnInterval)
            {
                SpawnMovingSegment();
                segmentSpawnTimer -= spawnInterval;
            }
        }
    }

    private IEnumerator AssembleConveyorRoutine()
    {
        if (actualSpawnPoint == null || actualDestroyPoint == null || segmentSprite == null)
        {
            Debug.LogError("ConveyorAssembler: Задайте Spawn Point, Destroy Point и Segment Sprite в инспекторе!");
            yield break;
        }

        float startX = actualSpawnPoint.localPosition.x;
        float endX = actualDestroyPoint.localPosition.x;
        float distance = Mathf.Abs(endX - startX);
        
        // Вычисляем количество необходимых сегментов
        int segmentCount = Mathf.CeilToInt(distance / segmentWidth);
        
        // Определяем направление сборки
        float direction = menuConveyor.moveLeft ? -1f : 1f;

        List<Coroutine> activeAnimations = new List<Coroutine>();

        // Сборка конвейера по сегментам
        for (int i = 0; i < segmentCount; i++)
        {
            float targetX = startX + (i * segmentWidth * direction);
            Vector3 targetLocalPos = new Vector3(targetX, actualSpawnPoint.localPosition.y, 0f);

            // Создаем сегмент программно из спрайта
            GameObject segmentGo = new GameObject("ConveyorSegment", typeof(Image));
            segmentGo.transform.SetParent(transform, false);

            // Настраиваем оверрайд сортировки
            Canvas segmentCanvas = segmentGo.AddComponent<Canvas>();
            segmentCanvas.overrideSorting = true;
            segmentCanvas.sortingOrder = sortingOrder;

            RectTransform segmentRt = segmentGo.GetComponent<RectTransform>();
            Image segmentImage = segmentGo.GetComponent<Image>();
            segmentImage.sprite = segmentSprite;
            segmentImage.SetNativeSize();

            activeSegments.Add(segmentRt);

            // Запускаем независимую анимацию приземления сегмента
            Coroutine co = StartCoroutine(AnimateSegmentLanding(segmentRt, segmentImage, targetLocalPos));
            activeAnimations.Add(co);

            // Ждем перед запуском сборки следующей плиты
            yield return new WaitForSeconds(segmentDelay);
        }

        // Ждем завершения анимации последнего сегмента
        foreach (Coroutine co in activeAnimations)
        {
            if (co != null)
            {
                yield return co;
            }
        }

        // Конвейер полностью собран! Включаем движение ленты и спавн ресурсов
        isMoving = true;
        menuConveyor.enabled = true;
    }

    private void SpawnMovingSegment()
    {
        if (actualSpawnPoint == null || segmentSprite == null) return;

        GameObject segmentGo = new GameObject("ConveyorSegment", typeof(Image));
        segmentGo.transform.SetParent(transform, false);

        Canvas segmentCanvas = segmentGo.AddComponent<Canvas>();
        segmentCanvas.overrideSorting = true;
        segmentCanvas.sortingOrder = sortingOrder;

        RectTransform segmentRt = segmentGo.GetComponent<RectTransform>();
        Image segmentImage = segmentGo.GetComponent<Image>();
        segmentImage.sprite = segmentSprite;
        segmentImage.SetNativeSize();

        // Задаем позицию точно в точке старта
        segmentRt.localPosition = actualSpawnPoint.localPosition;
        
        // Зеркалим спрайт по оси X, если движение идет влево
        float scaleX = menuConveyor.moveLeft ? -menuConveyor.itemScaleMultiplier : menuConveyor.itemScaleMultiplier;
        segmentRt.localScale = new Vector3(scaleX, menuConveyor.itemScaleMultiplier, 1f);

        activeSegments.Add(segmentRt);
    }

    private IEnumerator AnimateSegmentLanding(RectTransform rt, Image img, Vector3 targetLocalPos)
    {
        float elapsed = 0f;
        float baseScale = menuConveyor.itemScaleMultiplier;

        // Зеркалим спрайт по оси X, если движение идет влево
        float scaleX = menuConveyor.moveLeft ? -baseScale : baseScale;

        Vector3 startLocalPos = targetLocalPos + new Vector3(0f, landingHeightOffset, 0f);
        Vector3 startScale = new Vector3(scaleX * landingScaleOffset, baseScale * landingScaleOffset, 1f);
        Vector3 targetScale = new Vector3(scaleX, baseScale, 1f);

        // Делаем сегмент изначально прозрачным
        Color color = img.color;
        color.a = 0f;
        img.color = color;

        rt.localPosition = startLocalPos;
        rt.localScale = startScale;

        while (elapsed < landDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / landDuration;

            // Смягчение движения (плавный удар в конце)
            float tEase = EaseOutQuad(t);

            rt.localPosition = Vector3.Lerp(startLocalPos, targetLocalPos, tEase);
            rt.localScale = Vector3.Lerp(startScale, targetScale, tEase);

            // Плавное появление (альфа от 0 до 1)
            Color c = img.color;
            c.a = Mathf.Lerp(0f, 1f, t);
            img.color = c;

            yield return null;
        }

        // Устанавливаем точные финальные значения
        rt.localPosition = targetLocalPos;
        rt.localScale = targetScale;
        Color finalColor = img.color;
        finalColor.a = 1f;
        img.color = finalColor;

        // Создаем пыль при ударе
        if (landingParticlePrefab != null)
        {
            float totalScale = baseScale * particleScaleMultiplier;
            float verticalOffset = particleVerticalOffset * totalScale;

            if (doubleDustPuff)
            {
                // Пыль сверху (летит вверх)
                Vector3 topPos = rt.position + new Vector3(0f, verticalOffset, 0f);
                SpawnDust(topPos, Quaternion.identity, totalScale);

                // Пыль снизу (разворачиваем на 180 градусов по Z, чтобы летела вниз)
                Vector3 bottomPos = rt.position - new Vector3(0f, verticalOffset, 0f);
                SpawnDust(bottomPos, Quaternion.Euler(0f, 0f, 180f), totalScale);
            }
            else
            {
                // Стандартный одиночный спавн в центре
                SpawnDust(rt.position, Quaternion.identity, totalScale);
            }
        }
    }

    private void SpawnDust(Vector3 position, Quaternion rotation, float scale)
    {
        // Добавляем смещение по оси Z в мировых координатах (направление "от камеры" вглубь)
        Vector3 spawnWorldPos = position + new Vector3(0f, 0f, particleZOffset);

        ParticleSystem ps = Instantiate(landingParticlePrefab, spawnWorldPos, rotation, transform.parent);
        ps.transform.localScale = Vector3.one * scale;

        // Настраиваем сортировку частиц под слой Canvas
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                // Принудительно задаем тот же Sorting Layer (например, UI или Default), что и у Canvas
                psr.sortingLayerName = canvas.sortingLayerName;
            }
            
            // Устанавливаем сортировку на 1 уровень ниже, чем у плиток конвейера
            psr.sortingOrder = sortingOrder - 1;
        }

        ps.Play();
        Destroy(ps.gameObject, 1.5f);
    }

    private float EaseOutQuad(float t)
    {
        return t * (2f - t);
    }
}
