using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Diagnostics;

namespace ModuleTestV8
{
    class TestModule
    {
        private const int DefaultCmdTimeout = 1000;
        public static GpsMsgParser.ParsingStatus[] dvResult;
        public static UInt32 gdClockOffset = 0;
        public static void ClearResult()
        {
            if (dvResult == null)
            {
                return;
            }

            for (int i = 0; i < ModuleTestForm.ModuleCount; i++)
            {
                dvResult[i].ClearAllSate();
                dvResult[i].positionFixResult = 0;
            }        
        }

        public TestModule()
        {
            dvResult = new GpsMsgParser.ParsingStatus[ModuleTestForm.ModuleCount];
            for (int i = 0; i < ModuleTestForm.ModuleCount; i++)
            {
                dvResult[i] = new GpsMsgParser.ParsingStatus();
            }
        }

        private static EventWaitHandle controllerEvent = new ManualResetEvent(false);
        private static int TotalTestCount = 0;
        public static void IncreaseTotalTestCount()
        {
            ++TotalTestCount;
        }
        public static void DecreaseTotalTestCount()
        {
            --TotalTestCount;
            if (TotalTestCount <= WaitingCount)
            {
                controllerEvent.Set();
            }
        }
        public static void ResetTotalTestCount()
        {
            TotalTestCount = 0;
            WaitingCount = 0;
            controllerEvent.Reset();
        }

        static int motoPosition = 0;
        public static bool ResetMotoPosition(string mcuPort)
        {
            if (motoPosition == 1 || motoPosition == 2)
            {
                if (controllerIO == null)
                {
                    return false;
                }

                //6f 03 00 1f 1e 1c 14 10 00 00 1D B0 00 00 02 F0
                GPS_RESPONSE rep = controllerIO.SetControllerMoto(0, 31, 30, 28, 20, 16, motorDelay, motorStep);
                if (GPS_RESPONSE.ACK != rep)
                {
                    return false;
                }
                Thread.Sleep(3000);
                motoPosition = 0;
            }
            motoPosition = 0;
            return true;
        }

        private static int WaitingCount = 0;
        public static bool IncreaseWaitingCount()
        {
            if (++WaitingCount >= TotalTestCount)
            {
                controllerEvent.Set();
                WaitingCount = 0;
                return true;
            }
            else
            {
                controllerEvent.Reset();

            }
            return false;
        }

        public static void ResetWaitingCount()
        {
            WaitingCount = 0;
            controllerEvent.Reset();
        }

