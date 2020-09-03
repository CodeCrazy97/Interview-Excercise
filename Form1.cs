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
            // prompt user to find the 928 file 
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            openFileDialog1.ShowDialog();
            string fileName = openFileDialog1.FileName;
            if (fileName.Length == 0)
            {
                Console.WriteLine("No file chosen.");
                return;
            }
            try
            {
                if (fileName.Substring(fileName.Length - 3).Equals("txt"))  // only .txt files are allowed
                {
                    // Read contents of the 928.
                    string[] lines = System.IO.File.ReadAllLines(@fileName);

                    // reset text in the text box.
                    richTextBox1.Text = "";

                    string outputLine = "";
                    string headerInfo = "";  // used to hold BIX info 
                    int inspectionDetailSegmentNum = 0;  // use this to see if we've found the first inspection detail segment

                    foreach (string line in lines)
                    {

                        // get info from Beginning Segment for Automotive Inspection                       
                        if (line.StartsWith("BIX"))
                        {

                            int indexOfThirdDelim = findNthOccur(line, '*', 3);
                            int indexOfFirstDelim = findNthOccur(line, '*', 1);

                            if (indexOfFirstDelim == -1 || indexOfThirdDelim == -1)
                            {
                                richTextBox1.Text = "\n--- ERROR! PROBLEM WITH BIX ---\n";
                                break;
                            }

                            // get the Inspection date
                            headerInfo = line.Substring(indexOfThirdDelim, charCountBetweenDelimiters(line.ToString(), 4, '*'));

                            // get the Standard Carrier Alpha Code
                            headerInfo += " " + line.Substring(indexOfFirstDelim, charCountBetweenDelimiters(line.ToString(), 2, '*'));
                                                        
                        }

                        //  Transport Information
                        if (line.StartsWith("TI"))
                        {

                            int indexOfFirstDelim = findNthOccur(line, '*', 1);

                            if (indexOfFirstDelim == -1)
                            {
                                richTextBox1.Text = "\n--- ERROR! PROBLEM WITH TI ---\n";
                                break;
                            }

                            // Standard Carrier Alpha Code 
                            headerInfo += " " + line.Substring(indexOfFirstDelim, charCountBetweenDelimiters(line.ToString(), 2, '*'));

                        }

                        // collect info from VIN segment 
                        if (line.StartsWith("VC"))
                        {
                            inspectionDetailSegmentNum = 0; // reset inspection detail segment number so that we collect info and print for next ID segment
                            if (headerInfo.Equals(""))  // empty BIX - invalid!
                            {
                                richTextBox1.AppendText("--- ERROR! SOMETHING IS MISSING IN THE BIX LOOP ---");
                                continue; // don't even attempt to print out the VIN - user's might see it and get confused.
                            }
                            
                            // Set to a newline, since we're about to look at a new set of vehicles.
                            // Want next set of vehicles on a different line.
                            if (!outputLine.Equals(""))
                            {
                                // Not the first Vehicle in transaction set - add a newline to separate this one from previous.
                                outputLine = "\n";
                            }
                            outputLine += headerInfo + " " + line.Substring(findNthOccur(line, '*', 1), charCountBetweenDelimiters(line.ToString(), 2, '*'));
                                                        
                        }

                        // Inspection Detail Segment
                        if (line.StartsWith("ID") && inspectionDetailSegmentNum == 0) // first ID segment after VC
                        {
                            
                            int indexOfFirstDelim = findNthOccur(line, '*', 1);
                            int indexOfSecondDelim = findNthOccur(line, '*', 2);
                            int indexOfLastDelim = getIndexOfLastDelimiterOnLine(line, '*');
                            
                            // something went wrong attempting to get index of one of the delimiters.
                            if (indexOfLastDelim == -1 || indexOfFirstDelim == -1 || indexOfSecondDelim == -1)
                            {
                                richTextBox1.AppendText("\n--- ERROR! PROBLEM WITH ID SEGMENT ---\n");
                                continue;
                            }

                            // The first Damage Area Code noted for the vehicle.
                            outputLine += " " + line.Substring(indexOfFirstDelim, charCountBetweenDelimiters(line.ToString(), 2, '*'));

                            // The first Damage Area Type noted for the vehicle.
                            outputLine += " " + line.Substring(indexOfSecondDelim, charCountBetweenDelimiters(line.ToString(), 2, '*'));

                            // The first Damage Severity Code noted for the vehicle.
                            outputLine += " " + line.Substring(indexOfLastDelim);

                            // Output information for previous vehicle.
                            richTextBox1.AppendText(outputLine);

                            inspectionDetailSegmentNum += 1;
                        }

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

        // Gets the last field in a segment
        static int getIndexOfLastDelimiterOnLine(String str, char fieldDelimiter)
        {

            for (int i = str.Length - 1; i >= 0; i--)
            {
                
                if (str[i] == fieldDelimiter)
                {
                    return i + 1;  // need plus one because string arrays are zero-indexed
                }

            }
            return -1;
        }

        // Get number of characters between the nth and the nth - 1 positions of the fieldDelimiter character in the string str. 
        static int charCountBetweenDelimiters(String str, int n, char fieldDelimiter)
        {
            int occur = 0;
            int charCount = 0;

            for (int i = 0; i < str.Length; i++)
            {
                if (occur == (n - 1))  // time to start counting the characters that occur between nth occurrence of fieldDelimiter and nth - 1 occurrence of fieldDelimiter
                {
                    charCount += 1;
                }

                if (str[i] == fieldDelimiter)
                {
                    occur += 1;
                }
                
                if (occur == n) // done counting
                {
                    return charCount - 1; // need minus one because last occurrence of fieldDelimiter was counted
                } else if (i == str.Length - 1)  // end of the line
                {
                    return i + 1;
                }
            }
            return -1;
        }

        // Find the Nth occurrence of the field delimiter 
        static int findNthOccur(String str, char fieldDelimiter, int N)
        {
            int occur = 0;

            // Loop to find the Nth 
            // occurence of the character 
            for (int i = 0; i < str.Length; i++)
            {
                if (str[i] == fieldDelimiter)
                {
                    occur += 1;
                }
                if (occur == N)
                {
                    return i + 1;  // need plus one because char arrays are zero-based, whereas strings are one-based 
                }
            }
            return -1;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            richTextBox1.Text = "";
        }
    }
}
