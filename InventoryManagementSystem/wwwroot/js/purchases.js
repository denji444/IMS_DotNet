var table;
var isModalLoading = false;

$(document).ready(function () {
    // Initialize DataTable
    table = $("#purchasesTable").DataTable({
        "ajax": {
            "url": "/Purchases/GetPurchasesData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "purchaseNo" },
            { "data": "productName" },
            { "data": "supplierName" },
            { "data": "quantity" },
            { 
                "data": "unitPrice",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toFixed(2);
                }
            },
            { 
                "data": "totalCost",
                "render": function(data) {
                    return "PKR " + parseFloat(data).toFixed(2);
                }
            },
            { "data": "purchaseDate" },
            { "data": "notes" },
            {
                "data": "id",
                "render": function (data, type, row) {
                    var leaseBtn = "";
                    if (row.paymentMode === 1) {
                        leaseBtn = `
                            <button class="btn btn-sm btn-warning text-dark me-1" onclick="openLeaseModal(${data}, '${row.purchaseNo}', ${row.totalCost}, ${row.downPayment || 0})">
                                <i class="fas fa-calendar-alt"></i> Lease
                            </button>
                        `;
                    }
                    return `
                        <div class="text-center text-nowrap">
                            ${leaseBtn}
                            <button class="btn btn-sm btn-dark me-1" onclick="openEditModal(${data})">
                                <i class="fas fa-edit"></i> Edit
                            </button>
                            <button class="btn btn-sm btn-info text-white me-1" onclick="printVoucher(${data})">
                                <i class="fas fa-print"></i> Print
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deletePurchase(${data})">
                                <i class="fas fa-trash"></i> Delete
                            </button>
                        </div>
                    `;
                },
                "orderable": false,
                "width": "25%"
            }
        ],
        "order": [[6, "desc"]], // Sort by date desc
        "language": {
            "emptyTable": "No purchase history found. Click 'New Purchase' to stock in."
        }
    });

    // Initialize Supplier Select2
    $("#supplierSelect").select2({
        dropdownParent: $("#purchaseModal"),
        placeholder: "Search & Select Supplier",
        allowClear: true,
        ajax: {
            url: "/Suppliers/GetSuppliersJson",
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return { q: params.term };
            },
            processResults: function (data) {
                return { results: data };
            },
            cache: true
        }
    });

    // Toggle for New Supplier inline fields
    $("#newSupplierToggle").on("change", function () {
        if (this.checked) {
            $("#supplierSelectContainer").addClass("d-none");
            $("#supplierSelect").val(null).trigger("change");
            $("#supplierSelect").removeAttr("required");
            $("#newSupplierFields").removeClass("d-none");
        } else {
            $("#supplierSelectContainer").removeClass("d-none");
            $("#supplierSelect").attr("required", "required");
            $("#newSupplierFields").addClass("d-none");
            clearNewSupplierFields();
        }
    });


    // Initialize Product Select2
    $("#productSelect").select2({
        dropdownParent: $("#purchaseModal"),
        placeholder: "Search & Select Product SKU",
        allowClear: true,
        ajax: {
            url: "/Products/GetProductsJson",
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return { 
                    q: params.term
                };
            },
            processResults: function (data) {
                return { results: data };
            },
            cache: true
        }
    });

    // Listen to product selection to automatically populate unit price
    $("#productSelect").on("select2:select", function (e) {
        var data = e.params.data;
        if (data && data.price) {
            $("#unitPrice").val(parseFloat(data.price).toFixed(2));
            calculateTotalCost();
        }
    });

    // Update total cost automatically when Quantity changes
    $("#purchaseQty").on("input", function () {
        calculateTotalCost();
    });

    $("#paymentMode").on("change", function() {
        if ($(this).val() == "1") {
            $("#leaseFields").removeClass("d-none");
            calculateLeaseEstimate();
        } else {
            $("#leaseFields").addClass("d-none");
            $("#downPayment").val("0.00");
            $("#installmentsCount").val("3");
            $("#leaseEstimate").text("");
        }
    });

    $("#downPayment, #installmentsCount, #totalCost").on("input change", function() {
        if ($("#paymentMode").val() == "1") {
            calculateLeaseEstimate();
        }
    });

    // Handle form submit
    $("#purchaseForm").on("submit", function (e) {
        e.preventDefault();
        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        var isNewSupplier = $("#newSupplierToggle").is(":checked");
        var supplierId = isNewSupplier ? 0 : parseInt($("#supplierSelect").val());
        var newSupplier = null;

        if (isNewSupplier) {
            // Frontend validation for new supplier fields
            $("#newSupplierFields .text-danger").text("");
            var hasErrors = false;

            var name = $("#newSupplierName").val().trim();
            if (!name) {
                $("#newSupplierNameError").text("Supplier Name is required.");
                hasErrors = true;
            }

            var email = $("#newSupplierEmail").val().trim();
            if (!email) {
                $("#newSupplierEmailError").text("Email is required.");
                hasErrors = true;
            } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
                $("#newSupplierEmailError").text("Invalid email address format.");
                hasErrors = true;
            }

            var phone = $("#newSupplierPhone").val().trim();
            if (!phone) {
                $("#newSupplierPhoneError").text("Phone is required.");
                hasErrors = true;
            }

            var address = $("#newSupplierAddress").val().trim();
            if (!address) {
                $("#newSupplierAddressError").text("Address is required.");
                hasErrors = true;
            }

            if (hasErrors) {
                return false;
            }

            newSupplier = {
                Name: name,
                Email: email,
                Phone: phone,
                Address: address
            };
        }

        var id = parseInt($("#purchaseId").val());
        var url = id === 0 ? "/Purchases/Create" : "/Purchases/Edit/" + id;

        var purchaseData = {
            Id: id,
            SupplierId: supplierId,
            NewSupplier: newSupplier,
            ProductId: parseInt($("#productSelect").val()),
            Quantity: parseInt($("#purchaseQty").val()),
            UnitPrice: parseFloat($("#unitPrice").val()),
            TotalCost: parseFloat($("#totalCost").val()),
            Notes: $("#notes").val(),
            PaymentMode: parseInt($("#paymentMode").val()),
            DownPayment: parseFloat($("#downPayment").val()) || 0,
            InstallmentsCount: parseInt($("#installmentsCount").val()) || 0,
            InstallmentFrequency: $("#installmentFrequency").val()
        };

        var btn = $("#btnSavePurchase");
        var originalText = btn.html();
        btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

        $.ajax({
            url: url,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(purchaseData),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false).html(originalText);
                if (response.success) {
                    $("#purchaseModal").modal("hide");
                    table.ajax.reload();
                    Swal.fire({
                        title: 'Success!',
                        text: response.message,
                        icon: 'success',
                        confirmButtonColor: '#3085d6'
                    });
                } else {
                    Swal.fire({
                        title: 'Error!',
                        text: response.message,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
                }
            },
            error: function (xhr) {
                btn.prop("disabled", false).html(originalText);
                var msg = "An unexpected error occurred.";
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    msg = xhr.responseJSON.message;
                }
                Swal.fire({
                    title: 'Error!',
                    text: msg,
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
            }
        });
    });
});

