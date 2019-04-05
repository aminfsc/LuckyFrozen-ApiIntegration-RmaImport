using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WMSImportation
{
    public class DataAccessLayer
    {
        public static int isPackNumExistInRcvHead(string PackSlip, string VendorID)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("select count(*) from erp.RcvHead a left join  erp.Vendor b on a.Company=b.Company and a.VendorNum=b.VendorNum where b.VendorID=@VendorID and a.PackSlip=@PackSlip", connection);
                cmd.Parameters.AddWithValue("@PackSlip", PackSlip);
                cmd.Parameters.AddWithValue("@VendorID", VendorID);
                int i = (int)cmd.ExecuteScalar();
                return i;
            }
        }

        public static int isLineExistInRcvDtl(int VendorNum, string PackSlip, int PONum, int POLine, int PORelNum)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("select count(*) from erp.RcvDtl where VendorNum=@VendorNum and PackSlip=@PackSlip and PONum=@PONum and POLine=@POLine and PORelNum=@PORelNum", connection);
                cmd.Parameters.AddWithValue("@VendorNum", VendorNum);
                cmd.Parameters.AddWithValue("@PackSlip", PackSlip);                
                cmd.Parameters.AddWithValue("@PONum", PONum);
                cmd.Parameters.AddWithValue("@POLine", POLine);
                cmd.Parameters.AddWithValue("@PORelNum", PORelNum);
                int i = (int)cmd.ExecuteScalar();
                return i;
            }
        }
        public static int GetVendorNum(string VendorID)
        {
            int i = 0;
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("select VendorNum from erp.Vendor where VendorID=@VendorID", connection);                
                cmd.Parameters.AddWithValue("@VendorID", VendorID);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    i = Convert.ToInt32(dr["VendorNum"]);
                }
                return i;
            }
        }

        public void UpdateCurrentRowbyGenereicRowData(string GenericSysRowID, string NewRowSysRowID)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("[dbo].[GenericUDRowDataUpdation]", connection);
                
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@GenericSysRowID", SqlDbType.VarChar).Value = GenericSysRowID;
                    cmd.Parameters.Add("@NewRowSysRowID", SqlDbType.VarChar).Value = NewRowSysRowID;
               
                    cmd.ExecuteNonQuery();
                    
                
            }
        }
        public void InsertUd09VaryWgData(string Company,string packNum, string orderNum,string orderLine, string OrderRel, string SeqCount, decimal varyWg, string packLine)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("Insert into ice.ud09 (Company,Key1,Key2,Key3,Key4,Key5,Number01,ShortChar01,Number02,Number03,Number04,Number05) values (@Company,@packNum,@orderNum,@orderLine,@OrderRel,@SeqCount,@varyWg,@packLine,@SeqCount,@orderNum,@orderLine,@OrderRel) ", connection);
                cmd.Parameters.AddWithValue("@Company", Company);
                cmd.Parameters.AddWithValue("@packNum", packNum);
                cmd.Parameters.AddWithValue("@orderNum", orderNum);
                cmd.Parameters.AddWithValue("@orderLine", orderLine);
                cmd.Parameters.AddWithValue("@OrderRel", OrderRel);
                cmd.Parameters.AddWithValue("@SeqCount", SeqCount);
                cmd.Parameters.AddWithValue("@varyWg", varyWg);
                cmd.Parameters.AddWithValue("@packLine", packLine);
                
                cmd.ExecuteNonQuery();
                connection.Close();

            }
        }

        public DataTable OrderNumberInShipment(string company, string plant, string orderNumber)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter da = new SqlDataAdapter("select  PackNum from erp.ShipHead where Company=@company and Plant=@plant and OTSOrderNum=@orderNumber order by PackNum desc", connection);
                da.SelectCommand.Parameters.AddWithValue("@company", company);
                da.SelectCommand.Parameters.AddWithValue("@plant", plant);
                da.SelectCommand.Parameters.AddWithValue("@orderNumber", orderNumber);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
        public DataTable TFOrderNumInTFShipDtl(string TForderNum)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter da = new SqlDataAdapter("select distinct PackNum from erp.TFShipDtl where TFOrdNum=@TFOrdNum", connection);

                da.SelectCommand.Parameters.AddWithValue("@TFOrdNum", TForderNum);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
        public DataTable TFOrderNumInTFShipDtlForLnLevel(string TForderNum,int TFOrdLn)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter da = new SqlDataAdapter("select distinct PackNum,PackLine from erp.TFShipDtl where TFOrdNum=@TFOrdNum and TFOrdLine=@TFOrdLn", connection);

                da.SelectCommand.Parameters.AddWithValue("@TFOrdNum", TForderNum);
                da.SelectCommand.Parameters.AddWithValue("@TFOrdLn", TFOrdLn);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
        public string GetshipViaCode(string desc)
        {
            string shipViaCode = string.Empty;
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("select ShipViaCode From erp.ShipVia where [Description]=@desc", connection);
                cmd.Parameters.AddWithValue("@desc", desc);
                SqlDataReader sdr = cmd.ExecuteReader();
                
                while (sdr.Read())
                {
                    // get the results of each column
                    shipViaCode = (string)sdr["ShipViaCode"];
                }
                connection.Close();
            }
            return shipViaCode;
        }
        public int existShipLine(string Company, string Plant, int OrderNum, int OrderLine, int OrderRelNum, string PartNum)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("select count(*) from erp.ShipDtl where Company=@Company and Plant=@Plant and OrderNum=@OrderNum and OrderLine=@OrderLine and OrderRelNum=@OrderRelNum and PartNum=@PartNum ", connection);
                cmd.Parameters.AddWithValue("@Company", Company);
                cmd.Parameters.AddWithValue("@Plant", Plant);
                cmd.Parameters.AddWithValue("@OrderNum", OrderNum);
                cmd.Parameters.AddWithValue("@OrderLine", OrderLine);
                cmd.Parameters.AddWithValue("@OrderRelNum", OrderRelNum);
                cmd.Parameters.AddWithValue("@PartNum", PartNum);
                //cmd.Parameters.AddWithValue("@OurInventoryShipQty", OurInventoryShipQty);
                //cmd.Parameters.AddWithValue("@InventoryShipUOM", InventoryShipUOM);
                
                int i = (int)cmd.ExecuteScalar();
                return i;
            }
        }

        public void InsertGenericHistory(string FileName, int OrderNum, int OrderLn, int OrderRelNum, string PartNum, string GenericPartNum, int NewOrderLine)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["DataMigrationConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("Insert into GenericHistory ([FileName],OrderNum,OrderLn,OrderRelNum,PartNum,GenericPartNum,NewOrderLine) values (@FileName,@OrderNum,@OrderLn,@OrderRelNum,@PartNum,@GenericPartNum,@NewOrderLine)", connection);
                cmd.Parameters.AddWithValue("@FileName", FileName);
                cmd.Parameters.AddWithValue("@OrderNum", OrderNum);
                cmd.Parameters.AddWithValue("@OrderLn", OrderLn);
                cmd.Parameters.AddWithValue("@OrderRelNum", OrderRelNum);
                cmd.Parameters.AddWithValue("@PartNum", PartNum);
                cmd.Parameters.AddWithValue("@GenericPartNum", GenericPartNum);
                cmd.Parameters.AddWithValue("@NewOrderLine", NewOrderLine);

                cmd.ExecuteNonQuery();
                connection.Close();

            }
        }
        public void InsertWMSRMADtlInDataMigration(string UDRMANum, int CsvRMALine, string PartNum, int ErpRMANum, int ErpRMALine)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["DataMigrationConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("Insert into WMSRMADtl (UDRMANum,CsvRMALine,PartNum,ErpRMANum,ErpRMALine) values (@UDRMANum,@CsvRMALine,@PartNum,@ErpRMANum,@ErpRMALine)", connection);
                cmd.Parameters.AddWithValue("@UDRMANum", UDRMANum);
                cmd.Parameters.AddWithValue("@CsvRMALine", CsvRMALine);
                cmd.Parameters.AddWithValue("@ErpRMANum", ErpRMANum);
                cmd.Parameters.AddWithValue("@ErpRMALine", ErpRMALine);
                cmd.Parameters.AddWithValue("@PartNum", PartNum);
              
                cmd.ExecuteNonQuery();
                connection.Close();

            }
        }
        public DataTable GetGenericHistory(string FileName, int OrderNum, int OrderLn, int OrderRelNum, string PartNum, string GenericPartNum)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["DataMigrationConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter da = new SqlDataAdapter("select distinct NewOrderLine From GenericHistory where [FileName]=@FileName and OrderNum=@OrderNum and OrderLn=@OrderLn and OrderRelNum=@OrderRelNum and PartNum=@PartNum and GenericPartNum=@GenericPartNum", connection);
                da.SelectCommand.Parameters.AddWithValue("@FileName", FileName);
                da.SelectCommand.Parameters.AddWithValue("@OrderNum", OrderNum);
                da.SelectCommand.Parameters.AddWithValue("@OrderLn", OrderLn);
                da.SelectCommand.Parameters.AddWithValue("@OrderRelNum", OrderRelNum);
                da.SelectCommand.Parameters.AddWithValue("@PartNum", PartNum);
                da.SelectCommand.Parameters.AddWithValue("@GenericPartNum", GenericPartNum);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }
        public string GetBinNumByPartAndWhseCode(string PartNum, string WhseCode)
        {
            string binNum = string.Empty;
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("[dbo].[GetBinNumWMS]", connection);

                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add("@PartNum", SqlDbType.VarChar).Value = PartNum;
                cmd.Parameters.Add("@WhseCode", SqlDbType.VarChar).Value = WhseCode;

                SqlDataReader rdr = cmd.ExecuteReader();
                while (rdr.Read())
                {
                    // get the results of each column
                    binNum = (string)rdr["PartBin_BinNum"];
                    
                }

            }
            return binNum;
        }

        public  int GetCustNum(string custID)
        {
            int i = 0;
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("select CustNum from Customer where CustID=@CustID", connection);
                cmd.Parameters.AddWithValue("@CustID", custID);
                SqlDataReader dr = cmd.ExecuteReader();
                while (dr.Read())
                {
                    i = Convert.ToInt32(dr["CustNum"]);
                }
                return i;
            }
        }

        public DataTable UdRMANumExistInRMAHead(string udRMANum, int CustNum)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter da = new SqlDataAdapter("select RMANum,CustNum,RMANum_c from RMAHead where RMANum_c=@RMANum_c and CustNum=@CustNum", connection);

                da.SelectCommand.Parameters.AddWithValue("@RMANum_c", udRMANum);
                da.SelectCommand.Parameters.AddWithValue("@CustNum", CustNum);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public DataTable GetRMADtl(string UDRMANum, int ErpRMANum, int CsvRMALine, string PartNum)
        {
            string ConnectionString = ConfigurationManager.ConnectionStrings["DataMigrationConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlDataAdapter da = new SqlDataAdapter("select ErpRMALine From WMSRMADtl where UDRMANum=@UDRMANum and CsvRMALine=@CsvRMALine and PartNum=@PartNum and ErpRMANum=@ErpRMANum", connection);
                da.SelectCommand.Parameters.AddWithValue("@UDRMANum", UDRMANum);
                da.SelectCommand.Parameters.AddWithValue("@ErpRMANum", ErpRMANum);
                da.SelectCommand.Parameters.AddWithValue("@CsvRMALine", CsvRMALine);
                da.SelectCommand.Parameters.AddWithValue("@PartNum", PartNum);
                DataTable dt = new DataTable();
                da.Fill(dt);
                return dt;
            }
        }

        public bool UpdateRMAHead_UDTable(string sysRowId, string RMANum_c, string CustRTVNum_c)
        {
            bool isUpd = false;
            string ConnectionString = ConfigurationManager.ConnectionStrings["LuckyConStr"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                SqlCommand cmd = new SqlCommand("update erp.RMAHead_UD set RMANum_c=@UdRMANum,CustRTVNum_c=@UdCustRTVNum where ForeignSysRowID=@ForeignSysRowID", connection);
                cmd.Parameters.AddWithValue("@ForeignSysRowID", sysRowId);
                cmd.Parameters.AddWithValue("@UdRMANum", RMANum_c);
                cmd.Parameters.AddWithValue("@UdCustRTVNum", CustRTVNum_c);

                cmd.ExecuteNonQuery();
                connection.Close();
                isUpd = true;
                 
            }
            return isUpd;
        }
    }
}