        const int GpsCompareCount = 3;
        const int GlonassCompareCount = 2;
        const int BeidouCompareCount = 2;
        private bool CompareResult(WorkerParam p, ref WorkerReportParam r, GpsMsgParser.SateType t)
        {
            GpsMsgParser.ParsingStatus.sateInfo[] dvSate = null;
            int snrLimit = 0;
            String usingSnrTxt;
            String dvSnrTxt;
            String gdSnrTxt;
            String dvSnrAvgTxt;
            String gdSnrAvgTxt;
            String gdSnrLimitErrorTxt;
            String dvSnrLimitErrorTxt;
            //int passSel = 0;
            int testSnrUpper = 0;
            int testSnrLower = 0;
            String dvTestPassTxt;
            double snrOffset = 0.0;
            int CompareCount = 0;
            switch (t)
            {
                case GpsMsgParser.SateType.Gps:
                    dvSate = dvResult[p.index].GetSortedGpsSateArray();
                    snrLimit = p.profile.gpSnrLimit;
                    usingSnrTxt = "Using GPS PRN : ";
                    dvSnrTxt = "Device GPS SNR : ";
                    gdSnrTxt = "Golden GPS SNR : ";
                    dvSnrAvgTxt = "Device average GPS SNR" + "(" + p.gpSnrOffset.ToString() + ") : ";
                    gdSnrAvgTxt = "Golden average GPS SNR : ";
                    gdSnrLimitErrorTxt = "Golden sample GPS SNR over the limit.";
                    dvSnrLimitErrorTxt = "Device sample GPS SNR over the limit.";
                    //passSel = p.profile.gpPassSel;
                    testSnrUpper = p.profile.gpSnrUpper;
                    testSnrLower = p.profile.gpSnrLower;
                    dvTestPassTxt = "Device GPS SNR test pass.";
                    snrOffset = p.gpSnrOffset;
                    CompareCount = GpsCompareCount;
                    break;
                case GpsMsgParser.SateType.Glonass:
                    dvSate = dvResult[p.index].GetSortedGlonassSateArray();
                    snrLimit = p.profile.glSnrLimit;
                    usingSnrTxt = "Using Glonass PRN : ";
                    dvSnrTxt = "Device Glonass SNR : ";
                    gdSnrTxt = "Golden Glonass SNR : ";
                    dvSnrAvgTxt = "Device average Glonass SNR" + "(" + p.glSnrOffset.ToString() + ") : ";
                    gdSnrAvgTxt = "Golden average Glonass SNR : ";
                    gdSnrLimitErrorTxt = "Golden sample Glonass SNR over the limit.";
                    dvSnrLimitErrorTxt = "Device sample Glonass SNR over the limit.";
                    //passSel = p.profile.glPassSel;
                    testSnrUpper = p.profile.glSnrUpper;
                    testSnrLower = p.profile.glSnrLower;
                    dvTestPassTxt = "Device Glonass SNR test pass.";
                    snrOffset = p.glSnrOffset;
                    CompareCount = GlonassCompareCount;
                    break;
                case GpsMsgParser.SateType.Beidou:
                    dvSate = dvResult[p.index].GetSortedBeidouSateArray();
                    snrLimit = p.profile.bdSnrLimit;
                    usingSnrTxt = "Using Beidou PRN : ";
                    dvSnrTxt = "Device Beidou SNR : ";
                    gdSnrTxt = "Golden Beidou SNR : ";
                    dvSnrAvgTxt = "Device average Beidou SNR" + "(" + p.bdSnrOffset.ToString() + ") : ";
                    gdSnrAvgTxt = "Golden average Beidou SNR : ";
                    gdSnrLimitErrorTxt = "Golden sample Beidou SNR over the limit.";
                    dvSnrLimitErrorTxt = "Device sample Beidou SNR over the limit.";
                    //passSel = p.profile.bdPassSel;
                    testSnrUpper = p.profile.bdSnrUpper;
                    testSnrLower = p.profile.bdSnrLower;
                    dvTestPassTxt = "Device Beidou test pass.";
                    snrOffset = p.bdSnrOffset;
                    CompareCount = BeidouCompareCount;
                    break;
                default:
                    dvSate = dvResult[p.index].GetSortedGpsSateArray();
                    snrLimit = p.profile.gpSnrLimit;
                    usingSnrTxt = "Using GPS PRN : ";
                    dvSnrTxt = "Device GPS SNR : ";
                    gdSnrTxt = "Golden GPS SNR : ";
                    dvSnrAvgTxt = "Device average GPS SNR" + "(" + p.gpSnrOffset.ToString() + ") : ";
                    gdSnrAvgTxt = "Golden average GPS SNR : ";
                    gdSnrLimitErrorTxt = "Golden sample Gps SNR over the limit.";
                    dvSnrLimitErrorTxt = "Device sample Gps SNR over the limit.";
                    //passSel = p.profile.gpPassSel;
                    testSnrUpper = p.profile.gpSnrUpper;
                    testSnrLower = p.profile.gpSnrLower;
                    dvTestPassTxt = "Device Gps SNR test pass.";
                    snrOffset = p.gpSnrOffset;
                    CompareCount = GpsCompareCount;
                    break;
            }

            if (dvSate[2].snr <= 0 || dvSate[1].snr <= 0 || dvSate[0].snr <= 0)
            {
                //return false;
            }

            GpsMsgParser.ParsingStatus.sateInfo[] selGd = new GpsMsgParser.ParsingStatus.sateInfo[CompareCount];
            GpsMsgParser.ParsingStatus.sateInfo[] selDv = new GpsMsgParser.ParsingStatus.sateInfo[CompareCount];
            int idxGd = 0;
            int idxDv = 0;
            do
            {
                if (dvSate[idxDv].snr < 0)
                {
                    break;
                }

                if (t == GpsMsgParser.SateType.Glonass &&
                    (dvSate[idxDv].prn == 6 || dvSate[idxDv].prn == -7))
                {   //Pass glonass satellite with off-peak frequency.
                    ++idxDv;
                    continue;
                }

                if (dvResult[0].GetSnr(dvSate[idxDv].prn, t).snr > 0)
                {
                    selGd[idxGd] = dvResult[0].GetSnr(dvSate[idxDv].prn, t);
                    selDv[idxGd] = dvSate[idxDv];
                    ++idxGd;
                }
                ++idxDv;
            }
            while (idxGd < CompareCount);
            if (idxGd < CompareCount)
            {
                return false;
            }

            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            StringBuilder sb = new StringBuilder(128);
            sb.Append(usingSnrTxt);
            for (int i = 0; i < CompareCount; ++i)
            {
                sb.Append(selDv[i].prn.ToString());
                if (i < CompareCount - 1)
                {
                    sb.Append(", ");
                }
            }
            r.output = sb.ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            sb.Remove(0, sb.Length);
            sb.Append(dvSnrTxt);
            for (int i = 0; i < CompareCount; ++i)
            {
                sb.Append(selDv[i].snr.ToString());
                if (i < CompareCount - 1)
                {
                    sb.Append(", ");
                }
            }
            r.output = sb.ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            sb.Remove(0, sb.Length);
            sb.Append(gdSnrTxt);
            for (int i = 0; i < CompareCount; ++i)
            {
                sb.Append(selGd[i].snr.ToString());
                if (i < CompareCount - 1)
                {
                    sb.Append(", ");
                }
            }
            r.output = sb.ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            double gdAverage = 0, dvAverage = 0;
            for (int i = 0; i < CompareCount; ++i)
            {
                gdAverage += selGd[i].snr;
                dvAverage += selDv[i].snr;
            }
            gdAverage /= CompareCount;
            dvAverage /= CompareCount;
            dvAverage += snrOffset;

            r.output = dvSnrAvgTxt + dvAverage.ToString("F2");
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            r.output = gdSnrAvgTxt + gdAverage.ToString("F2");
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            if (gdAverage > snrLimit)
            {
                r.output = gdSnrLimitErrorTxt;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            if (dvAverage > p.profile.gpSnrLimit)
            {
                r.output = dvSnrLimitErrorTxt;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            double diff = dvAverage - gdAverage;
            if (diff <= testSnrUpper &&
                diff >= testSnrLower)
            {
                r.output = dvTestPassTxt;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return true;
            }
            return false;
        }

        private bool CompareBeidouResult(WorkerParam p, ref WorkerReportParam r)
        {
            GpsMsgParser.ParsingStatus.sateInfo[] dvSate = null;
            int snrLimit = 0;
            String usingSnrTxt;
            String dvSnrTxt;
            String gdSnrTxt;
            String dvSnrAvgTxt;
            String gdSnrAvgTxt;
            String gdSnrLimitErrorTxt;
            String dvSnrLimitErrorTxt;
            //int passSel = 0;
            int testSnrUpper = 0;
            int testSnrLower = 0;
            String dvTestPassTxt;
            double snrOffset = 0.0;
            int CompareCount = 0;

            dvSate = dvResult[p.index].GetSortedBeidouSateArray();
            snrLimit = p.profile.bdSnrLimit;
            usingSnrTxt = "Using Beidou PRN : ";
            dvSnrTxt = "Device Beidou SNR : ";
            gdSnrTxt = "Golden Beidou SNR : ";
            dvSnrAvgTxt = "Device average Beidou SNR" + "(" + p.bdSnrOffset.ToString() + ") : ";
            gdSnrAvgTxt = "Golden average Beidou SNR : ";
            gdSnrLimitErrorTxt = "Golden sample Beidou SNR over the limit.";
            dvSnrLimitErrorTxt = "Device sample Beidou SNR over the limit.";
            //passSel = p.profile.bdPassSel;
            testSnrUpper = p.profile.bdSnrUpper;
            testSnrLower = p.profile.bdSnrLower;
            dvTestPassTxt = "Device Beidou test pass.";
            snrOffset = p.bdSnrOffset;
            CompareCount = BeidouCompareCount;

            if (dvSate[2].snr <= 0 || dvSate[1].snr <= 0 || dvSate[0].snr <= 0)
            {
                //return false;
            }

            GpsMsgParser.ParsingStatus.sateInfo[] selGd = new GpsMsgParser.ParsingStatus.sateInfo[CompareCount];
            GpsMsgParser.ParsingStatus.sateInfo[] selDv = new GpsMsgParser.ParsingStatus.sateInfo[CompareCount];
            int idxGd = 0;
            int idxDv = 0;
            do
            {
                if (dvSate[idxDv].snr < 0)
                {
                    break;
                }

                if (dvResult[0].GetRealBeidouSnr(dvSate[idxDv].prn, GpsMsgParser.SateType.Beidou).snr > 0)
                {
                    selGd[idxGd] = dvResult[0].GetRealBeidouSnr(dvSate[idxDv].prn, GpsMsgParser.SateType.Beidou);
                    selDv[idxGd] = dvSate[idxDv];
                    ++idxGd;
                }
                ++idxDv;
            }
            while (idxGd < CompareCount);
            if (idxGd < CompareCount)
            {
                return false;
            }

            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            StringBuilder sb = new StringBuilder(128);
            sb.Append(usingSnrTxt);
            for (int i = 0; i < CompareCount; ++i)
            {
                sb.Append(selDv[i].prn.ToString());
                if (i < CompareCount - 1)
                {
                    sb.Append(", ");
                }
            }
            r.output = sb.ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            sb.Remove(0, sb.Length);
            sb.Append(dvSnrTxt);
            for (int i = 0; i < CompareCount; ++i)
            {
                sb.Append(selDv[i].snr.ToString());
                if (i < CompareCount - 1)
                {
                    sb.Append(", ");
                }
            }
            r.output = sb.ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            sb.Remove(0, sb.Length);
            sb.Append(gdSnrTxt);
            for (int i = 0; i < CompareCount; ++i)
            {
                sb.Append(selGd[i].snr.ToString());
                if (i < CompareCount - 1)
                {
                    sb.Append(", ");
                }
            }
            r.output = sb.ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            double gdAverage = 0, dvAverage = 0;
            for (int i = 0; i < CompareCount; ++i)
            {
                gdAverage += selGd[i].snr;
                dvAverage += selDv[i].snr;
            }
            gdAverage /= CompareCount;
            dvAverage /= CompareCount;
            dvAverage += snrOffset;

            r.output = dvSnrAvgTxt + dvAverage.ToString("F2");
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            r.output = gdSnrAvgTxt + gdAverage.ToString("F2");
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            if (gdAverage > snrLimit)
            {
                r.output = gdSnrLimitErrorTxt;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            if (dvAverage > p.profile.gpSnrLimit)
            {
                r.output = dvSnrLimitErrorTxt;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            double diff = dvAverage - gdAverage;
            if (diff <= testSnrUpper &&
                diff >= testSnrLower)
            {
                r.output = dvTestPassTxt;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return true;
            }
            return false;
        }

        public bool CheckAdc(uint adc)
        {
            const int adcRangeTop = 0x70;
            const int adcRangeBottom = 0x10;
            const int adcCenter = 0x300;
            if (adc > (adcCenter - adcRangeBottom) && adc < (adcCenter + adcRangeTop))
            {
                return true;
            }
            return false;
        }

        public static  EventWaitHandle antennaEvent = new AutoResetEvent(false);

        private bool OpenDevice(WorkerParam p, WorkerReportParam r, int baudIdx)
        {
            GPS_RESPONSE rep = p.gps.Open(p.comPort, baudIdx);

            if (GPS_RESPONSE.UART_FAIL == rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OpenPortFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                //EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Open " + p.comPort + " in " +
                    p.gps.GetBaudRate().ToString() + " success.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            return true;
        }
        
        private bool DoColdStart(WorkerParam p, WorkerReportParam r, int retry)
        {
            GPS_RESPONSE rep = p.gps.SendColdStart(retry, 2000);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ColdStartNack : WorkerParam.ErrorType.ColdStartTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                //                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Cold start success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(500);  //For venus 6 testing.
            }
            return true;
        }

        private bool DoQueryVersion(WorkerParam p, WorkerReportParam r)
        {
            String kVer = "";
            String sVer = "";
            String rev = "";
            GPS_RESPONSE rep = p.gps.QueryVersion(DefaultCmdTimeout, ref kVer, ref sVer, ref rev);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.QueryVersionNack : WorkerParam.ErrorType.QueryVersionTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                //EndProcess(p);
                return false;
            }
            else if (!p.profile.fwProfile.kVersion.Equals(kVer) ||
                    !p.profile.fwProfile.sVersion.Equals(sVer) ||
                    !p.profile.fwProfile.rVersion.Equals(rev))
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.FirmwareVersionError;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                //EndProcess(p);
                return false;
            }

            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "Check version pass";
            p.bw.ReportProgress(0, new WorkerReportParam(r));
            return true;
        }

        private bool TestNavSparkIo(WorkerParam p, WorkerReportParam r)
        {
            Thread.Sleep(1200);
            GPS_RESPONSE rep = p.gps.ChangeBaudrate((byte)5, 2);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.ChangeBaudRateFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Change baud rate success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            String dbgOutput = "";
            rep = p.gps.SendLoaderDownload(ref dbgOutput);
            if (GPS_RESPONSE.OK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.LoaderDownloadFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Loader Download success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));

                rep = p.gps.UploadLoader(Properties.Resources.NavSparkIoTester);
                if (GPS_RESPONSE.OK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.UploadLoaderFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }

                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Upload Loader success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(1000);
            }

            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            w.Reset();
            w.Start();

            bool ioTestPass = false;
            uint adc = 0;
            while (w.ElapsedMilliseconds < 5000)
            {
                byte[] buff = new byte[256];
                int l = p.gps.ReadLineNoWait(buff, 256, 2000);
                string line = Encoding.UTF8.GetString(buff, 0, l);

                if (line.Length > 0)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = line;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                if (line.Contains("FINISH"))
                {
                    ioTestPass = true;
                    break;
                }
                if (line.Contains("FAIL"))
                {
                    break;
                }
                if (line.Contains("ADC:"))
                {
                    uint a = Convert.ToUInt32(line.Split(' ')[2], 16);
                    if (CheckAdc(a))
                    {
                        adc = a;
                    }
                }
            };

            if (ioTestPass)
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Test IO success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.TestIoFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            if (CheckAdc(adc))
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Test ADC success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.TestAdcFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            return true;
        }

