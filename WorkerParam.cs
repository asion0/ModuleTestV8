using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace ModuleTestV8
{
    public class WorkerParam
    {
        [Flags] public enum ErrorType : ulong
        {
            NoError = 0,
            WaitingPositionFixTimeout = 1UL << 1,
            GetClockOffsetFail = 1UL << 2,
            ColdStartTimeOut = 1UL << 3,
            ColdStartNack = 1UL << 4,
            QueryVersionTimeOut = 1UL << 5,
            QueryVersionNack = 1UL << 6,
            QueryRtcTimeOut = 1UL << 7,
            QueryRtcNack = 1UL << 8,

            ConfigMessageOutputTimeOut = 1UL << 9,
            ConfigMessageOutputNack = 1UL << 10,
            ConfigNmeaOutputTimeOut = 1UL << 11,
            ConfigNmeaOutputNack = 1UL << 12,
            FactoryResetTimeOut = 1UL << 13,
            FactoryResetNack = 1UL << 14,

            FirmwareVersionError = 1UL << 15,
            CheckRtcError = 1UL << 16,
            GpsSnrError = 1UL << 17,
            GlonassSnrError = 1UL << 18,
            BeidouSnrError = 1UL << 19,
            GetClockOffsetTimeout = 1UL << 20,
            GetClockOffsetNack = 1UL << 21,
            CheckClockOffsetFail = 1UL << 22,
            SetPsti50Timeout = 1UL << 23,
            SetPsti50Nack = 1UL << 24,

            NoUsed = 1UL << 25,

            DownloadCmdTimeOut = 1UL << 26,
            DownloadCmdNack = 1UL << 27,
            BinsizeCmdTimeOut = 1UL << 28,
            DownloadEndTimeOut = 1UL << 29,
            DownloadWriteFail = 1UL << 30,

            TestNotComplete = 1UL << 31,
            OpenPortFail = 1UL << 32,

            ChangeBaudRateFail = 1UL << 33,
            LoaderDownloadFail = 1UL << 34,
            UploadLoaderFail = 1UL << 35,
            UnsupportTagType = 1UL << 36,
            QueryCrcTimeOut = 1UL << 37,
            QueryCrcNack = 1UL << 38,
            FirmwareCrcError = 1UL << 39,

            TestIcacheError = 1UL << 40,
            ResetDetectError = 1UL << 41,
            NmeaDelayDetectError = 1UL << 42,

            TestIcacheTimeout = 1UL << 43,
            TestIoFail = 1UL << 44,
            TestAdcFail = 1UL << 45,
            IoControllerFail = 1UL << 46,
            TestAntennaFail = 1UL << 47,
            TestUART2Fail = 1UL << 48,
            McuFail = 1UL << 49,
            OdoPluseFail = 1UL << 50,
            OdoDirectionFail = 1UL << 51,
            GyroFail = 1UL << 52,

            TestErr10 = 1UL << 53,
        }

        public const int ErrorCount = 53;
        public static String GetErrorString(ErrorType er)
        {
            if (ErrorType.NoError == er)
            {
                return "";
            }

            StringBuilder sb = new StringBuilder();
            UInt64 nErr = (UInt64)er;
            bool first = true;
            for (byte i = 0; i < 64; i++)
            {
                UInt64 tt = nErr & ((UInt64)1 << i);

                if ((nErr & ((UInt64)1 << i)) != 0)
                {
                    if (!first)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(i.ToString());
                    first = false;
                }
            }
            return sb.ToString();
        }

        public int index;
        public String comPort;
        public BackgroundWorker bw;
        public SkytraqGps gps;
        public GpsMsgParser parser;
        public ModuleTestProfile profile;
        public double gpSnrOffset;
        public double glSnrOffset;
        public double bdSnrOffset;
        public ErrorType error;
        //for Report
        //public DateTime startTime;
        public long duration;
        public StringBuilder log;
        public String annIoPort;

    }

    public class WorkerReportParam
    {

        public enum ReportType
        {
            ShowProgress,
            GoldenSampleReady,
            UpdateSnrChart,
            ShowError,
            ShowWaitingGoldenSample,
            HideWaitingGoldenSample,
            AllTaskFinished,
            ShowFinished,
        }

        public WorkerReportParam()
        {
            reportType = ReportType.ShowProgress;
        }

        public WorkerReportParam(WorkerReportParam r)
        {
            index = r.index;
            output = r.output;
            reportType = r.reportType;
        }
        public int index { get; set; }
        public String output { get; set; }
        //public ErrorType error { get; set; }
        public ReportType reportType { get; set; }
    }
}
