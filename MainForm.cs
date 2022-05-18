using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CoreScanner;
using System.Threading;
using System.Timers;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Management;


namespace ZScannerRecovery
{

    public enum UsbConnectionState : ushort
    {
        Unknown = 0,
        Plugged = 1,
        Unplugged = 2
    }


    public partial class MainForm : Form
    {
        CCoreScannerClass mCoreScanner = null;
        Logger log = new Logger();
        bool bCoreScannerOpened = false;
        System.Timers.Timer timer;
        System.Timers.Timer pnpMonTimer;
        short[] mScannerTypeList;
        ScannerStatus lastStatus;
        int nCheckCount = 0;

        // constants
        const string REG_SETTINGS_SUBKEY = "Software\\NEC\\EmbeddedScanner";
        const string REGVAL_ENABLE_LOG = "EnableLog";
        const string REGVAL_DELAYED_SECONDS = "DelayedSeconds";
        const string REGVAL_TIMER_INTERVAL = "TimerInterval";
        const string REGVAL_MAX_CONSEC_DISABLE = "MaxConsecutiveDisable";
        const string REGVAL_MAX_CONSEC_SEARCH = "MaxConsecutiveSearch";
        const string REGVAL_MAX_CHECK_COUNTS = "MaxCheckCounts";
        const string REGVAL_SCANNER_PORT = "ScannerPortNumber";
        const string REGVAL_VID = "VID";
        const string REGVAL_PID = "PID";
//        const string REGVAL_OPOS_DEVICE_NAME = "OposDeviceName";
        const string REGVAL_INIT_STATE = "EnableWhenDone";


        const int USB_RESET_INTERVAL = 1000;
        const int USB_RESET_WAIT_TIME = 2000;
        const int MILLISECONDS = 1000;
        const int DEFAULT_SCANNER_PORT = 10;
        const string OPOS_DEVICE_NAME = "ZEBRA_SCANNER";
        const int PNP_MONITORING_INTERVAL = 500;

        // Configurable settings
        bool bLogEnable = false;
        int nDelayedSeconds = 10; //seconds
        int nTimerInterval = 5000; //milliseconds 
        int nMaxConsecutiveDisable = 3;
        int nMaxConsecutiveSearch = 10;
        int nMaxCheckCounts = 10;
        ushort scannerVid = 0x05E0;
        ushort scannerPid = 0x1300;
        int nScannerPort = DEFAULT_SCANNER_PORT; // default port number
        bool bEnableWhenDone = false;
//        string sOposDeviceName = OPOS_DEVICE_NAME;

        public MainForm()
        {
            InitializeComponent();
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.ShowInTaskbar = false;

            readSettings();
            log.Enabled = bLogEnable;


            log.WriteLog("Started");

            try
            {
               mCoreScanner = new CCoreScannerClass();
            }
            catch (Exception ex)
            {
                log.WriteLog("Unexpected error. Failed to create CCoreScannerClass instance." 
                    + ex.ToString());
            }

            mScannerTypeList = new short[256];
            mScannerTypeList[0] = ScannerDefinitions.SCANNER_TYPES_ALL;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1, 1);
            this.Location = new Point(0, 0);
            this.Opacity = 0;

            pnpMonTimer = new System.Timers.Timer();
            pnpMonTimer.Interval = (double)(nDelayedSeconds * MILLISECONDS)/2; 
            pnpMonTimer.Elapsed += new ElapsedEventHandler(OnPnPMonitorEvent);
            pnpMonTimer.AutoReset = false;
            pnpMonTimer.Enabled = true; 


            if (mCoreScanner == null)
            {
                try
                {
                    Thread.Sleep(1000);
                    mCoreScanner = new CoreScanner.CCoreScannerClass();
                }
                catch (Exception ex)
                {
                    log.WriteLog("Unexpected error. Failed to create CCoreScannerClass instance."
                        + ex.ToString());
                    log.WriteLog("Close the application");
                    this.BeginInvoke(new MethodInvoker(this.Close));
                    return;
                }                
            }

            lastStatus = new ScannerStatus(DateTime.Now, UsbConnectionState.Unknown, false);

            timer = new System.Timers.Timer();
            timer.Interval = (double)(nDelayedSeconds * MILLISECONDS); 
            timer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            timer.AutoReset = false;
            timer.Enabled = true; 
             
