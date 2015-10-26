using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //Read config file
            //TextReader tr = new StreamReader(@"\obj.txt");
            //MessageBox.Show(Environment.CurrentDirectory);

            //Initialize PuppetMaster GUI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PuppetMasterForm());
        }
    }
}
