var table;

$(document).ready(function () {
    // Initialize DataTable with Ajax call
    table = $("#reportsTable").DataTable({
        "ajax": {
            "url": "/Reports/GetReportData",
            "type": "GET",
            "datatype": "json",
            "dataSrc": function (json) {
                // Populate metrics cards
                $("#totalProducts").text(json.summary.totalProducts);
                $("#totalAvailableQty").text(json.summary.totalAvailableQty);
                $("#totalPurchasedCost").text("PKR " + parseFloat(json.summary.totalPurchasedCost).toFixed(2));
                $("#totalSalesRevenue").text("PKR " + parseFloat(json.summary.totalSalesRevenue).toFixed(2));
                
                return json.data;
            }
        },
        "columns": [
            { "data": "sku" },
            { "data": "productName" },
            { "data": "variant", "render": function(data) { return data ? data : "N/A"; } },
            { "data": "availableQuantity", "className": "text-center" },
            { "data": "purchasedQuantity", "className": "text-center" },
            { "data": "soldQuantity", "className": "text-center" },
            { 
                "data": "purchasedCost",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toFixed(2);
                }
            },
            { 
                "data": "salesRevenue",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toFixed(2);
                }
            },
            { 
                "data": "netProfit",
                "render": function(data) {
                    var val = parseFloat(data);
                    var badgeClass = val >= 0 ? "bg-success" : "bg-danger";
                    return `<span class="badge ${badgeClass}">PKR ${val.toFixed(2)}</span>`;
                }
            }
        ],
        "order": [[8, "desc"]], // Default sort by Profit desc
        "language": {
            "emptyTable": "No product transactions found in history."
        }
    });
});