            return;
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            int status = ScannerDefinitions.STATUS_FALSE;

            if(timer != null)
                 timer.Stop();

            if ((mCoreScanner != null) && (bCoreScannerOpened == true))
            {
                mCoreScanner.Close(0, out status);
            }

            if (pnpMonTimer != null)
            {
                pnpMonTimer.Stop();
                pnpMonTimer = null;
            }

        }


        private bool FinalValidation()
        {
            bool bResult = false;
            int status = ScannerDefinitions.STATUS_FALSE;
            string sScannerID;
            bool bFound = false;
            uint dwResult = 0;

            if (!bCoreScannerOpened)
            {
                mCoreScanner.Open(0, mScannerTypeList, (short) 1, out status);
                if (status == ScannerDefinitions.STATUS_SUCCESS)
                    bCoreScannerOpened = true;
            }

            bFound = FindScanner(mCoreScanner, ScannerDefinitions.MODEL_NAME, out sScannerID);

            if(bFound)
            {
                log.WriteLog("Found the scanner. Perform last validation.");

                if(EnableScanner(mCoreScanner, bEnableWhenDone, sScannerID))
                {
                    log.WriteLog("Scanner was " + (bEnableWhenDone ? "enabled" : "disabled") +" successfully.");
                    lastStatus.Disabled = !bEnableWhenDone;
                }
                else
                {
                    log.WriteLog("Failed to " + (bEnableWhenDone ? "enable" : "disable") + " the scanner");
                }

                if (bCoreScannerOpened)
                {
                    mCoreScanner.Close(0, out status);
                    if (status == ScannerDefinitions.STATUS_SUCCESS)
                        bCoreScannerOpened = false;
                }
            }
            else
	        {
                if (bCoreScannerOpened)
                {
                    mCoreScanner.Close(0, out status);
                    if (status == ScannerDefinitions.STATUS_SUCCESS)
                        bCoreScannerOpened = false;
                }

                log.WriteLog("Reset USB port");
                dwResult = UsbControl.ResetUsbPort((ushort)nScannerPort, USB_RESET_INTERVAL);
                if (dwResult != UsbControl.error_success)
                {
                    log.WriteLog("Failed to reset USB port number " + nScannerPort + "(error=" + dwResult + ")");
                }
	        }

            log.WriteLog("Finished");
            return bResult;
        }

        private void OnPnPMonitorEvent(object source, ElapsedEventArgs e)
        {
            uint dwResult = UsbControl.FindUsbDevice(scannerVid, scannerPid);
            if (dwResult == UsbControl.error_success)
            {
                if (lastStatus.State != UsbConnectionState.Plugged)
                {
                    lastStatus.State = UsbConnectionState.Plugged;
                    lastStatus.Timestamp = DateTime.Now;
                    lastStatus.Disabled = false;
                    log.WriteLog("Scanner is plugged.");
                }
            }
            else if(dwResult == UsbControl.error_not_found)
            {
                if (lastStatus.State != UsbConnectionState.Unplugged)
                {
                    lastStatus.State = UsbConnectionState.Unplugged;
                    lastStatus.Timestamp = DateTime.Now;
                    lastStatus.Disabled = false;
                    log.WriteLog("Scanner is unplugged.");
                }
            }
            else if (dwResult == UsbControl.error_timeout)
            {
                 log.WriteLog("Timeout error while finding the scanner.");
            }
            else
            {
                log.WriteLog("Unexpected error while trying to check the existence of the scanner.");    
            }

            if (pnpMonTimer.Interval != PNP_MONITORING_INTERVAL)
            {
                pnpMonTimer.Interval = PNP_MONITORING_INTERVAL;
                pnpMonTimer.AutoReset = true;
                pnpMonTimer.Enabled = true;
            }
        }


        private void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            int status = ScannerDefinitions.STATUS_FALSE;
            string sScannerID;
            bool bFound = false;
            bool bDisableSuccess = false;
            uint dwResult = 0;

            nCheckCount++;
            if (nCheckCount >= nMaxCheckCounts)
            {
                log.WriteLog("Final validation");
                if (pnpMonTimer != null)
                {
                    pnpMonTimer.Stop();
                    pnpMonTimer = null;
                }
                FinalValidation();
                this.BeginInvoke(new MethodInvoker(this.Close));
                return;
            }

