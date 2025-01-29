using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;

namespace Siemens_DataBlock_Extractor
{
    public partial class Form1 : Form
    {
        // Variables to store last entered data
        private string lastDbNumber = string.Empty;
        private string lastPlcName = string.Empty;

        public Form1()
        {
            InitializeComponent();

            // Setup drag and drop
            richTextBox1.AllowDrop = true;
            richTextBox2.AllowDrop = true;
            richTextBox3.AllowDrop = true;

            richTextBox1.DragEnter += RichTextBox_DragEnter;
            richTextBox2.DragEnter += RichTextBox_DragEnter;
            richTextBox3.DragEnter += RichTextBox_DragEnter;

            richTextBox1.DragDrop += RichTextBox_DragDrop;
            richTextBox2.DragDrop += RichTextBox_DragDrop;
            richTextBox3.DragDrop += RichTextBox_DragDrop;

            // Number only validation for DataBlock number
            textBox1.KeyPress += (s, e) =>
            {
                if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                {
                    e.Handled = true;
                }
            };
        }

        private void RichTextBox_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text))
            {
                e.Effect = DragDropEffects.Copy;
            }
        }

        private void RichTextBox_DragDrop(object sender, DragEventArgs e)
        {
            if (sender is RichTextBox rtb)
            {
                rtb.Text = e.Data.GetData(DataFormats.Text).ToString();
            }
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Please enter a DataBlock number.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(textBox2.Text))
            {
                MessageBox.Show("Please enter a PLC name.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (string.IsNullOrWhiteSpace(richTextBox1.Text) ||
                string.IsNullOrWhiteSpace(richTextBox2.Text) ||
                string.IsNullOrWhiteSpace(richTextBox3.Text))
            {
                MessageBox.Show("Please fill in all required fields (Names, Data Types, and Offsets).",
                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (!ValidateInput()) return;

            try
            {
                string plcName = textBox2.Text.Trim();
                string dbNumber = textBox1.Text.Trim();

                // Store the last values
                lastDbNumber = dbNumber;
                lastPlcName = plcName;

                // Update textBox3 and textBox4 with the last values
                textBox3.Text = lastDbNumber;
                textBox4.Text = lastPlcName;

                List<string> names = richTextBox1.Lines
                    .Where(line => !string.IsNullOrWhiteSpace(line) &&
                                 !line.StartsWith("Input") &&
                                 !line.StartsWith("Output") &&
                                 !line.StartsWith("InOut") &&
                                 !line.StartsWith("Static"))
                    .Select(line => line.Trim())
                    .ToList();

                List<string> dataTypes = richTextBox2.Lines
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .ToList();

                List<string> offsets = richTextBox3.Lines
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim())
                    .ToList();

                Dictionary<string, string> typeConversion = new Dictionary<string, string>
                {
                    {"Bit", "X"}, {"Byte", "B"}, {"Char", "C"}, {"Word", "W"},
                    {"Int", "I"}, {"DWord", "D"}, {"DInt", "DI"}, {"Real", "REAL"},
                    {"Bool", "X"}
                };

                List<object> tagsList = new List<object>();
                bool skipUnsupported = false;

                for (int i = 0; i < names.Count; i++)
                {
                    if (i >= dataTypes.Count || i >= offsets.Count)
                    {
                        MessageBox.Show("Mismatch in number of entries between Names, Data Types, and Offsets.",
                            "Data Mismatch", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        break;
                    }

                    string dataType = dataTypes[i];
                    string offset = offsets[i];

                    if (dataType == "Time" || dataType == "Counter" || !typeConversion.ContainsKey(dataType))
                    {
                        if (!skipUnsupported)
                        {
                            DialogResult result = MessageBox.Show(
                                "Times and Counter Not Supported. Do you wish to skip them and continue?",
                                "Unsupported Data Types",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Warning);

                            if (result == DialogResult.No)
                            {
                                return;
                            }
                            skipUnsupported = true;
                        }
                        continue;
                    }

                    string syntaxType = typeConversion[dataType];
                    string opcItemPath = $"[{plcName}]DB{dbNumber},{syntaxType}{offset}";

                    tagsList.Add(new
                    {
                        valueSource = "opc",
                        opcItemPath = opcItemPath,
                        dataType = dataType == "Bit" || dataType == "Bool" ? "Boolean" : dataType,
                        name = names[i],
                        tagType = "AtomicTag",
                        opcServer = "Ignition OPC UA Server"
                    });
                }

                // Create the root object with tags array
                var rootObject = new { tags = tagsList };

                string jsonOutput = JsonConvert.SerializeObject(rootObject, Formatting.Indented);

                using (SaveFileDialog saveFileDialog = new SaveFileDialog())
                {
                    saveFileDialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    saveFileDialog.Title = "Save JSON File";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, jsonOutput);
                        MessageBox.Show("JSON file saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox1.Clear();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            richTextBox2.Clear();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            richTextBox3.Clear();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            textBox1.Clear();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            textBox2.Clear();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            textBox3.Clear();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox4.Clear();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            // Show a confirmation dialog
            DialogResult result = MessageBox.Show(
                "Are you sure you want to clear all fields?", // Message
                "Confirmation",                               // Title
                MessageBoxButtons.YesNo,                     // Buttons
                MessageBoxIcon.Question                      // Icon
            );

            // Check if the user clicked "Yes"
            if (result == DialogResult.Yes)
            {
                // Clear all fields
                richTextBox1.Clear();
                richTextBox2.Clear();
                richTextBox3.Clear();
                textBox1.Clear();
                textBox2.Clear();
                textBox3.Clear();
                textBox4.Clear();
            }
        }

        private void button10_Click(object sender, EventArgs e)
        {
            try
            {
                // Always copy "Ane Inna" to the clipboard
                Clipboard.SetText("3MNGZykocFt73oTYRdwFSWqEWRqEQYkD3u");
                MessageBox.Show("Bitcoin address copied successfully. Please use Bitcoin and the Bitcoin network only to provide your support.", "Thank You !!", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Remaining JSON generation and saving code...
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
