$(document).ready(function () {
    // Bind Customer Purchases table if present
    if ($("#myPurchasesTable").length) {
        $("#myPurchasesTable").DataTable({
            "ajax": {
                "url": "/CustomerDashboard/GetMyPurchasesData",
                "type": "GET",
                "datatype": "json"
            },
            "columns": [
                { "data": "invoiceNo" },
                { "data": "productSku" },
                { "data": "productName" },
                { "data": "quantity" },
                { 
                    "data": "unitPrice",
                    "render": function(data) {
                        return "PKR " + parseFloat(data).toFixed(2);
                    }
                },
                { 
                    "data": "totalAmount",
                    "render": function(data) {
                        return "PKR " + parseFloat(data).toFixed(2);
                    }
                },
                { "data": "saleDate" },
                { "data": "notes" },
                {
                    "data": "id",
                    "render": function (data) {
                        return `
                            <div class="text-center">
                                <button class="btn btn-sm btn-dark" onclick="printInvoice(${data})">
                                    <i class="fas fa-print"></i> Print Invoice
                                </button>
                            </div>
                        `;
                    },
                    "orderable": false,
                    "width": "15%"
                }
            ],
            "order": [[6, "desc"]],
            "language": {
                "emptyTable": "You have no purchase invoices recorded."
            }
        });
    }

    // Bind Customer Catalog table if present
    if ($("#catalogTable").length) {
        $("#catalogTable").DataTable({
            "ajax": {
                "url": "/CustomerDashboard/GetCatalogData",
                "type": "GET",
                "datatype": "json"
            },
            "columns": [
                { "data": "sku" },
                { "data": "name" },
                { "data": "description" },
                { 
                    "data": "price",
                    "render": function(data) {
                        return "PKR " + parseFloat(data).toFixed(2);
                    }
                },
                { "data": "stockQuantity" },
                { 
                    "data": "stockQuantity",
                    "render": function(data) {
                        if (parseInt(data) > 0) {
                            return '<span class="badge bg-success">In Stock</span>';
                        } else {
                            return '<span class="badge bg-danger">Out of Stock</span>';
                        }
                    }
                }
            ],
            "language": {
                "emptyTable": "No catalog products found."
            }
        });
    }
});

function printInvoice(id) {
    var printUrl = "/Sales/Print/" + id;
    window.open(printUrl, "_blank", "width=850,height=600");
}
