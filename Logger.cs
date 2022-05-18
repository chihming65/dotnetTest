using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace ZScannerRecovery
{
    class Logger
    {
        String path;
        private bool bEnable = false;

        public Logger()
        {
            String codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            path = Path.GetDirectoryName(Uri.UnescapeDataString(uri.Path));
        }


        public bool Enabled
        {
            get { return bEnable; }
            set { bEnable = value; } 
        }

        public void WriteLog(string log)
        {
            if (!bEnable)
                return;

            String sData, sFileName;
            DateTime timeStamp = DateTime.Now;        

            sFileName = path + "\\ZScannerRecovery" + timeStamp.ToString("yyyyMMdd") + ".log";
            sData = timeStamp.ToString("yyyy/MM/dd,HH:mm:ss:fff,") + log;

            if(!File.Exists(sFileName))
            {
                using (StreamWriter sw = File.CreateText(sFileName))
                {
                    sw.WriteLine(sData);
                }
            }
            else
            {
                using (StreamWriter sw = File.AppendText(sFileName))
                {
                    sw.WriteLine(sData);
                }
            }
        }
    }
}
