using System.Collections.Generic;

namespace Project4
{
    public class GetCoordinates
    {
        private double minZ;
        private double maxZ;
        private double minX;
        private double maxX;
        private double minY;
        private double maxY;
        public double [,] data;

        public void set(double minZ, double maxZ, double minX, double maxX, double minY, double maxY, double[,] data)
        {
            this.minZ = minZ;
            this.maxZ = maxZ;
            this.minX = minX;
            this.maxX = maxX;
            this.minY = minY;
            this.maxY = maxY;
            this.data = data;
        }

        public List<double> get()
        {
            return new List<double> { minZ, maxZ, minX, maxX, minY, maxY };
        }
    }
}
