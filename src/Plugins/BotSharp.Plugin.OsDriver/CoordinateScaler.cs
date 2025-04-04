namespace BotSharp.Plugin.OsDriver;

public enum ScalingSource
{
    COMPUTER,
    API,
    // Add other possible values if needed
}

public class Dimension
{
    public int Width { get; set; }
    public int Height { get; set; }

    public Dimension(int width, int height)
    {
        Width = width;
        Height = height;
    }
}

public class ToolError : Exception
{
    public ToolError(string message) : base(message)
    {
    }
}

public class CoordinateScaler
{
    private bool _scalingEnabled;
    private int _width;
    private int _height;

    private static readonly List<Dimension> MaxScalingTargets = new List<Dimension>
    {
        new Dimension(1024, 768), //XGA 4:3
        new Dimension(1280, 800), //WXGA 16:10
        new Dimension(1366, 768), //FWXGA 16:9
        // Add more dimensions as needed
    };

    public CoordinateScaler(bool scalingEnabled, int width, int height)
    {
        _scalingEnabled = scalingEnabled;
        _width = width;
        _height = height;
    }

    public (int x, int y) ScaleCoordinates(ScalingSource source, int x, int y)
    {
        if (!_scalingEnabled)
            return (x, y);

        double ratio = (double)_width / _height;
        Dimension targetDimension = null;

        foreach (var dimension in MaxScalingTargets)
        {
            if (Math.Abs((double)dimension.Width / dimension.Height - ratio) < 0.02)
            {
                if (dimension.Width < _width)
                {
                    targetDimension = dimension;
                }
                break;
            }
        }

        if (targetDimension == null)
            return (x, y);

        double xScalingFactor = (double)targetDimension.Width / _width;
        double yScalingFactor = (double)targetDimension.Height / _height;

        if (source == ScalingSource.API)
        {
            if (x > _width || y > _height)
                throw new ToolError($"Coordinates {x}, {y} are out of bounds");

            // Scale up
            int newX = (int)Math.Round(x / xScalingFactor);
            int newY = (int)Math.Round(y / yScalingFactor);
            return (newX, newY);
        }
        else
        {
            // Scale down
            int newX = (int)Math.Round(x * xScalingFactor);
            int newY = (int)Math.Round(y * yScalingFactor);
            return (newX, newY);
        }
    }
}