            if ((lastStatus.State == UsbConnectionState.Plugged && lastStatus.Disabled == true))
            {
                log.WriteLog(" Skip validataion (previous validation was successful).");
                goto OnTimedEvent_Exit;
            }

            if(lastStatus.State == UsbConnectionState.Unplugged)
            {
                TimeSpan ts = DateTime.Now - lastStatus.Timestamp;
                if (ts.TotalMilliseconds < USB_RESET_WAIT_TIME)
                {
                    log.WriteLog("Scanner is still not plugged. Waiting a bit longer.");
                    goto OnTimedEvent_Exit;
                }
            }

            if (!bCoreScannerOpened)
            {
                mCoreScanner.Open(0, mScannerTypeList, (short) 1, out status);
                if (status == ScannerDefinitions.STATUS_SUCCESS)
                    bCoreScannerOpened = true;
                else
                    log.WriteLog("Unexpected error. Failed to open the core scanner driver.");
            }

            bFound = FindScanner(mCoreScanner, ScannerDefinitions.MODEL_NAME, out sScannerID);
            if (bFound)
            {
                log.WriteLog("Scanner was found.");
                lastStatus.State = UsbConnectionState.Plugged;
                if (EnableScanner(mCoreScanner, false, sScannerID))
                {
                    log.WriteLog("Scanner was disabled successfully.");
                    bDisableSuccess = true;
                    lastStatus.Disabled = true;
                }
                else
                {
                    log.WriteLog("Failed to disable the scanner");
                    lastStatus.Disabled = false;
                }
            }

            if(bCoreScannerOpened)
            {
                mCoreScanner.Close(0, out status);
                if (status == ScannerDefinitions.STATUS_SUCCESS)
                    bCoreScannerOpened = false;
                else
                    log.WriteLog("Unexpected error. Failed to close the core scanner driver.");
            }