function calculateTotalCost() {
    var qty = parseInt($("#purchaseQty").val()) || 0;
    var price = parseFloat($("#unitPrice").val()) || 0.0;
    var total = qty * price;
    $("#totalCost").val(total.toFixed(2)).trigger('change');
}

function openCreateModal() {
    isModalLoading = true;
    $("#purchaseForm")[0].reset();
    $("#purchaseId").val(0);
    $("#supplierSelect").val(null).trigger('change');
    $("#supplierSelect").attr("required", "required");
    $("#supplierSelectContainer").removeClass("d-none");
    $("#newSupplierToggleContainer").show();
    $("#newSupplierToggle").prop("checked", false);
    $("#newSupplierFields").addClass("d-none");
    clearNewSupplierFields();
    $("#productSelect").val(null).trigger('change');
    $(".text-danger").text("");
    $("#paymentMode").val("0").trigger('change').prop('disabled', false);
    $("#purchaseModalLabel").text("New Purchase / Stock In");
    $("#purchaseModal").modal("show");
    isModalLoading = false;
}

function openEditModal(id) {
    isModalLoading = true;
    $(".text-danger").text("");
    $("#newSupplierToggleContainer").hide();
    $("#newSupplierFields").addClass("d-none");
    $("#supplierSelectContainer").removeClass("d-none");
    $("#supplierSelect").attr("required", "required");
    clearNewSupplierFields();
    $.ajax({
        url: "/Purchases/GetPurchase/" + id,
        type: "GET",
        success: function (data) {
            $("#purchaseId").val(data.id);
            $("#purchaseQty").val(data.quantity);
            $("#unitPrice").val(data.unitPrice);
            $("#totalCost").val(data.totalCost.toFixed(2));
            $("#notes").val(data.notes);

            // Set Supplier
            if (data.supplierId) {
                var supOpt = new Option(data.supplierName, data.supplierId, true, true);
                $("#supplierSelect").append(supOpt).trigger('change');
            } else {
                $("#supplierSelect").val(null).trigger('change');
            }

            // Set Product
            if (data.productId) {
                var prodOpt = new Option(data.productName, data.productId, true, true);
                $("#productSelect").append(prodOpt).trigger('change');
            } else {
                $("#productSelect").val(null).trigger('change');
            }

            // Set Payment Mode and lease values
            $("#paymentMode").val(data.paymentMode).trigger('change').prop('disabled', true);
            if (data.paymentMode === 1) {
                $("#downPayment").val(data.downPayment.toFixed(2));
                $("#installmentsCount").val(data.installmentsCount);
                $("#installmentFrequency").val(data.installmentFrequency);
                calculateLeaseEstimate();
            }

            $("#purchaseModalLabel").text("Edit Purchase / Stock In");
            $("#purchaseModal").modal("show");
            isModalLoading = false;
        },
        error: function () {
            isModalLoading = false;
            Swal.fire({
                title: 'Error!',
                text: 'Could not fetch purchase record.',
                icon: 'error',
                confirmButtonColor: '#d33'
            });
        }
    });
}

