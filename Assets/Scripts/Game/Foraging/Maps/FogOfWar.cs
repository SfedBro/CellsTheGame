using System;
using System.Collections.Generic;
using UnityEngine;

public class FogOfWar : MonoBehaviour
{
    #region fields

    [Header("References")]
    [SerializeField] private GameObject fogPrefab;

    [Header("Map Settings")]
    [SerializeField] private int mapSide = 160;
    [SerializeField] private int tilesCount = 40;
    [SerializeField] private int mapLeft = -80;
    [SerializeField] private int mapBot = -80;
    private GameObject[,] fogTiles;
    private HashSet<Vector2Int> revealedTiles = new HashSet<Vector2Int>();
    private float tileSize;

    [Header("Revealing Settings")]
    [SerializeField] private Transform player;
    [SerializeField] private int viewRadiusTiles = 2;
    private float viewRadius;
    private Vector2Int curPlayerTile = new Vector2Int(0, 0);
    private Vector2Int lastPlayerTile = new Vector2Int(0, 0);

    #endregion


    #region initialization

    void Awake()
    {
        generateFog();
        updateFog();
    }

    private void generateFog()
    {
        fogTiles = new GameObject[tilesCount + 1, tilesCount + 1];
        tileSize = mapSide / tilesCount;
        viewRadius = viewRadiusTiles * tileSize;

        for (int x = 0; x <= tilesCount; x++)
        {
            for (int y = 0; y <= tilesCount; y++)
            {
                GameObject tile = Instantiate(fogPrefab, new Vector3(x * tileSize + mapLeft, y * tileSize + mapBot, -20), Quaternion.identity);
                tile.transform.parent = transform;
                tile.transform.localScale = new Vector3(tileSize, tileSize, 1);
                fogTiles[x, y] = tile;
            }
        }
    }

    #endregion


    #region revealing

    void Update()
    {
        // Get player tile
        curPlayerTile.x = (int) ((player.position.x - mapLeft) / tileSize);
        curPlayerTile.y = (int) ((player.position.y - mapBot) / tileSize);
        if (lastPlayerTile.x != curPlayerTile.x || lastPlayerTile.y != curPlayerTile.y)
        {
            updateFog();
        }

        lastPlayerTile.x = curPlayerTile.x;
        lastPlayerTile.y = curPlayerTile.y;
    }

    private void updateFog()
    {
        for (int x = Math.Clamp(curPlayerTile.x - viewRadiusTiles, 0, tilesCount + 1); x < Math.Clamp(curPlayerTile.x + viewRadiusTiles, 0, tilesCount + 1); x++)
        {
            for (int y = Math.Clamp(curPlayerTile.y - viewRadiusTiles, 0, tilesCount + 1); y < Math.Clamp(curPlayerTile.y + viewRadiusTiles, 0, tilesCount + 1); y++)
            {
                Vector2Int coord = new Vector2Int(x, y);
                if (revealedTiles.Contains(coord)) continue;

                GameObject tile = fogTiles[x, y];
                if (Vector2.Distance(player.position, tile.transform.position) > viewRadius) continue;

                revealedTiles.Add(coord);
                tile.SetActive(false);
            }
        }
    }

    #endregion
}
