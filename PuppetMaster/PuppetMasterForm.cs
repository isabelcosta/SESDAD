using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PuppetMaster
{
    public partial class PuppetMasterForm : Form
    {

        //***************** Attributes **********************
        String RoutingPolicy = null;
        String Ordering = null;
        String LoggingLevel = null;
        
        //Array with processes

        public PuppetMasterForm()
        {
            InitializeComponent();
        }

        private void bt_Command_Click(object sender, EventArgs e)
        {
            String singleCommand = tb_Command.Text;
            
            if (singleCommand == "") {
                    return;
            }
            
            //testing input
            //MessageBox.Show(singleCommand);

            //erases command after clicking the button
            tb_Command.Clear();

            processCommand(singleCommand);
        }

        //use a single function to read every line
        private void bt_Script_Click(object sender, EventArgs e)
        {
            String[] scriptLines = tb_Script.Lines;

            try {
                if (scriptLines[0] == "")
                {
                    return;
                }
            } catch (IndexOutOfRangeException) {
                return;
            }

            String singleLine = scriptLines[0];

            //testing input
            //MessageBox.Show(singleLine);
                    
            //erases command after clicking the button
            tb_Script.Clear();

            foreach (String line in scriptLines) {
                processCommand(line);
            }
        }

        private void processCommand(String line) {

            /*String[] p = {" "};
            string[] parsed = line.Split(p, StringSplitOptions.None);

            switch (parsed[0])
            {
                case "Subscriber":
                    if (parsed[2] == "Subscribe")
                    {
                        subscribe(parsed[1], parsed[3]);//1 - process; 3 - topic
                    }
                    else if (parsed[2] == "Unsubscribe")
                    {
                        unsubscribe(parsed[1], parsed[3]);
                    }
                    break;
                case "Publisher":
                    publish(parsed[1], parsed[3], parsed[5], parsed[7]); //1 - process; 3 - nEvents; 5 - topic; 7 - interval;
                    break;
                case "Status": status(); break;
                case "Crash": crash(parsed[1]); break;
                case "Freeze": freeze(parsed[1]); break;
                case "Unfreeze": unfreeze(parsed[1]); break;
            }*/


        }

        //private void readConfigFile(File file) { }

        private void startSystemFromConfigFile() {
            /*switch (parsed[0])
            {
                case "Site":
                    break;
                case "Process":
                    publish(parsed[1], parsed[3], parsed[5], parsed[7]); //1 - process; 3 - nEvents; 5 - topic; 7 - interval;
                    break;
                case "RoutingPolicy": status(); break;
                case "Ordering": crash(parsed[1]); break;
                case "LoggingLevel": freeze(parsed[1]); break;
            }*/
        }
    }

}