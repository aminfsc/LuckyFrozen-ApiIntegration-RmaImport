using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ice.Core;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Reflection;
using System.Net;
//using Erp.Tables;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace WMSImportation
{
    class Schedular
    {
        System.Timers.Timer oTimer = null;
        double interval = 20000;
        bool isExecutionCompleted = true;
        string sourcePath = string.Empty; //ConfigurationManager.AppSettings["SourcePath"];
        string successLocation = string.Empty; //ConfigurationManager.AppSettings["SuccessLocation"];
        string failedLocation = string.Empty; //ConfigurationManager.AppSettings["FailedLocation"];
        string environment = ConfigurationManager.AppSettings["Environment"];
        StringBuilder sbFileLog = null;
        DataAccessLayer oDAL = new DataAccessLayer();

        string companies = ConfigurationManager.AppSettings["Company"]; //Amin

        public Schedular()
        {
            ServicePointManager.ServerCertificateValidationCallback += (sender, certificate, chain, errors) => { return true; };
        }

        public void Start()
        {
            oTimer = new System.Timers.Timer(interval);
            oTimer.AutoReset = true;
            oTimer.Enabled = true;
            oTimer.Start();

            oTimer.Elapsed += oTimer_Elapsed;
        }
        void oTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (!isExecutionCompleted)
                return;
            try
            {
                isExecutionCompleted = false;
                string[] lstCompanies = companies.Split(','); //Amin
                foreach (string comp in lstCompanies)
                {
                    sourcePath = ConfigurationManager.AppSettings[comp + "SourcePath"];
                    failedLocation = ConfigurationManager.AppSettings[comp + "FailedLocation"];
                    successLocation = ConfigurationManager.AppSettings[comp + "SuccessLocation"];

                    FileCreation("");
                    GetCsvFiles();
                }

            }
            catch (Exception ex)
            {
                //string errMsg = Logger.Log(ex, eventId++);
                FileCreation(ex.Message.ToString());
                ErrorInfoFile(sourcePath, ex.Message.ToString());
            }
            finally
            {
                isExecutionCompleted = true;
            }
        }
        void FileCreation(string errMsg)
        {
            //string sFile = @"C:\ExternalPrograms\WMS Importation\WMSImportation\OnStart.txt";
            string sFile = @"OnStart.txt";
            string destPath = AppDomain.CurrentDomain.BaseDirectory + sFile;

            string sDateTime = DateTime.Now.ToString();

            System.IO.StreamWriter oFileWriter = new System.IO.StreamWriter(destPath, true);
            oFileWriter.WriteLine("\n" + sDateTime);
            oFileWriter.WriteLine("\n" + errMsg);

            oFileWriter.Close();
        }
        void GetCsvFiles()
        {
            if (Directory.Exists(sourcePath))
            {
                //Get count of All .CSV files files from source path

                int filesCount = Directory.GetFiles(sourcePath, "*.CSV*").Length;
                //sbFileLog.AppendLine("FileCount =" + filesCount.ToString() + " in " + sourcePath);
                if (filesCount > 0)
                {
                    //All csv files storing into files array.
                    string[] files = Directory.GetFiles(sourcePath, "*.CSV*");
                    foreach (string file in files)
                    {

                        //Get All data from file to csvDt datatable
                        DataTable csvDt = new DataTable();
                        csvDt = CsvToDataTable(file);

                        //Pass the csvDt datatable for data importation to DB
                        if (csvDt.Rows.Count > 0)
                        {
                            sbFileLog = new StringBuilder();
                            bool Error = false;
                            try
                            {

                                sbFileLog.AppendLine("**********************************");
                                sbFileLog.AppendLine(DateTime.Now.ToString("yyyyMMddHHmmss"));
                                if (environment == "Prod")
                                {
                                    csvRMAProcImportation(csvDt, file);
                                }
                                else if (environment == "Pilot")
                                {
                                    //csvRMAProcPilotImportation(csvDt, file);
                                }
                                else
                                {
                                    sbFileLog.AppendLine("Environment is not valid");
                                    return;
                                }
                                sbFileLog.AppendLine(DateTime.Now.ToString("yyyyMMddHHmmss"));
                                sbFileLog.AppendLine("**********************************");
                            }
                            catch (Exception ex)
                            {
                                sbFileLog.AppendLine(Logger.Log(ex, eventId++));
                                Error = true;
                                throw new Exception(ex.ToString());

                            }
                            finally
                            {
                                if (!Error)
                                {
                                    WriteLogInfoFile(successLocation, file, sbFileLog);
                                }
                                else
                                {
                                    WriteLogInfoFile(failedLocation, file, sbFileLog);
                                }
                                if (sbFileLog != null)
                                {
                                    sbFileLog.Clear();
                                }
                            }
                        }
                        else
                        {
                            //csvDt File doesnot have rows.
                            throw new Exception(file + "  - File doesnot have rows.");

                        }

                    }
                }
                else
                {
                    //file count is 0
                    throw new Exception("No Files found in this path : " + sourcePath);
                }
            }
            else
            {
                throw new Exception("Source Path Not found : " + sourcePath);
            }
        }
        public DataTable CsvToDataTable(string file)
        {
            DataTable dtFile = null;
            StreamReader oStreamReader = null;
            try
            {
                if (!IsFileLocked(file))
                {
                    oStreamReader = new StreamReader(file);
                    //DataTable dtFile = null;
                    int rowCount = 1;
                    string[] columnNames = null;
                    string[] oStreamDataValues = null;

                    while (!oStreamReader.EndOfStream)
                    {
                        string oStreamRowData = oStreamReader.ReadLine().Trim();
                        //split by seperator within the first read line string
                        //char[] stringSeparators = new char[] {  };
                        oStreamDataValues = oStreamRowData.Split('|');
                        //row = oStreamRowData.Split('|');
                        //oStreamDataValues = oStreamDataValues.Select(s => s.Replace("\"", "")).ToArray();

                        if (oStreamDataValues[0].Length > 0)
                        {
                            if (rowCount == 1)
                            {
                                rowCount = 3;
                                columnNames = oStreamDataValues;
                                dtFile = new DataTable();
                                foreach (string column in columnNames)
                                {
                                    DataColumn oDataColumn = new DataColumn(column.Replace("\"", "").ToUpper(), typeof(string));
                                    oDataColumn.DefaultValue = string.Empty;
                                    dtFile.Columns.Add(oDataColumn);

                                }
                            }
                            else
                            {
                                if (columnNames[0] != oStreamDataValues[0])
                                {
                                    DataRow oDataRow = dtFile.NewRow();
                                    for (int i = 0; i < columnNames.Length; i++)
                                    {
                                        oDataRow[columnNames[i].Replace("\"", "")] = oStreamDataValues[i] == null ? string.Empty : oStreamDataValues[i].Replace("\"", "").ToString();

                                    }
                                    dtFile.Rows.Add(oDataRow);


                                }
                            }
                        }
                        else
                        {
                            //oStreamVlaues length is zero
                        }
                    }

                    oStreamReader.Close();

                }
                else
                {
                    //File is in Open Mode
                    oStreamReader.Close();
                    throw new System.UnauthorizedAccessException("File Name = " + file + " is read-only");
                }
            }
            catch (System.IndexOutOfRangeException e)
            {
                // Set IndexOutOfRangeException to the new exception's InnerException.
                oStreamReader.Close();
                StringBuilder csvSbLog = new StringBuilder();
                csvSbLog.AppendLine("FileName = " + file);
                csvSbLog.AppendLine("Message: Column  and Data Values count is not matching!");
                MoveFile(failedLocation, file);
                WriteLogInfoFile(failedLocation, file, csvSbLog);
                throw new System.ArgumentOutOfRangeException("FileName = " + file + "      Message: Column  and Data Values count is not matching!", e);
            }

            return dtFile;

        }

        
        public void csvRMAProcImportation(DataTable dtCsvRMAData, string file)
        {
             bool isSoUpdSuccess = false;
             string UserId = ConfigurationManager.AppSettings["UserId"];
             string Password = ConfigurationManager.AppSettings["Password"];
             string Company = ConfigurationManager.AppSettings["Company"];
             string Plant = ConfigurationManager.AppSettings["Plant"];
             
            svcRMAProc.RMAProcSvcContractClient svcRMAProcClient = new svcRMAProc.RMAProcSvcContractClient("BasicHttpBinding_RMAProcSvcContract");
            
             Ice.Core.Session oConn = new Ice.Core.Session(UserId, Password);
             oConn.CompanyID = Company;
             oConn.PlantID = Plant;
             svcRMAProcClient.ClientCredentials.UserName.UserName = UserId;
             svcRMAProcClient.ClientCredentials.UserName.Password = Password;
             svcRMAProcClient.Endpoint.EndpointBehaviors.Add(new HookServiceBehavior(new Guid(oConn.SessionID), UserId));
             svcRMAProc.RMAProcTableset dsRMAProc = null;
             try
             {
                 
                 var distRMANumsByCustID = dtCsvRMAData.AsEnumerable()
                                            .Select(w =>new{ UdRMANum= w.Field<string>("UD_RMANum"),custId=w.Field<string>("CustID")})
                                            .Distinct().ToList();
                 foreach (var UdRMANum in distRMANumsByCustID)
                 {
                     string udRMANo = UdRMANum.UdRMANum.ToString();
                     string custId = UdRMANum.custId.ToString();
                     sbFileLog.AppendLine("*****************************");
                     
                     //Detail Data
                     var dtlRMACsvData = dtCsvRMAData.AsEnumerable()
                                        .Where(w => w.Field<string>("UD_RMANum") == udRMANo && w.Field<string>("CustID") == custId)
                                        .Select(s => new
                                        {
                                            udRMNo = s.Field<string>("UD_RMANum"),
                                            whseCode = s.Field<string>("Whse_Code"),
                                            plant = s.Field<string>("Plant"),
                                            rmaDate = s.Field<string>("RMADate"),
                                            rmaLn = Convert.ToInt32(s.Field<string>("RMALine")),
                                            udCustRTVNo = s.Field<string>("UD_CustRTVNum"),
                                            refrnc = s.Field<string>("Reference"),
                                            partNo = s.Field<string>("PartNum"),
                                            retQty = s.Field<string>("ReturnQty"),
                                            retQtyUom = s.Field<string>("ReturnQtyUOM"),
                                            retReasonCode = s.Field<string>("ReturnReasonCode")
                                            //rcvDate = s.Field<string>("RcvDate"),
                                            //rcvQty = s.Field<string>("ReceivedQty"),
                                            //rcvQtyUom = s.Field<string>("ReceivedQtyUOM"),
                                            //binNum = s.Field<string>("BinNum")
                                        }).Distinct()
                                        .ToList().OrderBy(o => o.rmaLn);
                     DateTime rmaDate = Convert.ToDateTime(dtlRMACsvData.Select(s => s.rmaDate).FirstOrDefault());
                     int custNum = oDAL.GetCustNum(custId);
                     DataTable dtRMAHeadbyUdRMANo = new DataTable();
                     dtRMAHeadbyUdRMANo=oDAL.UdRMANumExistInRMAHead(udRMANo, custNum);
                     int RMAHdNum = 0;
                     if (dtRMAHeadbyUdRMANo.Rows.Count > 0)
                     {
                          RMAHdNum = dtRMAHeadbyUdRMANo.Rows[0].Field<int>("RMANum");
                          dsRMAProc = svcRMAProcClient.GetByID(RMAHdNum);
                         isSoUpdSuccess = true;
                         sbFileLog.AppendLine("RMANum=" + RMAHdNum.ToString() + " Already Exist for this UD_RMANum=" + udRMANo + " and CustId=" + custId);
                     }
                     else
                     {
                         dsRMAProc = new svcRMAProc.RMAProcTableset();
                         svcRMAProcClient.GetNewRMAHead(ref dsRMAProc);
                         int lastRMAHeadIndex=dsRMAProc.RMAHead.Count-1;
                         dsRMAProc.RMAHead[lastRMAHeadIndex].RMADate=rmaDate;
                         svcRMAProcClient.ChangeCustomer(ref dsRMAProc, custId);
                         //svcRMAProcClient.ChangeRMAHeadLegalNumber();
                         svcRMAProcClient.PreUpdate(ref dsRMAProc);
                         svcRMAProcClient.Update(ref dsRMAProc);
                         //UD Table updation by SysRowID
                         string sysRowId = dsRMAProc.RMAHead[lastRMAHeadIndex].SysRowID.ToString();
                         string udCustRTVNo = dtlRMACsvData.Select(s => s.udCustRTVNo).FirstOrDefault();
                         bool isUpd = oDAL.UpdateRMAHead_UDTable(sysRowId, udRMANo, udCustRTVNo);
                         if (isUpd)
                         {
                             //RMAHead_UD table updation Success.
                             sbFileLog.AppendLine("RMAHead_UD table updated.");
                         }
                         isSoUpdSuccess = true;
                         RMAHdNum = dsRMAProc.RMAHead[lastRMAHeadIndex].RMANum;
                         sbFileLog.AppendLine("RMANum=" + RMAHdNum.ToString()+" Created for this UD_RMANum=" + udRMANo + " and CustId=" + custId );
                     }

                     //int lastRMAHdIndex = dsRMAProc.RMAHead.Count - 1;
                     //int RMAHdNum = dsRMAProc.RMAHead[lastRMAHdIndex].RMANum;
                     foreach (var dtlrow in dtlRMACsvData)
                     {
                         int rmaLn = Convert.ToInt32(dtlrow.rmaLn.ToString());
                         int erpRMALn = 0;
                         DataTable dtRMADtl = new DataTable();
                         dtRMADtl=oDAL.GetRMADtl(udRMANo,RMAHdNum, rmaLn,dtlrow.partNo);
                         if (dtRMADtl.Rows.Count > 0)
                         {
                             //Already exist this record
                             erpRMALn=dtRMADtl.Rows[0].Field<int>("ErpRMALine");
                             isSoUpdSuccess = true;
                             sbFileLog.AppendLine();
                             sbFileLog.AppendLine("RMALine=" + erpRMALn.ToString() + " Already Exist for this UD_RMANum=" + udRMANo + " and CustId=" + custId + " and CSVRMALine=" + rmaLn.ToString());
                         }
                         else
                         {
                             string cPartNum=dtlrow.partNo;
                             System.Guid SysRowID = new Guid("00000000-0000-0000-0000-000000000000");
                             string rowType=string.Empty;
                             string questionString=string.Empty;
                             bool multipleMatch=false;
                             svcRMAProcClient.GetNewRMADtl(ref dsRMAProc, RMAHdNum);
                             int lastRMADtlIndex=dsRMAProc.RMADtl.Count-1;
                             svcRMAProcClient.ChangePartNum(ref dsRMAProc, ref cPartNum, SysRowID, rowType, out questionString, out multipleMatch);
                             dsRMAProc.RMADtl[lastRMADtlIndex].ReturnQty = Convert.ToDecimal(dtlrow.retQty);
                             dsRMAProc.RMADtl[lastRMADtlIndex].ReturnQtyUOM = dtlrow.retQtyUom;
                             dsRMAProc.RMADtl[lastRMADtlIndex].ReturnReasonCode = dtlrow.retReasonCode;
                             svcRMAProcClient.PreUpdate(ref dsRMAProc);
                             svcRMAProcClient.Update(ref dsRMAProc);
                             erpRMALn = dsRMAProc.RMADtl[lastRMADtlIndex].RMALine;

                             //WMSRMADtl table need to update to track csv data history with Erp RMA data.
                             oDAL.InsertWMSRMADtlInDataMigration(udRMANo, rmaLn, dtlrow.partNo, RMAHdNum, erpRMALn);
                             isSoUpdSuccess = true;
                             sbFileLog.AppendLine("RMALine=" + erpRMALn.ToString()+" Created for this UD_RMANum=" + udRMANo + " and CustId=" + custId + " and CSVRMALine=" + rmaLn.ToString());
                         }
                         //RMAReceipt Data processing...

                         var dtlRMARcptCsvData = dtCsvRMAData.AsEnumerable()
                                        .Where(w => w.Field<string>("UD_RMANum") == udRMANo && w.Field<string>("CustID") == custId && w.Field<string>("RMALine") == Convert.ToString(dtlrow.rmaLn))
                                        .Select(s => new
                                        {
                                            udRMNo = s.Field<string>("UD_RMANum"),
                                            whseCode = s.Field<string>("Whse_Code"),
                                            plant = s.Field<string>("Plant"),
                                            rmaDate =s.Field<string>("RMADate"),
                                            rmaLn = Convert.ToInt32(s.Field<string>("RMALine")),
                                            udCustRTVNo = s.Field<string>("UD_CustRTVNum"),
                                            refrnc = s.Field<string>("Reference"),
                                            partNo = s.Field<string>("PartNum"),
                                            retQty = s.Field<string>("ReturnQty"),
                                            retQtyUom = s.Field<string>("ReturnQtyUOM"),
                                            retReasonCode = s.Field<string>("ReturnReasonCode"),
                                            rcvDate = Convert.ToDateTime( s.Field<string>("RcvDate")),
                                            rcvQty =Convert.ToDecimal( s.Field<string>("ReceivedQty")),
                                            rcvQtyUom = s.Field<string>("ReceivedQtyUOM"),
                                            binNum = s.Field<string>("BinNum")
                                        }).Distinct()
                                        .ToList().OrderBy(o => o.rcvDate);
                         foreach (var dtlRcptRow in dtlRMARcptCsvData)
                         {
                             //string partNo = Regex.Replace(dtlRcptRow.partNo, @"\s+", "");
                             int erpRMAReceiptNo = 0;
                             var matchRMARcptData = dsRMAProc.RMARcpt.AsEnumerable()
                                                    .Where(w =>

                                                            w.RMANum == RMAHdNum &&
                                                            w.RMALine == erpRMALn &&
                                                            w.RcvDate == dtlRcptRow.rcvDate &&
                                                            w.ThisRcptQty == dtlRcptRow.rcvQty &&
                                                            w.PartNum == dtlRcptRow.partNo.Trim() &&
                                                            w.ThisRcptQtyUOM == dtlRcptRow.rcvQtyUom
                                                        ).Select(s => s.RMAReceipt).ToList();
                             int i =  matchRMARcptData.Count;
                             if (i > 0)
                             {
                                 //already exist data
                                  erpRMAReceiptNo = matchRMARcptData.FirstOrDefault();
                                 sbFileLog.AppendLine("RMAReceipt="+erpRMAReceiptNo.ToString()+" Already exist for this RMALine=" + erpRMALn.ToString());
                             }
                             else
                             {
                                 svcRMAProcClient.GetNewRMARcpt(ref dsRMAProc, RMAHdNum, erpRMALn);
                                 int lastRcptIndex = dsRMAProc.RMARcpt.Count - 1;
                                 dsRMAProc.RMARcpt[lastRcptIndex].RcvDate = dtlRcptRow.rcvDate;
                                 dsRMAProc.RMARcpt[lastRcptIndex].ReceivedQty = dtlRcptRow.rcvQty;
                                 dsRMAProc.RMARcpt[lastRcptIndex].ReceivedQtyUOM = dtlRcptRow.rcvQtyUom;
                                 dsRMAProc.RMARcpt[lastRcptIndex].WareHouseCode = dtlRcptRow.binNum;
                                 dsRMAProc.RMARcpt[lastRcptIndex].BinNum = dtlRcptRow.binNum;
                                 svcRMAProcClient.ChangeWarehouse(ref dsRMAProc);
                                 svcRMAProcClient.PreUpdate(ref dsRMAProc);
                                 svcRMAProcClient.Update(ref dsRMAProc);
                                 erpRMAReceiptNo = dsRMAProc.RMARcpt[lastRcptIndex].RMAReceipt;
                                 sbFileLog.AppendLine("RMAReceipt=" + erpRMAReceiptNo.ToString() + " Created for this RMALine=" + erpRMALn.ToString());
                             }
                         }
                         //RMAReceipt Data Process done.
                     }
                 }

             }
             catch (Exception ex)
             {
                 isSoUpdSuccess = false;
                 throw new Exception(ex.Message.ToString());
             }
             finally
             {
                 if (isSoUpdSuccess == false)
                 {
                     sbFileLog.AppendLine("Error Occured...");
                     MoveFile(failedLocation, file);
                     sbFileLog.AppendLine(file + " File Moved to " + failedLocation);
                 }
                 else
                 {
                     MoveFile(successLocation, file);
                     sbFileLog.AppendLine(file + " File Moved to " + successLocation);
                 }
                 if (dsRMAProc != null)
                 {
                     dsRMAProc = null;
                 }

                 if (oConn != null)
                 {
                     oConn.Dispose();
                     oConn = null;
                 }
             }
        }
        
       

        #region FileLock
        public bool IsFileLocked(string file)
        {
            bool Locked = false;
            try
            {
                FileStream fs =
                    File.Open(file, FileMode.OpenOrCreate,
                    FileAccess.ReadWrite, FileShare.None);
                fs.Close();
            }
            catch (IOException ex)
            {
                Locked = true;
            }
            return Locked;
        }

        #endregion FIleLock

        public int eventId { get; set; }

        public void MoveFile(string Loc, string file)
        {
            if (!string.IsNullOrEmpty(Loc))
            {
                if (!Directory.Exists(Loc))
                {
                    System.IO.Directory.CreateDirectory(Loc);
                    if (!IsFileLocked(file))
                    {
                        string fileName = Path.GetFileName(file);
                        string dstfile = Path.Combine(Loc, fileName);
                        if (File.Exists(dstfile))
                        {
                            File.Delete(dstfile);

                        }
                        File.Move(file, dstfile);


                    }
                    else
                    {
                        //logInfoBuild.AppendLine("File Locked");
                        //sbLog.AppendLine("File Locked");
                    }
                }
                else
                {
                    if (!IsFileLocked(file))
                    {
                        string fileName = Path.GetFileName(file);
                        string dstfile = Path.Combine(Loc, fileName);
                        if (File.Exists(dstfile))
                        {
                            File.Delete(dstfile);
                        }

                        File.Move(file, dstfile);


                    }
                    else
                    {
                        // logInfoBuild.AppendLine("File Locked");
                        //sbLog.AppendLine("File Locked");
                    }
                }
            }
        }
        public void WriteLogInfoFile(string fileLoc, string file, StringBuilder sbMessage)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);
            //string WriteLogfileLocation = ConfigurationManager.AppSettings["LogfileLocation"];
            if (!File.Exists(fileLoc + fileName + "_LogInfo.txt"))
                System.IO.File.Create(fileLoc + fileName + "_LogInfo.txt").Dispose();
            using (System.IO.StreamWriter fileWrite =
           new System.IO.StreamWriter(fileLoc + fileName + "_LogInfo.txt", true))
            {
                fileWrite.WriteLine(sbMessage.ToString());
                sbMessage.Clear();
            }

        }
        public void ErrorInfoFile(string location, string errMessage)
        {
            try
            {
                //string fileName = Path.GetFileNameWithoutExtension(file);
                //string WriteLogfileLocation = ConfigurationManager.AppSettings["LogfileLocation"];
                if (!File.Exists(location + "OnStartInfo.txt"))
                    System.IO.File.Create(location + "OnStartInfo.txt").Dispose();
                using (System.IO.StreamWriter fileWrite =
               new System.IO.StreamWriter(location + "OnStartInfo.txt", false))
                {
                    fileWrite.WriteLine(errMessage);
                    errMessage = string.Empty;
                }
            }
            catch (Exception ex)
            {
                FileCreation(ex.Message.ToString());
            }

        }

    }
}
