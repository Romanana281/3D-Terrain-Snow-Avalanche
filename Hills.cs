using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Media3D;

namespace Project4
{
    public class Hills
    {
        public int count { get; set; }

        public bool isActive { get; set; }

        public int lastCount { get; set; }

        public List<Point3D> pointsAboveLevel { get; set; }

        public List<Point3D> originalPoints { get; set; }

        public Dictionary<Point, int> pointIndices { get; set; }

        public List<ModelVisual3D> visuals { get; set; }

        public bool avalancheTriggered;

    }
}
