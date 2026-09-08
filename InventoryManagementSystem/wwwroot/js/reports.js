var productTable, pnlTable, agingTable;

$(document).ready(function () {
    // 1. Initialize Product Performance Table
    productTable = $("#reportsTable").DataTable({
        "ajax": {
            "url": "/Reports/GetReportData",
            "type": "GET",
            "datatype": "json",
            "dataSrc": function (json) {
                $("#totalProducts").text(json.summary.totalProducts);
                $("#totalAvailableQty").text(json.summary.totalAvailableQty);
                $("#totalPurchasedCost").text("PKR " + parseFloat(json.summary.totalPurchasedCost).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#totalSalesRevenue").text("PKR " + parseFloat(json.summary.totalSalesRevenue).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                return json.data;
            }
        },
        "columns": [
            { "data": "sku" },
            { "data": "productName" },
            { "data": "variant", "render": function(data) { return data ? data : "Standard"; } },
            { "data": "availableQuantity", "className": "text-center" },
            { "data": "purchasedQuantity", "className": "text-center" },
            { "data": "soldQuantity", "className": "text-center" },
            { 
                "data": "purchasedCost",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                }
            },
            { 
                "data": "salesRevenue",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                }
            },
            { 
                "data": "netProfit",
                "render": function(data) {
                    var val = parseFloat(data);
                    var badgeClass = val >= 0 ? "bg-success" : "bg-danger";
                    return `<span class="badge ${badgeClass}">PKR ${val.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>`;
                }
            }
        ],
        "order": [[8, "desc"]]
    });

    // 2. Initialize Profit & Loss Table
    pnlTable = $("#pnlTable").DataTable({
        "ajax": {
            "url": "/Reports/GetProfitAndLossData",
            "type": "GET",
            "datatype": "json",
            "data": function (d) {
                d.startDate = $("#pnlStartDate").val();
                d.endDate = $("#pnlEndDate").val();
            },
            "dataSrc": function (json) {
                $("#pnlGrossRevenue").text("PKR " + parseFloat(json.grossRevenue).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#pnlCogs").text("PKR " + parseFloat(json.cogs).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#pnlGrossProfit").text("PKR " + parseFloat(json.grossProfit).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#pnlPayroll").text("PKR " + parseFloat(json.estimatedPayrollExpense).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                
                var netVal = parseFloat(json.netOperatingIncome);
                var netText = "PKR " + netVal.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                $("#pnlNetIncome").text(netText);

                return json.items;
            }
        },
        "columns": [
            { "data": "sku" },
            { "data": "name" },
            { 
                "data": "revenue",
                "className": "text-end",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                }
            },
            { 
                "data": "cost",
                "className": "text-end",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                }
            },
            { 
                "data": "grossMargin",
                "className": "text-end",
                "render": function(data) {
                    var val = parseFloat(data);
                    var textClass = val >= 0 ? "text-success fw-bold" : "text-danger fw-bold";
                    return `<span class="${textClass}">PKR ${val.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>`;
                }
            }
        ],
        "order": [[4, "desc"]]
    });

    // P&L Filter Click
    $("#btnFilterPnl").click(function () {
        $(".pnl-preset").removeClass("active");
        pnlTable.ajax.reload();
    });

    // P&L Presets
    $(".pnl-preset").click(function () {
        $(".pnl-preset").removeClass("active");
        $(this).addClass("active");

        var preset = $(this).data("preset");
        var now = new Date();

        if (preset === "this-month") {
            var firstDay = new Date(now.getFullYear(), now.getMonth(), 1).toISOString().split('T')[0];
            var lastDay = new Date(now.getFullYear(), now.getMonth() + 1, 0).toISOString().split('T')[0];
            $("#pnlStartDate").val(firstDay);
            $("#pnlEndDate").val(lastDay);
        } else if (preset === "last-month") {
            var firstDay = new Date(now.getFullYear(), now.getMonth() - 1, 1).toISOString().split('T')[0];
            var lastDay = new Date(now.getFullYear(), now.getMonth(), 0).toISOString().split('T')[0];
            $("#pnlStartDate").val(firstDay);
            $("#pnlEndDate").val(lastDay);
        } else if (preset === "ytd") {
            var firstDay = new Date(now.getFullYear(), 0, 1).toISOString().split('T')[0];
            var todayStr = now.toISOString().split('T')[0];
            $("#pnlStartDate").val(firstDay);
            $("#pnlEndDate").val(todayStr);
        } else {
            $("#pnlStartDate").val("");
            $("#pnlEndDate").val("");
        }

        pnlTable.ajax.reload();
    });

    // 3. Initialize Installment Aging Table
    agingTable = $("#agingTable").DataTable({
        "ajax": {
            "url": "/Reports/GetInstallmentAgingData",
            "type": "GET",
            "datatype": "json",
            "dataSrc": function (json) {
                $("#agingTotalReceivables").text("PKR " + parseFloat(json.summary.totalReceivables).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#agingDueToday").text("PKR " + parseFloat(json.summary.totalDueToday).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#agingOverdue1To30").text("PKR " + parseFloat(json.summary.totalOverdue1To30).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                $("#agingOverdue30Plus").text("PKR " + parseFloat(json.summary.totalOverdue30Plus).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
                return json.installments;
            }
        },
        "columns": [
            { "data": "invoiceNo", "render": function(data) { return `<strong>${data}</strong>`; } },
            { 
                "data": "customerName",
                "render": function(data, type, row) {
                    return `<div><strong>${data}</strong></div><small class="text-muted">${row.customerEmail || ''}</small>`;
                }
            },
            { "data": "productName" },
            { "data": "installmentNumber", "className": "text-center", "render": function(data) { return `#${data}`; } },
            { 
                "data": "remainingBalance",
                "className": "text-end fw-bold",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                }
            },
            { "data": "dueDate" },
            { 
                "data": "daysOverdue",
                "className": "text-center",
                "render": function(data) {
                    return data > 0 ? `<span class="fw-bold text-danger">${data} Days</span>` : `<span class="text-muted">0 Days</span>`;
                }
            },
            { 
                "data": "agingBucket",
                "render": function(data, type, row) {
                    return `<span class="badge ${row.statusBadge}">${data}</span>`;
                }
            },
            {
                "data": "id",
                "className": "text-center",
                "render": function (data, type, row) {
                    return `<button class="btn btn-sm btn-outline-danger" onclick="sendAgingReminder(${data}, '${row.customerEmail}')">
                                <i class="fas fa-paper-plane me-1"></i>Remind
                            </button>`;
                }
            }
        ],
        "order": [[6, "desc"]]
    });

    // Fix DataTables column layout on tab switch
    $('button[data-bs-toggle="tab"]').on('shown.bs.tab', function (e) {
        $.fn.dataTable.tables({ visible: true, api: true }).columns.adjust();
    });
});

// 1-Click Send Reminder Function
function sendAgingReminder(installmentId, customerEmail) {
    if (!customerEmail) {
        Swal.fire('Notice', 'No customer email address on file for this installment.', 'warning');
        return;
    }

    Swal.fire({
        title: 'Send Overdue Reminder?',
        text: `Send an automated payment reminder to ${customerEmail}?`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes, Send Email',
        confirmButtonColor: '#dc3545'
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({ title: 'Sending...', text: 'Please wait', allowOutsideClick: false, didOpen: () => { Swal.showLoading(); } });

            $.ajax({
                url: '/Sales/SendInstallmentReminder',
                type: 'POST',
                data: { installmentId: installmentId },
                success: function (res) {
                    if (res.success) {
                        Swal.fire('Sent!', res.message, 'success');
                    } else {
                        Swal.fire('Error', res.message, 'error');
                    }
                },
                error: function () {
                    Swal.fire('Error', 'Failed to send reminder email.', 'error');
                }
            });
        }
    });
}
