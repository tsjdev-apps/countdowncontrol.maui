namespace CountdownControlMaui.Controls;

/// <summary>
/// A custom drawable for rendering a circular countdown ring in .NET MAUI.
/// It visually represents remaining time using a radial progress arc, 
/// rendered counter-clockwise, starting from the bottom. 
/// The arc dynamically adapts its thickness based on the control size,
/// and the colors are injected via delegates for full runtime flexibility.
/// </summary>
/// <param name="getProgress">Delegate that returns the current progress 
/// as a float between 0 (empty) and 1 (full).</param>
/// <param name="getActiveColor">Delegate that returns the color for the 
/// remaining time arc (active part).</param>
/// <param name="getInactiveColor">Delegate that returns the color for the 
/// full background ring (inactive part).</param>
public class CountdownDrawable(
    Func<float> getProgress,
    Func<Color> getActiveColor,
    Func<Color> getInactiveColor) : IDrawable
{
    /// <summary>
    /// Draws the countdown ring on the provided canvas 
    /// within the specified dirty rectangle.
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        // Determine the raw outer radius based
        // on the smaller side of the control
        float rawRadius
            = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f;

        // Dynamically calculate stroke thickness
        // based on control size. Ensures the arc
        // is always visually proportional,
        // with a minimum thickness of 2 pixels.
        float strokeWidth
            = Math.Max(2f, rawRadius * 0.12f);

        // Calculate the inner radius to draw
        // the arc centered correctly
        float radius
            = rawRadius - strokeWidth / 2f;
        PointF center
            = dirtyRect.Center;

        // Set the stroke size for the canvas
        // to match the calculated thickness
        canvas.StrokeSize = strokeWidth;

        // Clamp the progress value to ensure
        // it's always in [0, 1]
        float progress = Math.Clamp(getProgress(), 0f, 1f);

        // Step 1: Draw the full inactive background ring
        canvas.StrokeColor = getInactiveColor();
        canvas.DrawCircle(center, radius);

        // Step 2: If there's any progress,
        // draw the active arc over it
        if (progress <= 0f)
        {
            return;
        }

        // The countdown arc moves counter-clockwise
        // (so the arc "shrinks" over time)
        float endAngle = 270f;
        float startAngle = endAngle + 360f * (1 - progress);

        // Draw the remaining time arc with the active color
        canvas.StrokeColor = getActiveColor();
        canvas.DrawArc(
            center.X - radius,
            center.Y - radius,
            radius * 2,
            radius * 2,
            startAngle,
            endAngle,
            false,
            false
        );
    }
}

