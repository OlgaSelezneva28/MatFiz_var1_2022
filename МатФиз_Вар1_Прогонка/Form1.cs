using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace МатФиз_Вар1_Прогонка
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        Double[] Ci; //Набор точек - решений 
        Double[] xi; //Набор точек сетки с шагом h
        Int64 n; // число точек сетки 

        //Прогонка
        public void solveMatrix( Double[] a, Double[] c, Double[] b, Double[] f)
        {
            /*
	         * b - диагональ, лежащая над главной (нумеруется: [0;n-2])
	         * c - главная диагональ матрицы A (нумеруется: [0;n-1])
	         * a - диагональ, лежащая под главной (нумеруется: [1;n-1])
	         * f - правая часть (столбец)
	         */
            int n = f.Count(); 
            double cmp;
            for (int i = 1; i < n; i++)
            {
                cmp = a[i] / c[i - 1];
                c[i] = c[i] - cmp * b[i - 1];
                f[i] = f[i] - cmp * f[i - 1];
            }

            //
            Ci[n - 1] = f[n - 1] / c[n - 1];

            for (int i = n - 2; i >= 0; i--)
            {
                Ci[i] = (f[i] - b[i] * Ci[i + 1]) / c[i];
            }
        }

        //Start
        private void button1_Click(object sender, EventArgs e)
        {
            //Считаем шаг 
            Double h = Convert.ToDouble(textBox1.Text);
            
            n = (Int64)(1 / h); // число точек сетки 

            Ci = new Double[n + 1];
            xi = new Double[n + 1];
            Double[] fi = new Double[n + 1];
            Double[] bi = new Double[n + 1];
            Double[] ci = new Double[n + 1];
            Double[] ai = new Double[n + 1];

            //Формируем сетку 
            Double x = 0;
            
            for (int i = 0; i < n + 1; i++)
            {
                xi[i] = x;
                x += h;
            }

            //Заполним массивы 
            //j = 0
            fi[0] = ((-1) / 3) * Math.Pow(xi[1], 2) + (1/4) * Math.Pow(xi[1], 3);
            ci[0] = (1 / Math.Pow(xi[1], 2)) * (2 * xi[1] + 0.25 * Math.Sin(2 * xi[1])) + xi[1] / 3;
            bi[0] = (-1) * (1 / Math.Pow(xi[1], 2)) * (2 * xi[1] + 0.25 * Math.Sin(2 * xi[1])) + xi[1] / 6; 

            // j = N
            fi[n] = (-1 / 5) * Math.Pow(1 - xi[n - 1], 3) + Math.Pow(1 - xi[n - 1], 2) * ((1 / 4) + (1 / 2) * xi[n - 1])
                + (1 - xi[n - 1]) * ((-2 / 3) * xi[n] - (1 / 3) * Math.Pow(xi[n], 2)) - (1 / 2) * Math.Pow(xi[n], 2);
            ai[n] = ((-1) / (Math.Pow(1 - xi[n - 1], 2))) * (2 * (1 - xi[n - 1]) + (1 / 4) * Math.Sin(2 * (1 - xi[n - 1])))
                + ((1 / (Math.Pow(1 - xi[n - 1], 2))) * ((-1) * (1 / 3) * Math.Pow(1 - xi[n - 1], 3) +
                (1 / 2) * (1 + xi[n - 1]) * Math.Pow(1 - xi[n - 1], 2)
                - xi[n - 1] * (1 - xi[n - 1])) + (2 + (1 / 2) * Math.Cos(2)));
            ci[n] = ((-1) / (Math.Pow(1 - xi[n - 1], 2))) * (2 * (1 - xi[n - 1]) + (1 / 4) * Math.Sin(2 * (1 - xi[n - 1])))
                + ((1 / (Math.Pow(1 - xi[n - 1], 2))) * ((1 / 3) * Math.Pow(1 - xi[n - 1], 3) - xi[n - 1] * Math.Pow(1 - xi[n - 1], 2)
                + Math.Pow(xi[n - 1], 2) * (1 - xi[n - 1])) + (2 + (1 / 2) * Math.Cos(2)));
            
            //j = 1..N-1
            for (int i = 1; i < n; i++) 
            {
                ai[i] = Cj1(xi[i - 1], xi[i]);
                ci[i] = Cj(xi[i - 1], xi[i], xi[i + 1]);
                bi[i] = Cj1(xi[i], xi[i + 1]);
                fi[i] = Fj(xi[i - 1], xi[i], xi[i + 1]);
            }

            //Прогонка
            solveMatrix(ai, ci, bi, fi);

            //u = summ(Ci * si) i = 0...N

            Double[] ui = new Double[n + 1];
            for (int i = 0; i < n + 1; i++)
            {
                ui[i] = Ci[i] * si(xi[i], i);
            }

            //Строим график функции
            GraphPane panel = zedGraphControl1.GraphPane;
            panel.CurveList.Clear();

            PointPairList u_list = new PointPairList();

            // Устанавливаем интересующий нас интервал по оси X
            panel.XAxis.Scale.Min = xi[0] - h;
            panel.XAxis.Scale.Max = xi[n] + h;

            for (int i = 0; i < xi.Count(); i++)
            {
                u_list.Add(xi[i], ui[i]);
            }


            LineItem Curve1 = panel.AddCurve("ui(x)", u_list, Color.Red, SymbolType.Star);

            zedGraphControl1.AxisChange();
            // Обновляем график
            zedGraphControl1.Invalidate();
          

            //Таблица 
            //Очистка строк и столбцов таблицы
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
            //Заполнение
            //Поскольку возможно вывести только 655 столбцов, то ограничим область видимости до 500 элементов
            Int64 max_count = 0;
            if (n > 500)
            {
                max_count = 500;
            }
            else
                max_count = n;
            dataGridView1.RowCount = (int)max_count + 1;
            dataGridView1.ColumnCount = (int)max_count + 3;
            for (int i = 0; i < (int)max_count + 1; i++)
            {
                for (int j = 0; j < (int)max_count + 1; j++)
                {
                    dataGridView1.Rows[i].Cells[j].Value = 0;

                    if (i == j)
                        dataGridView1.Rows[i].Cells[j].Value = ci[i];
                    if ((i != 0) && (i == j - 1))
                        dataGridView1.Rows[i].Cells[j].Value = ai[i];
                    if ((i != n ) && (i == j + 1))
                        dataGridView1.Rows[i].Cells[j].Value = bi[i];

                }

                dataGridView1.Rows[i].Cells[(int)max_count + 2].Value = fi[i];
            }

            //Строим график коэффициентов 
            GraphPane panel2 = zedGraphControl2.GraphPane;
            panel2.CurveList.Clear();

            PointPairList u_list2 = new PointPairList();

            // Устанавливаем интересующий нас интервал по оси X (количество коэф)
            panel2.XAxis.Scale.Min = 0;
            panel2.XAxis.Scale.Max = n + 2;

            for (int i = 0; i < Ci.Count(); i++)
            {
                u_list2.Add(i, Ci[i]);
            }


            LineItem Curve2 = panel2.AddCurve("Ci", u_list2, Color.Blue, SymbolType.Star);

            zedGraphControl2.AxisChange();
            // Обновляем график
            zedGraphControl2.Invalidate();
          

        }


        //Коэффициенты перед С_j1 //(j-1, j) and (j, j +1)
        public Double Cj1(Double xj1, Double xj)
        {
            return ((-1) * (1 / Math.Pow(xj - xj1, 2)) * (2 * (xj - xj1) + 0.25 * Math.Sin(2 * (xj - xj1))  + (1 / 3) * Math.Pow(xj - xj1, 3)
                - (1 / 2) * (xj + xj1) * Math.Pow(xj - xj1, 2) + xj1 * xj * (xj - xj1)) + (2 + (1 / 2) * Math.Cos(2) *
                ((1 - xj ) * (1 - xj1) / ((xj - xj1) * (xj1 - xj)))));
        }

        //Коэффициенты перед С_j 
        public Double Cj(Double xj1, Double xj, Double xj2)
        {
            return ((1 / Math.Pow(xj - xj1, 2)) * (2 * (xj - xj1) + (1 / 4) * Math.Sin(2 * (xj - xj1))) +
                (1 / Math.Pow(xj2 - xj, 2)) * (2 * (xj2 - xj) + (1 / 4) * Math.Sin(2 * (xj2 - xj))) +
                (1 / Math.Pow(xj - xj1, 2)) * ((1 / 3) * Math.Pow(xj - xj1, 3) - xj1 * Math.Pow(xj - xj1, 2) + Math.Pow(xj1, 2) * (xj - xj1)) +
                (1 / Math.Pow(xj2 - xj, 2)) * ((1 / 3) * Math.Pow(xj2 - xj, 3) - xj2 * Math.Pow(xj2 - xj, 2) + Math.Pow(xj2, 2) * (xj2 - xj)) +
                ((2 + (1 / 2) * Math.Cos(2)) * (1 - xj) * (((1 - xj1) / ((xj - xj1) * (xj1 - xj))) + ((1 - xj2) / ((xj - xj2) * (xj2 - xj))))));
        }

        //Правая часть для j = 1..N-1
        public Double Fj(Double xj1, Double xj, Double xj2)
        {
            return ( (-1/5) * Math.Pow(xj2 - xj, 3) + (1/2) * xj2 * Math.Pow(xj2 - xj,2) + ((xj2 - xj) * (1/3 - 2/3 * xj2 - 1/3 * Math.Pow(xj2, 2))) + Math.Pow(xj2, 2)) 
                + 
                (-1/5) * Math.Pow(xj - xj1, 3) + (1/2) * xj1 * Math.Pow(xj - xj1,2) + ((xj - xj1) * (1/3 - 2/3 * xj1 - 1/3 * Math.Pow(xj1, 2))) + Math.Pow(xj1, 2) ;
        }



        //фи j (базис)
        public Double si(Double x, Int64 i)
        {
            if (i == 0)
            {
                if (x >= xi[1])
                    return 0;
                return -x/xi[1] + 1;
            }
        
            if (i == n)
            {
                if (x <= xi[n-1])
                    return 0;
                return (x - xi[n-1])/(1 - xi[n-1]);
            }
    
            if ((x < xi[i-1]) || (x >= xi[i+1]))
                return 0;
            if (x < xi[i])
                return (x - xi[i - 1]) / (xi[i] - xi[i - 1]);
            if (x >= xi[i])
                return (x - xi[i+1])/(xi[i] - xi[i+1]);
    
            return 0;
 
        }

        private void очистииьГрафикToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GraphPane panel = zedGraphControl1.GraphPane;
            panel.CurveList.Clear();

            zedGraphControl1.AxisChange();
            // Обновляем график
            zedGraphControl1.Invalidate();
        }

        private void очиститьВсеToolStripMenuItem_Click(object sender, EventArgs e)
        {
            очистииьГрафикToolStripMenuItem_Click(sender, e);
            очиститьТаблицуToolStripMenuItem_Click( sender,  e);
            очиститьГрафикКоэффициэнтовToolStripMenuItem_Click(sender, e);

        }

        private void очиститьТаблицуToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Очистка строк и столбцов таблицы
            dataGridView1.Rows.Clear();
            dataGridView1.Columns.Clear();
        }

        private void очиститьГрафикКоэффициэнтовToolStripMenuItem_Click(object sender, EventArgs e)
        {
            GraphPane panel2 = zedGraphControl2.GraphPane;
            panel2.CurveList.Clear();

            zedGraphControl2.AxisChange();
            // Обновляем график
            zedGraphControl2.Invalidate();
        }
    }
}
