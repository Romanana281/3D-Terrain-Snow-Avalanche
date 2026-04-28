using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Project4
{
    public partial class Viewer : UserControl
    {
        private Point lastMousePosition;
        private bool isRotating = false;
        private Transform3DGroup transformGroup;
        private double totalRotationX = 0;
        private double totalRotationY = 0;
        private double cameraDistance = 10;

        public bool isSnow = false;
        private int SnowCount = 50;
        private List<Snow> snows = new List<Snow>();
        private List<Hills> hills = new List<Hills>();
        private Random rand = new Random();
        private GetCoordinates coordinates = new GetCoordinates();
        public DispatcherTimer snowTimer = new DispatcherTimer();
        private List<ModelVisual3D> hillVisuals = new List<ModelVisual3D>();
        private MeshGeometry3D terrainMesh;
        private ModelVisual3D terrainVisual;
        private List<Point3D> originalTerrainPositions;
        private bool terrainDeformed = false;
        private List<Avalanche> avalanches = new List<Avalanche>();
        private List<Comet> comets = new List<Comet>();

        public Viewer()
        {
            InitializeComponent();
            InitializeViewport();

            snowTimer.Interval = TimeSpan.FromMilliseconds(17);
            snowTimer.Tick += SnowTimer_Tick;
        }

        private void InitializeViewport()
        {
            transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 0)));
            transformGroup.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 0)));

            UpdateCameraPosition();

            AddLights();

            Viewport.MouseRightButtonDown += Viewport_MouseRightButtonDown;
            Viewport.MouseRightButtonUp += Viewport_MouseRightButtonUp;
            Viewport.MouseMove += Viewport_MouseMove;
            Viewport.MouseWheel += Viewport_MouseWheel;
        }

        private void SnowTimer_Tick(object sender, EventArgs e)
        {
            if (snows.Count < SnowCount)
            {
                CreateSnow();
            }

            if (hills.Count == 0)
            {
                CreateHills();
            }

            DeleteSnow();
            DrawSnow();
            UpdateSnow();
            DrawHills();
            UpdateAvalanches();
            UpdateComets();
        }

        private void CreateSnow()
        {
            var list = coordinates.get();
            double maxZ = list[1];
            double minX = list[2];
            double maxX = list[3];
            double minY = list[4];
            double maxY = list[5];

            int countToCreate = SnowCount - snows.Count;
            for (int i = 0; i < countToCreate; i++)
            {
                snows.Add(
                new Snow
                {
                    Points =
                        new Point3D
                        {
                            X = minX + (maxX - minX) * rand.NextDouble(),
                            Y = minY + (maxY - minY) * rand.NextDouble(),
                            Z = maxZ + 4
                        },
                    Size = 0.04 + rand.NextDouble() * 0.01,
                    Speed = 0.03 + rand.NextDouble() * 0.01,
                    Angle = (rand.NextDouble() - 0.5) * 0.015,
                });
            }
        }

        private void UpdateSnow()
        {
            var list = coordinates.get();

            double minX = list[2];
            double maxX = list[3];
            double minY = list[4];
            double maxY = list[5];

            for (int k = snows.Count - 1; k >= 0; k--)
            {
                var snow = snows[k];

                double newX =
                    rand.Next(0, 2) == 0 ? snow.Points.X + snow.Angle : snow.Points.X - snow.Angle;
                double newY =
                    rand.Next(0, 2) == 0 ? snow.Points.Y + snow.Angle : snow.Points.Y - snow.Angle;

                snow.Points = new Point3D
                {
                    X = newX,
                    Y = newY,
                    Z = snow.Points.Z - snow.Speed
                };

                int rows = coordinates.data.GetLength(0);
                int cols = coordinates.data.GetLength(1);

                double normalizedX =
                (snow.Points.X - minX) / (maxX - minX);
                double normalizedY =
                (snow.Points.Y - minY) / (maxY - minY);

                int i = (int)(normalizedX * (rows - 1));
                int j = (int)(normalizedY * (cols - 1));

                i = Math.Max(0, Math.Min(rows - 1, i));
                j = Math.Max(0, Math.Min(cols - 1, j));

                bool snowRemoved = false;

                if (snow.Points.Z <= coordinates.data[i, j])
                {
                    snowRemoved = true;
                }
                else
                {
                    for (int l = 0; l < hills.Count && !snowRemoved; l++)
                    {
                        foreach (var point in hills[l].pointsAboveLevel)
                        {
                            double distanceX = Math.Abs(snow.Points.X - point.X);
                            double distanceY = Math.Abs(snow.Points.Y - point.Y);

                            double totalDistance = Math.Sqrt(distanceX * distanceX + distanceY * distanceY);

                            if (snow.Points.Z <= point.Z && totalDistance < 0.2)
                            {
                                snowRemoved = true;
                                hills[l].count++;
                                break;
                            }
                        }
                    }
                }

                if (snowRemoved)
                {
                    snows.RemoveAt(k);
                }

            }
        }

        private void DrawSnow()
        {
            foreach (var snow in snows)
            {
                var mesh = new MeshGeometry3D();

                int segments = 8;
                int rings = 8;

                for (int i = 0; i <= segments; i++)
                {
                    double theta = 2 * Math.PI * i / segments;

                    for (int j = 0; j <= rings; j++)
                    {
                        double phi = Math.PI * j / rings;

                        double x = snow.Points.X + snow.Size * Math.Sin(phi) * Math.Cos(theta);
                        double y = snow.Points.Y + snow.Size * Math.Cos(phi);
                        double z = snow.Points.Z + snow.Size * Math.Sin(phi) * Math.Sin(theta);

                        mesh.Positions.Add(new Point3D(x, y, z));
                    }
                }

                for (int i = 0; i < segments; i++)
                {
                    for (int j = 0; j < rings; j++)
                    {
                        int v1 = i * (rings + 1) + j;
                        int v2 = i * (rings + 1) + (j + 1);
                        int v3 = (i + 1) * (rings + 1) + j;
                        int v4 = (i + 1) * (rings + 1) + (j + 1);

                        mesh.TriangleIndices.Add(v1);
                        mesh.TriangleIndices.Add(v3);
                        mesh.TriangleIndices.Add(v2);

                        mesh.TriangleIndices.Add(v2);
                        mesh.TriangleIndices.Add(v3);
                        mesh.TriangleIndices.Add(v4);
                    }
                }

                var material = new DiffuseMaterial(new SolidColorBrush(Colors.White));
                var model = new GeometryModel3D(mesh, material);

                var visual = new ModelVisual3D();
                visual.Content = model;
                visual.Transform = transformGroup;
                Viewport.Children.Add(visual);
            }
        }

        private void DeleteSnow()
        {
            var snowToRemove = new List<ModelVisual3D>();

            foreach (var visual in Viewport.Children.OfType<ModelVisual3D>().ToList())
            {
                if (visual.Content is GeometryModel3D geometryModel)
                {
                    if (geometryModel.Material is DiffuseMaterial diffuseMaterial)
                    {
                        if (diffuseMaterial.Brush is SolidColorBrush solidColorBrush)
                        {
                            if (solidColorBrush.Color == Colors.White)
                            {
                                snowToRemove.Add(visual);
                            }
                        }
                    }
                }
            }

            foreach (var visual in snowToRemove)
            {
                Viewport.Children.Remove(visual);
            }
        }

        private void CreateHills()
        {
            int rows = coordinates.data.GetLength(0);
            int cols = coordinates.data.GetLength(1);

            var list = coordinates.get();
            double minZ = list[0];
            double maxZ = list[1];
            double minX = list[2];
            double maxX = list[3];
            double minY = list[4];
            double maxY = list[5];

            double level = minZ + (maxZ - minZ) * 0.6;
            bool[,] visited = new bool[rows, cols];
            this.hills = new List<Hills>();

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (coordinates.data[i, j] >= level && !visited[i, j])
                    {
                        var hillPoints = new List<Point3D>();
                        var pointIndices = new Dictionary<Point, int>();

                        Queue<(int k, int m)> queue = new Queue<(int k, int m)>();
                        queue.Enqueue((i, j));
                        visited[i, j] = true;

                        var neighbors = new (int di, int dj)[]
                        {
                            (-1, 0), (1, 0), (0, -1), (0, 1)
                        };

                        while (queue.Count > 0)
                        {
                            var (k, m) = queue.Dequeue();

                            double x = minX + (maxX - minX) * k / (rows - 1);
                            double y = minY + (maxY - minY) * m / (cols - 1);
                            double z = coordinates.data[k, m];

                            hillPoints.Add(new Point3D(x, y, z));
                            pointIndices[new Point(k, m)] = hillPoints.Count - 1;

                            foreach (var (di, dj) in neighbors)
                            {
                                int ni = k + di;
                                int nj = m + dj;

                                if (ni >= 0 && ni < rows && nj >= 0 && nj < cols &&
                                    !visited[ni, nj] && coordinates.data[ni, nj] >= level * 0.95)
                                {
                                    visited[ni, nj] = true;
                                    queue.Enqueue((ni, nj));
                                }
                            }
                        }

                        if (hillPoints.Count >= 3)
                        {
                            this.hills.Add(new Hills
                            {
                                count = 0,
                                lastCount = 0,
                                pointsAboveLevel = hillPoints,
                                originalPoints = hillPoints
                                                    .Select(p =>
                                                        new Point3D(p.X, p.Y, p.Z))
                                                    .ToList(),
                                pointIndices = pointIndices,
                                isActive = true,
                                visuals = null,
                                avalancheTriggered = false
                            });

                        }
                    }
                }
            }
        }

        private void DrawHills()
        {

            foreach (var hill in hills)
            {

                if (hill.count <= 0 || !hill.isActive)
                    continue;

                if (hill.count >= Math.Min(25, 75 / hills.Count))
                {
                    TriggerAvalanche(hill);
                    hill.count = 0;
                    hill.lastCount = 0;
                    hill.isActive = false;
                    hill.pointsAboveLevel = hill.originalPoints
                       .Select(p => new Point3D(p.X, p.Y, p.Z))
                       .ToList();

                    if (hill.visuals != null)
                    {
                        foreach (var visual in hill.visuals)
                        {
                            if (Viewport.Children.Contains(visual))
                                Viewport.Children.Remove(visual);
                            hillVisuals.Remove(visual);
                        }
                        hill.visuals.Clear();
                    }
                    continue;
                }

                double minZ = double.MaxValue;
                double maxZ = double.MinValue;

                foreach (var point in hill.pointsAboveLevel)
                {
                    if (point.Z < minZ) minZ = point.Z;
                    if (point.Z > maxZ) maxZ = point.Z;
                }

                if (hill.lastCount != hill.count)
                {
                    double increment = (hill.count - hill.lastCount) * 0.01;

                    double centerX = hill.pointsAboveLevel.Average(p => p.X);
                    double centerY = hill.pointsAboveLevel.Average(p => p.Y);

                    for (int i = 0; i < hill.pointsAboveLevel.Count; i++)
                    {
                        double distance = Math.Sqrt(
                            Math.Pow(hill.pointsAboveLevel[i].X - centerX, 2) +
                            Math.Pow(hill.pointsAboveLevel[i].Y - centerY, 2));

                        double smoothFactor = Math.Max(0, 1 - distance / 2.0);
                        double actualIncrement = increment * smoothFactor;

                        hill.pointsAboveLevel[i] = new Point3D
                        {
                            X = hill.pointsAboveLevel[i].X,
                            Y = hill.pointsAboveLevel[i].Y,
                            Z = hill.pointsAboveLevel[i].Z + actualIncrement
                        };
                    }
                    hill.lastCount = hill.count;
                }

                var mesh = new MeshGeometry3D();

                for (int i = 0; i < hill.pointsAboveLevel.Count; i++)
                {
                    mesh.Positions.Add(hill.pointsAboveLevel[i]);

                    double textureZ = (hill.pointsAboveLevel[i].Z - minZ) / (maxZ - minZ);
                    mesh.TextureCoordinates.Add(new Point(0, textureZ));
                }

                int rows = coordinates.data.GetLength(0);
                int cols = coordinates.data.GetLength(1);

                for (int i = 0; i < rows - 1; i++)
                {
                    for (int j = 0; j < cols - 1; j++)
                    {
                        Point p1 = new Point(i, j);
                        Point p2 = new Point(i, j + 1);
                        Point p3 = new Point(i + 1, j);
                        Point p4 = new Point(i + 1, j + 1);

                        if (hill.pointIndices.ContainsKey(p1) && hill.pointIndices.ContainsKey(p2) &&
                            hill.pointIndices.ContainsKey(p3) && hill.pointIndices.ContainsKey(p4))
                        {
                            int v1 = hill.pointIndices[p1];
                            int v2 = hill.pointIndices[p2];
                            int v3 = hill.pointIndices[p3];
                            int v4 = hill.pointIndices[p4];

                            mesh.TriangleIndices.Add(v1);
                            mesh.TriangleIndices.Add(v3);
                            mesh.TriangleIndices.Add(v2);

                            mesh.TriangleIndices.Add(v2);
                            mesh.TriangleIndices.Add(v3);
                            mesh.TriangleIndices.Add(v4);
                        }
                    }
                }

                if (mesh.TriangleIndices.Count > 0)
                {
                    CalculateNormal(mesh);

                    var gradient = new LinearGradientBrush();
                    gradient.StartPoint = new Point(0, 0);
                    gradient.EndPoint = new Point(0, 1);
                    gradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
                    gradient.GradientStops.Add(new GradientStop(Colors.LightGray, 1.0));

                    var material = new DiffuseMaterial(gradient);
                    var model = new GeometryModel3D(mesh, material);

                    var visual = new ModelVisual3D();
                    visual.Content = model;
                    visual.Transform = transformGroup;
                    Viewport.Children.Add(visual);

                    if (hill.visuals == null)
                        hill.visuals = new List<ModelVisual3D>();
                    hill.visuals.Add(visual);
                    hillVisuals.Add(visual);
                }
            }
        }

        private void DeleteHills()
        {
            if (hillVisuals.Count == 0) return;

            foreach (var v in hillVisuals)
            {
                if (Viewport.Children.Contains(v))
                    Viewport.Children.Remove(v);
            }
            hillVisuals.Clear();
            hills.Clear();
            ClearAvalanches();
            ResetTerrainShape();
            ClearComets();
        }

        public void SnowStop()
        {
            snowTimer.Stop();
            DeleteSnow();
            DeleteHills();
            snows.Clear();
            hillVisuals.Clear();
            hills.Clear();
            ClearAvalanches();
            ResetTerrainShape();
            ClearComets();
        }

        private void TriggerAvalanche(Hills hill)
        {
            if (hill == null || hill.pointsAboveLevel == null || hill.pointsAboveLevel.Count == 0)
                return;

            if (hill.avalancheTriggered)
                return;

            var avalanchePoints = hill.pointsAboveLevel
                .Select(p => new Point3D(p.X, p.Y, p.Z))
                .ToList();

            var avalanche = new Avalanche
            {
                Points = avalanchePoints,
                PointIndices = hill.pointIndices != null
                    ? new Dictionary<Point, int>(hill.pointIndices)
                    : new Dictionary<Point, int>(),
                Mass = hill.count,
                HeightOffsets = new List<double>(),
                Progress = 0,
                Thickness = 0,
                StretchAmount = 0
            };

            double threshold = Math.Min(20, Math.Max(1, 70 / Math.Max(1, hills.Count)));
            double normalizedMass = Math.Min(1.0, threshold > 0 ? hill.count / threshold : 0);
            normalizedMass = Math.Max(normalizedMass, 0.1);

            avalanche.Speed = 0.01 + normalizedMass * 0.04;

            double centerX = avalanchePoints.Average(p => p.X);
            double centerY = avalanchePoints.Average(p => p.Y);

            for (int i = 0; i < avalanchePoints.Count; i++)
            {
                var point = avalanchePoints[i];
                double terrainZ = SampleCurrentTerrainHeight(point.X, point.Y);
                double offset = Math.Max(0.01, point.Z - terrainZ);
                avalanche.HeightOffsets.Add(offset);
            }

            avalanche.Thickness = avalanche.HeightOffsets.Count > 0
                ? avalanche.HeightOffsets.Average()
                : 0.05;

            avalanche.TargetPoint = GetLowestTerrainPoint();
            avalanche.Direction = CalculateAvalancheDirection(centerX, centerY, avalanche.TargetPoint);
            avalanche.InitialDistanceToTarget =
                Math.Max(0.001, Math.Sqrt(Math.Pow(centerX - avalanche.TargetPoint.X, 2) +
                                          Math.Pow(centerY - avalanche.TargetPoint.Y, 2)));
            avalanche.ReachedBottom = false;

            avalanches.Add(avalanche);
            hill.avalancheTriggered = true;
        }

        private void UpdateAvalanches()
        {
            if (avalanches.Count == 0 || coordinates.data == null)
                return;

            var bounds = coordinates.get();
            double minZ = bounds[0];
            double minX = bounds[2];
            double maxX = bounds[3];
            double minY = bounds[4];
            double maxY = bounds[5];

            foreach (var avalanche in avalanches)
            {
                if (avalanche.Points == null || avalanche.Points.Count == 0)
                    continue;

                if (avalanche.Speed <= 0 && avalanche.ReachedBottom)
                    continue;

                double lowestTerrain = double.MaxValue;

                double centerX = avalanche.Points.Average(p => p.X);
                double centerY = avalanche.Points.Average(p => p.Y);
                avalanche.Direction = CalculateAvalancheDirection(centerX, centerY, avalanche.TargetPoint);
                double distanceToTarget = Math.Sqrt(
                    Math.Pow(centerX - avalanche.TargetPoint.X, 2) +
                    Math.Pow(centerY - avalanche.TargetPoint.Y, 2));

                double approachRatio = 1.0 - Math.Min(1.0, distanceToTarget / avalanche.InitialDistanceToTarget);
                double dynamicStretch = approachRatio > 0 ? approachRatio * avalanche.Thickness * 4 : 0;

                for (int i = 0; i < avalanche.Points.Count; i++)
                {
                    var current = avalanche.Points[i];

                    double newX = current.X + avalanche.Direction.X * avalanche.Speed;
                    double newY = current.Y + avalanche.Direction.Y * avalanche.Speed;

                    newX = Math.Max(minX, Math.Min(maxX, newX));
                    newY = Math.Max(minY, Math.Min(maxY, newY));

                    double terrainZ = SampleCurrentTerrainHeight(newX, newY);
                    lowestTerrain = Math.Min(lowestTerrain, terrainZ);
                    double heightOffset = avalanche.HeightOffsets != null && avalanche.HeightOffsets.Count > i
                       ? avalanche.HeightOffsets[i]
                       : avalanche.Thickness;

                    if (avalanche.ReachedBottom)
                    {
                        heightOffset = Math.Max(0.0005, heightOffset * 0.95);
                        avalanche.HeightOffsets[i] = heightOffset;
                    }

                    double snowTop = Math.Min(bounds[1], terrainZ + heightOffset);

                    avalanche.Points[i] = new Point3D(newX, newY, snowTop);
                }

                avalanche.Progress += avalanche.Speed;
                BuildOrUpdateAvalancheVisual(avalanche);

                if (dynamicStretch > 0.001)
                {
                    double targetStretch = Math.Max(dynamicStretch, avalanche.StretchAmount);
                    avalanche.StretchAmount = targetStretch;
                }

                if (!avalanche.ReachedBottom)
                {
                    double reachRadius = Math.Max(0.2, avalanche.Thickness * 6);
                    if (distanceToTarget <= reachRadius ||
                        Math.Abs(lowestTerrain - avalanche.TargetPoint.Z) <= avalanche.Thickness * 0.5)
                    {
                        avalanche.ReachedBottom = true;
                    }
                }

                if (avalanche.ReachedBottom)
                {
                    avalanche.Speed = Math.Max(0, avalanche.Speed - 0.0015);
                    avalanche.StretchAmount = Math.Min(avalanche.Thickness * 2, avalanche.StretchAmount + 0.01);
                }
            }

            UpdateTerrainStretchFromAvalanches();
        }

        private Point3D GetLowestTerrainPoint()
        {
            if (coordinates.data == null)
                return new Point3D(0, 0, 0);

            int rows = coordinates.data.GetLength(0);
            int cols = coordinates.data.GetLength(1);
            double minHeight = double.MaxValue;
            int minI = 0;
            int minJ = 0;

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (coordinates.data[i, j] < minHeight)
                    {
                        minHeight = coordinates.data[i, j];
                        minI = i;
                        minJ = j;
                    }
                }
            }

            var bounds = coordinates.get();
            double minX = bounds[2];
            double maxX = bounds[3];
            double minY = bounds[4];
            double maxY = bounds[5];

            double x = minX + (maxX - minX) * minI / Math.Max(1, rows - 1);
            double y = minY + (maxY - minY) * minJ / Math.Max(1, cols - 1);

            return new Point3D(x, y, minHeight);
        }

        private Vector CalculateAvalancheDirection(double centerX, double centerY, Point3D targetPoint)
        {
            Vector downhill = GetDownhillDirection(centerX, centerY);
            Vector towardTarget = new Vector(targetPoint.X - centerX, targetPoint.Y - centerY);

            if (towardTarget.Length > 0.0001)
            {
                towardTarget.Normalize();
            }

            Vector blended;
            if (downhill.Length < 0.001)
            {
                blended = towardTarget;
            }
            else
            {
                blended = downhill * 0.7 + towardTarget * 0.3;
            }

            if (blended.Length < 0.0001)
            {
                blended = new Vector(0, -1);
            }
            else
            {
                blended.Normalize();
            }

            return blended;
        }

        private void BuildOrUpdateAvalancheVisual(Avalanche avalanche)
        {
            var mesh = BuildAvalancheMesh(avalanche);
            if (mesh == null || mesh.Positions.Count == 0 || mesh.TriangleIndices.Count == 0)
                return;

            CalculateNormal(mesh);

            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1)
            };
            gradient.GradientStops.Add(new GradientStop(Colors.White, 0.0));
            gradient.GradientStops.Add(new GradientStop(Color.FromRgb(220, 220, 220), 1.0));

            var material = new DiffuseMaterial(gradient);
            var model = new GeometryModel3D(mesh, material)
            {
                BackMaterial = material
            };

            if (avalanche.Visual == null)
            {
                avalanche.Visual = new ModelVisual3D();
                avalanche.Visual.Transform = transformGroup;
                Viewport.Children.Add(avalanche.Visual);
            }

            avalanche.Visual.Content = model;
        }

        private MeshGeometry3D BuildAvalancheMesh(Avalanche avalanche)
        {
            var mesh = new MeshGeometry3D();

            if (avalanche.Points == null || avalanche.PointIndices == null || coordinates.data == null)
                return mesh;

            double minZ = avalanche.Points.Min(p => p.Z);
            double maxZ = avalanche.Points.Max(p => p.Z);
            double range = Math.Max(0.0001, maxZ - minZ);

            foreach (var point in avalanche.Points)
            {
                mesh.Positions.Add(point);
                double textureZ = (point.Z - minZ) / range;
                mesh.TextureCoordinates.Add(new Point(0, textureZ));
            }

            int rows = coordinates.data.GetLength(0);
            int cols = coordinates.data.GetLength(1);

            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < cols - 1; j++)
                {
                    Point p1 = new Point(i, j);
                    Point p2 = new Point(i, j + 1);
                    Point p3 = new Point(i + 1, j);
                    Point p4 = new Point(i + 1, j + 1);

                    if (avalanche.PointIndices.ContainsKey(p1) && avalanche.PointIndices.ContainsKey(p2) &&
                        avalanche.PointIndices.ContainsKey(p3) && avalanche.PointIndices.ContainsKey(p4))
                    {
                        int v1 = avalanche.PointIndices[p1];
                        int v2 = avalanche.PointIndices[p2];
                        int v3 = avalanche.PointIndices[p3];
                        int v4 = avalanche.PointIndices[p4];

                        mesh.TriangleIndices.Add(v1);
                        mesh.TriangleIndices.Add(v3);
                        mesh.TriangleIndices.Add(v2);

                        mesh.TriangleIndices.Add(v2);
                        mesh.TriangleIndices.Add(v3);
                        mesh.TriangleIndices.Add(v4);
                    }
                }
            }

            return mesh;
        }

        private double SampleTerrainHeight(double x, double y)
        {
            if (coordinates.data == null)
                return 0;

            var bounds = coordinates.get();
            double minX = bounds[2];
            double maxX = bounds[3];
            double minY = bounds[4];
            double maxY = bounds[5];

            int rows = coordinates.data.GetLength(0);
            int cols = coordinates.data.GetLength(1);

            double normalizedX = (x - minX) / (maxX - minX);
            double normalizedY = (y - minY) / (maxY - minY);

            double scaledI = normalizedX * (rows - 1);
            double scaledJ = normalizedY * (cols - 1);

            scaledI = Math.Max(0, Math.Min(rows - 1.0001, scaledI));
            scaledJ = Math.Max(0, Math.Min(cols - 1.0001, scaledJ));

            int i0 = (int)Math.Floor(scaledI);
            int j0 = (int)Math.Floor(scaledJ);
            int i1 = Math.Min(rows - 1, i0 + 1);
            int j1 = Math.Min(cols - 1, j0 + 1);

            double tx = scaledI - i0;
            double ty = scaledJ - j0;

            double h00 = coordinates.data[i0, j0];
            double h10 = coordinates.data[i1, j0];
            double h01 = coordinates.data[i0, j1];
            double h11 = coordinates.data[i1, j1];

            double h0 = h00 * (1 - tx) + h10 * tx;
            double h1 = h01 * (1 - tx) + h11 * tx;

            return h0 * (1 - ty) + h1 * ty;
        }

        private double SampleCurrentTerrainHeight(double x, double y)
        {
            if (terrainMesh == null || terrainMesh.Positions.Count == 0 || coordinates.data == null)
            {
                return SampleTerrainHeight(x, y);
            }

            var bounds = coordinates.get();
            double minX = bounds[2];
            double maxX = bounds[3];
            double minY = bounds[4];
            double maxY = bounds[5];

            int rows = coordinates.data.GetLength(0);
            int cols = coordinates.data.GetLength(1);

            double normalizedX = (x - minX) / (maxX - minX);
            double normalizedY = (y - minY) / (maxY - minY);

            double scaledI = normalizedX * (rows - 1);
            double scaledJ = normalizedY * (cols - 1);

            scaledI = Math.Max(0, Math.Min(rows - 1.0001, scaledI));
            scaledJ = Math.Max(0, Math.Min(cols - 1.0001, scaledJ));

            int i0 = (int)Math.Floor(scaledI);
            int j0 = (int)Math.Floor(scaledJ);
            int i1 = Math.Min(rows - 1, i0 + 1);
            int j1 = Math.Min(cols - 1, j0 + 1);

            double tx = scaledI - i0;
            double ty = scaledJ - j0;

            double h00 = terrainMesh.Positions[i0 * cols + j0].Z;
            double h10 = terrainMesh.Positions[i1 * cols + j0].Z;
            double h01 = terrainMesh.Positions[i0 * cols + j1].Z;
            double h11 = terrainMesh.Positions[i1 * cols + j1].Z;

            double h0 = h00 * (1 - tx) + h10 * tx;
            double h1 = h01 * (1 - tx) + h11 * tx;

            return h0 * (1 - ty) + h1 * ty;
        }

        private Vector GetDownhillDirection(double x, double y)
        {
            if (coordinates.data == null)
                return new Vector(0, -1);

            var bounds = coordinates.get();
            double stepX = (bounds[3] - bounds[2]) / Math.Max(1, coordinates.data.GetLength(0) - 1);
            double stepY = (bounds[5] - bounds[4]) / Math.Max(1, coordinates.data.GetLength(1) - 1);
            double step = Math.Max(stepX, stepY) * 2;

            double hxPlus = SampleCurrentTerrainHeight(x + step, y);
            double hxMinus = SampleCurrentTerrainHeight(x - step, y);
            double hyPlus = SampleCurrentTerrainHeight(x, y + step);
            double hyMinus = SampleCurrentTerrainHeight(x, y - step);

            Vector direction = new Vector(hxMinus - hxPlus, hyMinus - hyPlus);

            if (direction.Length < 0.0001)
            {
                return new Vector(0, -1);
            }

            direction.Normalize();
            return direction;
        }

        private void ClearAvalanches()
        {
            foreach (var avalanche in avalanches)
            {
                if (avalanche.Visual != null && Viewport.Children.Contains(avalanche.Visual))
                {
                    Viewport.Children.Remove(avalanche.Visual);
                }
            }

            avalanches.Clear();
            ResetTerrainShape();
        }

        private void ResetTerrainShape()
        {
            if (terrainMesh == null || originalTerrainPositions == null)
                return;

            for (int i = 0; i < terrainMesh.Positions.Count && i < originalTerrainPositions.Count; i++)
            {
                terrainMesh.Positions[i] = originalTerrainPositions[i];
            }

            CalculateNormal(terrainMesh);
            terrainDeformed = false;
        }

        private void UpdateTerrainStretchFromAvalanches()
        {
            if (terrainMesh == null || originalTerrainPositions == null)
                return;

            if (avalanches.All(a => a.StretchAmount <= 0))
            {
                if (terrainDeformed)
                    ResetTerrainShape();
                return;
            }

            var bounds = coordinates.get();
            double minZ = bounds[0];
            int vertexCount = Math.Min(terrainMesh.Positions.Count, originalTerrainPositions.Count);
            double[] pulls = new double[vertexCount];
            bool hasStretch = false;

            foreach (var avalanche in avalanches)
            {
                if (avalanche.StretchAmount <= 0 || avalanche.Points == null || avalanche.Points.Count == 0)
                    continue;

                double minX = avalanche.Points.Min(p => p.X);
                double maxX = avalanche.Points.Max(p => p.X);
                double minY = avalanche.Points.Min(p => p.Y);
                double maxY = avalanche.Points.Max(p => p.Y);
                double centerX = (minX + maxX) / 2.0;
                double centerY = (minY + maxY) / 2.0;
                double halfWidth = Math.Max(0.001, (maxX - minX) / 2.0);
                double halfHeight = Math.Max(0.001, (maxY - minY) / 2.0);

                for (int i = 0; i < vertexCount; i++)
                {
                    var basePoint = originalTerrainPositions[i];
                    double normalizedX = Math.Abs((basePoint.X - centerX) / halfWidth);
                    double normalizedY = Math.Abs((basePoint.Y - centerY) / halfHeight);

                    double distance = Math.Sqrt(normalizedX * normalizedX + normalizedY * normalizedY);
                    if (distance >= 1.2)
                        continue;

                    double influence = Math.Max(0, 1 - (distance / 1.2));
                    double pull = avalanche.StretchAmount * influence * influence * 1.4;

                    if (pull > pulls[i])
                    {
                        pulls[i] = pull;
                        hasStretch = true;
                    }
                }
            }

            if (!hasStretch)
            {
                if (terrainDeformed)
                    ResetTerrainShape();
                return;
            }

            for (int i = 0; i < vertexCount; i++)
            {
                var basePoint = originalTerrainPositions[i];
                double newZ = Math.Max(minZ - 1, basePoint.Z - pulls[i]);
                terrainMesh.Positions[i] = new Point3D(basePoint.X, basePoint.Y, newZ);
            }

            CalculateNormal(terrainMesh);
            terrainDeformed = true;
        }

        private bool TryLaunchComet(Point clickPoint)
        {
            Hills targetHill = null;
            Point3D hitPoint = new Point3D();

            VisualTreeHelper.HitTest(
                Viewport,
                null,
                result =>
                {
                    if (result is RayMeshGeometry3DHitTestResult meshResult)
                    {
                        var candidate = FindHillAtPoint(meshResult.PointHit);
                        if (candidate != null)
                        {
                            targetHill = candidate;
                            hitPoint = meshResult.PointHit;
                            return HitTestResultBehavior.Stop;
                        }
                    }
                    return HitTestResultBehavior.Continue;
                },
                new PointHitTestParameters(clickPoint));

            if (targetHill == null)
                return false;

            LaunchComet(targetHill, hitPoint);
            return true;
        }

        private Hills FindHillAtPoint(Point3D point)
        {
            const double maxDistanceSquared = 0.4 * 0.4;

            foreach (var hill in hills)
            {
                if (hill == null || hill.pointsAboveLevel == null || hill.pointsAboveLevel.Count == 0)
                    continue;
                if (!hill.isActive || hill.count <= 0 || hill.avalancheTriggered)
                    continue;

                foreach (var hillPoint in hill.pointsAboveLevel)
                {
                    double dx = hillPoint.X - point.X;
                    double dy = hillPoint.Y - point.Y;
                    if ((dx * dx + dy * dy) <= maxDistanceSquared)
                    {
                        return hill;
                    }
                }
            }

            return null;
        }

        private void LaunchComet(Hills hill, Point3D impactPoint)
        {
            if (comets.Any(c => !c.Impacted && c.TargetHill == hill))
                return;

            double offsetX = 1.5 + rand.NextDouble();
            double offsetY = -1.5 - rand.NextDouble();
            double offsetZ = 4 + rand.NextDouble() * 2;

            var startPoint = new Point3D(
                impactPoint.X + offsetX,
                impactPoint.Y + offsetY,
                impactPoint.Z + offsetZ);

            var comet = new Comet
            {
                Start = startPoint,
                Target = new Point3D(impactPoint.X, impactPoint.Y, impactPoint.Z + 0.3),
                Position = startPoint,
                Progress = 0,
                Speed = 0.035 + rand.NextDouble() * 0.025,
                Impacted = false,
                LifeAfterImpact = 0.5,
                TargetHill = hill
            };

            comets.Add(comet);
        }

        private void UpdateComets()
        {
            if (comets.Count == 0)
                return;

            for (int i = comets.Count - 1; i >= 0; i--)
            {
                var comet = comets[i];
                comet.Progress = Math.Min(1.0, comet.Progress + comet.Speed);

                double t = comet.Progress;
                comet.Position = new Point3D(
                    comet.Start.X + (comet.Target.X - comet.Start.X) * t,
                    comet.Start.Y + (comet.Target.Y - comet.Start.Y) * t,
                    comet.Start.Z + (comet.Target.Z - comet.Start.Z) * t);

                UpdateCometVisual(comet);

                if (!comet.Impacted && t >= 1.0)
                {
                    OnCometImpact(comet);
                }

                if (comet.Impacted)
                {
                    comet.LifeAfterImpact -= 0.02;
                    if (comet.LifeAfterImpact <= 0)
                    {
                        RemoveCometVisual(comet);
                        comets.RemoveAt(i);
                        continue;
                    }
                }
            }
        }

        private void UpdateCometVisual(Comet comet)
        {
            var mesh = CreateSphereMesh(comet.Position, 0.2, 10);
            var material = new DiffuseMaterial(new SolidColorBrush(Color.FromRgb(255, 160, 0)));
            var model = new GeometryModel3D(mesh, material)
            {
                BackMaterial = material
            };

            if (comet.Visual == null)
            {
                comet.Visual = new ModelVisual3D();
                comet.Visual.Transform = transformGroup;
                Viewport.Children.Add(comet.Visual);
            }

            comet.Visual.Content = model;
        }

        private void RemoveCometVisual(Comet comet)
        {
            if (comet.Visual != null && Viewport.Children.Contains(comet.Visual))
            {
                Viewport.Children.Remove(comet.Visual);
            }
            comet.Visual = null;
        }

        private void OnCometImpact(Comet comet)
        {
            comet.Impacted = true;
            comet.LifeAfterImpact = 0.2;
            SpawnSnowBurst(comet.Target);

            if (comet.TargetHill != null)
            {
                comet.TargetHill.avalancheTriggered = false;
                if (comet.TargetHill.count <= 0)
                    comet.TargetHill.count = 1;

                TriggerAvalanche(comet.TargetHill);

                comet.TargetHill.count = 0;
                comet.TargetHill.lastCount = 0;
                comet.TargetHill.isActive = false;
                comet.TargetHill.pointsAboveLevel = comet.TargetHill.originalPoints
                    .Select(p => new Point3D(p.X, p.Y, p.Z))
                    .ToList();

                if (comet.TargetHill.visuals != null)
                {
                    foreach (var visual in comet.TargetHill.visuals.ToList())
                    {
                        if (Viewport.Children.Contains(visual))
                            Viewport.Children.Remove(visual);
                        hillVisuals.Remove(visual);
                    }
                    comet.TargetHill.visuals.Clear();
                }
            }
        }

        private void SpawnSnowBurst(Point3D targetPoint)
        {
            double baseZ = targetPoint.Z + 0.5;
            int burstCount = 6;
            var bounds = coordinates.get();
            double minX = bounds[2];
            double maxX = bounds[3];
            double minY = bounds[4];
            double maxY = bounds[5];

            for (int i = 0; i < burstCount; i++)
            {
                double offsetX = (rand.NextDouble() - 0.5) * 0.8;
                double offsetY = (rand.NextDouble() - 0.5) * 0.8;
                double x = Math.Max(minX, Math.Min(maxX, targetPoint.X + offsetX));
                double y = Math.Max(minY, Math.Min(maxY, targetPoint.Y + offsetY));

                snows.Add(new Snow
                {
                    Points = new Point3D(
                        x,
                        y,
                        baseZ + rand.NextDouble() * 0.4),
                    Size = 0.05 + rand.NextDouble() * 0.02,
                    Speed = 0.04 + rand.NextDouble() * 0.02,
                    Angle = (rand.NextDouble() - 0.5) * 0.04,
                });
            }
        }

        private void ClearComets()
        {
            if (comets.Count == 0)
                return;

            foreach (var comet in comets)
            {
                RemoveCometVisual(comet);
            }

            comets.Clear();
        }

        private MeshGeometry3D CreateSphereMesh(Point3D center, double radius, int segments)
        {
            var mesh = new MeshGeometry3D();

            for (int i = 0; i <= segments; i++)
            {
                double theta = 2 * Math.PI * i / segments;
                for (int j = 0; j <= segments; j++)
                {
                    double phi = Math.PI * j / segments;
                    double x = center.X + radius * Math.Sin(phi) * Math.Cos(theta);
                    double y = center.Y + radius * Math.Cos(phi);
                    double z = center.Z + radius * Math.Sin(phi) * Math.Sin(theta);
                    mesh.Positions.Add(new Point3D(x, y, z));
                }
            }

            int ringSize = segments + 1;
            for (int i = 0; i < segments; i++)
            {
                for (int j = 0; j < segments; j++)
                {
                    int v1 = i * ringSize + j;
                    int v2 = v1 + 1;
                    int v3 = v1 + ringSize;
                    int v4 = v3 + 1;

                    mesh.TriangleIndices.Add(v1);
                    mesh.TriangleIndices.Add(v3);
                    mesh.TriangleIndices.Add(v2);

                    mesh.TriangleIndices.Add(v2);
                    mesh.TriangleIndices.Add(v3);
                    mesh.TriangleIndices.Add(v4);
                }
            }

            return mesh;
        }

        private void UpdateCameraPosition()
        {
            var z = (coordinates.get()[0] + coordinates.get()[1] / 2);
            Camera.Position = new Point3D(0, cameraDistance, z);
            Camera.LookDirection = new Vector3D(0, -1, 0);
            Camera.UpDirection = new Vector3D(0, 0, 1);
        }

        private void AddLights()
        {
            var ambientLight = new AmbientLight(Color.FromRgb(128, 128, 128));
            var ambientVisual = new ModelVisual3D();
            ambientVisual.Content = ambientLight;
            Viewport.Children.Add(ambientVisual);

            var directionalLight = new DirectionalLight(Color.FromRgb(255, 255, 255), new Vector3D(-1, -1, -1));
            var directionalVisual = new ModelVisual3D();
            directionalVisual.Content = directionalLight;
            Viewport.Children.Add(directionalVisual);
        }

        public void Create(double[,] data, double minX, double maxX, double minY, double maxY)
        {
            while (Viewport.Children.Count > 2)
            {
                Viewport.Children.RemoveAt(Viewport.Children.Count - 1);
            }

            terrainMesh = null;
            terrainVisual = null;
            originalTerrainPositions = null;
            terrainDeformed = false;

            int rows = data.GetLength(0);
            int cols = data.GetLength(1);

            double minZ = double.MaxValue;
            double maxZ = double.MinValue;
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (data[i, j] < minZ) minZ = data[i, j];
                    if (data[i, j] > maxZ) maxZ = data[i, j];
                }
            }

            coordinates.set(minZ, maxZ, minX, maxX, minY, maxY, data);

            var mesh = new MeshGeometry3D();

            for (int i = 0; i < rows; i++)
            {
                double x = minX + (maxX - minX) * i / (rows - 1);
                for (int j = 0; j < cols; j++)
                {
                    double y = minY + (maxY - minY) * j / (cols - 1);
                    double z = data[i, j];
                    mesh.Positions.Add(new Point3D(x, y, z));

                    double textureZ = (z - minZ) / (maxZ - minZ);
                    mesh.TextureCoordinates.Add(new Point(0, textureZ));
                }
            }

            for (int i = 0; i < rows - 1; i++)
            {
                for (int j = 0; j < cols - 1; j++)
                {
                    int v1 = i * cols + j;
                    int v2 = i * cols + (j + 1);
                    int v3 = (i + 1) * cols + j;
                    int v4 = (i + 1) * cols + (j + 1);

                    mesh.TriangleIndices.Add(v1);
                    mesh.TriangleIndices.Add(v3);
                    mesh.TriangleIndices.Add(v2);

                    mesh.TriangleIndices.Add(v2);
                    mesh.TriangleIndices.Add(v3);
                    mesh.TriangleIndices.Add(v4);
                }
            }

            CalculateNormal(mesh);

            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new Point(0, 0);
            gradient.EndPoint = new Point(0, 1);
            gradient.GradientStops.Add(new GradientStop(Colors.Blue, 0.0));
            gradient.GradientStops.Add(new GradientStop(Colors.Red, 1.0));

            var material = new DiffuseMaterial(gradient);
            var model = new GeometryModel3D(mesh, material);
            model.BackMaterial = material;

            var visual = new ModelVisual3D();
            visual.Content = model;
            visual.Transform = transformGroup;
            Viewport.Children.Add(visual);

            terrainMesh = mesh;
            terrainVisual = visual;
            originalTerrainPositions = mesh.Positions
                .Select(p => new Point3D(p.X, p.Y, p.Z))
                .ToList();

            UpdateCameraPosition();
        }

        private void CalculateNormal(MeshGeometry3D mesh)
        {
            mesh.Normals.Clear();

            for (int i = 0; i < mesh.Positions.Count; i++)
            {
                mesh.Normals.Add(new Vector3D(0, 0, 0));
            }

            for (int i = 0; i < mesh.TriangleIndices.Count; i += 3)
            {
                int index1 = mesh.TriangleIndices[i];
                int index2 = mesh.TriangleIndices[i + 1];
                int index3 = mesh.TriangleIndices[i + 2];

                Point3D p1 = mesh.Positions[index1];
                Point3D p2 = mesh.Positions[index2];
                Point3D p3 = mesh.Positions[index3];

                Vector3D v1 = p2 - p1;
                Vector3D v2 = p3 - p1;
                Vector3D normal = Vector3D.CrossProduct(v1, v2);
                normal.Normalize();

                mesh.Normals[index1] += normal;
                mesh.Normals[index2] += normal;
                mesh.Normals[index3] += normal;
            }

            for (int i = 0; i < mesh.Normals.Count; i++)
            {
                mesh.Normals[i].Normalize();
            }
        }

        private void Viewport_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                if (TryLaunchComet(e.GetPosition(Viewport)))
                {
                    return;
                }
            }

            isRotating = true;
            lastMousePosition = e.GetPosition(Viewport);
            Viewport.CaptureMouse();
        }

        private void Viewport_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            isRotating = false;
            Viewport.ReleaseMouseCapture();
        }

        private void Viewport_MouseMove(object sender, MouseEventArgs e)
        {
            if (!isRotating) return;

            Point currentPosition = e.GetPosition(Viewport);
            double deltaX = currentPosition.X - lastMousePosition.X;
            double deltaY = currentPosition.Y - lastMousePosition.Y;

            totalRotationY += deltaX * 0.5;
            totalRotationX += deltaY * 0.5;

            var rotateX = (RotateTransform3D)transformGroup.Children[0];
            var rotateY = (RotateTransform3D)transformGroup.Children[1];

            ((AxisAngleRotation3D)rotateX.Rotation).Angle = totalRotationX;
            ((AxisAngleRotation3D)rotateY.Rotation).Angle = totalRotationY;

            lastMousePosition = currentPosition;
        }

        private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        {

            if (e.Delta > 0)
            {
                cameraDistance *= 1.1;
            }
            else
            {
                cameraDistance *= 0.9;
            }

            cameraDistance = Math.Max(2, Math.Min(20.0, cameraDistance));

            UpdateCameraPosition();
        }
    }
} 