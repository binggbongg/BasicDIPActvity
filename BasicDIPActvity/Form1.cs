using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BasicDIPActvity
{
    public partial class Form1 : Form
    {
        Bitmap loaded;

        int cents5 = 0;
        int cents10 = 0;
        int cents25 = 0;
        int peso1 = 0;
        int peso5 = 0;
        int totalCoins = 0;
        double totalPesos = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private Bitmap applyBinaryFilter()
        {
            // utilize binary filter from lecture
            Bitmap processed = new Bitmap(loaded.Width, loaded.Height);
            Color pixel;
            int gray;

            for (int col = 0; col < loaded.Width; col++)
                for (int row = 0; row < loaded.Height; row++)
                {
                    pixel = loaded.GetPixel(col, row);
                    gray = (int)(pixel.R + pixel.G + pixel.B) / 3;
                    if (gray < 200)
                        processed.SetPixel(col, row, Color.Black);
                    else
                        processed.SetPixel(col, row, Color.White);

                }
            return processed;
        }

        private int[,] twoPassCCL(Bitmap processed, Color targetColor)
        {
            // using the two-pass connected component labeling
            Color temp;
            int w = processed.Width;
            int h = processed.Height;

            int[,] labelMap = new int[w, h];
            int nextLabel = 1;

            int maxLabels = w * h;
            int[] p = new int[maxLabels];
            
            for(int i = 0; i < maxLabels; i++)
            {
                p[i] = i;
            }

            // first pass: assign temp labels 
            for (int row = 0; row < processed.Height; row++)
            {
                for (int col = 0; col < processed.Width; col++)
                {
                    temp = processed.GetPixel(col, row);
                    if (temp.ToArgb() != targetColor.ToArgb()) continue;

                    int left = (col > 0) ? labelMap[col - 1, row] : 0;
                    int top = (row > 0) ? labelMap[col, row - 1] : 0;

                    if (left == 0 && top == 0)
                    {
                        // assign a new label
                        labelMap[col, row] = nextLabel;
                        nextLabel++;
                    }
                    else if (left != 0 && top == 0)
                    {
                        labelMap[col, row] = left;
                    }
                    else if (left == 0 && top != 0)
                    {
                        labelMap[col, row] = top;
                    }
                    else
                    {
                        labelMap[col, row] = Math.Min(left, top);
                        if (left != top)
                            combine(p, left, top);
                    }
                }
            }

            // second pass: resolving equaivalent labels
            for(int row = 0; row < processed.Height; row++)
            {
                for(int col = 0; col < processed.Width; col++)
                {
                    if (labelMap[col, row] > 0) 
                        labelMap[col, row] = findParent(p, labelMap[col, row]);
                }
            }

            return labelMap;
        }

        // denomination classification
        private void classifyCoins(List<int> validAreas)
        {
            validAreas.Sort();

            // estimated cutoff values for different sized coins
            foreach(int area in validAreas)
            {
                if (area < 7900)
                    cents5++;
                else if (area < 10000)
                    cents10++;
                else if (area < 14500)
                    cents25++;
                else if (area < 18500)
                    peso1++;
                else
                    peso5++;
            }
        }

        // helper functions
        private int findParent(int[] parent, int label)
        {
            if (parent[label] != label) parent[label] = findParent(parent, parent[label]);
            return parent[label];
        }

        private void combine(int[] parent, int label, int label2)
        {
            int r1 = findParent(parent, label);
            int r2 = findParent(parent, label2);

            if(r1 != r2)
            {
                if (r1 < r2)
                {
                    parent[r2] = r1;
                }
                else
                {
                    parent[r1] = r2;
                }
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // reset values first
            cents5 = cents10 = cents25 = peso1 = peso5 = 0;
            totalCoins = 0;
            totalPesos = 0;

            loaded = new Bitmap(pictureBox1.Image);

            Bitmap processed = applyBinaryFilter();
            int [,] labelMap = twoPassCCL(processed, Color.Black);

            // measure the pixel area per labeled objects
            Dictionary<int, int> coinAreas = new Dictionary<int, int>();
            for (int r = 0; r < processed.Height; r++)
            {
                for (int c = 0; c < processed.Width; c++)
                {
                    int id = labelMap[c, r];
                    if (id > 0)
                    {
                        if (!coinAreas.ContainsKey(id))
                            coinAreas[id] = 0;
                        coinAreas[id]++;
                    }
                }
            }

            // filtering out noise in the picture
            List<int> validAreas = coinAreas.Values
                .Where(a => a >= 500)
                .OrderBy(a => a)
                .ToList();

            classifyCoins(validAreas);

            totalCoins = cents5 + cents10 + cents25 + peso1 + peso5;
            totalPesos = (cents5 * 0.05) + (cents10 * 0.10) + (cents25 * 0.25) + (peso1 * 1.00) + (peso5 * 5.00);

            richTextBox1.Text = "Philippine Coin Count Results:\n" +
                $"5 centavos coins: {cents5}\n" +
                $"10 centavos coins: {cents10}\n" +
                $"25 centavos coins: {cents25}\n" +
                $"1 peso coins: {peso1}\n" +
                $"5 peso coins: {peso5}\n" +
                $"Total number of coins: {totalCoins}\n" +
                $"Total amount: {totalPesos} Pesos";

        }

        
    }
}
