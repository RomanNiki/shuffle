using System.Collections.Generic;
using UnityEngine;
using Image = UnityEngine.UI.Image;

public class GridItem : MonoBehaviour
{
    [SerializeField] private Image image;

    public Vector2Int gridPosition { get; private set; }

    public List<GridItem> Neighbors;
    
    public void Setup(Color color, Vector2Int position)
    {
        Neighbors = new List<GridItem>();
        gridPosition = position;
        image.color = color;
        image.transform.position = position * image.rectTransform.sizeDelta + Vector2.right * 300 + Vector2.up * 300;
    }

    public void SetColor(Color color)
    {
        image.color = color;
    }

    public Color GetColor()
    {
        return image.color;
    }
}