        private bool TestNavSparkMiniIo(WorkerParam p, WorkerReportParam r)
        {
            //TEST01 = Flag IoCount io1 io2 io3 io4 ......
            //Flag - bit wise for test function : 0 - IO Test, 1 - GSN MAG Test, 2 - Rtc Test
            //IoCount - Test IO pair count
            //io1, io2... - High byte - gpio pin from, Low byte gpio pin to.
            return DoIoSrecTest(p, r, Properties.Resources.IoTesterSrec, "TEST01 = 0001 0004 0304 0305 1E1F 1C1D ", 5000);
            //     DoIoSrecTest(p, r, Properties.Resources.IoTesterSrec, "TEST01 = 0001 000F 0119 1C1D 0C0D 0E16 0809 151B 100F 1406 0002 181A 1807 0B0A 0B17 031E 031F ", 5000))
       }

        private bool DoIoSrecTest(WorkerParam p, WorkerReportParam r, string testSrec, string srecCmd, int timeout)
        {
            String dbgOutput = "";
            GPS_RESPONSE rep = p.gps.SendLoaderDownload(ref dbgOutput);
            if (dbgOutput != "")
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = dbgOutput;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            if (GPS_RESPONSE.OK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.TestLoaderDownloadFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Loader Download success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));

                rep = p.gps.UploadLoader(testSrec);
                if (GPS_RESPONSE.OK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUploadLoaderFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }

                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Upload Loader success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(1000);
            }
            //TEST01 = Flag IoCount io1 io2 io3 io4 ......
            //Flag - bit wise for test function : 0 - IO Test, 1 - GSN MAG Test, 2 - Rtc Test
            //IoCount - Test IO pair count
            //io1, io2... - High byte - gpio pin from, Low byte gpio pin to.
            if (srecCmd.Length > 0)
            {
                rep = p.gps.SendTestSrecCmd(srecCmd, 1000);
            }
            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            w.Reset();
            w.Start();

            bool ioTestPass = true;
            bool ioTestFinished = false;
            while (w.ElapsedMilliseconds < timeout)
            {
                byte[] buff = new byte[256];
                int l = p.gps.ReadLineNoWait(buff, 256, 2000);
                string line = Encoding.UTF8.GetString(buff, 0, l);

                if (line.Length > 0)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = line;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                if (line.Contains("FINISH"))
                {
                    ioTestFinished = true;
                    break;
                }
                if (line.Contains("FAIL"))
                {
                    ioTestPass = false;
                    break;
                }
            };

            if (!ioTestFinished || !ioTestPass)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.TestIoTestFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "IO Test pass";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            return true;
        }

        private bool DoQueryCrc(WorkerParam p, WorkerReportParam r)
        {
            uint crc = 0;
            GPS_RESPONSE rep = p.gps.QueryCrc(DefaultCmdTimeout, ref crc);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.QueryCrcNack : WorkerParam.ErrorType.QueryCrcTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                //EndProcess(p);
                return false;
            }
            else if (p.profile.fwProfile.crc != crc)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.FirmwareCrcError;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                //EndProcess(p);
                return false;
            }

            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "Check CRC pass";
            p.bw.ReportProgress(0, new WorkerReportParam(r));
            return true;
        }

        private bool TestRtc(WorkerParam p, WorkerReportParam r)
        {
            UInt32 rtc1 = 0, rtc2 = 0;
            GPS_RESPONSE rep = p.gps.QueryRtc(ref rtc1);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.QueryRtcNack : WorkerParam.ErrorType.QueryRtcTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get RTC1 " + rtc1.ToString();
                p.bw.ReportProgress(0, new WorkerReportParam(r));

                Thread.Sleep(1010);
                rep = p.gps.QueryRtc(ref rtc2);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.QueryRtcNack : WorkerParam.ErrorType.QueryRtcTimeOut;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }

                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get RTC2 " + rtc2.ToString();
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                if ((rtc2 - rtc1) > 3 || (rtc2 - rtc1) < 1)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.CheckRtcError;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }
            }

            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "Check rtc pass";
            p.bw.ReportProgress(0, new WorkerReportParam(r));
            return true;
        }

        private bool TestSnr(WorkerParam p, WorkerReportParam r/*, ref Stopwatch sw, int lowerBound, int upperBound, int duration*/)
        {
            bool gpSnrPass = !p.profile.testGpSnr;
            bool glSnrPass = !p.profile.testGlSnr;
            bool bdSnrPass = !p.profile.testBdSnr;
            GPS_RESPONSE rep = GPS_RESPONSE.NONE;

            if (p.profile.testGlSnr)
            {
                rep = p.gps.SetRegister(1000, 0x90000000, 0x01);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK)
                        ? WorkerParam.ErrorType.SetPsti50Nack
                        : WorkerParam.ErrorType.SetPsti50Timeout;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Set PSTI 50 Interval success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
            }

            rep = p.gps.ConfigMessageOutput(0x01);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ConfigMessageOutputNack : WorkerParam.ErrorType.ConfigMessageOutputTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Config Message Output success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            rep = p.gps.ConfigNmeaOutput(1, 1, 1, 0, 1, 1, 0, 0);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ConfigNmeaOutputNack : WorkerParam.ErrorType.ConfigNmeaOutputTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Config NMEA Interval success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            do
            {
                byte[] buff = new byte[256];
                int l = p.gps.ReadLineNoWait(buff, 256, 2000);
                string line = Encoding.UTF8.GetString(buff, 0, l);
                if (GpsMsgParser.CheckNmea(line))
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = line;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));

                    GpsMsgParser.ParsingResult ps = p.parser.ParsingNmea(line);
                    if (GpsMsgParser.ParsingResult.UpdateSate == ps)
                    {   //Update SNR Chart
                        p.parser.parsingStat.CopyTo(dvResult[p.index]);
                        r.reportType = WorkerReportParam.ReportType.UpdateSnrChart;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                    }

                    if (!gpSnrPass && p.profile.testGpSnr && GpsMsgParser.ParsingResult.UpdateSate == ps)
                    {
                        gpSnrPass = CompareResult(p, ref r, GpsMsgParser.SateType.Gps);
                    }
                    if (!glSnrPass && p.profile.testGlSnr && GpsMsgParser.ParsingResult.UpdateSate == ps)
                    {
                        glSnrPass = CompareResult(p, ref r, GpsMsgParser.SateType.Glonass);
                    }
                    if (!bdSnrPass && p.profile.testBdSnr && GpsMsgParser.ParsingResult.UpdateSate == ps)
                    {
                        bdSnrPass = CompareBeidouResult(p, ref r);
                    }
                }
            } while (!(gpSnrPass && glSnrPass && bdSnrPass) && !p.bw.CancellationPending);

            if (gpSnrPass && glSnrPass && bdSnrPass)
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Test SNR success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.NoError;
                if (!gpSnrPass)
                {
                    p.error |= WorkerParam.ErrorType.GpsSnrError;
                }
                if (!glSnrPass)
                {
                    p.error |= WorkerParam.ErrorType.GlonassSnrError;
                }
                if (!bdSnrPass)
                {
                    p.error |= WorkerParam.ErrorType.BeidouSnrError;
                }
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            rep = p.gps.ConfigMessageOutput(0x00);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ConfigNmeaOutputNack : WorkerParam.ErrorType.ConfigNmeaOutputTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Turn off NMEA output success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            return true;
        }

        private void CheckControllerEvent(WorkerParam p, int timeCount)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;
            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "Before WaitOn.";
            p.bw.ReportProgress(0, new WorkerReportParam(r));
            if (!IncreaseWaitingCount())
            {
                int waitCount = timeCount;
                while (!controllerEvent.WaitOne(100))
                {
                    if (--waitCount == 0)
                    {
                        //break;
                    }
                }
            }
            r.output = "After WaitOn.";
            p.bw.ReportProgress(0, new WorkerReportParam(r));
        }

        public bool DoTest(WorkerParam p)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;

            if (!OpenDevice(p, r, GpsBaudRateConverter.BaudRate2Index(p.profile.fwProfile.dvBaudRate)))
            {
                return false;
            }
#if  !(DEBUG)
            if (!DoColdStart(p, r, 3))
            {
                return false;
            }
