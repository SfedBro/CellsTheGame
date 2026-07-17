using System.Collections.Generic;
using UnityEngine;

public class PlayerWeaponVisuals : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private SpriteRenderer hullRenderer;
    [SerializeField] private SpriteRenderer gunRenderer;

    [Header("Defaults")]
    [SerializeField] private Sprite defaultHullSprite;
    [SerializeField] private Sprite defaultGunSprite;

    private List<GameObject> activeMuzzleVisuals = new List<GameObject>();
    private List<Vector2> currentMuzzleOffsets = new List<Vector2>();

    public IReadOnlyList<Vector2> MuzzleOffsets => currentMuzzleOffsets;

    private void Start()
    {
        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged += OnModuleEquipStatusChanged;
            RebuildVisuals();
        }
    }

    private void OnDestroy()
    {
        if (PlayerModuleManager.Instance != null)
        {
            PlayerModuleManager.Instance.OnModuleEquipStatusChanged -= OnModuleEquipStatusChanged;
        }
    }

    private void OnModuleEquipStatusChanged(PlayerModule module, bool equipped)
    {
        RebuildVisuals();
    }

    public void RebuildVisuals()
    {
        // 1. Reset to defaults
        if (hullRenderer != null) hullRenderer.sprite = defaultHullSprite;
        if (gunRenderer != null) gunRenderer.sprite = defaultGunSprite;

        currentMuzzleOffsets.Clear();
        // Default muzzle: offset slightly forward along X axis (since 0 degrees is right/forward)
        currentMuzzleOffsets.Add(new Vector2(0.5f, 0f));

        foreach (var obj in activeMuzzleVisuals)
        {
            if (obj != null) Destroy(obj);
        }
        activeMuzzleVisuals.Clear();

        if (PlayerModuleManager.Instance == null) return;

        List<Sprite> activeModifiers = new List<Sprite>();

        // 2. Iterate equipped modules to extract overrides
        foreach (PlayerModule module in PlayerModuleManager.Instance.EquipedModules)
        {
            if (module == null) continue;

            if (module.overrideWeaponSprite != null && gunRenderer != null)
            {
                gunRenderer.sprite = module.overrideWeaponSprite;
            }

            if (module.muzzleOffsets != null && module.muzzleOffsets.Length > 0)
            {
                currentMuzzleOffsets.Clear();
                currentMuzzleOffsets.AddRange(module.muzzleOffsets);
            }

            if (module.overrideHullSprite != null && hullRenderer != null)
            {
                hullRenderer.sprite = module.overrideHullSprite;
            }

            if (module.muzzleModifierSprite != null)
            {
                activeModifiers.Add(module.muzzleModifierSprite);
            }
        }

        // 3. Instantiate stacked modifiers on each active muzzle
        if (gunRenderer == null) return;

        int sortingOrderOffset = 1;
        foreach (Vector2 muzzleOffset in currentMuzzleOffsets)
        {
            foreach (Sprite modifierSprite in activeModifiers)
            {
                GameObject modObj = new GameObject("MuzzleModifierVisual", typeof(SpriteRenderer));
                modObj.transform.SetParent(gunRenderer.transform, false);
                modObj.transform.localPosition = (Vector3)muzzleOffset;

                SpriteRenderer modSr = modObj.GetComponent<SpriteRenderer>();
                modSr.sprite = modifierSprite;
                // Layer on top of gun renderer
                modSr.sortingOrder = gunRenderer.sortingOrder + sortingOrderOffset;
                sortingOrderOffset++;

                activeMuzzleVisuals.Add(modObj);
            }
        }
    }

    public Vector3[] GetMuzzleWorldPositions()
    {
        if (gunRenderer == null)
        {
            return new Vector3[] { transform.position };
        }

        Vector3[] worldPositions = new Vector3[currentMuzzleOffsets.Count];
        for (int i = 0; i < currentMuzzleOffsets.Count; i++)
        {
            worldPositions[i] = gunRenderer.transform.TransformPoint(currentMuzzleOffsets[i]);
        }
        return worldPositions;
    }
}
