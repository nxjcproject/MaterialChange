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
                onSelect: function (record) {
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