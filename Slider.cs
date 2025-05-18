using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CMPersonalityManager
{
    public partial class Slider : UserControl
    {
        public int Minimum { 
            get { return trackBar1.Minimum;  }
            set { trackBar1.Minimum = value; } 
        }
        public int Maximum
        {
            get { return trackBar1.Maximum; }
            set { trackBar1.Maximum = value; }
        }
        public float DisplayFactor { get; set; }
        public int Value {
            get 
            {
                return trackBar1.Value; 
            }
            set { 
                trackBar1.Value = value;
                UpdateTextbox();
            } 
        }

        public Slider()
        {
            InitializeComponent();
            UpdateTextbox();
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            UpdateTextbox();
        }

        private void UpdateTextbox()
        {
            if (DisplayFactor == 1) { maskedTextBox1.Text = trackBar1.Value.ToString(); }
            else { maskedTextBox1.Text = Math.Round(trackBar1.Value * DisplayFactor, 1).ToString("F"); }
        }
    }
}
