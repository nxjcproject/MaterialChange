var SelectOrganizationName = "";
var SelectDatetime = "";

$(function () {
    InitialDate();
    LoadTreeGrid("first");
});
function InitialDate() {
    var nowDate = new Date();
    var beforeDate = new Date();
    beforeDate.setDate(nowDate.getDate() - 1);
    var nowString = nowDate.getFullYear() + '-' + (nowDate.getMonth() + 1) + '-' + nowDate.getDate() + " " + nowDate.getHours() + ":" + nowDate.getMinutes() + ":" + nowDate.getSeconds();
    var beforeString = beforeDate.getFullYear() + '-' + (beforeDate.getMonth() + 1) + '-' + beforeDate.getDate() + " 00:00:00";
    $('#startTime').datetimebox('setValue', beforeString);
    $('#endTime').datetimebox('setValue', nowString);
}
function onOrganisationTreeClick(node) {
    $('#organizationName').textbox('setText', node.text); 
    $('#organizationId').val(node.OrganizationId);
    mOrganizationId = node.OrganizationId;
    LoadProductionLine(mOrganizationId);
}
function LoadProductionLine(value) {
    $.ajax({
        type: "POST",
        url: "MaterialDetail.aspx/GetMaterialName",
        data: " {mOrganizationId:'" + value + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            var myData = jQuery.parseJSON(msg.d);
            $('#materialType').combobox({
                valueField: 'ID',
                textField: 'Name',
                panelHeight: 'auto',
                data: myData.rows,
                onLoadSuccess: function () { //加载完成后,设置选中第一项
                    var val = $(this).combobox("getData");
                    for (var item in val[0]) {
                        if (item == "ID") {
                            $(this).combobox("select", val[0][item]);
                        }
                    }
                }
            });
        },
        error: function () {
            $.messager.alert('失败', '加载失败！');
        }
    });
}
function LoadTreeGrid(type, myData) {
    if (type == "first") {
        $('#grid_Main').treegrid({
            columns: [[
                  { field: 'VariableId', title: '水泥品种', width: 150 },
                  {
                      field: 'ChangeStartTime', title: '开始时间', width: 120,
                      formatter: function (value, row) {
                          if (row.NodeType=="node") {
                              return "";
                          }
                          else {
                              return row.ChangeStartTime;
                          }
                      }
                  },
                  {
                      field: 'ChangeEndTime', title: '结束时间', width: 120,
                      formatter: function (value, row){
                          if (row.NodeType=="node") {
                              return "";
                          }
                          else {
                              return row.ChangeEndTime;
                          }
                      }
                  },
                  { field: 'RunTime', title: '运行时间', width: 90 },
                  { field: 'Production', title: '产量', width: 70, align: 'right' },
                  { field: 'HourProduction', title: '台时产量', width: 70, align: 'right' },
                  { field: 'ClinkerConsumptionValue', title: '熟料消耗量', width: 80, align: 'right' },
                  { field: 'ClinkerConsumption', title: '熟料料耗(%)', width: 80, align: 'right' },
                  { field: 'Formula', title: '工序电量', width: 80, align: 'right' },
                  { field: 'Consumption', title: '工序电耗', width: 70, align: 'right' }
            ]],
            fit: true,
            toolbar: "#toorBar",
            idField: 'LevelCode',
            treeField: "VariableId",
            rownumbers: true,
            singleSelect: true,
            striped: true,
            data: []
        });
    }
    else {
        $('#grid_Main').treegrid('loadData', myData);
    }
}
function Query() {
    SelectOrganizationName = $('#organizationName').textbox('getText');
    var startDate = $('#startTime').datetimespinner('getValue');//开始时间
    var endDate = $('#endTime').datetimespinner('getValue');//结束时间
    SelectDatetime = startDate + ' 至 ' + endDate;

    var startTime = $('#startTime').datebox('getValue');
    var endTime = $('#endTime').datebox('getValue');
    var materialType = $('#materialType').combobox('getText');
    var win = $.messager.progress({
        title: '请稍后',
        msg: '数据载入中...'
    });
    $.ajax({
        type: "POST",
        url: "MaterialDetail.aspx/GetMaterialProduction",
        data: "{mOrganizationId:'" + mOrganizationId + "',materialType:'" + materialType + "',startTime:'" + startTime + "',endTime:'" + endTime + "'}",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        success: function (msg) {
            $.messager.progress('close');
            var myData = jQuery.parseJSON(msg.d);
            if (myData.length == 0) {
                LoadTreeGrid("last", []);
                $.messager.alert('提示', '没有查询到记录！');
            } else {
                LoadTreeGrid("last", myData);
            }
        },
        beforeSend: function (XMLHttpRequest) {
            //alert('远程调用开始...');
            win;
        },
        error: function () {
            $.messager.progress('close');
            $("#grid_Main").treegrid('loadData', []);
            $.messager.alert('失败', '加载失败！');
        }
    });
}

function ExportFileFun() {
    var m_FunctionName = "ExcelStream";
    var m_Parameter1 = GetTreeTableHtml("grid_Main", "水泥分品种报表", "VariableId", SelectOrganizationName, SelectDatetime);
    var m_Parameter2 = SelectOrganizationName;

    var m_ReplaceAlllt = new RegExp("<", "g");
    var m_ReplaceAllgt = new RegExp(">", "g");
    m_Parameter1 = m_Parameter1.replace(m_ReplaceAlllt, "&lt;");
    m_Parameter1 = m_Parameter1.replace(m_ReplaceAllgt, "&gt;");

    var form = $("<form id = 'ExportFile'>");   //定义一个form表单
    form.attr('style', 'display:none');   //在form表单中添加查询参数
    form.attr('target', '');
    form.attr('method', 'post');
    form.attr('action', "MaterialDetail.aspx");

    var input_Method = $('<input>');
    input_Method.attr('type', 'hidden');
    input_Method.attr('name', 'myFunctionName');
    input_Method.attr('value', m_FunctionName);
    var input_Data1 = $('<input>');
    input_Data1.attr('type', 'hidden');
    input_Data1.attr('name', 'myParameter1');
    input_Data1.attr('value', m_Parameter1);
    var input_Data2 = $('<input>');
    input_Data2.attr('type', 'hidden');
    input_Data2.attr('name', 'myParameter2');
    input_Data2.attr('value', m_Parameter2);

    $('body').append(form);  //将表单放置在web中 
    form.append(input_Method);   //将查询参数控件提交到表单上
    form.append(input_Data1);   //将查询参数控件提交到表单上
    form.append(input_Data2);   //将查询参数控件提交到表单上
    form.submit();
    //释放生成的资源
    form.remove();
}