function printVoucher(id) {
    var printUrl = "/Purchases/Print/" + id;
    window.open(printUrl, "_blank", "width=850,height=600");
}

function deletePurchase(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "This will adjust the product stock downwards. You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/Purchases/Delete/" + id,
                type: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                success: function (response) {
                    if (response.success) {
                        table.ajax.reload();
                        Swal.fire({
                            title: 'Deleted!',
                            text: response.message,
                            icon: 'success',
                            confirmButtonColor: '#3085d6'
                        });
                    } else {
                        Swal.fire({
                            title: 'Error!',
                            text: response.message,
                            icon: 'error',
                            confirmButtonColor: '#d33'
                        });
                    }
                },
                error: function (xhr) {
                    var msg = "An unexpected error occurred.";
                    if (xhr.responseJSON && xhr.responseJSON.message) {
                        msg = xhr.responseJSON.message;
                    }
                    Swal.fire({
                        title: 'Error!',
                        text: msg,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
                }
            });
        }
    });
}

function clearNewSupplierFields() {
    $("#newSupplierName").val("");
    $("#newSupplierEmail").val("");
    $("#newSupplierPhone").val("");
    $("#newSupplierAddress").val("");
    $("#newSupplierFields .text-danger").text("");
}

function calculateLeaseEstimate() {
    var total = parseFloat($("#totalCost").val()) || 0;
    var down = parseFloat($("#downPayment").val()) || 0;
    var count = parseInt($("#installmentsCount").val()) || 1;

    if (down >= total) {
        $("#leaseEstimate").html("<span class='text-danger'>Down payment must be less than Total Cost.</span>");
        return;
    }

    var balance = total - down;
    var installment = balance / count;
    $("#leaseEstimate").html("Remaining Balance: PKR " + balance.toFixed(2) + "<br/>Estimated Installment: " + count + " x PKR " + installment.toFixed(2));
}

var currentLeasePurchaseId = 0;

