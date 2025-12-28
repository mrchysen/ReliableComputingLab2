using ScottPlot;
using System.Numerics;

// Система 1 x1 > 0 x2 > 0 

Func<double, double, bool> system1 = (x1, x2) =>
    x2 >= 2 * x1 - 2 &&
    x2 >= -3 * x1 - 2 &&
    x1 >= 2 * x2 - 1 &&
    x1 >= (-3 * x2 - 1) / 2;

// Система 2 x1 < 0 x2 > 0
Func<double, double, bool> system2 = (x1, x2) =>
    x2 >= - 2 * x1 - 2 &&
    x2 >= 3 * x1 - 2 &&
    x1 <= -2 * x2 + 1 &&
    x1 <= (3 * x2 + 1) / 2;

// Система 3 x1 < 0 x2 < 0
Func<double, double, bool> system3 = (x1, x2) =>
    x2 <= 2 + 2 * x1 &&
    x2 <= -3 * x1 + 2 &&
    x1 <= 2 * x2 + 1 &&
    x1 <= (-3 * x2 + 1) / 2;

// Система 4 x1 > 0 x2 < 0
Func<double, double, bool> system4 = (x1, x2) =>
    x2 <= 2 - 2 * x1 &&
    x2 <= 3 * x1 + 2 &&
    x1 >= -2 * x2 - 1 &&
    x1 >= (3 * x2 - 1) / 2;

var pointCount = 1024;
Point[,] points = new Point[pointCount + 1,pointCount + 1];
var from = -10.0;
var to = 10.0;
var delta = (to - from) / pointCount;

for (int i = 0; i < pointCount + 1; i++)
{
    for (int j = 0; j < pointCount + 1; j++)
    {
        var x = from + j * delta;
        var y = to - i * delta;
        points[i, j] = new Point(x, y);
    }
}

//PrintArea(points, pointCount);

var areaPoints = GetAllPointsInArea(
    points,
    pointCount,
    system1,
    system2,
    system3,
    system4);

Plot myPlot = new();

var standartColor = Color.RandomHue();
foreach (var p in areaPoints)
{
    var m = myPlot.Add.Marker(p.X, p.Y);
    m.MarkerSize = 5f;
    m.Color = standartColor;
}

// Точки посчитаны в отчете методом Крамера
var poly = myPlot.Add.Polygon([
        new Coordinates(-4,-3.5),
        new Coordinates(-4,3.5),
        new Coordinates(4,3.5),
        new Coordinates(4,-3.5)]);
poly.FillColor = new Color(0, 0, 255, 68);

Interval[][] A = [
    [new(2,3), new(-1,1)],
    [new(-1,2), new(2,3)]
    ];

Interval[] b = [
    new(-2,2),
    new (-1,1),
    ];

var monteCarloPoints = SolveWithMonteCarlo(A, b, 12_500_00);

foreach (var p in monteCarloPoints)
{
    var m = myPlot.Add.Marker(p.X, p.Y);
    m.MarkerSize = 5f;
    m.MarkerShape = MarkerShape.OpenDiamond;
}

myPlot.GetImage(1024, 1024).SavePng("points1.png").LaunchFile();


List<Coordinates> SolveWithMonteCarlo(Interval[][] a, Interval[] vec, int iterations, double eps = 1.0e-8)
{
    List<Coordinates> result = new(iterations);

    for (int i = 0; i < iterations; i++)
    {
        var rnd = new Random();
        double[][] A = [
            [a[0][0].From + rnd.NextDouble() * Math.Abs(a[0][0].From - a[0][0].To), a[0][1].From + rnd.NextDouble() * Math.Abs(a[0][1].From - a[0][1].To)],
        [a[1][0].From + rnd.NextDouble() * Math.Abs(a[1][0].From - a[1][0].To), a[1][1].From + rnd.NextDouble() * Math.Abs(a[1][1].From - a[1][1].To)],
        ];

        double[] b = [vec[0].From + rnd.NextDouble() * Math.Abs(vec[0].From - vec[0].To), vec[1].From + rnd.NextDouble() * Math.Abs(vec[1].From - vec[1].To)];

        //Console.WriteLine($"Matrix:\n{A[0][0]} {A[0][1]}\n{A[1][0]} {A[1][1]}");
        //Console.WriteLine($"vec:\n{b[0]} {b[1]}");

        result.Add(SolveSIM(A, b, eps));
    }

    return result;
}

static Coordinates SolveSIM(double[][] A, double[] b, double eps)
{
    double[] x = [0d, 0d];
    double[] x_old = [0d, 0d];
    int it = 0;
    double norm = 0;

    do
    {
        x[0] = 1.0 / A[0][0] * (b[0] - A[0][1] * x_old[1]);
        x[1] = 1.0 / A[1][1] * (b[1] - A[1][0] * x_old[0]);

        norm = Math.Max(
            Math.Abs(x[0] - x_old[0]),
            Math.Abs(x[1] - x_old[1]));

        x_old[0] = x[0];
        x_old[1] = x[1];
    }
    while (norm > eps 
            && ++it < 1_000_000);

    return new(x_old[0], x_old[1]);
}

static void PrintArea(Point[,] points, int pointCount)
{
    for (int i = 0; i < pointCount + 1; i++)
    {
        for (int j = 0; j < pointCount + 1; j++)
        {
            Console.Write(points[i, j].ToString() + " ");
        }
        Console.WriteLine();
    }
}

static List<Point> GetAllPointsInArea(
    Point[,] points,
    int pointCount,
    Func<double,double, bool> s1,
    Func<double, double, bool> s2,
    Func<double, double, bool> s3,
    Func<double, double, bool> s4)
{
    List<Point> resultPoints = [];

    Func<double, double, bool> condition = (x, y) =>
    {
        if (x >= 0 && y >= 0) return s1(x, y);
        else if (x <= 0 && y >= 0) return s2(x, y);
        else if (x <= 0 && y <= 0) return s3(x, y);
        else if (x >= 0 && y <= 0) return s4(x, y);
        return false;
    };

    for (int i = 0; i < pointCount + 1; i++)
    {
        for (int j = 0; j < pointCount + 1; j++)
        {
            if (condition(points[i, j].X, points[i, j].Y))
                resultPoints.Add(points[i, j]);
        }
    }

    return resultPoints;
}

public record Interval(double From, double To);

public record Point(double X, double Y)
{
    public override string ToString() => $"({X}, {Y})";
}