#endif
            if(!DoQueryVersion(p, r))
            {
                return false;
            }

            if (p.profile.checkPromCrc && !DoQueryCrc(p, r))
            {
                return false;
            }

            if (p.profile.checkRtc && !TestRtc(p, r))
            {
                return false;
            }

            GPS_RESPONSE rep = GPS_RESPONSE.NONE;
            if (p.profile.testAntenna)
            {
                antennaEvent.WaitOne();
                SkytraqGps annIO = new SkytraqGps();

                for (int i = 0; i < 2; ++i)
                {
                    rep = annIO.Open(p.annIoPort, 5);
                    if (GPS_RESPONSE.UART_FAIL == rep)
                    {
                        annIO.Close();
                    }
                    else
                    {
                        break;
                    }
                }

                if (GPS_RESPONSE.UART_FAIL == rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.IoControllerFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    EndAntennaProcess(annIO);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open controller IO " + p.annIoPort + " in 115200 success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                rep = annIO.AntennaIO(0x0A);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                Thread.Sleep(300);
                byte detect = 0;
                rep = p.gps.QueryAntennaDetect(ref detect);
                if (GPS_RESPONSE.ACK != rep || detect != 0x02)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Query antenna detect status #A pass.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                /////////////////////////////////////////////////            
                rep = annIO.AntennaIO(0x0B);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                Thread.Sleep(300);
                rep = p.gps.QueryAntennaDetect(ref detect);
                if (GPS_RESPONSE.ACK != rep || detect != 0x01)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Query antenna detect status #B pass.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                /////////////////////////////////////////////////            
                rep = annIO.AntennaIO(0x0C);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                Thread.Sleep(300);
                rep = p.gps.QueryAntennaDetect(ref detect);
                if (GPS_RESPONSE.ACK != rep || detect != 0x03)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Query antenna detect status #C pass.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                EndAntennaProcess(annIO);
            }

            if (p.profile.testUart2TxRx)
            {
                //ioreg[0xf014/4] &= (~(0x3UL << 9));
                UInt32 reg = 0;
                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x2000F014, ref reg);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Get Reg(2000F014) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

                rep = p.gps.SetRegister(DefaultCmdTimeout, 0x2000F014, reg & (~(0x3U << 9)));
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Set Reg(2000F014) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }                
                /////////////////////////////////////////////////////////////////////
                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x2000F078, ref reg);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Get Reg(0x2000F078) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

                rep = p.gps.SetRegister(DefaultCmdTimeout, 0x2000F078, reg | (0x1U << 1));
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Set Reg(0x2000F078) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }                
                
                
                antennaEvent.WaitOne();
                SkytraqGps annIO = new SkytraqGps();

                for (int i = 0; i < 2; ++i)
                {
                    rep = annIO.Open(p.annIoPort, 5);
                    if (GPS_RESPONSE.UART_FAIL == rep)
                    {
                        annIO.Close();
                    }
                    else
                    {
                        break;
                    }
                }

                if (GPS_RESPONSE.UART_FAIL == rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.IoControllerFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    EndAntennaProcess(annIO);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open IO controller " + p.annIoPort + " in 115200 success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

                rep = annIO.AntennaIO(0x0A);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Set IO controller to FE00000A.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(300);

                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x20001008, ref reg);
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get GPIO Reg 0x20001008 : " + reg.ToString("X4");
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                if (GPS_RESPONSE.ACK != rep || (reg & 0x6) != 0x4)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                ///////////////////////////////////////////////////////////////////////////////////
                rep = annIO.AntennaIO(0x0B);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Set IO controller to FE00000B.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(300);

                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x20001008, ref reg);
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get GPIO Reg 0x20001008 : " + reg.ToString("X4");
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                if (GPS_RESPONSE.ACK != rep || (reg & 0x6) != 0x2)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                ///////////////////////////////////////////////////////////////////////////////////
                rep = annIO.AntennaIO(0x0C);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Set IO controller to FE00000C.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(300);

                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x20001008, ref reg);
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get GPIO Reg 0x20001008 : " + reg.ToString("X4");
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                if (GPS_RESPONSE.ACK != rep || (reg & 0x6) != 0x6)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                EndAntennaProcess(annIO);
            }

            if ((p.profile.testGpSnr || p.profile.testGlSnr || p.profile.testBdSnr) && !TestSnr(p, r))
            {
                EndProcess(p);
                return false;
            }

            if (!p.bw.CancellationPending && p.profile.testClockOffset)
            {
                int tryCount = 3;
                int sumOfClockOffset = 0;
                int prnCount = 0;
                do 
                {
                    for (int i = 0; i < GpsMsgParser.ParsingStatus.MaxSattellite; i++)
                    {
                        GpsMsgParser.ParsingStatus.sateInfo s = TestModule.dvResult[r.index].GetGpsSate(i);
#if !(DEBUG)
                        if (s.snr <= 30)
                        {
                            continue;
                        }
#endif
                        UInt32 prn = 0, freq = 0;
                        rep = p.gps.QueryChannelDoppler((byte)i, ref prn, ref freq);
                        if (GPS_RESPONSE.ACK != rep)
                        {
                            r.reportType = WorkerReportParam.ReportType.ShowError;
                            p.error = WorkerParam.ErrorType.GetClockOffsetFail;
                            p.bw.ReportProgress(0, new WorkerReportParam(r));
                            EndProcess(p);
                            return false;
                        }
                        if (prn == 0xFFFF)
                        {
                            continue;
                        }

                        UInt32 clkData = 0;
                        rep = p.gps.QueryChannelClockOffset(gdClockOffset, prn, freq, ref clkData);
                        if (GPS_RESPONSE.ACK != rep)
                        {
                            r.reportType = WorkerReportParam.ReportType.ShowError;
                            p.error = WorkerParam.ErrorType.GetClockOffsetFail;
                            p.bw.ReportProgress(0, new WorkerReportParam(r));
                            EndProcess(p);
                            return false;
                        }

                        sumOfClockOffset += unchecked((int)clkData);
                        prnCount++;
                    }
#if !(DEBUG)
                    if (prnCount < 3)
#else
                    if (prnCount < 1)
#endif
                    {
                        sumOfClockOffset = 0;
                        prnCount = 0;
                    }
                    else
                    {
                        break;
                    }
                } while (--tryCount > 0);

                if (tryCount == 0)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.GetClockOffsetFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                double avgClockOffset = sumOfClockOffset / prnCount;
                double clkPpm = avgClockOffset / (96.25 * 16.367667);
                if (avgClockOffset.ToString("F0") == "-1")
                {
                    Console.WriteLine("avgClockOffset:" + avgClockOffset.ToString());
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Device clock offset " + avgClockOffset.ToString("F0") + "(" + clkPpm.ToString("F2") + " ppm)";
                p.bw.ReportProgress(0, new WorkerReportParam(r));


                if (clkPpm < 0)
                {
                    clkPpm = -clkPpm;
                }
                if (clkPpm > p.profile.clockOffsetThreshold)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.CheckClockOffsetFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
            }

            if (!p.bw.CancellationPending)
            {
                rep = p.gps.FactoryReset();
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.FactoryResetNack : WorkerParam.ErrorType.FactoryResetTimeOut;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Factory Reset success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
            }

            if (p.profile.testIo)
            {
                switch (p.profile.testIoType)
                {
                    case ModuleTestProfile.IoType.NavSpark:
                        if (!TestNavSparkIo(p, r))
                        {
                            return false;
                        }
                        break;
                    case ModuleTestProfile.IoType.NavSparkMini:
                        if (!TestNavSparkMiniIo(p, r))
                        {
                            return false;
                        }
                        break;
                }
            }

            r.reportType = WorkerReportParam.ReportType.ShowFinished;
            p.error = WorkerParam.ErrorType.NoError;
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            EndProcess(p);
            return true;
        }

        private static SkytraqGps controllerIO = null;
        private static bool controllerInit = false;
        private static bool DoDrInitIo(WorkerParam p, WorkerReportParam r)
        {
            if (controllerInit)
            {
                Thread.Sleep(300);
                return true;
            }
            controllerInit = true;
            controllerIO = new SkytraqGps();
            GPS_RESPONSE rep = controllerIO.Open(p.annIoPort, 5);
            if (GPS_RESPONSE.UART_FAIL == rep)
            {
                controllerIO.Close();
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.IoControllerFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                controllerInit = false;
                return false;
            }
            //DR test use GPIO 1,2,3,4 ,5,6,10,12,13,16,20,28,30,31, 1101-0000-0001-0001-0011-0100-0111-1110 = D01137Eh
            //GPIO 28, 30, 31 for input, 1101-0000-0000-0000-0000-0000-0000-0000 = D000000h
            rep = controllerIO.InitControllerIO(0xd011347e, 0xd0000000);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.McuFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                controllerInit = false;
                controllerIO.Close();
                return false;
            }

            //Reset for Slot 1, 2, 3 in GPIO 10, 12, 13, 0011-0100-0000-0000 = 3400h
            rep = controllerIO.SetControllerIO(0x00003400, 0x00000000);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.McuFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                controllerInit = false;
                controllerIO.Close();
                return false;
            }
            return true;
        }

        public static void ResetDrMcuStatus()
        {
            setDrMcuStep = false;
            controllerInit = false;
            if (controllerIO != null)
            {
                controllerIO.Close();
            }
        }

        private static bool setDrMcuStep = false;
        private static uint motorDelay = 8000;
        private static uint motorStep = 752;
        private static bool SetDrMcuStep(WorkerParam p, WorkerReportParam r, byte step)
        {
            if (setDrMcuStep)
            {
                Thread.Sleep(500);
                return true;
            }
            setDrMcuStep = true;
            try
            {
                if (controllerIO == null)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.IoControllerFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }

                GPS_RESPONSE rep;
                if (step == 0)
                {   //Turn Dir to 1, Rotate CCW 80 degree.
                    //MCU IO Set for DR Test all direction high
                    //6f 02 00 00 00 2A 00 00 00 00
                    rep = controllerIO.SetControllerIO(0x0000002A, 0x00000000);
                    if (GPS_RESPONSE.ACK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.McuFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        return false;
                    }

                    //6f 03 00 1f 1e 1c 14 10 00 00 1D B0 00 00 02 F0
                    rep = controllerIO.SetControllerMoto(1, 31, 30, 28, 20, 16, motorDelay, motorStep);
                    if (GPS_RESPONSE.ACK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.McuFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        return false;
                    }
                    motoPosition = 1;
                }
                else if (step == 1)
                {   //Turn Dir to 0, Start ODO clock, Rotate CW 80 degree.
                    //GPIO 2, 4, 6 for ODO Clock
                    rep = controllerIO.SetControllerClock(1, 1000 / (50 * 2), 0x00000054);
                    if (GPS_RESPONSE.ACK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.McuFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        return false;
                    }

                    //MCU IO Set for DR Test all direction low
                    //6f 02 00 00 00 00 00 00 00 2A
                    rep = controllerIO.SetControllerIO(0x00000000, 0x0000002A);
                    if (GPS_RESPONSE.ACK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.McuFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        return false;
                    }

                    //6f 03 00 1f 1e 1c 14 10 00 00 1D B0 00 00 02 F0
                    //rep = controllerIO.SetControllerMoto(0, 31, 30, 28, 20, 16, motorDelay, motorStep);
                    //if (GPS_RESPONSE.ACK != rep)
                    //{
                    //    r.reportType = WorkerReportParam.ReportType.ShowError;
                    //    p.error = WorkerParam.ErrorType.McuFail;
                    //    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    //    return false;
                    //}
                    motoPosition = 2;

                }
                else if (step == 2)
                {   //Turn Dir to 0, stop ODO clock, Rotate CW 80 degree.
                    rep = controllerIO.SetControllerClock(0, 0, 0x00000054);
                    if (GPS_RESPONSE.ACK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.McuFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        return false;
                    }

                    //MCU IO Set for DR Test all direction high
                    //6f 02 00 00 00 2A 00 00 00 00
                    rep = controllerIO.SetControllerIO(0x0000002A, 0x00000000);
                    if (GPS_RESPONSE.ACK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.McuFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        return false;
                    }

                    //6f 03 00 1f 1e 1c 14 10 00 00 1D B0 00 00 02 F0
                    rep = controllerIO.SetControllerMoto(0, 31, 30, 28, 20, 16, motorDelay, motorStep);
                    if (GPS_RESPONSE.ACK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.McuFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        return false;
                    }
                    motoPosition = 3;

                }
                else if (step == 3)
                {
                    //6f 03 00 1f 1e 1c 14 10 00 00 1D B0 00 00 02 F0
                    //rep = controllerIO.SetControllerMoto(1, 31, 30, 28, 20, 16, motorDelay, motorStep);
                    //if (GPS_RESPONSE.ACK != rep)
                    //{
                    //    r.reportType = WorkerReportParam.ReportType.ShowError;
                    //    p.error = WorkerParam.ErrorType.McuFail;
                    //    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    //    return false;
                    //}
                    motoPosition = 4;
                }
            }
            catch
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "MCU exception!";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            setDrMcuStep = false;
            return true;
        }

        public bool DoDrTest(WorkerParam p)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;
            GPS_RESPONSE rep = GPS_RESPONSE.NONE;

            if (!DoDrInitIo(p, r))
            {
                return false;
            }
            CheckControllerEvent(p, 30);
            if (!controllerInit)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.McuFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }
            Thread.Sleep(1000);
            ResetWaitingCount();

            if (!OpenDevice(p, r, GpsBaudRateConverter.BaudRate2Index(p.profile.fwProfile.dvBaudRate)))
            {
                return false;
            }
