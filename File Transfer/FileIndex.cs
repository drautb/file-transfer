using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace File_Transfer
{
    /**
     * This class represents the index that the program uses to make sure that
     * it doesn't transfer files that have already been transferred.
     */
    class FileIndex
    {
        public static string INDEX_FILENAME = "transferindex.txt";

        private string indexLocation = "";

        private HashSet<string> index = new HashSet<string>();

        public FileIndex(string indexLocation = "")
        {
            index.Clear();

            this.indexLocation = indexLocation;
            if (indexLocation.Length > 0)
                this.LoadIndex(indexLocation);
        }

        public bool LoadIndex(string indexLocation)
        {
            this.indexLocation = indexLocation;

            // Attempt to open the file, if it doesn't exist, it will be created.
            if (!File.Exists(this.indexLocation + "\\" + INDEX_FILENAME))
                return false;

            StreamReader idxFile = new StreamReader(this.indexLocation + "\\" + INDEX_FILENAME);

            string line = "";
            while (idxFile.Peek() > 0)
            {
                line = idxFile.ReadLine();
                index.Add(line);
            }

            idxFile.Close();

            return true;
        }

        public bool Save()
        {
            StreamWriter outFile = new StreamWriter(this.indexLocation + "\\" + INDEX_FILENAME);

            foreach (string hash in index)
            {
                outFile.WriteLine(hash);
            }

            outFile.Close();
            index.Clear();

            return true;
        }

        public bool ContainsHash(string hash)
        {
            return index.Contains(hash);
        }

        public bool AddHash(string hash)
        {
            return index.Add(hash);
        }

        public void DumpIndex(System.Windows.Forms.TextBoxBase textBox)
        {
            textBox.Text += "\nINDEX DUMP:";
            foreach (string line in index)
                textBox.Text += "\n" + line;
            textBox.Text += "\n";
        }
    }
}
