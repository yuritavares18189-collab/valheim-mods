using System.Drawing;
using System.Drawing.Drawing2D;

class Icone
{
    static void Main()
    {
        Bitmap bmp = new Bitmap(256, 256);
        Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.FromArgb(255, 22, 26, 22));
        SolidBrush ouro = new SolidBrush(Color.FromArgb(255, 212, 168, 52));
        SolidBrush sombra = new SolidBrush(Color.FromArgb(255, 92, 64, 18));
        g.FillEllipse(sombra, 48, 148, 160, 36);
        g.FillEllipse(ouro, 58, 118, 52, 52);
        g.FillEllipse(ouro, 102, 104, 58, 58);
        g.FillEllipse(ouro, 146, 120, 50, 50);
        Pen risco = new Pen(Color.FromArgb(255, 176, 42, 36), 18f);
        risco.StartCap = LineCap.Round;
        risco.EndCap = LineCap.Round;
        g.DrawLine(risco, 54, 196, 202, 58);
        bmp.Save(@"C:\Users\Yuri\Documents\LimpaDrop\Thunderstore\icon.png", System.Drawing.Imaging.ImageFormat.Png);
    }
}
