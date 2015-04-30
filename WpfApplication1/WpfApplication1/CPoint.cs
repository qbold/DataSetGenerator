namespace WpfApplication1
{
    /// <summary>
    /// Характеристическая точка
    /// </summary>
    public class CPoint
    {
        public double Mc;
        public Pixel pixel;
        public int Color { get { return pixel.Color; } set { if (value > 255) value = 255; if (value < 0) value = 0; pixel.Color = value; } }
        public int X { get { return pixel.X; } set { pixel.X = value; } }
        public int Y { get { return pixel.Y; } set { pixel.Y = value; } }

        public double Vx, Vy; // параметры оптического потока

        public CPoint(int x, int y, double Mc)
        {
            pixel = new Pixel(x, y, Mc < 0 ? 0 : Mc > 255 ? 255 : (int)Mc);
            this.Mc = Mc;
        }
    }

    public class Pixel
    {
        public int X, Y;
        public int Color;

        public Pixel(int x, int y, int c)
        {
            X = x;
            Y = y;
            Color = c;
        }
    }
}
