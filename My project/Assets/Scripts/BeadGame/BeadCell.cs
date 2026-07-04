using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class BeadCell : MonoBehaviour, IPointerDownHandler
{
    [HideInInspector] public int x;
    [HideInInspector] public int y;

    private BeadGameManager manager;
    private Image image;

    public void Initialize(BeadGameManager mgr, int gridX, int gridY, Sprite beadSprite)
    {
        manager = mgr;
        x = gridX;
        y = gridY;
        image = GetComponent<Image>();
        image.sprite = beadSprite;
    }

    public void SetColor(Color color)
    {
        image.color = color;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
            manager.PlaceBead(x, y);
        else if (eventData.button == PointerEventData.InputButton.Right)
            manager.RemoveBead(x, y);
    }
}
