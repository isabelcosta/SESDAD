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
            String[] lines = tb_Command.Lines;
            try {
                if (lines[0] == "") {
                    return;
                }
            } catch (IndexOutOfRangeException) {
                return;
            }
            String singleCommand = lines[0];

            //erases command after clicking the button
            tb_Command.Clear();
/*
            String[] p = {" "};
            string[] parsed = singleCommand.Split(p, StringSplitOptions.None);

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

        private void bt_Script_Click(object sender, EventArgs e)
        {

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