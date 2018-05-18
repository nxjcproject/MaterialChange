using SqlServerDataAdapter;
using MaterialChange.Infrastructure.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace MaterialChange.Service.Production
{
    public class MaterialProductionService
    {
        public static DataTable GetProductionLineDataTable(string mOrganizationId)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            string sql = @"SELECT 
                                   A.[OrganizationID]
                                  ,B.[Name]
                              FROM [dbo].[material_MaterialChangeContrast] A,[dbo].[system_Organization] B
                              WHERE A.[OrganizationID]=B.[OrganizationID]
                              AND A.[OrganizationID] like @mOrganizationID + '%'
                              GROUP BY A.[OrganizationID],B.[Name]";
            SqlParameter parameter = new SqlParameter("mOrganizationID", mOrganizationId);
            DataTable table = dataFactory.Query(sql, parameter);
            DataRow newrow = table.NewRow();
            newrow["OrganizationID"] = "cementmill";
            newrow["Name"] = "全部";
            table.Rows.InsertAt(newrow, 0);
            return table;
        }
        public static DataTable GetMaterialChangeDataTable(string mOrganizationId, string productionLine, string startTime, string endTime)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            DataTable table = new DataTable();
            if (productionLine == "cementmill")
            {
                string Allsql = @"(SELECT 
                                     A.[OrganizationID]
                                    ,C.[Name]
		                            ,B.[MaterialColumn]
		                            ,A.[ChangeStartTime]
		                            ,A.[ChangeEndTime]
		                            ,B.[VariableId]
		                            ,B.[MaterialDataBaseName]
		                            ,B.[MaterialDataTableName]
                                    ,'' as [LevelCode]
		                            ,'leafnode' as [NodeType]
	                            FROM [dbo].[material_MaterialChangeLog] A,
                                     [dbo].[material_MaterialChangeContrast] B,
                                     [dbo].[system_Organization] C
	                            where A.OrganizationID like @mOrganizationId + '%'
                                    and A.OrganizationID=C.OrganizationID
	                                and B.[ContrastID]=A.[ContrastID]
		                            and A.[VariableType]='Cement'
		                            and LOWER(A.EventValue) = LOWER(B.Valid)
                                    and B.[VariableId] != '自产/外购熟料'
		                            and ((A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@startTime) or
                                    (A.[ChangeStartTime]>=@startTime
                                    and A.[ChangeEndTime]<=@endTime) or (A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@endTime)
	                                or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime]>=@endTime)
                                    or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime] is NULL))
                                )
	                            union all
	                            (SELECT  A.[OrganizationID]
                                        ,C.[Name]
                                        ,'' as [MaterialColumn]
                                        ,'' as [ChangeStartTime]
                                        ,'' as [ChangeEndTime]
                                        ,'' as [VariableId]
                                        ,'' as [MaterialDataBaseName]
                                        ,'' as [MaterialDataTableName]
                                        ,'' as [LevelCode]
                                        ,'node' as [NodeType]
	                             from [dbo].[material_MaterialChangeLog] A,
                                      [dbo].[material_MaterialChangeContrast] B,
                                      [dbo].[system_Organization] C
	                            where A.OrganizationID like @mOrganizationId + '%'
                                    and A.OrganizationID=C.OrganizationID
	                                and B.[ContrastID]=A.[ContrastID]
		                            and A.[VariableType]='Cement'
		                            and LOWER(A.EventValue) = LOWER(B.Valid)
                                    and B.[VariableId] != '自产/外购熟料'
		                            and ((A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@startTime) or
                                    (A.[ChangeStartTime]>=@startTime
                                    and A.[ChangeEndTime]<=@endTime) or (A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@endTime)
	                                or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime]>=@endTime)
                                    or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime] is NULL))
		                            group by A.[OrganizationID],C.[Name])
		                            order by A.[OrganizationID],A.[ChangeStartTime]";
                SqlParameter[] Allparameter ={
                                          new SqlParameter("mOrganizationId", mOrganizationId),
                                          new SqlParameter("startTime", startTime),
                                          new SqlParameter("endTime", endTime)
                                       };
                table = dataFactory.Query(Allsql, Allparameter);
            }
            else
            {
                string sql = @"(SELECT  
                                   A.[OrganizationID]
                                  ,C.[Name]
		                          ,B.[MaterialColumn]
		                          ,A.[ChangeStartTime]
		                          ,A.[ChangeEndTime]
		                          ,B.[VariableId]
		                          ,B.[MaterialDataBaseName]
		                          ,B.[MaterialDataTableName]
                                  ,'' as [LevelCode]
                                  ,'leafnode' as [NodeType]
	                          FROM [dbo].[material_MaterialChangeLog] A,
                                   [dbo].[material_MaterialChangeContrast] B,
                                   [dbo].[system_Organization] C
	                          where A.OrganizationID=@productionLine
                                    and A.OrganizationID=C.OrganizationID
	                                and B.[ContrastID]=A.[ContrastID]
			                        and A.[VariableType]='Cement'
			                        and LOWER(A.EventValue) = LOWER(B.Valid)
                                    and B.[VariableId] != '自产/外购熟料'
			                        and ((A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@startTime) or
                                    (A.[ChangeStartTime]>=@startTime
                                    and A.[ChangeEndTime]<=@endTime) or (A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@endTime)
	                                or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime]>=@endTime)
                                    or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime] is NULL))
                              union all
                              (SELECT  
                                   A.[OrganizationID]
                                  ,C.[Name]
		                          ,'' as [MaterialColumn]
		                          ,'' as [ChangeStartTime]
		                          ,'' as [ChangeEndTime]
		                          ,'' as [VariableId]
		                          ,'' as [MaterialDataBaseName]
		                          ,'' as [MaterialDataTableName]
                                  ,'' as [LevelCode]
                                  ,'node' as [NodeType]
	                          FROM [dbo].[material_MaterialChangeLog] A,
                                   [dbo].[material_MaterialChangeContrast] B,
                                   [dbo].[system_Organization] C
	                          where A.OrganizationID=@productionLine
                                    and A.OrganizationID=C.OrganizationID
	                                and B.[ContrastID]=A.[ContrastID]
			                        and A.[VariableType]='Cement'
			                        and LOWER(A.EventValue) = LOWER(B.Valid)
                                    and B.[VariableId] != '自产/外购熟料'
			                        and ((A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@startTime) or
                                    (A.[ChangeStartTime]>=@startTime
                                    and A.[ChangeEndTime]<=@endTime) or (A.[ChangeStartTime]<=@startTime and A.[ChangeEndTime]>=@endTime)
	                                or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime]>=@endTime)
                                    or (A.[ChangeStartTime]<=@endTime and A.[ChangeEndTime] is NULL))
                                    group by A.[OrganizationID],C.[Name]))
                              order by A.[ChangeStartTime]";
                SqlParameter[] parameter ={
                                          new SqlParameter("productionLine", productionLine),
                                          new SqlParameter("startTime", startTime),
                                          new SqlParameter("endTime", endTime)
                                       };
                table = dataFactory.Query(sql, parameter);
                DataRow row = table.NewRow();
                row["OrganizationID"] = productionLine;
                row["LevelCode"] = "M01";
            }         
            table.Columns.Add("Production");
            table.Columns.Add("Formula");
            table.Columns.Add("Consumption");
            table.Columns.Add("ClinkerConsumptionValue");//熟料消耗量
            table.Columns.Add("ClinkerConsumption");//熟料料耗
            int count = table.Rows.Count;
            for (int j = 0; j < count; j++)
            {
                if (table.Rows[j]["ChangeEndTime"].ToString() == "")
                {
                    table.Rows[j]["ChangeEndTime"] = endTime;
                }
                DateTime m_startTime = Convert.ToDateTime(table.Rows[j]["ChangeStartTime"].ToString().Trim());
                DateTime m_endTime = Convert.ToDateTime(table.Rows[j]["ChangeEndTime"].ToString().Trim());
                if (DateTime.Compare(m_startTime, Convert.ToDateTime(startTime))== -1)
                {
                    table.Rows[j]["ChangeStartTime"] = startTime;
                }
                if (DateTime.Compare(m_endTime,Convert.ToDateTime(endTime))==1)
                {
                    table.Rows[j]["ChangeEndTime"] = endTime;
                }
            }
            for (int i = 0; i < count; i++)
            {
                string nodeType = table.Rows[i]["NodeType"].ToString().Trim();
                if (nodeType == "leafnode")
                {
                    string materialDataBaseName = table.Rows[i]["MaterialDataBaseName"].ToString().Trim();
                    string materialDataTableName = table.Rows[i]["MaterialDataTableName"].ToString().Trim();
                    string changeStartTime = table.Rows[i]["ChangeStartTime"].ToString().Trim();
                    string changeEndTime = table.Rows[i]["ChangeEndTime"].ToString().Trim();
                    string materialColumn = table.Rows[i]["MaterialColumn"].ToString().Trim();
                    string m_productionLine = table.Rows[i]["OrganizationID"].ToString().Trim();
                    //计算电量
                    string mSql = @"select cast(sum(FormulaValue) as decimal(18,2)) as Formula 
                                    from {0}.[dbo].[HistoryFormulaValue]
                                    where vDate>=@changeStartTime
                                      and vDate<=@changeEndTime
                                      and variableId = 'cementPreparation'
	                                  and OrganizationID=@m_productionLine";
                    SqlParameter[] para ={
                                        new SqlParameter("m_productionLine", m_productionLine),
                                        new SqlParameter("changeStartTime", changeStartTime),
                                        new SqlParameter("changeEndTime", changeEndTime)
                                     };
                    DataTable passTable = dataFactory.Query(string.Format(mSql, materialDataBaseName), para);
                    string mFormula = passTable.Rows[0]["Formula"].ToString().Trim();
                    //计算水泥产量
                    string mSsql = @"select cast(sum({0}) as decimal(18,2)) as [MaterialProduction] 
                                       from {1}.[dbo].{2}
                                      where vDate>=@changeStartTime
                                        and vDate<=@changeEndTime";
                    SqlParameter[] paras ={ new SqlParameter("changeStartTime", changeStartTime),
                                            new SqlParameter("changeEndTime", changeEndTime)};
                    DataTable resultTable = dataFactory.Query(string.Format(mSsql, materialColumn, materialDataBaseName, materialDataTableName), paras);
                    string mProduction = resultTable.Rows[0]["MaterialProduction"].ToString().Trim();
                    //计算熟料消耗量 闫潇华添加
                    string mClinkerConsumptionFormulaSql= @"SELECT A.VariableId
                                                         ,A.Name
                                                         ,A.KeyID
                                                         ,A.Type
                                                         ,A.TagTableName
                                                         ,A.Formula
                                                    FROM [dbo].[material_MaterialDetail] A
                                                        ,[dbo].[tz_Material] B   
                                                    where B.OrganizationID='{0}'
                                                      and B.KeyID=A.KeyID
                                                      and A.VariableId in ('clinker_ClinkerInput','clinker_ClinkerOutsourcingInput','clinker_ClinkerCompanyTransportInput','clinker_ClinkerFactoryTransportInput')";
                    mClinkerConsumptionFormulaSql = string.Format(mClinkerConsumptionFormulaSql, m_productionLine);
                    DataTable mClinkerConsumptionFormulaTable = new DataTable();
                    try
                    {
                        mClinkerConsumptionFormulaTable = dataFactory.Query(mClinkerConsumptionFormulaSql);
                    }
                    catch 
                    {
                        return null;
                    }
                    StringBuilder mClinkerConsumptionFormula = new StringBuilder();//熟料消耗量公式
                    if (mClinkerConsumptionFormulaTable != null) 
                    {
                        int mCount = mClinkerConsumptionFormulaTable.Rows.Count;
                        for (int j = 0; j < mCount; j++)
                        {
                            if (mClinkerConsumptionFormulaTable.Rows[j]["Formula"].ToString().Trim() != "0" || mClinkerConsumptionFormulaTable.Rows[j]["Formula"].ToString().Trim() != "")
                            {
                                if (j == 0)
                                {
                                    mClinkerConsumptionFormula.Append(mClinkerConsumptionFormulaTable.Rows[j]["Formula"].ToString().Trim());
                                }
                                else
                                {
                                    mClinkerConsumptionFormula.Append("+" + mClinkerConsumptionFormulaTable.Rows[j]["Formula"].ToString().Trim());
                                }
                            }
                            
                        }
                    }
                    string mClinkerConsumptionValueSql = @"select cast(sum({0}) as decimal(18,2)) as ClinkerConsumptionValue
                                                             from {1}.[dbo].{2}
                                                            where vDate>=@changeStartTime
                                                              and vDate<=@changeEndTime";
                    SqlParameter[] mClinkerConsumptionValueParas ={ new SqlParameter("changeStartTime", changeStartTime),
                                                                    new SqlParameter("changeEndTime", changeEndTime)};
                    DataTable mClinkerConsumptionValueTable = dataFactory.Query(string.Format(mClinkerConsumptionValueSql, mClinkerConsumptionFormula, materialDataBaseName, materialDataTableName), mClinkerConsumptionValueParas);
                    string mClinkerConsumptionValue = mClinkerConsumptionValueTable.Rows[0]["ClinkerConsumptionValue"].ToString().Trim();
                    table.Rows[i]["ClinkerConsumptionValue"] = mClinkerConsumptionValue;
                    table.Rows[i]["Production"] = mProduction;
                    table.Rows[i]["Formula"] = mFormula;
                }               
            }
            //对同一品种进行合并
            #region            //DataTable result = table.Clone();
            //for (int i = 0; i < table.Rows.Count; i++)
            //{
            //    if (table.Rows[i]["Production"].ToString() !="" && table.Rows[i]["Formula"].ToString() !="")
            //    {
            //        DataRow[] firstRow = table.Select("Name = '" + table.Rows[i]["Name"].ToString() + " ' and VariableId='" + table.Rows[i]["VariableId"].ToString() + "'");
            //        DataTable temp = table.Clone();
            //        foreach (DataRow row in firstRow)
            //        {
            //            temp.Rows.Add(row.ItemArray);
            //        }
            //        DataRow dr = table.NewRow();
            //        dr["Name"] = table.Rows[i][0].ToString();
            //        dr["Production"] = temp.Compute("sum(Production)", "");
            //        dr["Formula"] = temp.Compute("sum(Formula)", "");
            //        result.Rows.Add(dr);
            //    }              
            //}
            #endregion
            #region
            //DataTable dtResult = table.Clone();   
            //for (int i = 0; i < table.Rows.Count;)
            //{
            //    if (table.Rows[i]["Production"].ToString()=="")
            //    {
            //        table.Rows[i]["Production"] = "0";
            //    }
            //    if (table.Rows[i]["Formula"].ToString()=="")
            //    {
            //        table.Rows[i]["Formula"] = "0";
            //    }
            //    DataRow dr = dtResult.NewRow();
            //    string name = table.Rows[i]["Name"].ToString();
            //    string variableid = table.Rows[i]["VariableId"].ToString();
            //    dr["Name"] = name;
            //    dr["VariableId"] = variableid;
            //    double m_production = 0;
            //    double m_formula = 0;
            //    for (; i < dtResult.Rows.Count;)
            //    {
            //        if (name==table.Rows[i]["Name"].ToString() && variableid==table.Rows[i]["VariableId"].ToString())
            //        {
            //            m_production += Convert.ToDouble(table.Rows[i]["Production"].ToString());
            //            m_formula += Convert.ToDouble(table.Rows[i]["Formula"].ToString());
            //            dr["Production"] = m_production;
            //            dr["Formula"] = m_formula;
            //            i++;
            //        }
            //        else
            //        {
            //            break;
            //        }
            //    }
            //    dtResult.Rows.Add(dr);
            //}
            #endregion
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i]["Production"].ToString() == "")
                {
                    table.Rows[i]["Production"] = "0";
                }
                if (table.Rows[i]["ClinkerConsumptionValue"].ToString() == "")
                {
                    table.Rows[i]["ClinkerConsumptionValue"] = "0";
                }
                if (table.Rows[i]["Formula"].ToString() == "")
                {
                    table.Rows[i]["Formula"] = "0";
                }
            }
            for (int i = 0; i < table.Rows.Count; i++)
            {                
                for (int j = i+1; j < table.Rows.Count; j++)
                {
                    if (table.Rows[i]["Name"].ToString().Trim() == table.Rows[j]["Name"].ToString().Trim() && table.Rows[i]["VariableId"].ToString() == table.Rows[j]["VariableId"].ToString())
                    {
                        
                        string m_Production = (Convert.ToDouble(table.Rows[i]["Production"].ToString()) + Convert.ToDouble(table.Rows[j]["Production"].ToString())).ToString();
                        string m_ClinkerConsumptionValue = (Convert.ToDouble(table.Rows[i]["ClinkerConsumptionValue"].ToString()) + Convert.ToDouble(table.Rows[j]["ClinkerConsumptionValue"].ToString())).ToString();
                        string m_Formula = (Convert.ToDouble(table.Rows[i]["Formula"].ToString()) + Convert.ToDouble(table.Rows[j]["Formula"].ToString())).ToString();
                        table.Rows[i]["Production"] = m_Production;
                        table.Rows[i]["ClinkerConsumptionValue"] = m_ClinkerConsumptionValue;
                        table.Rows[i]["Formula"] = m_Formula;
                        table.Rows.RemoveAt(j);
                        j = j - 1;
                    }
                }    
            }
            //增加层次码         
            int mcode = 0;
            for (int i = 0; i < table.Rows.Count; i++)
            {
                string id = table.Rows[i]["NodeType"].ToString();
                if (id == "node")
                {
                    string nodeCode = "M01" + (++mcode).ToString("00");
                    table.Rows[i]["LevelCode"] = nodeCode;
                    int mleafcode = 0;
                    for (int j = 0; j < table.Rows.Count; j++)
                    {
                        if (table.Rows[j]["OrganizationID"].ToString().Trim() == table.Rows[i]["OrganizationID"].ToString().Trim() && table.Rows[j]["NodeType"].ToString().Equals("leafnode"))
                        {
                            table.Rows[j]["LevelCode"] = nodeCode + (++mleafcode).ToString("00");
                        }
                    }
                }
            }
            DataColumn stateColumn = new DataColumn("state", typeof(string));
            table.Columns.Add(stateColumn);
            //此处代码是控制树开与闭的
            //foreach (DataRow dr in table.Rows)
            //{
            //    if (dr["NodeType"].ToString() == "node")
            //    {
            //        dr["state"] = "closed";                           
            //    }
            //    else
            //    {
            //        dr["state"] = "open";
            //    }
            //}
            //计算电耗和产线总计
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i]["Production"].ToString().Trim() != "0.00" && table.Rows[i]["Production"].ToString().Trim() != "")
                {
                    string mProduction = table.Rows[i]["Production"].ToString().Trim();
                    double lastProduction = Convert.ToDouble(mProduction);
                    string mFormula = table.Rows[i]["Formula"].ToString().Trim();
                    string mClinkerConsumptionValue = table.Rows[i]["ClinkerConsumptionValue"].ToString().Trim();
                    if (mFormula == "")
                    {
                        mFormula = "0";
                    }
                    if (mClinkerConsumptionValue == "")
                    {
                        mClinkerConsumptionValue = "0";
                    }
                    double lastFormula = Convert.ToDouble(mFormula);
                    double lastClinkerConsumptionValue = Convert.ToDouble(mClinkerConsumptionValue);                   
                    double mConsumption = Convert.ToDouble((lastFormula / lastProduction).ToString("0.00"));
                    double mClinkerConsumption = Convert.ToDouble((lastClinkerConsumptionValue / lastProduction*100).ToString("0.00"));
                    table.Rows[i]["Consumption"] = mConsumption;
                    table.Rows[i]["ClinkerConsumption"] = mClinkerConsumption;
                }
                if (table.Rows[i]["NodeType"].ToString() == "leafnode" && (table.Rows[i]["Production"].ToString().Trim() == "0.00" || table.Rows[i]["Production"].ToString().Trim() == ""))
                {
                    string mConsumption = "";
                    table.Rows[i]["Consumption"] = mConsumption;
                }   
            }
            for (int i = 0; i < table.Rows.Count; )
            {
                string m_Name = table.Rows[i]["Name"].ToString();
                DataRow[] m_SubRoot = table.Select("Name = '" + m_Name + "'");
                int length = m_SubRoot.Length;
                double sumProduction = 0;
                double sumFormula = 0;
                double sumClinkerConsumptionValue = 0;
                for (int j = 0; j < length; j++)
                {
                    //产量
                    string mmProduction = m_SubRoot[j]["Production"].ToString().Trim();
                    if (mmProduction == "")
                    {
                        mmProduction = "0";
                    }
                    double m_Prodcution = Convert.ToDouble(mmProduction);
                    sumProduction = sumProduction + m_Prodcution;
                    //电量
                    string mmFormula = m_SubRoot[j]["Formula"].ToString().Trim();
                    if (mmFormula == "")
                    {
                        mmFormula = "0";
                    }
                    double m_formula = Convert.ToDouble(mmFormula);
                    sumFormula = sumFormula + m_formula;
                    //熟料消耗量
                    string mmClinkerConsumptionValue = m_SubRoot[j]["ClinkerConsumptionValue"].ToString().Trim();
                    if (mmClinkerConsumptionValue == "")
                    {
                        mmClinkerConsumptionValue = "0";
                    }
                    double m_ClinkerConsumptionValue = Convert.ToDouble(mmClinkerConsumptionValue);
                    sumClinkerConsumptionValue = sumClinkerConsumptionValue + m_ClinkerConsumptionValue;
                }
                table.Rows[i]["Production"] = sumProduction;
                table.Rows[i]["Formula"] = sumFormula;
                table.Rows[i]["ClinkerConsumptionValue"] = sumClinkerConsumptionValue;
                //计算熟料料耗
                if (sumProduction.ToString("0.00") == "0.00" || sumClinkerConsumptionValue.ToString("0.00") == "0.00")
                {
                    table.Rows[i]["ClinkerConsumption"] = Convert.ToString("0.00");
                }
                else
                {
                    table.Rows[i]["ClinkerConsumption"] = Convert.ToDouble((sumClinkerConsumptionValue / sumProduction*100).ToString("0.00"));
                }               
                //计算电耗
                if (sumProduction.ToString("0.00") == "0.00" || sumFormula.ToString("0.00") == "0.00")
                {
                    table.Rows[i]["Consumption"] = Convert.ToString("0.00");
                }
                else
                {
                    table.Rows[i]["Consumption"] = Convert.ToDouble((sumFormula / sumProduction).ToString("0.00"));
                }

                i = i + length;
            }
            return table;
        }
    }
}
