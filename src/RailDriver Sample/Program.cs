using System;
using System.Windows.Forms;

namespace RailDriver.Sample
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (Form1 form1 = new Form1())
            {
                Application.Run(form1);
            }
        }
    }
}
