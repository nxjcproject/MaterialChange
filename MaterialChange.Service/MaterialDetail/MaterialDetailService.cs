using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using SqlServerDataAdapter;
using MaterialChange.Infrastructure.Configuration;

namespace MaterialChange.Service.MaterialDetail
{
    public class MaterialDetailService
    {
        //加载水泥品种combobox
        public static DataTable GetMaterialNameTable(string mOrganizationId)
        {
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            string sql = @"SELECT [ID],[Name]
                            FROM [NXJC].[dbo].[inventory_Warehouse]
                            where  @mOrganizationId like [OrganizationID] +'%'
                                and [LevelCode] like 'W070' + '%'
		                        and [Enabled]=1
		                        order by [LevelCode]";
            SqlParameter parameter = new SqlParameter("mOrganizationID", mOrganizationId);
            DataTable table = dataFactory.Query(sql, parameter);
            DataRow newrow;
            newrow = table.NewRow();
            newrow["ID"] = "All";
            newrow["Name"] = "全部";
            table.Rows.InsertAt(newrow, 0);
            return table;
        }
        //得到符合条件的设备开停机记录
        public static DataTable GetMachineRunTime(string mOrganizationId, string startTime, string endTime)
        {
            //计算StopTime时的算法因为后续程序的改动没有用到
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            string Sql = @"SELECT [MachineHaltLogID]
                                  ,[EquipmentName]
                                  ,[StartTime]
                                  ,[HaltTime]
                                  ,[RecoverTime]
                                  ,convert(varchar,DATEDIFF(MINUTE,(case when [HaltTime] < @startTime then @startTime
									                                     when [HaltTime] >= @startTime then [HaltTime] end ) ,case when [RecoverTime] < @endTime then [RecoverTime] 
                                                                         when [RecoverTime] >= @endTime then @endTime 
                                                                         when [RecoverTime] is NULL then @endTime end)/60/24)+'天'+
                                    convert(varchar,DATEDIFF(Minute,(case when [HaltTime] < @startTime then @startTime
									                                      when [HaltTime] >= @startTime then [HaltTime] end ),case when [RecoverTime] < @endTime then [RecoverTime] 
                                                                          when [RecoverTime] >= @endTime then @endTime 
                                                                          when [RecoverTime] is NULL then @endTime end)/60-DATEDIFF(MINUTE,(case when [HaltTime] < @startTime then @startTime
									                                      when [HaltTime] >= @startTime then [HaltTime] end ),case when [RecoverTime] < @endTime then [RecoverTime] 
                                                                          when [RecoverTime] >= @endTime then @endTime 
                                                                          when [RecoverTime] is NULL then @endTime end)/60/24*24)+'时'+
                                    convert(varchar,DATEDIFF(minute,(case when [HaltTime] < @startTime then @startTime
									                                      when [HaltTime] >= @startTime then [HaltTime] end ),case when [RecoverTime] < @endTime then [RecoverTime] 
                                                                          when [RecoverTime] >= @endTime then @endTime 
                                                                          when [RecoverTime] is NULL then @endTime end)-DATEDIFF(minute,(case when [HaltTime] < @startTime then @startTime
									                                      when [HaltTime] >= @startTime then [HaltTime] end ),case when [RecoverTime] < @endTime then [RecoverTime] 
                                                                          when [RecoverTime] >= @endTime then @endTime 
                                                                          when [RecoverTime] is NULL then @endTime end)/60/24*24*60-(DATEDIFF(Minute,(case when [HaltTime] < @startTime then @startTime
									                                      when [HaltTime] >= @startTime then [HaltTime] end ),case when [RecoverTime] < @endTime then [RecoverTime] 
                                                                          when [RecoverTime] >= @endTime then @endTime 
                                                                          when [RecoverTime] is NULL then @endTime end)/60-DATEDIFF(MINUTE,(case when [HaltTime] < @startTime then @startTime
									                                      when [HaltTime] >= @startTime then [HaltTime] end ),case when [RecoverTime] < @endTime then [RecoverTime] 
                                                                          when [RecoverTime] >= @endTime then @endTime 
                                                                          when [RecoverTime] is NULL then @endTime end)/60/24*24)*60)+'分' as [StopTime]
                              FROM [NXJC].[dbo].[shift_MachineHaltLog]
                              where [OrganizationID]=@mOrganizationId
                              and (([StartTime]<=@startTime and [HaltTime]>=@startTime)or 
                              ([StartTime]<=@startTime and [HaltTime] is NUll) or 
                              ([StartTime]>=@startTime and [HaltTime]<=@endTime)or
                              ([StartTime]>=@startTime and [HaltTime]>=@endTime)or
                              ([StartTime]<=@startTime and [HaltTime]>=@endTime)or
                              ([StartTime]>=@startTime and [HaltTime] is NULL))
                              order by [StartTime] asc";
            SqlParameter[] parameter ={
                                          new SqlParameter("mOrganizationId", mOrganizationId),
                                          new SqlParameter("startTime", startTime),
                                          new SqlParameter("endTime", endTime)
                                       };
            DataTable table = dataFactory.Query(Sql, parameter);
            return table;
        }
        //综合品种变化表和设备开停表
        public static DataTable CompositeTable(DataTable machineTable, DataTable table)
        {
            //若水泥品种变化出现在停机时间内，则去除
            for (int i = 0; i < machineTable.Rows.Count; i++)
            {
                for (int j = 0; j < table.Rows.Count; j++)
                {
                    if (machineTable.Rows[i]["HaltTime"].ToString() != "" && machineTable.Rows[i]["RecoverTime"].ToString() != "")
                    {
                        if (DateTime.Compare(Convert.ToDateTime(table.Rows[j]["ChangeStartTime"].ToString()), Convert.ToDateTime(machineTable.Rows[i]["HaltTime"].ToString())) == 1
                                && DateTime.Compare(Convert.ToDateTime(table.Rows[j]["ChangeEndTime"].ToString()), Convert.ToDateTime(machineTable.Rows[i]["RecoverTime"].ToString())) == -1)
                        {
                            table.Rows.RemoveAt(j);
                            j--;
                        }
                    }
                }
            }
            //若某一水泥品种时间段内，出现了开停机，则将停机时间去掉
            for (int i = 0; i < table.Rows.Count; i++)
            {
                DataRow newRow = table.NewRow();
                for (int j = 0; j < machineTable.Rows.Count; j++ )
                {
                    if (machineTable.Rows[j]["HaltTime"].ToString() != "" && machineTable.Rows[j]["RecoverTime"].ToString() != "")
                    {
                        if (DateTime.Compare(Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString()), Convert.ToDateTime(machineTable.Rows[j]["HaltTime"].ToString())) == -1
                            && DateTime.Compare(Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString()), Convert.ToDateTime(machineTable.Rows[j]["RecoverTime"].ToString())) == 1)
                        {                           
                            //DataRow newRow = table.NewRow();
                            newRow["OrganizationID"] = table.Rows[i]["OrganizationID"].ToString();
                            newRow["Name"] = table.Rows[i]["Name"].ToString();
                            newRow["MaterialColumn"] = table.Rows[i]["MaterialColumn"].ToString();
                            newRow["VariableId"] = table.Rows[i]["VariableId"].ToString();
                            newRow["ChangeStartTime"] = machineTable.Rows[j]["RecoverTime"].ToString();
                            newRow["ChangeEndTime"] = table.Rows[i]["ChangeEndTime"].ToString();
                            newRow["RunTime"] = table.Rows[i]["RunTime"].ToString();
                            newRow["MaterialDataBaseName"] = table.Rows[i]["MaterialDataBaseName"].ToString();
                            newRow["MaterialDataTableName"] = table.Rows[i]["MaterialDataTableName"].ToString();
                            newRow["LevelCode"] = "";
                            newRow["NodeType"] = "leafnode";
                            table.Rows.InsertAt(newRow, i+1);//增加该行
                            //machineTable.Rows.RemoveAt(j);
                            table.Rows[i]["ChangeEndTime"] = machineTable.Rows[j]["HaltTime"].ToString();
                            j = machineTable.Rows.Count; // 此处的作用是只让里面的循环循环一次则跳出
                            //i++;
                        }
                    }
                }
            }
            //for (int i = 0; i < table.Rows.Count; i++)
            //{
            //    for (int j = 0; j < machineTable.Rows.Count; j++)
            //    {
            //        if (DateTime.Compare(Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString()), Convert.ToDateTime(machineTable.Rows[j]["StartTime"].ToString())) == -1
            //            && DateTime.Compare(Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString()), Convert.ToDateTime(machineTable.Rows[j]["HaltTime"].ToString())) == 1)
            //        {
            //            table.Rows[i]["ChangeEndTime"]=
            //        }
            //    }
            //}
            return table;
        }
        //#region
        //此处注释的程序是第一个版本
        //此处将只是将运行时间中去除停机时间，后面的电量台式产量算法中没有去除
        //此处主要运用了split，可好好研究研究
        //public static DataTable MachineProduction(DataTable machineTable, DataTable table)
        //{
        //    for (int i = 0; i < table.Rows.Count; i++)
        //    {
        //        if (table.Rows[i]["NodeType"].ToString() == "leafnode")
        //        {
        //            string[] dayTime = Regex.Split(table.Rows[i]["RunTime"].ToString(), "天", RegexOptions.IgnoreCase);
        //            string day = dayTime[0];
        //            string[] hourTime = Regex.Split(table.Rows[i]["RunTime"].ToString(), "时", RegexOptions.IgnoreCase);
        //            string[] hourArr = Regex.Split(hourTime[0], "天", RegexOptions.IgnoreCase);
        //            string hour = hourArr[1];
        //            string[] minArr = Regex.Split(hourTime[1], "分", RegexOptions.IgnoreCase);
        //            string min = minArr[0];
        //            table.Rows[i]["RunTime"] = (Convert.ToDecimal(day) * 24) + Convert.ToDecimal(hour) + Convert.ToDecimal((Convert.ToDecimal(min) / 60).ToString("0.00"));
        //        }
        //    }
        //    for (int i = 0; i < machineTable.Rows.Count; i++)
        //    {
        //        if (machineTable.Rows[i]["StopTime"].ToString() != "")
        //        {
        //            string[] dayTime = Regex.Split(machineTable.Rows[i]["StopTime"].ToString(), "天", RegexOptions.IgnoreCase);
        //            string day = dayTime[0];
        //            string[] hourTime = Regex.Split(machineTable.Rows[i]["StopTime"].ToString(), "时", RegexOptions.IgnoreCase);
        //            string[] hourArr = Regex.Split(hourTime[0], "天", RegexOptions.IgnoreCase);
        //            string hour = hourArr[1];
        //            string[] minArr = Regex.Split(hourTime[1], "分", RegexOptions.IgnoreCase);
        //            string min = minArr[0];
        //            machineTable.Rows[i]["StopTime"] = (Convert.ToDecimal(day) * 24) + Convert.ToDecimal(hour) + Convert.ToDecimal((Convert.ToDecimal(min) / 60).ToString("0.00"));
        //        }
        //    }
        //    for (int i = 0; i < table.Rows.Count; i++)
        //    {
        //        if (table.Rows[i]["NodeType"].ToString() == "leafnode")
        //        {
        //            for (int j = 0; j < machineTable.Rows.Count; j++)
        //            {
        //                if (machineTable.Rows[j]["HaltTime"].ToString() != "" && machineTable.Rows[j]["RecoverTime"].ToString() != "")
        //                {
        //                    if (DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["HaltTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString())) == 1
        //                        && DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["RecoverTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString())) == -1)
        //                    {
        //                        table.Rows[i]["RunTime"] = (Convert.ToDecimal(table.Rows[i]["RunTime"].ToString()) - Convert.ToDecimal(machineTable.Rows[j]["StopTime"].ToString())).ToString();
        //                    }
        //                    if (DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["HaltTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString())) == -1
        //                        && DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["RecoverTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString())) == -1
        //                        && DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["RecoverTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString())) == 1)
        //                    {
        //                        double hours = (Convert.ToDateTime(machineTable.Rows[j]["RecoverTime"].ToString()) - Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString())).TotalHours;
        //                        table.Rows[i]["RunTime"] = (Convert.ToDecimal(table.Rows[i]["RunTime"].ToString()) - Convert.ToDecimal(hours)).ToString();
        //                    }
        //                    if (DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["RecoverTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString())) == 1
        //                        && DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["HaltTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString())) == -1
        //                        && DateTime.Compare(Convert.ToDateTime(machineTable.Rows[j]["HaltTime"].ToString()), Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString())) == 1)
        //                    {
        //                        double hours = (Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString()) - Convert.ToDateTime(machineTable.Rows[j]["HaltTime"].ToString())).TotalHours;
        //                        table.Rows[i]["RunTime"] = (Convert.ToDecimal(table.Rows[i]["RunTime"].ToString()) - Convert.ToDecimal(hours)).ToString();
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    return table;
        //}
        public static DataTable GetMaterialProductionTable(string mOrganizationId, string materialType, string startTime, string endTime)
        {
            DataTable machineTable = GetMachineRunTime(mOrganizationId, startTime, endTime);     
            string connectionString = ConnectionStringFactory.NXJCConnectionString;
            ISqlServerDataFactory dataFactory = new SqlServerDataFactory(connectionString);
            DataTable table = new DataTable();
            //选择全部时查出的满足条件的水泥品种改变
            //此处在计算运行时间时，因为后续程序的改动，没有用到
            if (materialType=="全部")
            {
                string allSql = @"(SELECT 
                                     A.[OrganizationID]
                                    ,C.[Name]
		                            ,B.[MaterialColumn]
									,A.[VariableId]
		                            ,A.[ChangeStartTime]
		                            ,A.[ChangeEndTime]
									,convert(varchar,DATEDIFF(MINUTE,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ) ,case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24)+'天'+
                                    convert(varchar,DATEDIFF(Minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60-DATEDIFF(MINUTE,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24*24)+'时'+
                                    convert(varchar,DATEDIFF(minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)-DATEDIFF(minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24*24*60-(DATEDIFF(Minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60-DATEDIFF(MINUTE,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24*24)*60)+'分' as RunTime
		                            ,B.[MaterialDataBaseName]
		                            ,B.[MaterialDataTableName]
                                    ,'' as [LevelCode]
		                            ,'leafnode' as [NodeType]
	                            FROM [NXJC].[dbo].[material_MaterialChangeLog] A,[NXJC].[dbo].[material_MaterialChangeContrast] B,[NXJC].[dbo].[system_Organization] C
	                            where A.OrganizationID=@mOrganizationId 
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
	                            (SELECT  A.[OrganizationID],C.[Name],'' as [MaterialColumn],A.[VariableId],'' as [ChangeStartTime],'' as [ChangeEndTime],'' as RunTime
	                            ,'' as [MaterialDataBaseName],'' as [MaterialDataTableName],'' as [LevelCode], 'node' as [NodeType]
	                             from [NXJC].[dbo].[material_MaterialChangeLog] A,[NXJC].[dbo].[material_MaterialChangeContrast] B,[NXJC].[dbo].[system_Organization] C
	                            where A.OrganizationID=@mOrganizationId
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
		                            group by A.[OrganizationID],C.[Name],A.[VariableId])
		                            order by A.[VariableId], A.[ChangeStartTime]";
                SqlParameter[] Allparameter ={
                                          new SqlParameter("mOrganizationId", mOrganizationId),
                                          new SqlParameter("startTime", startTime),
                                          new SqlParameter("endTime", endTime)
                                       };
                table = dataFactory.Query(allSql, Allparameter);
            }
            else
            {
                string sql = @"(SELECT 
                                     A.[OrganizationID]
                                    ,C.[Name]
		                            ,B.[MaterialColumn]
									,A.[VariableId]
		                            ,A.[ChangeStartTime]
		                            ,A.[ChangeEndTime]
									,convert(varchar,DATEDIFF(MINUTE,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ) ,case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24)+'天'+
                                    convert(varchar,DATEDIFF(Minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60-DATEDIFF(MINUTE,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24*24)+'时'+
                                    convert(varchar,DATEDIFF(minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)-DATEDIFF(minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24*24*60-(DATEDIFF(Minute,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60-DATEDIFF(MINUTE,(case when A.[ChangeStartTime] < @startTime then @startTime
									                                       when A.[ChangeStartTime] >= @startTime then [ChangeStartTime] end ),case when A.[ChangeEndTime] < @endTime then A.[ChangeEndTime] 
                                                                            when A.[ChangeEndTime]>= @endTime then @endTime 
                                                                            when A.[ChangeEndTime] is NULL then @endTime end)/60/24*24)*60)+'分' as RunTime
		                            ,B.[MaterialDataBaseName]
		                            ,B.[MaterialDataTableName]
                                    ,'' as [LevelCode]
		                            ,'leafnode' as [NodeType]
	                            FROM [NXJC].[dbo].[material_MaterialChangeLog] A,[NXJC].[dbo].[material_MaterialChangeContrast] B,[NXJC].[dbo].[system_Organization] C
	                            where A.OrganizationID=@mOrganizationId 
                                    and A.[VariableId]=@materialType
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
	                            (SELECT  A.[OrganizationID],C.[Name],'' as [MaterialColumn],A.[VariableId],'' as [ChangeStartTime],'' as [ChangeEndTime],'' as RunTime
	                            ,'' as [MaterialDataBaseName],'' as [MaterialDataTableName],'' as [LevelCode], 'node' as [NodeType]
	                             from [NXJC].[dbo].[material_MaterialChangeLog] A,[NXJC].[dbo].[material_MaterialChangeContrast] B,[NXJC].[dbo].[system_Organization] C
	                            where A.OrganizationID=@mOrganizationId
                                    and A.[VariableId]=@materialType
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
		                            group by A.[OrganizationID],C.[Name],A.[VariableId])
		                            order by A.[VariableId], A.[ChangeStartTime]";
                SqlParameter[] parameter ={
                                              new SqlParameter("mOrganizationId", mOrganizationId),
                                              new SqlParameter("materialType", materialType),
                                              new SqlParameter("startTime", startTime),
                                              new SqlParameter("endTime", endTime)
                                           };
                table = dataFactory.Query(sql, parameter);
            }     
            int count = table.Rows.Count;
            //时间的判断，若水泥品种改变的时间与查询时间相比，如果查询开始时间大于品种改变开始时间，则把查询时间赋予开始时间，类似的结束时间
            //此处有结束时间为空的情况，是因为到现在为止是该品种水泥没再改变，则把查询结束时间赋予品种结束时间
            for (int j = 0; j < count; j++)
            {
                if (table.Rows[j]["ChangeEndTime"].ToString() == "")
                {
                    table.Rows[j]["ChangeEndTime"] = endTime;
                }
                DateTime m_startTime = Convert.ToDateTime(table.Rows[j]["ChangeStartTime"].ToString().Trim());
                DateTime m_endTime = Convert.ToDateTime(table.Rows[j]["ChangeEndTime"].ToString().Trim());
                if (DateTime.Compare(m_startTime, Convert.ToDateTime(startTime)) == -1)
                {
                    table.Rows[j]["ChangeStartTime"] = startTime;
                }
                if (DateTime.Compare(m_endTime, Convert.ToDateTime(endTime)) == 1)
                {
                    table.Rows[j]["ChangeEndTime"] = endTime;
                }
            }
            table = CompositeTable(machineTable, table);
            table.Columns.Add("Production");
            table.Columns.Add("HourProduction");
            table.Columns.Add("Formula");
            table.Columns.Add("Consumption");
            table.Columns.Add("ClinkerConsumptionValue");//熟料消耗量
            table.Columns.Add("ClinkerConsumption");//熟料料耗
            //计算分品种电量、产量
            for (int i = 0; i < table.Rows.Count; i++)
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
                    string mSql = @"select cast(sum([FormulaValue]) as decimal(18,1)) as [Formula] from {0}.[dbo].[HistoryFormulaValue]
                                where vDate>=@changeStartTime
                                        and vDate<=@changeEndTime
                                        and variableId = 'cementPreparation'
	                                    and [OrganizationID]=@m_productionLine";
                    SqlParameter[] para ={
                                        new SqlParameter("m_productionLine", m_productionLine),
                                        new SqlParameter("changeStartTime", changeStartTime),
                                        new SqlParameter("changeEndTime", changeEndTime)
                                     };
                    DataTable passTable = dataFactory.Query(string.Format(mSql, materialDataBaseName), para);
                    string mFormula = passTable.Rows[0]["Formula"].ToString().Trim();
                    //计算产量
                    string mSsql = @"select cast(sum({0}) as decimal(18,1)) as [MaterialProduction] from {1}.[dbo].{2}
                                where vDate>=@changeStartTime
                                      and vDate<=@changeEndTime";
                    SqlParameter[] paras ={ new SqlParameter("changeStartTime", changeStartTime),
                                            new SqlParameter("changeEndTime", changeEndTime)};
                    DataTable resultTable = dataFactory.Query(string.Format(mSsql, materialColumn, materialDataBaseName, materialDataTableName), paras);
                    string mProduction = resultTable.Rows[0]["MaterialProduction"].ToString().Trim();
                    //计算熟料消耗量 闫潇华添加
                    string mClinkerConsumptionFormulaSql = @"SELECT A.VariableId
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
                    string mClinkerConsumptionValueSql = @"select cast(sum({0}) as decimal(18,1)) as ClinkerConsumptionValue
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
            //增加LeveCode层次码
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
                        if (table.Rows[j]["VariableId"].ToString().Trim() == table.Rows[i]["VariableId"].ToString().Trim() && table.Rows[j]["NodeType"].ToString().Equals("leafnode"))
                        {
                            table.Rows[j]["LevelCode"] = nodeCode + (++mleafcode).ToString("00");
                        }
                    }
                }
            }

            //计算电耗
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
                    double mConsumption = Convert.ToDouble((lastFormula / lastProduction).ToString("0.00"));
                    double lastClinkerConsumptionValue = Convert.ToDouble(mClinkerConsumptionValue);
                    double mClinkerConsumption = Convert.ToDouble((lastClinkerConsumptionValue / lastProduction * 100).ToString("0.00"));
                    table.Rows[i]["Consumption"] = mConsumption;
                    table.Rows[i]["ClinkerConsumption"] = mClinkerConsumption;
                }
                if (table.Rows[i]["NodeType"].ToString() == "leafnode" && (table.Rows[i]["Production"].ToString().Trim() == "0.00" || table.Rows[i]["Production"].ToString().Trim() == ""))
                {
                    string mConsumption = "";
                    string mClinkerConsumption = "";
                    table.Rows[i]["Consumption"] = mConsumption;
                    table.Rows[i]["ClinkerConsumption"] = mClinkerConsumption;
                }           
            }
            //得到主节点以品种的总计
            for (int i = 0; i < table.Rows.Count; )
            {
                string m_Name = table.Rows[i]["VariableId"].ToString();
                DataRow[] m_SubRoot = table.Select("VariableId = '" + m_Name + "'");
                int length = m_SubRoot.Length;
                double sumProduction = 0;
                double sumFormula = 0;
                double sumClinkerConsumptionValue = 0;
                for (int j = 0; j < length; j++)
                {
                    //计算产量
                    string mmProduction = m_SubRoot[j]["Production"].ToString().Trim();
                    if (mmProduction == "")
                    {
                        mmProduction = "0";
                    }
                    double m_Prodcution = Convert.ToDouble(mmProduction);
                    sumProduction = sumProduction + m_Prodcution;
                    //计算电量
                    string mmFormula = m_SubRoot[j]["Formula"].ToString().Trim();
                    if (mmFormula == "")
                    {
                        mmFormula = "0";
                    }
                    double m_formula = Convert.ToDouble(mmFormula);
                    sumFormula = sumFormula + m_formula;
                    //熟料消耗量 闫潇华添加
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
                //计算电耗
                if (sumProduction.ToString("0.00").Trim() == "0.00" || sumProduction.ToString("0.00").Trim() == "0.00")
                {
                    table.Rows[i]["Consumption"] = "0.00";
                }
                else 
                {
                    table.Rows[i]["Consumption"] = Convert.ToDouble((sumFormula / sumProduction).ToString("0.00"));
                }
                //计算熟料料耗
                if (sumProduction.ToString("0.00") == "0.00" || sumClinkerConsumptionValue.ToString("0.00") == "0.00")
                {
                    table.Rows[i]["ClinkerConsumption"] = Convert.ToString("0.00");
                }
                else
                {
                    table.Rows[i]["ClinkerConsumption"] = Convert.ToDouble((sumClinkerConsumptionValue / sumProduction * 100).ToString("0.00"));
                }               
                i = i + length;
            }
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i]["Consumption"].ToString() == "非数字" || table.Rows[i]["Consumption"].ToString() == "")
                {
                    table.Rows[i]["Consumption"] = "0";
                }
                if (table.Rows[i]["Production"].ToString() == "")
                {
                    table.Rows[i]["Production"] = 0;
                }
                if (table.Rows[i]["Formula"].ToString() == "")
                {
                    table.Rows[i]["Formula"] = 0;
                }
            }
            DataTable connectTable = PerMachineProduction(table);
            DataTable resultTbal = ChangeRunTime(connectTable);
            return connectTable;
        }
        //计算台式产量
        public static DataTable PerMachineProduction(DataTable table)
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i]["NodeType"].ToString() == "leafnode")
                {
                    TimeSpan ts = Convert.ToDateTime(table.Rows[i]["ChangeEndTime"].ToString()) - Convert.ToDateTime(table.Rows[i]["ChangeStartTime"].ToString());
                    table.Rows[i]["RunTime"] = ts.TotalHours;
                }              
                if (table.Rows[i]["RunTime"].ToString() != "")
                {
                    table.Rows[i]["HourProduction"] = (Convert.ToDouble(table.Rows[i]["Production"].ToString()) / Convert.ToDouble(table.Rows[i]["RunTime"].ToString())).ToString("0.00");
                }
            }
            for (int i = 0; i < table.Rows.Count; )
            {
                string m_Name = table.Rows[i]["VariableId"].ToString();
                DataRow[] m_SubRoot = table.Select("VariableId = '" + m_Name + "'");
                int length = m_SubRoot.Length;
                double sumProduction = Convert.ToDouble(table.Rows[i]["Production"].ToString());
                double sumRunTime = 0;
                for (int j = 0; j < length; j++)
                {
                    string mmRunTime = m_SubRoot[j]["RunTime"].ToString().Trim();
                    if (mmRunTime == "")
                    {
                        mmRunTime = "0";
                    }
                    double m_RunTime = Convert.ToDouble(mmRunTime);
                    sumRunTime = sumRunTime + m_RunTime;
                }
                table.Rows[i]["RunTime"] = sumRunTime;
                table.Rows[i]["HourProduction"] = Convert.ToDouble((sumProduction / sumRunTime).ToString("0.00"));
                i = i + length;
            }
            for (int i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i]["HourProduction"].ToString()=="非数字" || table.Rows[i]["HourProduction"].ToString()==""
                    || table.Rows[i]["HourProduction"].ToString() == "0.00")
                {
                    table.Rows[i]["HourProduction"] = 0;
                }
            }
            return table;
        }
        //将小时转换为 天+时+分 的形式
        public static DataTable ChangeRunTime(DataTable table)
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                double runTime = Convert.ToDouble(table.Rows[i]["RunTime"].ToString());
                int second = Convert.ToInt32(runTime * 60 * 60);
                TimeSpan ts = new TimeSpan(0, 0, second);
                string m_RunTime = ts.Days + "天" + ts.Hours + "时" + ts.Minutes + "分"; // 此方法要比用sql语句计算简单
                table.Rows[i]["RunTime"] = m_RunTime;
            }
            return table;
        }
    }
}
