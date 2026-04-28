using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Project4
{
    public class Avalanche
    {
        public List<Point3D> Points { get; set; }

        public Dictionary<Point, int> PointIndices { get; set; }

        public List<double> HeightOffsets { get; set; }

        public double Mass { get; set; }

        public double Speed { get; set; }

        public Vector Direction { get; set; }

        public Point3D TargetPoint { get; set; }

        public double InitialDistanceToTarget { get; set; }

        public double Thickness { get; set; }

        public double Progress { get; set; }

        public bool ReachedBottom { get; set; }

        public ModelVisual3D Visual { get; set; }

        public double StretchAmount { get; set; }
    }
}