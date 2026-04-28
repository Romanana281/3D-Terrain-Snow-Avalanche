using System.Windows.Media.Media3D;

namespace Project4
{
    public class Comet
    {
        public Point3D Start { get; set; }
        public Point3D Target { get; set; }
        public Point3D Position { get; set; }
        public double Progress { get; set; }
        public double Speed { get; set; }
        public bool Impacted { get; set; }
        public double LifeAfterImpact { get; set; }
        public ModelVisual3D Visual { get; set; }
        public Hills TargetHill { get; set; }
    }
}
