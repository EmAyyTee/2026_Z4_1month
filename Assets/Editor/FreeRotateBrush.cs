using UnityEngine;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine.Tilemaps;

[CreateAssetMenu(
    fileName = "FreeRotateBrush",
    menuName = "Brushes/Free Rotate Brush"
)]
[CustomGridBrush(false, false, false, "Free Rotate Brush")]
public class FreeRotateBrush : GridBrush
{
    public float angle = 45f;

    public override void Paint(
        GridLayout grid,
        GameObject brushTarget,
        Vector3Int position)
    {
        if (grid == null)
            return;

        Transform decorationsTransform = grid.transform.Find("Decorations");

        if (decorationsTransform == null)
        {
            Debug.LogWarning("Nie znaleziono bezpośredniego dziecka Decorations w Grid!");
            return;
        }

        Tilemap decorations = decorationsTransform.GetComponent<Tilemap>();

        if (decorations == null)
        {
            Debug.LogWarning("Obiekt Decorations nie ma komponentu Tilemap!");
            return;
        }

        // Umieść kafelek na Decorations
        base.Paint(grid, decorations.gameObject, position);

        // Obróć kafelek
        decorations.SetTransformMatrix(position, Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, angle)));
        
        Debug.Log(
            "Ustawiony kąt: " + angle +
            " | Transform: " + decorations.GetTransformMatrix(position)
        );
    }
}