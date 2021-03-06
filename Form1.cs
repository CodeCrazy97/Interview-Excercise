﻿using System;
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
                    bool BixOrTiErrors = false;  // records if an error is encountered in one of the segments
                    bool VcErrors = false;

                    foreach (string line in lines)
                    {

                        // get info from Beginning Segment for Automotive Inspection                       
                        if (line.StartsWith("BIX"))
                        {
                            BixOrTiErrors = false;  // there's nothing wrong with the current transaction set (no errors yet encountered)

                            int indexOfThirdDelim = findNthOccur(line, '*', 3);

                            if (indexOfThirdDelim == -1)
                            {
                                richTextBox1.AppendText("\n--- ERROR! PROBLEM WITH BIX ---\n");
                                BixOrTiErrors = true;
                                continue;
                            }
                            try
                            {
                                // get the Inspection date
                                headerInfo = line.Substring(indexOfThirdDelim, charCountBetweenDelimiters(line.ToString(), 4, '*'));

                            } catch (Exception e2)
                            {
                                richTextBox1.AppendText(e2.Message);
                                BixOrTiErrors = true;
                            }                         
                        } 

                        //  Transport Information
                        if (line.StartsWith("TI")  && !BixOrTiErrors)
                        {

                            int indexOfFirstDelim = findNthOccur(line, '*', 1);

                            if (indexOfFirstDelim == -1)
                            {
                                richTextBox1.Text = "\n--- ERROR! PROBLEM WITH TI ---\n";
                                BixOrTiErrors = true;
                                break;
                            }
                            try
                            {
                                // Standard Carrier Alpha Code (Transportation Carrier)
                                headerInfo += " " + line.Substring(indexOfFirstDelim, charCountBetweenDelimiters(line.ToString(), 2, '*'));
                            } catch (Exception e2)
                            {
                                richTextBox1.AppendText(e2.Message);
                                BixOrTiErrors = true;
                            }

                        } 

                        // collect info from VIN segment 
                        if (line.StartsWith("VC") && !BixOrTiErrors)
                        {
                            VcErrors = false;  // no errors encountered yet for this VC segment
                            inspectionDetailSegmentNum = 0; // reset inspection detail segment number so that we collect info and print for next ID segment
                            if (headerInfo.Equals(""))  // empty BIX - invalid!
                            {
                                richTextBox1.AppendText("--- ERROR! SOMETHING IS MISSING IN THE BIX LOOP ---");
                                VcErrors = true;
                                continue; // don't even attempt to print out VIN info - user's might see it and get confused.
                            }

                            // Set to a newline, since we're about to look at a new set of vehicles.
                            // Want next set of vehicles on a different line.
                            if (!outputLine.Equals(""))
                            {
                                // Not the first Vehicle in transaction set - add a newline to separate this one from previous.
                                outputLine = "\n";
                            }
                            try
                            {
                                outputLine += line.Substring(findNthOccur(line, '*', 1), charCountBetweenDelimiters(line.ToString(), 2, '*')) + " " + headerInfo;
                            }
                            catch (Exception e2)
                            {
                                richTextBox1.AppendText(e2.Message);
                                VcErrors = true;  // we've encountered an error in the VC segment - don't allow info to be collected from the ID segment
                            }
                        }

                        // Inspection Detail Segment
                        if (line.StartsWith("ID") && inspectionDetailSegmentNum == 0 && !BixOrTiErrors && !VcErrors) // first ID segment after VC
                        {
                            inspectionDetailSegmentNum += 1;

                            int indexOfFirstDelim = findNthOccur(line, '*', 1);
                            int indexOfSecondDelim = findNthOccur(line, '*', 2);
                            int indexOfThirdDelim = findNthOccur(line, '*', 3);
                            
                            // something went wrong attempting to get index of one of the delimiters.
                            if (indexOfThirdDelim == -1 || indexOfFirstDelim == -1 || indexOfSecondDelim == -1)
                            {
                                richTextBox1.AppendText("\n--- ERROR! PROBLEM WITH ID SEGMENT ---\n");
                                if (richTextBox1.Text.Equals(""))  // there's nothing in the text area. Set outputline to empty (this will prevent a pointless newline from being inserted onto the text area when collecting info from the VC segment, as outputline is checked for data to determine if a newline should be inserted)
                                {
                                    outputLine = "";
                                }
                                continue;
                            }

                            try
                            {
                                // The first Damage Area Code noted for the vehicle.
                                outputLine += " " + line.Substring(indexOfFirstDelim, charCountBetweenDelimiters(line.ToString(), 2, '*'));

                                // The first Damage Area Type noted for the vehicle.
                                outputLine += " " + line.Substring(indexOfSecondDelim, charCountBetweenDelimiters(line.ToString(), 3, '*'));

                                // The first Damage Severity code
                                outputLine += " " + line.Substring(indexOfThirdDelim, charCountBetweenDelimiters(line.ToString(), 4, '*'));

                            }
                            catch (Exception e2)
                            {
                                richTextBox1.AppendText(e2.Message);
                                continue;
                            }
                                                        
                            // Output information for previous vehicle.
                            richTextBox1.AppendText(outputLine);
 
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

        // Get number of characters between the nth and the nth - 1 positions of the fieldDelimiter character in the string str. 
        static int charCountBetweenDelimiters(String str, int n, char fieldDelimiter)
        {
            int occur = 0;  // number of times we have found the fieldDelimiter character
            int charCount = 0;  // number of characters between nth and nth -1 occurrences of fieldDelimiter

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
                
                if (occur == n) // found nth occurrence of the character
                {
                    return charCount - 1; // need minus one because last occurrence of fieldDelimiter was counted
                } else if (i == str.Length - 1)  // end of the line - don't keep counting
                {
                    if (occur == n - 1)  // in this case, the field was the last field on the line (there was not a postceding delimiter)
                    {
                        if (charCount > 0)
                        {
                            return charCount;
                        } else  // in this case, the last field was the only place that the expected field could have been in - but it wasn't there
                        {
                            throw new Exception("\n --- ERROR! THE BELOW LINE IS MISSING AN EXPECTED FIELD:\n" + str + "\n");
                        }                        
                    } else
                    {
                        throw new Exception("\n --- ERROR! THE BELOW LINE DID NOT CONTAIN AN EXPECTED FIELD:\n" + str + "\n");
                    }
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
