using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace File_Transfer
{
    public partial class Form1 : Form
    {
        private const int SRC_ARG = 0;
        private const int DEST_ARG = 1;
        private const int AUTORUN_ARG = 2;

        private const string AUTORUN_CMD = "autorun";

        private const int DATE_TAKEN_PROPERTY = 36867;

        private FileIndex index = new FileIndex();

        private static Regex r = new Regex(":");

        /**
         * Constructor
         * 
         * Handles Cmd line arguments
         */
        public Form1(string[] args)
        {
            InitializeComponent();

            historyText.Text = "File Transfer Program 1.0\nSelect the source and destination directories, and then click transfer to move your files.";
            
            // Parse command line arguments if any..
            // Source folder
            if (args.Length > SRC_ARG)
            {
                historyText.Text += "\n\nSource From Cmd Line: " + args[SRC_ARG];
                sourceFolderDialog.SelectedPath = args[SRC_ARG];
                UpdateSourceLabel(args[SRC_ARG]);
            }
            else
                srcLabel.Text = "(None Selected)";

            // Dest Folder
            if (args.Length > DEST_ARG)
            {
                historyText.Text += "\nDestination From Cmd Line: " + args[DEST_ARG];
                destLabel.Text = args[DEST_ARG];
                destFolderDialog.SelectedPath = args[DEST_ARG];
            }
            else
                destLabel.Text = "(None Selected)";

            // Autorun?
            if (args.Length > AUTORUN_ARG)
            {
                if (args[AUTORUN_ARG] == AUTORUN_CMD)
                {
                    historyText.Text += "\nAutorun argument received, autorunning...\n";

                    // attemp to run transfer
                    if (TransferFiles())
                        this.Shown += new EventHandler(CloseOnStart);
                    else
                        historyText.Text += "\n\n\t*****     AUTORUN FAILED     *****";
                }
                else
                    historyText.Text += "\nUnrecognized command line argument: " + args[AUTORUN_ARG];
            }
        }

        /**
         * Click Handler for the "Select Source" button
         */
        private void button1_Click(object sender, EventArgs e)
        {
            // Change src directory
            if (sourceFolderDialog.ShowDialog() == DialogResult.OK)
                UpdateSourceLabel(sourceFolderDialog.SelectedPath);
        }

        /**
         * Click Handler for the "Select Destination" button
         */
        private void button2_Click(object sender, EventArgs e)
        {
            // Change dest directory
            if (destFolderDialog.ShowDialog() == DialogResult.OK)
            {
                destLabel.Text = destFolderDialog.SelectedPath;
                historyText.Text += "\nDestination directory changed:\n\tNew Destination: " + destFolderDialog.SelectedPath;
            }
        }

        /**
         * Click handler for "Transfer" button
         */
        private void button3_Click(object sender, EventArgs e)
        {
            TransferFiles();
        }

        /**
         * UpdateSourceLabel
         * 
         * Sets the source path to be srcPath and attempts to load the index
         * file in that location.
         */
        private bool UpdateSourceLabel(string srcPath)
        {
            // Check for an index file. If no index exists, display a message saying that one will be created
            // if the transfer continues.
            srcLabel.Text = srcPath;
            historyText.Text += "\nSource directory changed:\n\tNew Source: " + srcPath;

            if (index.LoadIndex(srcPath))
                historyText.Text += "\nTransfer index loaded successfullly";
            else
                historyText.Text += "\nFailed to load transfer index, will copy all files and create new index";

            return true;
        }

        /**
         * TransferFiles
         */
        private bool TransferFiles()
        {
            // Directories we're dealing with
            string srcDir = sourceFolderDialog.SelectedPath;
            string destDir = destFolderDialog.SelectedPath;
            
            // Keep track of how many files were transferred, and how many weren't
            int transferred = 0, notTransferred = 0;

            string hash, destFile;
            DateTime dateTaken;

            // Transfer fails if either the src or dest does not exist.
            if (Directory.Exists(srcDir) && Directory.Exists(destDir))
            {
                historyText.Text += "\n\n\t*****     STARTING TRANSFER     *****\n";

                // Make sure that we load the index if it's there
                index.LoadIndex(srcDir);

                string[] files = Directory.GetFiles(srcDir); // This will only get files, not directories
                foreach (string path in files)
                {
                    if (Path.GetFileName(path) == FileIndex.INDEX_FILENAME)
                        continue;   // Skip the index file

                    historyText.Text += "\nCurrent File: " + path;

                    hash = GetMD5FromFile(path);
                    historyText.Text += "\n\tMD5: " + hash;

                    if (index.ContainsHash(hash))
                    {
                        historyText.Text += "\n\tFound in index, not transferring";
                        notTransferred++;
                    }
                    else
                    {
                        historyText.Text += "\n\tNot found in index, transferring file...";

                        index.AddHash(hash);

                        // Get the date it was taken
                        dateTaken = GetDateTakenFromImage(path);
                        if (dateTaken.Ticks == 0)
                        {
                            historyText.Text += "\n\tNo date taken property found, using file creation date";
                            dateTaken = File.GetCreationTime(path);
                            historyText.Text += "\n\tDate Created: " + dateTaken;
                        }
                        else
                            historyText.Text += "\n\tDate Taken: " + dateTaken;
                        
                        string year, month;
                        year = dateTaken.Year.ToString();
                        month = dateTaken.Month.ToString().PadLeft(2, '0');

                        historyText.Text += "\n\tCopying to directory " + year + "\\" + month;                            
                        destFile = destDir + "\\" + year + "\\" + month + "\\" + Path.GetFileName(path);

                        // Make sure destination directory exists
                        if (!Directory.Exists(destDir + "\\" + year))
                        {
                            Directory.CreateDirectory(destDir + "\\" + year); // Year directory
                            historyText.Text += "\n\tCreated Directory: " + destDir + "\\" + year;
                        }

                        if (!Directory.Exists(destDir + "\\" + year + "\\" + month))
                        {
                            Directory.CreateDirectory(destDir + "\\" + year + "\\" + month); // Year directory
                            historyText.Text += "\n\tCreated Directory: " + destDir + "\\" + year + "\\" + month;
                        }
                        
                        // Transfer the file
                        File.Copy(path, destFile, true);
                        transferred++;

                        historyText.Text += "\n\tDone";
                    }

                    historyText.Text += "\n";
                }

                // Save index!
                historyText.Text += "\nSaving Index...";
                index.Save();

                historyText.Text += "\n\n\t*****     TRANSFER COMPLETE     *****";
                historyText.Text += "\n\tTransferred:  " + transferred + "\t\tSkipped:  " + notTransferred + "\n\n";
            }
            else
            {
                historyText.Text += "\nSource or Destination directory not found, aborting transfer.\n\n";
                return false;
            }

            return true;
        }

        /**
         * Simple event handler used to tell the form to close 
         * itself when it finishes autorunning.
         */
        private void CloseOnStart(object sender, EventArgs e)
        {
            this.Close();
        }

        /**
         * GetMD5FromFile 
         * 
         * Returns the MD5 hash of the contents of the file specified.
         */
        private string GetMD5FromFile(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open);
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(file);
            file.Close();
 
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
                sb.Append(retVal[i].ToString("x2"));

            return sb.ToString();
        }

        /**
         * GetDateTakenFromImage
         * 
         * Retrieves the Date Taken property of an image file as a DateTime
         * object. If the property isn't found, it returns a DateTime object
         * with 0 ticks elapsed.
         */
        public DateTime GetDateTakenFromImage(string path)
        {
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    PropertyItem propItem = myImage.GetPropertyItem(DATE_TAKEN_PROPERTY);
                    string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                    return DateTime.Parse(dateTaken);                    
                }
            }
            catch (Exception e)
            {
                return new DateTime(0);
            }
        }
    }
}
