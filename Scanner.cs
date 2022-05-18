using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZScannerRecovery
{
    class ScannerDefinitions
    {
        public const int STATUS_SUCCESS = 0;
        public const int STATUS_FALSE = 1;
        public const int STATUS_LOCKED = 10;

        public const int MAX_NUM_SCANNERS = 256;
        public const string MODEL_NAME = "SE4107";

        public const short SCANNER_TYPES_ALL = 1;

        public const int DEVICE_SCAN_DISABLE = 2013;
        public const int DEVICE_SCAN_ENABLE = 2014;
    }
}
