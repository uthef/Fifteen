using Godot;

public class Block : Sprite
{
    public bool IsBeingInterpolated = false;
    public int Column = 0, Row = 0;

    public void UpdateArrayPosition(Vector2 direction, bool interpolation = true)
    {
        IsBeingInterpolated = interpolation;
        Column += (int) direction.x;
        Row += (int) direction.y;
    }
}