function openLeaseModal(purchaseId, purchaseNo, totalCost, downPayment) {
    currentLeasePurchaseId = purchaseId;
    $("#leasePurchaseNo").text(purchaseNo);
    $("#leaseTotalCost").text("PKR " + parseFloat(totalCost).toFixed(2));
    $("#leaseDownPayment").text("PKR " + parseFloat(downPayment).toFixed(2));
    var balance = parseFloat(totalCost) - parseFloat(downPayment);
    $("#leaseRemainingBalance").text("PKR " + balance.toFixed(2));

    loadLeaseSchedule(purchaseId);
}

function loadLeaseSchedule(purchaseId) {
    $("#leaseScheduleBody").html('<tr><td colspan="7" class="text-center"><span class="spinner-border spinner-border-sm me-2"></span>Loading schedule...</td></tr>');
    $("#leaseScheduleModal").modal("show");

    $.ajax({
        url: "/Purchases/GetInstallments?purchaseId=" + purchaseId,
        type: "GET",
        success: function(response) {
            if (response.success) {
                var html = "";
                if (response.data.length === 0) {
                    html = '<tr><td colspan="7" class="text-center text-muted">No installments generated.</td></tr>';
                } else {
                    response.data.forEach(function(item) {
                        var badgeClass = "bg-warning text-dark";
                        if (item.status === "Paid") badgeClass = "bg-success";
                        else if (item.status === "Overdue") badgeClass = "bg-danger";

                        var actionButton = "";
                        if (item.status !== "Paid") {
                            var remaining = item.amount - item.paidAmount;
                            actionButton = `
                                <button class="btn btn-sm btn-success px-3 py-1" onclick="payLeaseInstallment(${item.id}, ${remaining})">
                                    <i class="fas fa-hand-holding-dollar"></i> Pay
                                </button>
                            `;
                        } else {
                            actionButton = '<span class="text-success fw-bold"><i class="fas fa-check-circle"></i> Complete</span>';
                        }

                        html += `
                            <tr>
                                <td>Installment #${item.installmentNumber}</td>
                                <td>${item.dueDate}</td>
                                <td class="fw-bold">PKR ${parseFloat(item.amount).toFixed(2)}</td>
                                <td class="text-success">PKR ${parseFloat(item.paidAmount).toFixed(2)}</td>
                                <td>${item.paymentDate}</td>
                                <td><span class="badge ${badgeClass}">${item.status}</span></td>
                                <td class="text-center">${actionButton}</td>
                            </tr>
                        `;
                    });
                }
                $("#leaseScheduleBody").html(html);
            } else {
                $("#leaseScheduleBody").html('<tr><td colspan="7" class="text-center text-danger">Failed to load schedule.</td></tr>');
            }
        },
        error: function() {
            $("#leaseScheduleBody").html('<tr><td colspan="7" class="text-center text-danger">Error loading schedule.</td></tr>');
        }
    });
}

function payLeaseInstallment(installmentId, remainingAmount) {
    Swal.fire({
        title: 'Pay Installment',
        text: 'Enter the amount you wish to pay (Max: PKR ' + remainingAmount.toFixed(2) + '):',
        input: 'number',
        inputValue: remainingAmount.toFixed(2),
        inputAttributes: {
            min: 0.01,
            max: remainingAmount.toFixed(2),
            step: 0.01
        },
        showCancelButton: true,
        confirmButtonText: 'Pay Now',
        showLoaderOnConfirm: true,
        preConfirm: (amount) => {
            if (!amount || parseFloat(amount) <= 0 || parseFloat(amount) > remainingAmount) {
                Swal.showValidationMessage('Please enter a valid amount up to PKR ' + remainingAmount.toFixed(2));
                return false;
            }
            var data = {
                InstallmentId: installmentId,
                Amount: parseFloat(amount)
            };
            return $.ajax({
                url: "/Purchases/PayInstallment",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(data),
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                }
            }).then(response => {
                if (!response.success) {
                    throw new Error(response.message || 'Payment failed.');
                }
                return response;
            }).catch(error => {
                Swal.showValidationMessage(`Request failed: ${error.message || error}`);
            });
        },
        allowOutsideClick: () => !Swal.isLoading()
    }).then((result) => {
        if (result.isConfirmed) {
            Swal.fire({
                title: 'Success!',
                text: 'Payment recorded successfully.',
                icon: 'success'
            });
            loadLeaseSchedule(currentLeasePurchaseId);
        }
    });
}
