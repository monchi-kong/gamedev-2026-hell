using System.Collections.Generic;
using UnityEngine;

// Camera-relative tiling for the non-seamless bright forest artwork.
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(SpriteRenderer))]
public class BrightForestLayer : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float cameraFollow = 0.5f;
    private readonly List<SpriteRenderer> tiles = new List<SpriteRenderer>();
    private Camera view;
    private SpriteRenderer source;
    private Vector3 origin;
    private Vector3 initialCamera;
    private Vector3 initialScale;

    private void Start()
    {
        view = Camera.main;
        source = GetComponent<SpriteRenderer>();
        origin = transform.position;
        initialScale = transform.localScale;
        if (view != null) initialCamera = view.transform.position;
    }

    private void LateUpdate()
    {
        if (view == null || source.sprite == null || !view.orthographic) return;
        float screenHeight = view.orthographicSize * 2f;
        float originalHeight = source.sprite.bounds.size.y * Mathf.Abs(initialScale.y);
        float enlargement = Mathf.Max(1f, (screenHeight + 4f) / originalHeight);
        transform.localScale = initialScale * enlargement;
        float width = source.sprite.bounds.size.x * Mathf.Abs(transform.lossyScale.x);
        float height = source.sprite.bounds.size.y * Mathf.Abs(transform.lossyScale.y);
        if (width <= 0f) return;

        float anchorX = origin.x + (view.transform.position.x - initialCamera.x) * cameraFollow;
        int center = Mathf.RoundToInt((view.transform.position.x - anchorX) / width);
        int radius = Mathf.CeilToInt(view.orthographicSize * view.aspect / width) + 1;
        int count = radius * 2 + 1;
        while (tiles.Count < count)
        {
            var tile = new GameObject("Bright Forest Tile").AddComponent<SpriteRenderer>();
            tile.transform.SetParent(transform, false);
            tile.sprite = source.sprite;
            tile.sharedMaterial = source.sharedMaterial;
            tile.sortingLayerID = source.sortingLayerID;
            tile.sortingOrder = source.sortingOrder;
            tile.color = source.color;
            tiles.Add(tile);
        }

        // Keep the painting stationary vertically until an edge approaches the viewport.
        float margin = (height - screenHeight) * 0.5f;
        float y = Mathf.Clamp(origin.y, view.transform.position.y - margin, view.transform.position.y + margin);
        transform.position = new Vector3(anchorX, y, origin.z);
        source.enabled = false;
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].gameObject.SetActive(i < count);
            if (i >= count) continue;
            int index = center + i - radius;
            tiles[i].transform.position = new Vector3(anchorX + index * width, y, origin.z);
            tiles[i].flipX = (index & 1) != 0;
        }
    }
}
