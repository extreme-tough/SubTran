using System;
using System.Collections.Generic;
using System.Windows.Forms;


namespace SubTran
{
    static class Program 
    {        
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Global.Logs.Debug("Starting program");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Global.Logs.Debug("Triggering Application.Run");

            Application.Run(new mainForm());

            Global.Logs.Debug("Ending Application.Run");
            Global.Logs.Debug("Ending programs");
        }
    }
}