#if  !(DEBUG)
            if (!DoColdStart(p, r, 3))
            {
                return false;
            }
            if (!DoQueryVersion(p, r))
            {
                return false;
            }
#endif

            if (p.profile.checkPromCrc && !DoQueryCrc(p, r))
            {
                return false;
            }

            if (p.profile.checkRtc && !TestRtc(p, r))
            {
                return false;
            }

            CheckControllerEvent(p, 90);
            Thread.Sleep(1000);
            ResetWaitingCount();

            //int DrSleep1 = 2000;
            //int DrSleep2 = 500;
            
            //==================== DR Test Step 0
            if (!SetDrMcuStep(p, r, 0))
            {
                return false;
            }
            Thread.Sleep(1500);

            //Test Here
            UInt32 temp = 0, odo_plus = 0;
            float gyro = 0;
            byte odo_bw = 0;
            double gyroCcw = 0, gyroCw = 0, gyro_avg;
            int testTimes = 10;

            for (int i = 0; i < testTimes; ++i)
            {
                rep = p.gps.QueryDrStatus(1000, ref temp, ref gyro, ref odo_plus, ref odo_bw);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.OdoPluseFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "gyro:" + gyro.ToString() + " odo_plus:" + odo_plus.ToString() + " odo_bw:" + odo_bw.ToString();
                gyroCcw += gyro;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(100);
            }

            gyro_avg = -1 * gyroCcw / testTimes;
            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "gyro ccw avg:" + (gyroCcw / testTimes).ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));
            Thread.Sleep(1000);

            if (gyro_avg > p.profile.uslAnticlockWise || gyro_avg < p.profile.lslAnticlockWise)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.GyroFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            CheckControllerEvent(p, 90);
            Thread.Sleep(1000);
            ResetWaitingCount();

            //==================== DR Test Step 1
            if (!SetDrMcuStep(p, r, 1))
            {
                return false;
            }
            Thread.Sleep(2000);

            //Test Here
            rep = p.gps.QueryDrStatus(1000, ref temp, ref gyro, ref odo_plus, ref odo_bw);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OdoPluseFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "gyro:" + gyro.ToString() + " odo_plus:" + odo_plus.ToString() + " odo_bw:" + odo_bw.ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            if (odo_plus > 600 || odo_plus < 400)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OdoPluseFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            if (odo_bw != 0)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OdoDirectionFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            //Thread.Sleep(1000);
            Thread.Sleep(200);

            CheckControllerEvent(p, 90);
            Thread.Sleep(1000);
            ResetWaitingCount();

            //==================== DR Test Step 2
            if (!SetDrMcuStep(p, r, 2))
            {
                return false;
            }
            Thread.Sleep(2000);
            for (int i = 0; i < 10; ++i)
            {
                rep = p.gps.QueryDrStatus(1000, ref temp, ref gyro, ref odo_plus, ref odo_bw);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.OdoPluseFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "gyro:" + gyro.ToString() + " odo_plus:" + odo_plus.ToString() + " odo_bw:" + odo_bw.ToString();
                gyroCw += gyro;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(100);
            }
            gyro_avg = gyroCw / testTimes;
            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "gyro cw avg:" + (gyroCw / testTimes).ToString();
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            if (gyro_avg > p.profile.uslClockWise || gyro_avg < p.profile.lslClockWise)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.GyroFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            if (odo_bw != 1)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OdoDirectionFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                return false;
            }

            //Thread.Sleep(500);
            //CheckControllerEvent(p, 90);
            //Thread.Sleep(1000);
            //ResetWaitingCount();

            //==================== DR Test Step 3
            //if (!SetDrMcuStep(p, r, 3))
            //{
            //    return false;
            //}
            //Thread.Sleep(10);
            //r.reportType = WorkerReportParam.ReportType.ShowProgress;
            //r.output = "Step 3 Finished";
            //p.bw.ReportProgress(0, new WorkerReportParam(r));
            //Thread.Sleep(10);

            //CheckControllerEvent(p, 90);
            //Thread.Sleep(1000);
            //ResetWaitingCount();


            if (p.profile.testAntenna)
            {
                antennaEvent.WaitOne();
                SkytraqGps annIO = new SkytraqGps();

                for (int i = 0; i < 2; ++i)
                {
                    rep = annIO.Open(p.annIoPort, 5);
                    if (GPS_RESPONSE.UART_FAIL == rep)
                    {
                        annIO.Close();
                    }
                    else
                    {
                        break;
                    }
                }

                if (GPS_RESPONSE.UART_FAIL == rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.IoControllerFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    EndAntennaProcess(annIO);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open controller IO " + p.annIoPort + " in 115200 success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                rep = annIO.AntennaIO(0x0A);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                Thread.Sleep(300);
                byte detect = 0;
                rep = p.gps.QueryAntennaDetect(ref detect);
                if (GPS_RESPONSE.ACK != rep || detect != 0x02)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Query antenna detect status #A pass.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                /////////////////////////////////////////////////            
                rep = annIO.AntennaIO(0x0B);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                Thread.Sleep(300);
                rep = p.gps.QueryAntennaDetect(ref detect);
                if (GPS_RESPONSE.ACK != rep || detect != 0x01)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Query antenna detect status #B pass.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                /////////////////////////////////////////////////            
                rep = annIO.AntennaIO(0x0C);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                Thread.Sleep(300);
                rep = p.gps.QueryAntennaDetect(ref detect);
                if (GPS_RESPONSE.ACK != rep || detect != 0x03)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Query antenna detect status #C pass.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                EndAntennaProcess(annIO);
            }

            if (p.profile.testUart2TxRx)
            {
                //ioreg[0xf014/4] &= (~(0x3UL << 9));
                UInt32 reg = 0;
                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x2000F014, ref reg);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Get Reg(2000F014) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

                rep = p.gps.SetRegister(DefaultCmdTimeout, 0x2000F014, reg & (~(0x3U << 9)));
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Set Reg(2000F014) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                /////////////////////////////////////////////////////////////////////
                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x2000F078, ref reg);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Get Reg(0x2000F078) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

                rep = p.gps.SetRegister(DefaultCmdTimeout, 0x2000F078, reg | (0x1U << 1));
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Set Reg(0x2000F078) success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }


                antennaEvent.WaitOne();
                SkytraqGps annIO = new SkytraqGps();

                for (int i = 0; i < 2; ++i)
                {
                    rep = annIO.Open(p.annIoPort, 5);
                    if (GPS_RESPONSE.UART_FAIL == rep)
                    {
                        annIO.Close();
                    }
                    else
                    {
                        break;
                    }
                }

                if (GPS_RESPONSE.UART_FAIL == rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.IoControllerFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    EndAntennaProcess(annIO);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open IO controller " + p.annIoPort + " in 115200 success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

                rep = annIO.AntennaIO(0x0A);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Set IO controller to FE00000A.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(300);

                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x20001008, ref reg);
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get GPIO Reg 0x20001008 : " + reg.ToString("X4");
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                if (GPS_RESPONSE.ACK != rep || (reg & 0x6) != 0x4)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                ///////////////////////////////////////////////////////////////////////////////////
                rep = annIO.AntennaIO(0x0B);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Set IO controller to FE00000B.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(300);

                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x20001008, ref reg);
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get GPIO Reg 0x20001008 : " + reg.ToString("X4");
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                if (GPS_RESPONSE.ACK != rep || (reg & 0x6) != 0x2)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                ///////////////////////////////////////////////////////////////////////////////////
                rep = annIO.AntennaIO(0x0C);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAntennaFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndAntennaProcess(annIO);
                    EndProcess(p);
                    return false;
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Set IO controller to FE00000C.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(300);

                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x20001008, ref reg);
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Get GPIO Reg 0x20001008 : " + reg.ToString("X4");
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                if (GPS_RESPONSE.ACK != rep || (reg & 0x6) != 0x6)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestUART2Fail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                EndAntennaProcess(annIO);

            }

            if ((p.profile.testGpSnr || p.profile.testGlSnr || p.profile.testBdSnr) && !TestSnr(p, r))
            {
                return false;
            }

            if (!p.bw.CancellationPending && p.profile.testClockOffset)
            {
                int tryCount = 3;
                int sumOfClockOffset = 0;
                int prnCount = 0;
                do
                {
                    for (int i = 0; i < GpsMsgParser.ParsingStatus.MaxSattellite; i++)
                    {
                        GpsMsgParser.ParsingStatus.sateInfo s = TestModule.dvResult[r.index].GetGpsSate(i);
#if !(DEBUG)
                        if (s.snr <= 30)
                        {
                            continue;
                        }
#endif
                        UInt32 prn = 0, freq = 0;
                        rep = p.gps.QueryChannelDoppler((byte)i, ref prn, ref freq);
                        if (GPS_RESPONSE.ACK != rep)
                        {
                            r.reportType = WorkerReportParam.ReportType.ShowError;
                            p.error = WorkerParam.ErrorType.GetClockOffsetFail;
                            p.bw.ReportProgress(0, new WorkerReportParam(r));
                            EndProcess(p);
                            return false;
                        }
                        if (prn == 0xFFFF)
                        {
                            continue;
                        }

                        UInt32 clkData = 0;
                        rep = p.gps.QueryChannelClockOffset(gdClockOffset, prn, freq, ref clkData);
                        if (GPS_RESPONSE.ACK != rep)
                        {
                            r.reportType = WorkerReportParam.ReportType.ShowError;
                            p.error = WorkerParam.ErrorType.GetClockOffsetFail;
                            p.bw.ReportProgress(0, new WorkerReportParam(r));
                            EndProcess(p);
                            return false;
                        }

                        sumOfClockOffset += unchecked((int)clkData);
                        prnCount++;
                    }
#if !(DEBUG)
                    if (prnCount < 3)
#else
                    if (prnCount < 1)
#endif
                    {
                        sumOfClockOffset = 0;
                        prnCount = 0;
                    }
                    else
                    {
                        break;
                    }
                } while (--tryCount > 0);

                if (tryCount == 0)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.GetClockOffsetFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                double avgClockOffset = sumOfClockOffset / prnCount;
                double clkPpm = avgClockOffset / (96.25 * 16.367667);
                if (avgClockOffset.ToString("F0") == "-1")
                {
                    Console.WriteLine("avgClockOffset:" + avgClockOffset.ToString());
                }
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Device clock offset " + avgClockOffset.ToString("F0") + "(" + clkPpm.ToString("F2") + " ppm)";
                p.bw.ReportProgress(0, new WorkerReportParam(r));


                if (clkPpm < 0)
                {
                    clkPpm = -clkPpm;
                }
                if (clkPpm > p.profile.clockOffsetThreshold)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.CheckClockOffsetFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
            }

            //CheckControllerEvent(p, 100);
            //Thread.Sleep(1000);
            //ResetWaitingCount();

            if (!p.bw.CancellationPending)
            {
                rep = p.gps.FactoryReset();
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.FactoryResetNack : WorkerParam.ErrorType.FactoryResetTimeOut;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Factory Reset success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
            }

            if (p.profile.testIo)
            {
                Thread.Sleep(1200);
                rep = p.gps.ChangeBaudrate((byte)5, 2);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.ChangeBaudRateFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Change baud rate success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                String dbgOutput = "";
                rep = p.gps.SendLoaderDownload(ref dbgOutput);
                if (GPS_RESPONSE.OK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.LoaderDownloadFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Loader Download success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));

                    rep = p.gps.UploadLoader(Properties.Resources.NavSparkIoTester);
                    if (GPS_RESPONSE.OK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.UploadLoaderFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        EndProcess(p);
                        return false;
                    }

                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Upload Loader success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    Thread.Sleep(1000);
                }

                System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
                w.Reset();
                w.Start();

                bool ioTestPass = false;
                uint adc = 0;
                while (w.ElapsedMilliseconds < 5000)
                {
                    byte[] buff = new byte[256];
                    int l = p.gps.ReadLineNoWait(buff, 256, 2000);
                    string line = Encoding.UTF8.GetString(buff, 0, l);

                    if (line.Length > 0)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowProgress;
                        r.output = line;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                    }
                    if (line.Contains("FINISH"))
                    {
                        ioTestPass = true;
                        break;
                    }
                    if (line.Contains("FAIL"))
                    {
                        break;
                    }
                    if (line.Contains("ADC:"))
                    {
                        uint a = Convert.ToUInt32(line.Split(' ')[2], 16);
                        if (CheckAdc(a))
                        {
                            adc = a;
                        }
                    }
                };

                if (ioTestPass)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Test IO success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestIoFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }

                if (CheckAdc(adc))
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Test ADC success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestAdcFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
            }

            r.reportType = WorkerReportParam.ReportType.ShowFinished;
            p.error = WorkerParam.ErrorType.NoError;
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            EndProcess(p);
            return true;
        }

        public bool OpenPortTest(WorkerParam p)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;
            GPS_RESPONSE rep;

            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            while(!p.bw.CancellationPending)
            {
                w.Reset();
                w.Start();
                rep = p.gps.Open(p.comPort, 1);
                if (GPS_RESPONSE.UART_FAIL == rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.OpenPortFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    w.Stop();
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open spend " + w.ElapsedMilliseconds + " ms";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    p.gps.Close();
                }
                Thread.Sleep(500);
            }
            return true;
        }

        private byte CalcCheckSum16(byte[] data, int start, int len)
        {
            UInt16 checkSum = 0;
            //const U08* ptr = dataPtr;
            //int loopCount = len / sizeof(UInt16);
            //int i;

            for (int i = 0; i < len; i += sizeof(UInt16))
            {
                UInt16 word = Convert.ToUInt16(data[start + i + 1] | data[start + i] << 8);
	            checkSum += word;
            }
            return Convert.ToByte(((checkSum >> 8) + (checkSum & 0xFF)) & 0xFF);
        }

        private int ScanBaudRate(WorkerParam p, WorkerReportParam r, int first)
        {
            GPS_RESPONSE rep;
            int TestDeviceTimeout = 500;
            int[] testingOrder = { 5, 1, 0, 3, 2, 4, 6, 7, 8 };

            if (first != -1)
            {
                rep = p.gps.Open(p.comPort, first);
                if (GPS_RESPONSE.UART_OK != rep)
                {   //This com port can't open.
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open " + p.comPort + " fail!";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return -1;
                }

                //TestDeviceTimeout = (first < 2) ? 1500 : 1000;
                TestDeviceTimeout = (first < 2) ? 500 : 500;
                rep = p.gps.TestDevice(TestDeviceTimeout, 1);
                if (GPS_RESPONSE.NACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Baud rate " + GpsBaudRateConverter.Index2BaudRate(first).ToString() + " invalid.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    p.gps.Close();
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Found working baud rate " + GpsBaudRateConverter.Index2BaudRate(first).ToString() + ".";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return first;
                }
            }

            foreach (int i in testingOrder)
            {
                if (i == first)
                {
                    continue;
                }
                if (p.bw.CancellationPending)
                {
                    return -1;
                }

                rep = p.gps.Open(p.comPort, i);
                if (GPS_RESPONSE.UART_OK != rep)
                {   //This com port can't open.
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open " + p.comPort + " fail!";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return -1;
                }

                TestDeviceTimeout = (i < 2) ? 500 : 500;
                rep = p.gps.TestDevice(TestDeviceTimeout, 1);
                if (GPS_RESPONSE.NACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Baud rate " + GpsBaudRateConverter.Index2BaudRate(i).ToString() + " invalid!";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    p.gps.Close();
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Found working baud rate " + GpsBaudRateConverter.Index2BaudRate(i).ToString() + ".";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    return i;
                }
            }
            return -1;
        }

        private static int lastDeviceBaudIdx = -1;
        private static int lastRomBaudIdx = 1;
        public bool DoDownload(WorkerParam p)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;
            GPS_RESPONSE rep = GPS_RESPONSE.UART_OK;
            
            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "Scanning " + p.comPort + " baud rate...";
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Retry three times for disable auto uart firmware, it'll 
            // change uart output baud rate after 5 seconds.
            int baudIdx = -1;
            for (int i = 0; i < 3; ++i)
            {
                baudIdx = ScanBaudRate(p, r, lastDeviceBaudIdx);
                if (-1 != baudIdx)
                {
                    lastDeviceBaudIdx = baudIdx;
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open " + p.comPort + " in " +
                        GpsBaudRateConverter.Index2BaudRate(baudIdx).ToString() +
                        " success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    break;
                }
                Thread.Sleep(50);
            }

            if (-1 == baudIdx)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OpenPortFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            String kVer = "";
            String sVer = "";
            String rev = "";
            
            rep = p.gps.QueryVersion(DefaultCmdTimeout, ref kVer, ref sVer, ref rev);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.QueryVersionNack : WorkerParam.ErrorType.QueryVersionTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else if (rev != "20130221")
            {
                //Reboot to ROM Code
                rep = p.gps.SetRegister(2000, 0x2000F050, 0x00000000);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ColdStartNack : WorkerParam.ErrorType.ColdStartTimeOut;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Reboot from ROM success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    p.gps.Close();
                    Thread.Sleep(3000);  //Waiting for reboot
                }

                // Retry three times for disable auto uart firmware, it'll 
                // change uart output baud rate after 5 seconds.
                baudIdx = -1;
                for (int i = 0; i < 3; ++i)
                {
                    baudIdx = ScanBaudRate(p, r, lastRomBaudIdx);
                    if (-1 != baudIdx)
                    {
                        lastRomBaudIdx = baudIdx;
                        r.reportType = WorkerReportParam.ReportType.ShowProgress;
                        r.output = "Open " + p.comPort + " in " +
                            GpsBaudRateConverter.Index2BaudRate(baudIdx).ToString() +
                            " success.";
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        break;
                    }
                    Thread.Sleep(50);
                }

                if (-1 == baudIdx)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.OpenPortFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
            }