            if (!bFound || !bDisableSuccess)
            {
                if (bFound)
                {
                    log.WriteLog("Reset USB device");
                    dwResult = UsbControl.ResetUsbDevice(scannerVid, scannerPid, USB_RESET_INTERVAL);
                    if (dwResult != UsbControl.error_success)
                    {
                        log.WriteLog("Failed to reset USB port device (error=" + dwResult + ")");
                    }
                }
                else
                {
                    log.WriteLog("Reset USB port");
                    dwResult = UsbControl.ResetUsbPort((ushort)nScannerPort, USB_RESET_INTERVAL);
                    if (dwResult != UsbControl.error_success)
                    {
                        log.WriteLog("Failed to reset USB port number " + nScannerPort + " (error=" + dwResult + ")");
                    }
                }
            }


OnTimedEvent_Exit:
             timer.Interval = (double)nTimerInterval;
             timer.Enabled = true;
        }

        private bool FindScanner(CCoreScannerClass coreScanner, string name, out string sScannerID)
        {
            sScannerID = null;
            short numOfScanners;
            int[] scannerIdList = new int[ScannerDefinitions.MAX_NUM_SCANNERS];  
            string outXml;
            int status = ScannerDefinitions.STATUS_FALSE;
            bool bFound = false;

            if (coreScanner == null || !bCoreScannerOpened)
            {
                log.WriteLog("Unexpected error. Invalid parameter or the core scanner is not opened.");
                return false;
            }

            try
            {
                for (int i = 0; i < nMaxConsecutiveSearch; i++) 
                {
                    coreScanner.GetScanners(out numOfScanners, scannerIdList, out outXml, out status);
                    if (status == ScannerDefinitions.STATUS_SUCCESS)
                    {
                        sScannerID = XmlReader.GetScannerIdFromXml(outXml, ScannerDefinitions.MODEL_NAME);

                        if (!String.IsNullOrEmpty(sScannerID))
                        {
                            bFound = true;
                            break;
                        }
                    }
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex) 
            {
                log.WriteLog("Failed to get scanners." + ex.ToString());
                return false;
            }

            return bFound;
        }

        private bool EnableScanner(CCoreScannerClass coreScanner, bool bEnable, string sScannerID)
        {
            bool bResult = false;
            int status; 
            string outXml;
            int iOpcode = bEnable ? ScannerDefinitions.DEVICE_SCAN_ENABLE : ScannerDefinitions.DEVICE_SCAN_DISABLE;
            string inXml = "<inArgs>" +
                              "<scannerID>" + sScannerID + "</scannerID>" +
                           "</inArgs>";

            if (coreScanner == null || !bCoreScannerOpened)
            {
                log.WriteLog("Unexpected error. Invalid parameter or the core scanner is not opened.");
                return false;
            }

            for (int i = 0; i < nMaxConsecutiveDisable; i++) 
            {
                coreScanner.ExecCommand(iOpcode, ref inXml, out outXml, out status);
                if (status != ScannerDefinitions.STATUS_SUCCESS)
                {
                    Thread.Sleep(500);
                    continue;
                }
                else
                {
                    bResult = true;
                    break;
                }
            }

            return bResult;
        }

        private void readSettings()
        {
            RegistryKey key = null;

            try
            {
                key = Registry.LocalMachine.OpenSubKey(REG_SETTINGS_SUBKEY);
                if (key == null)
                    return;

                int regValue = 0;
                regValue = (int)key.GetValue(REGVAL_ENABLE_LOG, regValue);
                bLogEnable = (regValue == 0) ? false : true;

                regValue = 0;
                regValue = (int)key.GetValue(REGVAL_INIT_STATE, regValue);
                bEnableWhenDone = (regValue == 0) ? false : true;

                scannerVid = (ushort) (int)key.GetValue(REGVAL_VID, scannerVid);
                scannerPid = (ushort) (int)key.GetValue(REGVAL_PID, scannerPid);
                nDelayedSeconds = (int) key.GetValue(REGVAL_DELAYED_SECONDS, nDelayedSeconds);
                nDelayedSeconds = (nDelayedSeconds < 1) ? 1 : nDelayedSeconds;
                nDelayedSeconds = (nDelayedSeconds > 10) ? 10 : nDelayedSeconds;
                nTimerInterval = (int) key.GetValue(REGVAL_TIMER_INTERVAL, nTimerInterval);
                nTimerInterval = (nTimerInterval < 2000) ? 2000 : nTimerInterval;
                nTimerInterval = (nTimerInterval > 20000) ? 20000 : nTimerInterval;
                nMaxConsecutiveDisable = (int) key.GetValue(REGVAL_MAX_CONSEC_DISABLE, nMaxConsecutiveDisable);
                nMaxConsecutiveDisable = (nMaxConsecutiveDisable < 1) ? 1 : nMaxConsecutiveDisable;
                nMaxConsecutiveDisable = (nMaxConsecutiveDisable > 10) ? 10 : nMaxConsecutiveDisable; 
                nMaxConsecutiveSearch = (int)key.GetValue(REGVAL_MAX_CONSEC_SEARCH, nMaxConsecutiveSearch);
                nMaxConsecutiveSearch = (nMaxConsecutiveSearch < 1) ? 1 : nMaxConsecutiveSearch;
                nMaxConsecutiveSearch = (nMaxConsecutiveSearch > 30) ? 30 : nMaxConsecutiveSearch;
                nMaxCheckCounts = (int)key.GetValue(REGVAL_MAX_CHECK_COUNTS, nMaxCheckCounts);
                nMaxCheckCounts = (nMaxCheckCounts < 3) ? 3 : nMaxCheckCounts;
                nMaxCheckCounts = (nMaxCheckCounts > 20) ? 20 : nMaxCheckCounts;
                nScannerPort = (int)key.GetValue(REGVAL_SCANNER_PORT, nScannerPort);
//                sOposDeviceName = (string)key.GetValue(REGVAL_OPOS_DEVICE_NAME, sOposDeviceName);

                UsbControl.ResetUsbPort((ushort)nScannerPort, USB_RESET_INTERVAL);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                if (key != null)
                    key.Close();
            }
        }

    }


    public class ScannerStatus
    {

        private UsbConnectionState state;
        private DateTime timestamp;
        private bool disabled;

        public ScannerStatus(DateTime t, UsbConnectionState usbStatus = UsbConnectionState.Unknown,
                            bool bDisabled = false)
        {
            state = UsbConnectionState.Unknown;
            timestamp = t;
            disabled = bDisabled;
        }

        public UsbConnectionState State
        {
            get { return state; }
            set { state = value; }
        }

        public DateTime Timestamp
        {
            get { return timestamp; }
            set { timestamp = value; }
        }

        public bool Disabled
        {
            get { return disabled; }
            set { disabled = value; }
        }
    };
}
