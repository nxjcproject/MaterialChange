﻿<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MaterialDetail.aspx.cs" Inherits="MaterialChange.Web.UI_MaterialDetail.MaterialDetail" %>
<%@ Register Src="~/UI_WebUserControls/OrganizationSelector/OrganisationTree.ascx" TagPrefix="uc1" TagName="OrganisationTree" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head id="Head1" runat="server">
<meta http-equiv="Content-Type" content="text/html; charset=utf-8"/>
    <title>水泥分品种明细表</title>
    <link rel="stylesheet" type="text/css" href="/lib/ealib/themes/gray/easyui.css"/>
	<link rel="stylesheet" type="text/css" href="/lib/ealib/themes/icon.css"/>
    <link rel="stylesheet" type="text/css" href="/lib/extlib/themes/syExtIcon.css"/>
    <link rel="stylesheet" type="text/css" href="/lib/extlib/themes/syExtCss.css"/>

	<script type="text/javascript" src="/lib/ealib/jquery.min.js" charset="utf-8"></script>
	<script type="text/javascript" src="/lib/ealib/jquery.easyui.min.js" charset="utf-8"></script>
    <script type="text/javascript" src="/lib/ealib/easyui-lang-zh_CN.js" charset="utf-8"></script>

    <script type="text/javascript" src="/lib/ealib/extend/jquery.PrintArea.js" charset="utf-8"></script> 
    <script type="text/javascript" src="/lib/ealib/extend/jquery.jqprint.js" charset="utf-8"></script>
    <!--[if lt IE 8 ]><script type="text/javascript" src="/js/common/json2.min.js"></script><![endif]-->
    <script type="text/javascript" src="/js/common/PrintFile.js" charset="utf-8"></script> 
    <script type="text/javascript" src="js/page/MaterialDetail.js" charset="utf-8"></script>
</head>
<body>        
    <div id="cc" class="easyui-layout"data-options="fit:true,border:false" >   
         <div data-options="region:'west'" style="width: 150px;">
            <uc1:OrganisationTree ID="OrganisationTree_ProductionLine" runat="server" />
        </div>
          <div id="toorBar" title="" style="height:55px;padding:5px;">
            <div>
                <table>
                    <tr>
                        <td style="width: 60px; text-align: right;">组织机构</td>
                        <td >                               
                            <input id="organizationName" class="easyui-textbox" readonly="readonly"style="width:100px" />  
                            <input id="organizationId" readonly="readonly" style="display: none;" />             
                        </td>
                        <td style="width: 60px; text-align: right;">开始时间</td>
                        <td>
                             <input id="startTime" type="text" class="easyui-datetimebox" style="width:150px;" required="required"/>
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 60px; text-align: right;">水泥品种</td>
                        <td>
                            <input id="materialType" type="text" class="easyui-combobox" style="width:100px;" />
                        </td>
                           <td style="width: 60px; text-align: right;">结束时间</td>
                         <td>
                             <input id="endTime" type="text" class="easyui-datetimebox" style="width:150px;" required="required"/>
                        </td>
                        <td style="width: 60px; text-align: right;">
                            <a href="#" class="easyui-linkbutton" data-options="iconCls:'icon-search'" onclick="Query()">查询</a>
                        </td>
                        <td style="width: 80px; text-align: right;">
                            <a href="#" class="easyui-linkbutton" data-options="iconCls:'ext-icon-page_white_excel',plain:true" onclick="ExportFileFun();">导出</a>
                        </td>               
                    </tr>
                </table>         
            </div>
	    </div> 
         <div data-options="region:'center'" style="padding:5px;background:#eee;">
             <table id="grid_Main" class="easyui-treegrid"></table>
         </div>
    </div>
</body>
</html>