/*         
            if ((p.profile.fwProfile.tagAddress == 0 && p.profile.fwProfile.tagContent == 0) ||
                (p.profile.fwProfile.tagAddress == 0xAAAAAAAA && p.profile.fwProfile.tagContent == 0x55555555))
            {   //No tag, using rom loader command
                rep = p.gps.StartDownload((byte)p.profile.dlBaudSel);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK)
                        ? WorkerParam.ErrorType.DownloadCmdNack
                        : WorkerParam.ErrorType.DownloadCmdTimeOut;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    p.gps.Close();
                    rep = p.gps.Open(p.comPort, p.profile.dlBaudSel);
                    if (GPS_RESPONSE.UART_FAIL == rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = (rep == GPS_RESPONSE.NACK)
                            ? WorkerParam.ErrorType.DownloadCmdNack
                            : WorkerParam.ErrorType.DownloadCmdTimeOut;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        //EndProcess(p);
                        return false;
                    }
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Download command success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));

                    Thread.Sleep(1000);
                }
            }
            else
*/
            {   //Need tag, using "$LOADER DOWNLOAD" command
                rep = p.gps.ChangeBaudrate((byte)p.profile.dlBaudSel, 2);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.ChangeBaudRateFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Change baud rate success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

                String dbgOutput = "";
                rep = p.gps.SendLoaderDownload(ref dbgOutput);
                if (GPS_RESPONSE.OK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.LoaderDownloadFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Loader Download success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    
                    rep = p.gps.UploadLoader(LoaderData.v8TagLoader);
                    if (GPS_RESPONSE.OK != rep)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.UploadLoaderFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        EndProcess(p);
                        return false;
                    }  

                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Upload Loader success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    Thread.Sleep(1000);
                }
            }

            if ((p.profile.fwProfile.tagAddress == 0 && p.profile.fwProfile.tagContent == 0) ||
                (p.profile.fwProfile.tagAddress == 0xAAAAAAAA && p.profile.fwProfile.tagContent == 0x55555555))
            {
                rep = p.gps.SendRomBinSize(p.profile.fwProfile.promRaw.Length,
                    p.profile.fwProfile.CalcPromRawCheckSum());
            }
            else
            {
                rep = p.gps.SendTagBinSize(p.profile.fwProfile.promRaw.Length, p.profile.fwProfile.CalcPromRawCheckSum(),
                    p.profile.dlBaudSel, p.profile.fwProfile.tagAddress, p.profile.fwProfile.tagContent);
            }
            if (GPS_RESPONSE.OK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.BinsizeCmdTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Start update firmware";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            const int nFlashBytes = 8 * 1024;
            const int headerSize = 3;

            byte[] header = new byte[headerSize];
            int lSize = p.profile.fwProfile.promRaw.Length;
            int sentBytes = 0;
            int totalByte = 0;
            UInt16 sequence = 0;
            int rawItr = 0;

            int failCount = 0;
            while (lSize > 0)
            {
                sentBytes = (lSize >= nFlashBytes) ? nFlashBytes : lSize;
                totalByte += sentBytes;

                header[0] = (byte)(sequence >> 24 & 0xFF);
                header[1] = (byte)(sequence & 0xff);
                header[2] = CalcCheckSum16(p.profile.fwProfile.promRaw, rawItr, sentBytes);

                //p.gps.SendDataNoWait(header, headerSize);
                rep = p.gps.SendDataWaitStringAck(p.profile.fwProfile.promRaw, rawItr, sentBytes, 10000, "OK\0");

                if (rep == GPS_RESPONSE.OK)
                {
                    sequence++;
                    lSize -= sentBytes;
                    rawItr += nFlashBytes;
                }
                else
                {
                    if (++failCount > 0)
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.DownloadWriteFail;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        EndProcess(p);
                        return false;
                    }
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Write block respone " + rep.ToString() + ", retry " + failCount.ToString();
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    continue;
                }
                failCount = 0;
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "left " + lSize.ToString() + " bytes";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                //Thread.Sleep(100);
            }

            rep = p.gps.WaitStringAck(10000, "END\0");
            if (GPS_RESPONSE.OK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.DownloadEndTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Total time : " + (sw.ElapsedMilliseconds / 1000).ToString() + " seconds";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
/*
            p.gps.Close();
            //Wait for Flash code boot up.
            Thread.Sleep(1000);
            rep = p.gps.Open(p.comPort,
                            GpsBaudRateConverter.BaudRate2Index(p.profile.fwProfile.dvBaudRate));
            if (GPS_RESPONSE.UART_FAIL == rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OpenPortFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Open " + p.comPort + " in " +
                    p.gps.GetBaudRate().ToString() + " success.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            for (int i = 0; i < 3; ++i)
            {
                rep = p.gps.QueryVersion(DefaultCmdTimeout, ref kVer, ref sVer, ref rev);
                if (GPS_RESPONSE.ACK == rep)
                {
                    break;
                }
            }

            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.QueryVersionNack : WorkerParam.ErrorType.QueryVersionTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else if (rev == "20130221")
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.FirmwareVersionError;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }

            rep = p.gps.FactoryReset();
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.FactoryResetNack : WorkerParam.ErrorType.FactoryResetTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Factory Reset success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
*/
            r.reportType = WorkerReportParam.ReportType.ShowFinished;
            p.error = WorkerParam.ErrorType.NoError;
            p.bw.ReportProgress(0, new WorkerReportParam(r));            
            EndProcess(p);
            return true;
        }

        public bool DoiCacheTest(WorkerParam p)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;
            GPS_RESPONSE rep = GPS_RESPONSE.UART_OK;
            
            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "Scanning " + p.comPort + " baud rate...";
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            // Retry three times for disable auto uart firmware, it'll 
            // change uart output baud rate after 5 seconds.
            int baudIdx = -1;
            for (int i = 0; i < 3; ++i)
            {   
                baudIdx = ScanBaudRate(p, r, lastDeviceBaudIdx);
                if (-1 != baudIdx)
                {
                    lastDeviceBaudIdx = baudIdx;
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open " + p.comPort + " in " +
                        GpsBaudRateConverter.Index2BaudRate(baudIdx).ToString() +
                        " success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    break;
                }
                Thread.Sleep(50);
            }

            if (-1 == baudIdx)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OpenPortFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }

            //Reboot to ROM Code
            rep = p.gps.SetRegister(2000, 0x2000F050, 0x00000000);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ColdStartNack : WorkerParam.ErrorType.ColdStartTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Reboot from ROM success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                p.gps.Close();
                Thread.Sleep(3000);  //Waiting for reboot
            }

            // Retry three times for disable auto uart firmware, it'll 
            // change uart output baud rate after 5 seconds.
            baudIdx = -1;
            for (int i = 0; i < 3; ++i)
            {
                baudIdx = ScanBaudRate(p, r, lastRomBaudIdx);
                if (-1 != baudIdx)
                {
                    lastRomBaudIdx = baudIdx;
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Open " + p.comPort + " in " +
                        GpsBaudRateConverter.Index2BaudRate(baudIdx).ToString() +
                        " success.";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    break;
                }
                Thread.Sleep(50);
            }

            if (-1 == baudIdx)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OpenPortFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }

            rep = p.gps.ChangeBaudrate((byte)5, 2);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.ChangeBaudRateFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Change baud rate success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            String dbgOutput = "";
            rep = p.gps.SendLoaderDownload(ref dbgOutput);
            if (GPS_RESPONSE.OK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.LoaderDownloadFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Loader Download success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));

                rep = p.gps.UploadLoader(Properties.Resources.iCacheTester1);
                if (GPS_RESPONSE.OK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.UploadLoaderFail;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }  

                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Upload Loader success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(1000);
            }

            bool testPass = true;
            byte[] buff = new byte[1024];
            System.Diagnostics.Stopwatch w = new System.Diagnostics.Stopwatch();
            p.error = WorkerParam.ErrorType.NoError;
            w.Reset();
            w.Start();
            do
            {
                buff.Initialize();
                int l = p.gps.ReadLineNoWait(buff, 1024, 2000);
                string line = Encoding.UTF8.GetString(buff, 0, l);

                if (line.Length <= 0)
                {
                    if (w.ElapsedMilliseconds > 10000 && testPass)
                    {
                        p.error |= WorkerParam.ErrorType.TestIcacheTimeout;
                        testPass = false;
                        r.reportType = WorkerReportParam.ReportType.ShowProgress;
                        r.output = "i cache timeout defected.";
                        p.bw.ReportProgress(0, new WorkerReportParam(r));

                    }
                    continue;
                }
                w.Reset();
                w.Start();

                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = line;
                p.bw.ReportProgress(0, new WorkerReportParam(r));

                if (line.Contains("$ER:"))
                {
                    p.error |= WorkerParam.ErrorType.TestIcacheError;
                    testPass = false;
                }
            } while (!p.bw.CancellationPending);

            if (testPass && p.error == WorkerParam.ErrorType.NoError)
            {
                r.reportType = WorkerReportParam.ReportType.ShowFinished;
                p.error = WorkerParam.ErrorType.NoError;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                //p.error |= WorkerParam.ErrorType.TestIcacheError;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }

            EndProcess(p);
            return true;
        }
        
        public bool DoResetTest(WorkerParam p)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;
            GPS_RESPONSE rep;

            rep = p.gps.Open(p.comPort, resetTesterLogin.BootBaudRate);
            if (GPS_RESPONSE.UART_FAIL == rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OpenPortFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Open " + p.comPort + " in " + 
                    p.gps.GetBaudRate().ToString() + " success.";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            rep = p.gps.SendColdStart(3, 2000);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ColdStartNack : WorkerParam.ErrorType.ColdStartTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Cold start success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                Thread.Sleep(500);  //For venus 6 testing.
            }

            rep = p.gps.ConfigMessageOutput(0x01);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ConfigMessageOutputNack : WorkerParam.ErrorType.ConfigMessageOutputTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Config Message Output success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            rep = p.gps.ConfigNmeaOutput(1, 1, 1, 0, 1, 1, 0, 0);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.ConfigNmeaOutputNack : WorkerParam.ErrorType.ConfigNmeaOutputTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Config NMEA Interval success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            Thread.Sleep(500);
            bool testResetError = false;
            System.Diagnostics.Stopwatch swNmea = new System.Diagnostics.Stopwatch();
            System.Diagnostics.Stopwatch swResetTest = new System.Diagnostics.Stopwatch();
            swNmea.Start();
            swResetTest.Start();
            do
            {
                byte[] buff = new byte[256];
                int l = p.gps.ReadLineNoWait(buff, 256, 2000);
                string line = Encoding.UTF8.GetString(buff, 0, l);
                if (line.Length > 0)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = line;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));

                    if(line.Contains("$SkyTraq"))
                    {
                        r.reportType = WorkerReportParam.ReportType.ShowError;
                        p.error = WorkerParam.ErrorType.ResetDetectError;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                        EndProcess(p);
                        return false;
                    }

                    if (GpsMsgParser.CheckNmea(line))
                    {
                        swNmea.Reset();
                        swNmea.Start();
                    }
                }

                if (swNmea.ElapsedMilliseconds > resetTesterLogin.CheckInterval)
                {
                    long a = swNmea.ElapsedMilliseconds;
                    testResetError = true;
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.CheckRtcError;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
            } while (!testResetError && !p.bw.CancellationPending);

            //if (!p.bw.CancellationPending)
            {
                rep = p.gps.FactoryReset();
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK) ? WorkerParam.ErrorType.FactoryResetNack : WorkerParam.ErrorType.FactoryResetTimeOut;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Factory Reset success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
            }

            if (!testResetError)
            {
                r.reportType = WorkerReportParam.ReportType.ShowFinished;
                p.error = WorkerParam.ErrorType.NoError;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.NoError;
                p.error |= WorkerParam.ErrorType.FirmwareVersionError;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            EndProcess(p);
            return true;
        }

        public bool DoGolden(WorkerParam p)
        {
            WorkerReportParam r = new WorkerReportParam();
            r.index = p.index;
            GPS_RESPONSE rep;

            rep = p.gps.Open(p.comPort, p.profile.gdBaudSel);
            if (GPS_RESPONSE.UART_FAIL == rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = WorkerParam.ErrorType.OpenPortFail;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Open " + p.comPort + " success ------------------------------------";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            if (p.profile.testGlSnr)
            {
                rep = p.gps.SetRegister(1000, 0x90000000, 0x01);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = (rep == GPS_RESPONSE.NACK)
                        ? WorkerParam.ErrorType.SetPsti50Nack
                        : WorkerParam.ErrorType.SetPsti50Timeout;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Set PSTI 50 Interval success";
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }
            }

            if (p.profile.testClockOffset)
            {
                p.parser.parsingStat.positionFixResult = 0;
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Waiting for position fix to get clock offset...";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                r.reportType = WorkerReportParam.ReportType.ShowWaitingGoldenSample;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                do
                {
                    byte[] buff = new byte[256];
                    int l = p.gps.ReadLineNoWait(buff, 256, 2000);
                    string line = Encoding.UTF8.GetString(buff, 0, l);
                    if (GpsMsgParser.CheckNmea(line))
                    {
                        if (GpsMsgParser.ParsingResult.UpdateSate == p.parser.ParsingNmea(line))
                        {
                            p.parser.parsingStat.CopyTo(dvResult[0]);
                            r.reportType = WorkerReportParam.ReportType.UpdateSnrChart;
                            p.bw.ReportProgress(0, new WorkerReportParam(r));
                        }
                    }
                } while (p.parser.parsingStat.positionFixResult <= 2 && !p.bw.CancellationPending);
                r.reportType = WorkerReportParam.ReportType.HideWaitingGoldenSample;
                p.bw.ReportProgress(0, new WorkerReportParam(r));

                if (p.bw.CancellationPending)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;
                    p.error = WorkerParam.ErrorType.TestNotComplete;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }

                rep = p.gps.GetRegister(DefaultCmdTimeout, 0x00000001, ref gdClockOffset);
                if (GPS_RESPONSE.ACK != rep)
                {
                    r.reportType = WorkerReportParam.ReportType.ShowError;

                    p.error = (rep == GPS_RESPONSE.NACK)
                        ? WorkerParam.ErrorType.GetClockOffsetNack
                        : WorkerParam.ErrorType.GetClockOffsetTimeout;
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                    EndProcess(p);
                    return false;
                }
                else
                {
                    r.reportType = WorkerReportParam.ReportType.ShowProgress;
                    r.output = "Get clock offset : " + unchecked((int)gdClockOffset).ToString();
                    p.bw.ReportProgress(0, new WorkerReportParam(r));
                }

            }

            rep = p.gps.ConfigMessageOutput(0x01);
            if (GPS_RESPONSE.ACK != rep)
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;

                p.error = (rep == GPS_RESPONSE.NACK) 
                    ? WorkerParam.ErrorType.ConfigMessageOutputNack
                    : WorkerParam.ErrorType.ConfigMessageOutputTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Config Message Output success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            rep = p.gps.ConfigNmeaOutput(1, 1, 1, 0, 1, 1, 0, 0);
            if (GPS_RESPONSE.ACK != p.gps.ConfigNmeaOutput(1, 1, 1, 0, 1, 1, 0, 0))
            {
                r.reportType = WorkerReportParam.ReportType.ShowError;
                p.error = (rep == GPS_RESPONSE.NACK)
                    ? WorkerParam.ErrorType.ConfigNmeaOutputNack
                    : WorkerParam.ErrorType.ConfigNmeaOutputTimeOut;
                p.bw.ReportProgress(0, new WorkerReportParam(r));
                EndProcess(p);
                return false;
            }
            else
            {
                r.reportType = WorkerReportParam.ReportType.ShowProgress;
                r.output = "Config NMEA Interval success";
                p.bw.ReportProgress(0, new WorkerReportParam(r));
            }

            r.reportType = WorkerReportParam.ReportType.GoldenSampleReady;
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            do
            {
                byte[] buff = new byte[256];
                int l = p.gps.ReadLineNoWait(buff, 256, 2000);
                string line = Encoding.UTF8.GetString(buff, 0, l);
                if (GpsMsgParser.CheckNmea(line))
                {
                    if (GpsMsgParser.ParsingResult.UpdateSate == p.parser.ParsingNmea(line))
                    {
                        p.parser.parsingStat.CopyTo(dvResult[0]);
                        r.reportType = WorkerReportParam.ReportType.UpdateSnrChart;
                        p.bw.ReportProgress(0, new WorkerReportParam(r));
                    }
                }
            } while (p.bw.CancellationPending != true);

            EndProcess(p);
            r.reportType = WorkerReportParam.ReportType.ShowProgress;
            r.output = "Closed UART";
            p.bw.ReportProgress(0, new WorkerReportParam(r));

            return false;
        }

        public void EndProcess(WorkerParam p)
        {
            p.gps.Close();
        }

        private void EndAntennaProcess(SkytraqGps p)
        {
            p.Close();
            antennaEvent.Set();
        }
    }
}
