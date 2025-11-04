using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace FunctionPlotter
{
    public class GraphForm : Form
    {
        // ===================================================================
        //                  ПАРАМЕТРИ ГРАФІКА
        // ===================================================================

        // Діапазон значень X та крок, як у завданні
        private const double XMin = 3.8;
        private const double XMax = 7.6;
        private const double Dx = 0.6;

        // Розраховані межі для осі Y
        private double _yMin;
        private double _yMax;

        // Відступ від країв вікна до області графіка
        private const int Padding = 60;

        // Кешовані точки графіка у світових координатах
        private readonly List<PointF> _worldPoints = new List<PointF>();

        public GraphForm()
        {
            this.Text = "Графік функції y = cos²(x) / (x² + 1)";
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;
            
            // Більш надійний спосіб увімкнути подвійну буферизацію для зменшення мерехтіння
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);

            // Підписуємось на події
            this.Paint += GraphForm_Paint;
            this.Resize += GraphForm_Resize;

            // Розраховуємо діапазон Y та точки графіка один раз
            CalculateYRange();
            CacheWorldPoints();
        }

        // ===================================================================
        //                  ОСНОВНІ ПОДІЇ ФОРМИ
        // ===================================================================

        /// <summary>
        /// Головна подія малювання. Викликається системою, коли потрібно оновити вікно.
        /// </summary>
        private void GraphForm_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Вмикаємо згладжування

            // Визначаємо розміри області для малювання графіка
            int graphWidth = this.ClientSize.Width - 2 * Padding;
            int graphHeight = this.ClientSize.Height - 2 * Padding;

            // Якщо вікно занадто мале, нічого не малюємо
            if (graphWidth <= 0 || graphHeight <= 0) return;

            DrawAxesAndGrid(g, graphWidth, graphHeight);
            DrawFunctionGraph(g, graphWidth, graphHeight);
        }

        /// <summary>
        /// Подія зміни розміру. Викликається, коли користувач змінює розмір вікна.
        /// </summary>
        private void GraphForm_Resize(object sender, EventArgs e)
        {
            // Кажемо формі, що її вміст "недійсний" і його потрібно перемалювати.
            // Це автоматично викличе подію GraphForm_Paint.
            this.Invalidate();
        }

        // ===================================================================
        //                  ЛОГІКА МАЛЮВАННЯ
        // ===================================================================

        /// <summary>
        /// Малює осі координат, сітку та підписи до них.
        /// </summary>
        private void DrawAxesAndGrid(Graphics g, int graphWidth, int graphHeight)
        {
            using (Pen axisPen = new Pen(Color.Black, 2))
            using (Pen gridPen = new Pen(Color.LightGray, 1) { DashStyle = DashStyle.Dash })
            using (Brush textBrush = new SolidBrush(Color.Black))
            using (Font textFont = new Font("Arial", 8))
            using (StringFormat formatX = new StringFormat { Alignment = StringAlignment.Center })
            using (StringFormat formatY = new StringFormat { Alignment = StringAlignment.Far })
            {
                // --- Вісь X та її сітка/підписи ---
                g.DrawLine(axisPen, Padding, Padding + graphHeight, Padding + graphWidth, Padding + graphHeight);
                for (double x = XMin; x <= XMax; x += Dx)
                {
                    int screenX = (int)MapWorldToScreenX(x, graphWidth);
                    g.DrawLine(gridPen, screenX, Padding, screenX, Padding + graphHeight);
                    g.DrawString(x.ToString("F1"), textFont, textBrush, screenX, Padding + graphHeight + 5, formatX);
                }

                // --- Вісь Y та її сітка/підписи ---
                g.DrawLine(axisPen, Padding, Padding, Padding, Padding + graphHeight);
                int numYTicks = 5;
                for (int i = 0; i <= numYTicks; i++)
                {
                    double y = _yMin + i * (_yMax - _yMin) / numYTicks;
                    int screenY = (int)MapWorldToScreenY(y, graphHeight);
                    g.DrawLine(gridPen, Padding, screenY, Padding + graphWidth, screenY);
                    g.DrawString(y.ToString("F3"), textFont, textBrush, Padding - 5, screenY - (textFont.Height / 2), formatY);
                }
            }
        }

        /// <summary>
        /// Малює сам графік функції.
        /// </summary>
        private void DrawFunctionGraph(Graphics g, int graphWidth, int graphHeight)
        {
            if (_worldPoints.Count < 2) return;

            // Трансформуємо кешовані точки у екранні координати
            PointF[] screenPoints = new PointF[_worldPoints.Count];
            for (int i = 0; i < _worldPoints.Count; i++)
            {
                double x = _worldPoints[i].X;
                double y = _worldPoints[i].Y;
                screenPoints[i] = new PointF(
                    (float)MapWorldToScreenX(x, graphWidth),
                    (float)MapWorldToScreenY(y, graphHeight)
                );
            }

            using (Pen graphPen = new Pen(Color.Blue, 2))
            {
                g.DrawLines(graphPen, screenPoints);
            }
        }

        // ===================================================================
        //                  ДОПОМІЖНІ ФУНКЦІЇ
        // ===================================================================

        /// <summary>
        /// Функція з вашого завдання.
        /// y = cos^2(x) / (x^2 + 1)
        /// </summary>
        private double F(double x)
        {
            return Math.Pow(Math.Cos(x), 2) / (x * x + 1);
        }

        /// <summary>
        /// Розраховує та кешує точки функції у світових координатах для плавної лінії.
        /// </summary>
        private void CacheWorldPoints()
        {
            _worldPoints.Clear();
            // Використовуємо фіксований, достатньо великий крок для плавності
            double step = (XMax - XMin) / 2000.0;
            for (double x = XMin; x <= XMax; x += step)
            {
                _worldPoints.Add(new PointF((float)x, (float)F(x)));
            }
        }

        /// <summary>
        /// Знаходить мінімальне та максимальне значення Y в нашому діапазоні X.
        /// Це потрібно для правильного масштабування графіка по висоті.
        /// </summary>
        private void CalculateYRange()
        {
            // Перевіряємо 1000 точок, щоб знайти екстремуми
            double step = (XMax - XMin) / 1000.0;
            _yMin = F(XMin);
            _yMax = F(XMin);

            for (double x = XMin; x <= XMax; x += step)
            {
                double y = F(x);
                if (y < _yMin) _yMin = y;
                if (y > _yMax) _yMax = y;
            }

            // Додамо 10% буфер зверху і знизу, щоб графік не торкався країв
            double yBuffer = (_yMax - _yMin) * 0.1;
            if (yBuffer == 0) yBuffer = 0.1; // На випадок, якщо функція - константа
            _yMax += yBuffer;
            _yMin -= yBuffer;

            // Оскільки наша функція (cos^2 / ...) завжди >= 0,
            // ми можемо зробити нижню межу 0, якщо вона близько.
            if (_yMin < 0)
            {
                _yMin = 0;
            }
        }

        /// <summary>
        /// Перетворює "світову" X-координату (з функції) у піксельну X-координату (для екрану).
        /// </summary>
        private double MapWorldToScreenX(double x, int graphWidth)
        {
            return Padding + (x - XMin) * graphWidth / (XMax - XMin);
        }

        /// <summary>
        /// Перетворює "світову" Y-координату (з функції) у піксельну Y-координату (для екрану).
        /// </summary>
        private double MapWorldToScreenY(double y, int graphHeight)
        {
            double yRange = _yMax - _yMin;
            if (Math.Abs(yRange) < 1e-9) // Захист від ділення на нуль
            {
                return Padding + graphHeight / 2.0;
            }
            // (yMax - y) - це інверсія, оскільки Y=0 на екрані знаходиться *зверху*,
            // а в математичному графіку - *знизу*.
            return Padding + (_yMax - y) * graphHeight / yRange;
        }
    }

    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphForm());
        }
    }
}
