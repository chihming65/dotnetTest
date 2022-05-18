using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace ZScannerRecovery
{
    static class Program
    {
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            using (Mutex mutex = new Mutex(false, "SE4107 Scanner Recovery Program"))
            {
                if (!mutex.WaitOne(0, false))
                {
#if !DEBUG
                    MessageBox.Show("Another instance is already running.", "SE4107 Scanner Recovery Program",
                        MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
#endif
                    return;

                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }
    }
}
