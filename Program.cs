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
        private const double xMin = 3.8;
        private const double xMax = 7.6;
        private const double dx = 0.6;

        // Розраховані межі для осі Y
        private double yMin;
        private double yMax;

        // Відступ від країв вікна до області графіка
        private const int padding = 60;

        public GraphForm()
        {
            this.Text = "Графік функції y = cos²(x) / (x² + 1)";
            this.Size = new Size(800, 600);
            this.BackColor = Color.White;
            this.DoubleBuffered = true; // Зменшує мерехтіння при перемальовуванні

            // Підписуємось на події
            this.Paint += GraphForm_Paint;
            this.Resize += GraphForm_Resize;

            // Розраховуємо діапазон Y один раз при запуску
            CalculateYRange();
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
            int graphWidth = this.ClientSize.Width - 2 * padding;
            int graphHeight = this.ClientSize.Height - 2 * padding;

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
            {
                StringFormat formatX = new StringFormat { Alignment = StringAlignment.Center };
                StringFormat formatY = new StringFormat { Alignment = StringAlignment.Far };

                // --- Вісь X та її сітка/підписи ---
                g.DrawLine(axisPen, padding, padding + graphHeight, padding + graphWidth, padding + graphHeight);
                for (double x = xMin; x <= xMax; x += dx)
                {
                    int screenX = (int)MapWorldToScreenX(x, graphWidth);
                    g.DrawLine(gridPen, screenX, padding, screenX, padding + graphHeight);
                    g.DrawString(x.ToString("F1"), textFont, textBrush, screenX, padding + graphHeight + 5, formatX);
                }

                // --- Вісь Y та її сітка/підписи ---
                g.DrawLine(axisPen, padding, padding, padding, padding + graphHeight);
                int numYTicks = 5;
                for (int i = 0; i <= numYTicks; i++)
                {
                    double y = yMin + i * (yMax - yMin) / numYTicks;
                    int screenY = (int)MapWorldToScreenY(y, graphHeight);
                    g.DrawLine(gridPen, padding, screenY, padding + graphWidth, screenY);
                    g.DrawString(y.ToString("F3"), textFont, textBrush, padding - 5, screenY - (textFont.Height / 2), formatY);
                }
            }
        }

        /// <summary>
        /// Малює сам графік функції.
        /// </summary>
        private void DrawFunctionGraph(Graphics g, int graphWidth, int graphHeight)
        {
            // Використовуємо маленький крок для плавної лінії
            double step = (xMax - xMin) / graphWidth;
            List<PointF> points = new List<PointF>();

            for (double x = xMin; x <= xMax; x += step)
            {
                double y = F(x);
                points.Add(new PointF((float)MapWorldToScreenX(x, graphWidth), (float)MapWorldToScreenY(y, graphHeight)));
            }

            if (points.Count > 1)
            {
                using (Pen graphPen = new Pen(Color.Blue, 2))
                {
                    g.DrawLines(graphPen, points.ToArray());
                }
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
        /// Знаходить мінімальне та максимальне значення Y в нашому діапазоні X.
        /// Це потрібно для правильного масштабування графіка по висоті.
        /// </summary>
        private void CalculateYRange()
        {
            // Перевіряємо 1000 точок, щоб знайти екстремуми
            double step = (xMax - xMin) / 1000.0;
            yMin = F(xMin);
            yMax = F(xMin);

            for (double x = xMin; x <= xMax; x += step)
            {
                double y = F(x);
                if (y < yMin) yMin = y;
                if (y > yMax) yMax = y;
            }

            // Додамо 10% буфер зверху і знизу, щоб графік не торкався країв
            double yBuffer = (yMax - yMin) * 0.1;
            if (yBuffer == 0) yBuffer = 0.1; // На випадок, якщо функція - константа
            yMax += yBuffer;
            yMin -= yBuffer;

            // Оскільки наша функція (cos^2 / ...) завжди >= 0,
            // ми можемо зробити нижню межу 0, якщо вона близько.
            if (yMin < 0)
            {
                yMin = 0;
            }
        }

        /// <summary>
        /// Перетворює "світову" X-координату (з функції) у піксельну X-координату (для екрану).
        /// </summary>
        private double MapWorldToScreenX(double x, int graphWidth)
        {
            return padding + (x - xMin) * graphWidth / (xMax - xMin);
        }

        /// <summary>
        /// Перетворює "світову" Y-координату (з функції) у піксельну Y-координату (для екрану).
        /// </summary>
        private double MapWorldToScreenY(double y, int graphHeight)
        {
            // (yMax - y) - це інверсія, оскільки Y=0 на екрані знаходиться *зверху*,
            // а в математичному графіку - *знизу*.
            return padding + (yMax - y) * graphHeight / (yMax - yMin);
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
