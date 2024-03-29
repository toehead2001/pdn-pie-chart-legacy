﻿// Author:          Evan Olds
// Creation Date:   January 6, 2012

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PaintDotNet.Effects
{
    internal partial class PieChartConfigDialog : EffectConfigDialog<RenderPieChart, PieChartConfigToken>
    {
        private class Item
        {
            public Color Color;

            private string m_name;

            public double Value;

            public Item(string name, double value, Color color)
            {
                m_name = name;
                Value = value;
                Color = color;
            }

            public string Name
            {
                get
                {
                    return m_name;
                }
            }

            public override string ToString()
            {
                return m_name;
            }
        }
        
        public PieChartConfigDialog()
        {
            InitializeComponent();
        }

        public void helpButtonClicked(Object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PieChartConfigDialog));
            MessageBox.Show(resources.GetString("textBox1.Text"), "Pie Chart - Help");
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            // Make sure there's a category name
            if (string.IsNullOrEmpty(tbCategoryName.Text))
            {
                tbCategoryName.Focus();
                return;
            }

            double d;
            if (!double.TryParse(tbCategoryValue.Text, out d))
            {
                MessageBox.Show(this, "Category values must be numerical", this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbCategoryValue.Focus();
                return;
            }

            // Ensure that the values are > 0.0
            if (d <= 0.0)
            {
                MessageBox.Show(this, "Category values must be > 0.0", this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                tbCategoryValue.Focus();
                return;
            }

            // Create the item and add it to the list box
            Item item = new Item(tbCategoryName.Text,
                Convert.ToDouble(tbCategoryValue.Text), pnlColor.BackColor);
            lbCategories.Items.Add(item);

            Random randomGen = new Random();
            KnownColor[] names = (KnownColor[])Enum.GetValues(typeof(KnownColor));
            KnownColor randomColorName = names[randomGen.Next(names.Length)];
            Color randomColor = Color.FromKnownColor(randomColorName);

            // Clear the fields for the next item
            tbCategoryName.Text = string.Empty;
            tbCategoryValue.Text = string.Empty;
            pnlColor.BackColor = randomColor;

            // Focus back on the name box
            tbCategoryName.Focus();

            // Update
            FinishTokenUpdate();
        }

        private void btnRemoveCategory_Click(object sender, EventArgs e)
        {
            if ((lbCategories.SelectedIndex != -1) && (lbCategories.Items.Count == 1))
            {
                lbCategories.Items.RemoveAt(lbCategories.SelectedIndex);

                FinishTokenUpdate();
            }
            else if ((lbCategories.SelectedIndex != -1) && (lbCategories.SelectedIndex < lbCategories.Items.Count - 1))
            {
                lbCategories.SelectedIndex += 1;
                lbCategories.Items.RemoveAt(lbCategories.SelectedIndex - 1);

                FinishTokenUpdate();
            }
            else if ((lbCategories.SelectedIndex != -1) && (lbCategories.SelectedIndex == lbCategories.Items.Count - 1))
            {
                lbCategories.SelectedIndex -= 1;
                lbCategories.Items.RemoveAt(lbCategories.SelectedIndex + 1);

                FinishTokenUpdate();
            }
        }

        private void btnMoveUpCategory_Click(object sender, EventArgs e)
        {
            if (lbCategories.SelectedIndex > 0)
            {
                lbCategories.Items.Insert(lbCategories.SelectedIndex - 1, lbCategories.SelectedItem);
                lbCategories.SelectedIndex -= 2;
                lbCategories.Items.RemoveAt(lbCategories.SelectedIndex + 2);

                FinishTokenUpdate();
            }
        }

        private void btnMoveDownCategory_Click(object sender, EventArgs e)
        {
            if ((lbCategories.SelectedIndex != -1) && (lbCategories.SelectedIndex < lbCategories.Items.Count - 1))
            {
                lbCategories.Items.Insert(lbCategories.SelectedIndex + 2, lbCategories.SelectedItem);
                lbCategories.SelectedIndex += 2;
                lbCategories.Items.RemoveAt(lbCategories.SelectedIndex - 2);

                FinishTokenUpdate();
            }
        }

        private void lbCategories_DrawItem(object sender, DrawItemEventArgs e)
        {
            bool selected = lbCategories.SelectedIndex == e.Index;

            if (-1 == e.Index)
            {
                // Fill with background color and return
                using (SolidBrush temp = new SolidBrush(e.BackColor))
                {
                    e.Graphics.FillRectangle(temp, e.Bounds);
                }
                return;
            }

            // Get the item
            Item item = (Item)lbCategories.Items[e.Index];
            
            SolidBrush sb = new SolidBrush(e.BackColor);

            // Start by filling the backgorund
            if (selected)
            {
                using (SolidBrush lgb = new SolidBrush(Color.LightBlue))
                {
                    e.Graphics.FillRectangle(lgb, e.Bounds);
                }
            }
            else
            {
                e.Graphics.FillRectangle(sb, e.Bounds);
            }

            // Draw a small box for the item's color
            Rectangle box = e.Bounds;
            box.Height -= 4;
            box.Width = box.Height;
            box.X += 2;
            box.Y += 2;
            sb.Color = item.Color;
            e.Graphics.FillRectangle(sb, box);

            // Draw the item's text
            sb.Color = lbCategories.ForeColor;
            string itemText = item.ToString() + " (" + item.Value + ")";
            e.Graphics.DrawString(itemText, lbCategories.Font, sb,
                (float)(box.Right + 2), (float)e.Bounds.Y);
        }

        private void lbCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            lbCategories.Refresh();
        }

        private void pnlColor_Click(object sender, EventArgs e)
        {
            if (DialogResult.OK == colorDialog1.ShowDialog())
            {
                pnlColor.BackColor = colorDialog1.Color;
            }
        }

        private void angleSelector1_AngleChanged()
        {
            txtAngle1.Value = (decimal)angleSelector1.Angle;
            FinishTokenUpdate();
        }

        private void txtAngle1_ValueChanged(object sender, EventArgs e)
        {
            if (txtAngle1.Value == txtAngle1.Maximum)
                txtAngle1.Value = txtAngle1.Minimum + 1;

            if (txtAngle1.Value == txtAngle1.Minimum)
                txtAngle1.Value = txtAngle1.Maximum - 1;

            angleSelector1.Angle = (double)txtAngle1.Value;
            FinishTokenUpdate();
        }

        private void radioBlack_CheckedChanged(object sender, EventArgs e)
        {
            FinishTokenUpdate();
        }

        private void radioGray_CheckedChanged(object sender, EventArgs e)
        {
            FinishTokenUpdate();
        }

        private void radioWhite_CheckedChanged(object sender, EventArgs e)
        {
            FinishTokenUpdate();
        }

        #region EffectConfigDialog stuff

        protected override PieChartConfigToken CreateInitialToken()
        {
            return new PieChartConfigToken();
        }

        protected override void InitDialogFromToken(PieChartConfigToken effectTokenCopy)
        {
            Random randomGen = new Random();
            KnownColor[] names = (KnownColor[])Enum.GetValues(typeof(KnownColor));
            KnownColor randomColorName = names[randomGen.Next(names.Length)];
            Color randomColor = Color.FromKnownColor(randomColorName);

            // Start by clearing the data on the form
            tbCategoryName.Name = string.Empty;
            tbCategoryValue.Name = string.Empty;
            pnlColor.BackColor = randomColor;
            lbCategories.Items.Clear();

            int i = 1;
            foreach (EOFC.Drawing32.PieChartSection pcs in effectTokenCopy.Data)
            {
                Item item = new Item("Category " + i.ToString(), pcs.Value,
                    Color.FromArgb((int)pcs.Color));
                lbCategories.Items.Add(item);
                i++;
            }

            txtAngle1.Value = (decimal)effectTokenCopy.Angle;

            if (effectTokenCopy.Outline == 0xffffffff)
                radioWhite.Checked = true;
            else if (effectTokenCopy.Outline == 0xff444444)
                radioGray.Checked = true;
            else
                radioBlack.Checked = true;
        }

        protected override void LoadIntoTokenFromDialog(PieChartConfigToken writeValuesHere)
        {
            writeValuesHere.Angle = (double)txtAngle1.Value;

            if (radioWhite.Checked)
                writeValuesHere.Outline = 0xffffffff;
            else if (radioGray.Checked)
                writeValuesHere.Outline = 0xff444444;
            else
                writeValuesHere.Outline = 0xff000000;

            writeValuesHere.Data.Clear();

            for (int i = 0; i < lbCategories.Items.Count; i++)
            {
                Item item = (Item)lbCategories.Items[i];

                writeValuesHere.Data.Add(new EOFC.Drawing32.PieChartSection(item.Value, (uint)item.Color.ToArgb()));
            }
        }

        #endregion
    }
}
