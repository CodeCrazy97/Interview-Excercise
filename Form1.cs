using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VASCOR_Exercise
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();            
        }

        private void documentFinderButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.ShowDialog();
            Console.WriteLine("Fname = " + openFileDialog1.FileName);
            string fileName = openFileDialog1.FileName;
            try
            {
                if (fileName.Substring(fileName.Length - 3).Equals("txt"))
                {
                    // Read contents of the 928.
                    string[] lines = System.IO.File.ReadAllLines(@fileName);

                    foreach (string line in lines)
                    {
                        // Use a tab to indent each line of the file.
                        richTextBox1.AppendText("\t" + line);
                    }
                } else
                {
                    richTextBox1.Text = "Invalid filetype! Must be a .txt file.";
                }
            }
            catch (ArgumentOutOfRangeException e2)
            {
                richTextBox1.Text = "ERROR: " + e2.Message;
            }
        }
    }
}
