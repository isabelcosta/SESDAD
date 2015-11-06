﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Threading;
using SESDADInterfaces;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;

namespace SESDAD
{
    //class Program
    partial class PuppetMasterForm
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(String[] args)
        {

            //BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            //provider.TypeFilterLevel = TypeFilterLevel.Full;
            //IDictionary props = new Hashtable();
            //props["port"] = 30000 + args[0];
            TcpChannel channel = new TcpChannel(30000 + int.Parse(args[0]));
            //TcpChannel channel = new TcpChannel(props, null, provider); 
            //TcpChannel channel = new TcpChannel(puppetPort);
            ChannelServices.RegisterChannel(channel, false);

            PuppetServices servicos = new PuppetServices();
            RemotingServices.Marshal(servicos, "puppet",
                typeof(PuppetServices));

            //Initialize PuppetMaster GUI
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new PuppetMasterForm(args));
        }
    }
}
