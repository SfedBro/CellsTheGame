using UnityEngine;

public class MenuTankAnimator : MonoBehaviour
{
    [Header("Части Танка")]
    [Tooltip("Трансформ башни/пушки, которая будет вращаться")]
    public Transform turretTransform;
    
    [Tooltip("Трансформ корпуса, который будет вибрировать (если пустой, вибрирует этот объект)")]
    public Transform bodyTransform;

    [Tooltip("Трансформ тени пушки (он должен лежать в иерархии отдельно, не внутри объекта самой пушки!)")]
    public Transform turretShadowTransform;

    [Tooltip("Постоянное смещение тени относительно пушки в локальных координатах (обычно вниз и вправо, например X=5, Y=-5)")]
    public Vector3 shadowOffset = new Vector3(5f, -5f, 0f);

    [Header("Вибрация Двигателя (Idle)")]
    [Tooltip("Включить покачивание/вибрацию корпуса")]
    public bool enableVibration = true;
    
    [Tooltip("Скорость вибрации двигателя")]
    public float vibrationSpeed = 25f;
    
    [Tooltip("Амплитуда вибрации корпуса (высота покачивания)")]
    public float vibrationAmount = 1.5f;

    [Tooltip("Задержка фазы вибрации башни относительно корпуса. Создает эффект отставания и мягкого физического соединения деталей (0 — синхронно)")]
    public float turretPhaseDelay = 0.5f;

    [Tooltip("Множитель силы вибрации башни. Позволяет сделать тряску башни сильнее (например, 1.2) или слабее (например, 0.8) корпуса")]
    public float turretVibrationMultiplier = 1f;

    [Header("Поведение Башни")]
    [Tooltip("Следить за курсором мыши? Если выключено, башня будет просто сканировать область")]
    public bool trackMouse = true;

    [Tooltip("Вращать башню мгновенно вслед за мышью?")]
    public bool instantRotation = false;
    
    [Tooltip("Скорость поворота башни к цели (если мгновенное вращение выключено)")]
    public float rotationSpeed = 10f;

    [Tooltip("Смещение угла в градусах. Если пушка нарисована смотрящей вправо, поставьте 0. Если смотрит вверх, укажите -90.")]
    public float angleOffset = 0f;

    [Header("Режим сканирования (если слежение за мышью выключено)")]
    [Tooltip("Максимальный угол поворота от начального в градусах")]
    public float scanAngle = 35f;
    
    [Tooltip("Скорость сканирования")]
    public float scanSpeed = 1.5f;

    private Vector3 initialBodyPosition;
    private Vector3 initialTurretPosition;
    private float initialTurretZRotation;

    void Start()
    {
        if (bodyTransform == null)
            bodyTransform = transform;

        initialBodyPosition = bodyTransform.localPosition;
        
        if (turretTransform != null)
        {
            initialTurretPosition = turretTransform.localPosition;
            initialTurretZRotation = turretTransform.localRotation.eulerAngles.z;
        }
    }

    void Update()
    {
        // 1. Симуляция работы двигателя (вибрация корпуса и башни вверх-вниз)
        if (enableVibration)
        {
            float timeFactor = Time.time * vibrationSpeed;
            float yOffsetBody = Mathf.Sin(timeFactor) * vibrationAmount;
            
            if (bodyTransform != null)
            {
                bodyTransform.localPosition = initialBodyPosition + new Vector3(0, yOffsetBody, 0);
            }

            // Если башня существует и не является дочерней деталью корпуса (например, для плоских UI слоев),
            // то вибрируем ее со сдвигом фазы (задержкой) и собственным множителем силы
            if (turretTransform != null && !turretTransform.IsChildOf(bodyTransform))
            {
                float yOffsetTurret = Mathf.Sin(timeFactor - turretPhaseDelay) * (vibrationAmount * turretVibrationMultiplier);
                turretTransform.localPosition = initialTurretPosition + new Vector3(0, yOffsetTurret, 0);
            }
        }

        // 2. Вращение башни
        if (turretTransform != null)
        {
            if (trackMouse)
            {
                RotateTurretTowardsMouse();
            }
            else
            {
                ScanArea();
            }

            // Синхронизируем положение и поворот тени башни
            if (turretShadowTransform != null)
            {
                // Поворот тени совпадает с поворотом пушки
                turretShadowTransform.localRotation = turretTransform.localRotation;
                
                // Позиция всегда смещена на константный вектор (не вращается по орбите вокруг башни)
                turretShadowTransform.localPosition = turretTransform.localPosition + shadowOffset;
            }
        }
    }

    void RotateTurretTowardsMouse()
    {
        Vector3 targetPoint;
        
        // Проверяем, находится ли объект в Canvas (UI) или в мировом пространстве (2D/3D Sprite)
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
        {
            // Для UI: определяем положение курсора относительно центра башни
            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, turretTransform.position);
            targetPoint = Input.mousePosition - (Vector3)screenPos;
        }
        else
        {
            // Для Sprite в мире: переводим позицию мыши в мировые координаты
            Camera mainCam = Camera.main != null ? Camera.main : Camera.current;
            if (mainCam != null)
            {
                Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
                mouseWorldPos.z = turretTransform.position.z;
                targetPoint = mouseWorldPos - turretTransform.position;
            }
            else
            {
                targetPoint = Vector3.zero;
            }
        }

        if (targetPoint != Vector3.zero)
        {
            float targetAngle = Mathf.Atan2(targetPoint.y, targetPoint.x) * Mathf.Rad2Deg;
            
            // Складываем угол со смещением
            float finalAngle = targetAngle + angleOffset; 

            Quaternion targetRotation = Quaternion.Euler(0, 0, finalAngle);
            
            if (instantRotation)
            {
                turretTransform.rotation = targetRotation;
            }
            else
            {
                turretTransform.rotation = Quaternion.Lerp(turretTransform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
    }

    void ScanArea()
    {
        // Плавно водим башней влево-вправо по синусоиде
        float angleOffset = Mathf.Sin(Time.time * scanSpeed) * scanAngle;
        turretTransform.localRotation = Quaternion.Euler(0, 0, initialTurretZRotation + angleOffset);
    }
}
