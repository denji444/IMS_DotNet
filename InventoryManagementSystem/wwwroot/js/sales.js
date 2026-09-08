var table;

$(document).ready(function () {
    // Initialize DataTable
    table = $("#salesTable").DataTable({
        "ajax": {
            "url": "/Sales/GetSalesData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "invoiceNo" },
            { "data": "productName" },
            { "data": "customerName" },
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
            { 
                "data": "batchNumber",
                "render": function(d) {
                    return (d && d !== "N/A") ? `<span class="badge bg-primary text-white"><i class="fas fa-layer-group me-1"></i>${d}</span>` : '<span class="badge bg-light text-secondary">N/A</span>';
                }
            },
            { "data": "notes" },
            {
                "data": "id",
                "render": function (data, type, row) {
                    var leaseBtn = "";
                    if (row.paymentMode === 1) {
                        leaseBtn = `
                            <button class="btn btn-sm btn-warning text-dark me-1" onclick="openLeaseModal(${data}, '${row.invoiceNo}', ${row.totalAmount}, ${row.downPayment || 0})">
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
                            <button class="btn btn-sm btn-info text-white me-1" onclick="printInvoice(${data})">
                                <i class="fas fa-print"></i> Invoice
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteSale(${data})">
                                <i class="fas fa-trash"></i> Delete
                            </button>
                        </div>
                    `;
                },
                "orderable": false,
                "width": "25%"
            }
        ],
        "order": [[6, "desc"]], // Order by date desc
        "language": {
            "emptyTable": "No sales record found. Click 'Record Sale' to process a stock out."
        }
    });

    // Initialize Customer Select2
    $("#customerSelect").select2({
        dropdownParent: $("#saleModal"),
        placeholder: "Search & Select Customer",
        allowClear: true,
        ajax: {
            url: "/Customers/GetCustomersJson",
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

    // Initialize Product Select2
    $("#productSelect").select2({
        dropdownParent: $("#saleModal"),
        placeholder: "Search & Select Product SKU",
        allowClear: true,
        ajax: {
            url: "/Products/GetProductsJson",
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

    // Prefill price and update stock info when Product changes
    $("#productSelect").on("change", function () {
        var id = $(this).val();
        if (id) {
            $.ajax({
                url: "/Products/GetProduct/" + id,
                type: "GET",
                success: function (data) {
                    // Show real-time quantity
                    $("#availableStockQty").text(data.stockQuantity);
                    
                    var hasPrice = (data.price !== null && data.price !== undefined && !isNaN(parseFloat(data.price)));
                    var priceFormatted = hasPrice ? parseFloat(data.price).toFixed(2) : "";

                    // Show set unit price
                    if (hasPrice) {
                        $("#setUnitPriceValue").text("PKR " + priceFormatted);
                    } else {
                        $("#setUnitPriceValue").text("N/A");
                    }
                    
                    $("#unitPrice").removeAttr("min");
                    
                    // Set color based on stock availability
                    if (data.stockQuantity > 0) {
                        $("#availableStockContainer")
                            .removeClass("text-danger")
                            .addClass("text-success");
                    } else {
                        $("#availableStockContainer")
                            .removeClass("text-success")
                            .addClass("text-danger");
                    }
                    
                    $("#availableStockContainer").removeClass("d-none");

                    // Only prefill unit price if we are creating a new sale (i.e. saleId is 0)
                    if (parseInt($("#saleId").val()) === 0) {
                        if (hasPrice) {
                            $("#unitPrice").val(priceFormatted);
                        }
                        calculateTotalAmount();
                    }
                }
            });

            // Fetch active batches for selected product
            $.ajax({
                url: "/Purchases/GetAvailableBatchesForProduct?productId=" + id,
                type: "GET",
                success: function (batches) {
                    var $batchSelect = $("#saleBatchSelect");
                    var currentVal = $batchSelect.val();
                    $batchSelect.empty();
                    $batchSelect.append('<option value="">-- General / Unbatched Stock --</option>');
                    if (batches && batches.length > 0) {
                        $.each(batches, function (i, b) {
                            $batchSelect.append(new Option(b.displayName, b.batchNumber));
                        });
                    }
                    if (currentVal) {
                        $batchSelect.val(currentVal);
                    }
                }
            });
        } else {
            $("#availableStockContainer")
                .addClass("d-none")
                .removeClass("text-success text-danger");
            $("#availableStockQty").text("0");
            $("#setUnitPriceValue").text("PKR 0.00");
            $("#unitPrice").attr("min", "0.01");
            $("#saleBatchSelect").empty().append('<option value="">-- General / Unbatched Stock --</option>');
        }
    });

    // Toggle for New Customer inline fields
    $("#newCustomerToggle").on("change", function () {
        if (this.checked) {
            $("#customerSelectContainer").addClass("d-none");
            $("#customerSelect").val(null).trigger("change");
            $("#customerSelect").removeAttr("required");
            $("#newCustomerFields").removeClass("d-none");
        } else {
            $("#customerSelectContainer").removeClass("d-none");
            $("#customerSelect").attr("required", "required");
            $("#newCustomerFields").addClass("d-none");
            clearNewCustomerFields();
        }
    });

    // Calculate total amount automatically when Qty or Unit Price changes
    $("#saleQty, #unitPrice").on("input", function () {
        calculateTotalAmount();
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

    $("#downPayment, #installmentsCount, #totalAmount").on("input change", function() {
        if ($("#paymentMode").val() == "1") {
            calculateLeaseEstimate();
        }
    });

    // Handle Form Submit
    $("#saleForm").on("submit", function (e) {
        e.preventDefault();
        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        var isNewCustomer = $("#newCustomerToggle").is(":checked");
        var customerId = isNewCustomer ? "" : $("#customerSelect").val();
        var newCustomerFirstName = null;
        var newCustomerLastName = null;
        var newCustomerEmail = null;
        var newCustomerPhone = null;

        if (isNewCustomer) {
            $("#newCustomerFields .text-danger").text("");
            var hasErrors = false;

            var firstName = $("#newCustomerFirstName").val().trim();
            if (!firstName) {
                $("#newCustomerFirstNameError").text("First Name is required.");
                hasErrors = true;
            }

            var lastName = $("#newCustomerLastName").val().trim();
            if (!lastName) {
                $("#newCustomerLastNameError").text("Last Name is required.");
                hasErrors = true;
            }

            var email = $("#newCustomerEmail").val().trim();
            if (!email) {
                $("#newCustomerEmailError").text("Email is required.");
                hasErrors = true;
            } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
                $("#newCustomerEmailError").text("Invalid email format.");
                hasErrors = true;
            }

            var phone = $("#newCustomerPhone").val().trim();
            if (!phone) {
                $("#newCustomerPhoneError").text("Phone Number is required.");
                hasErrors = true;
            }

            if (hasErrors) {
                return false;
            }

            newCustomerFirstName = firstName;
            newCustomerLastName = lastName;
            newCustomerEmail = email;
            newCustomerPhone = phone;
        }

        var id = parseInt($("#saleId").val());
        var url = id === 0 ? "/Sales/Create" : "/Sales/Edit/" + id;

        var saleData = {
            Id: id,
            CustomerId: customerId,
            ProductId: parseInt($("#productSelect").val()),
            Quantity: parseInt($("#saleQty").val()),
            UnitPrice: parseFloat($("#unitPrice").val()),
            TotalAmount: parseFloat($("#totalAmount").val()),
            BatchNumber: $("#saleBatchSelect").val(),
            Notes: $("#notes").val(),
            NewCustomerFirstName: newCustomerFirstName,
            NewCustomerLastName: newCustomerLastName,
            NewCustomerEmail: newCustomerEmail,
            NewCustomerPhone: newCustomerPhone,
            PaymentMode: parseInt($("#paymentMode").val()),
            DownPayment: parseFloat($("#downPayment").val()) || 0,
            InstallmentsCount: parseInt($("#installmentsCount").val()) || 0,
            InstallmentFrequency: $("#installmentFrequency").val()
        };

        var btn = $("#btnSaveSale");
        var originalText = btn.html();
        btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

        $.ajax({
            url: url,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(saleData),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false).html(originalText);
                if (response.success) {
                    $("#saleModal").modal("hide");
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

function calculateTotalAmount() {
    var qty = parseInt($("#saleQty").val()) || 0;
    var price = parseFloat($("#unitPrice").val()) || 0.0;
    var total = qty * price;
    $("#totalAmount").val(total.toFixed(2)).trigger('change');
}

function openCreateModal() {
    $("#saleForm")[0].reset();
    $("#saleId").val(0);
    $("#customerSelect").val(null).trigger('change');
    $("#customerSelect").attr("required", "required");
    $("#customerSelectContainer").removeClass("d-none");
    $("#newCustomerToggleContainer").show();
    $("#newCustomerToggle").prop("checked", false);
    $("#newCustomerFields").addClass("d-none");
    clearNewCustomerFields();
    $("#productSelect").val(null).trigger('change');
    $("#saleBatchSelect").empty().append('<option value="">-- General / Unbatched Stock --</option>');
    $("#availableStockContainer").addClass("d-none");
    $("#availableStockQty").text("0");
    $(".text-danger").text("");
    $("#paymentMode").val("0").trigger('change').prop('disabled', false);
    $("#saleModalLabel").text("Record Sale / Stock Out");
    $("#saleModal").modal("show");
}

function openEditModal(id) {
    $(".text-danger").text("");
    $("#newCustomerToggleContainer").hide();
    $("#newCustomerFields").addClass("d-none");
    $("#customerSelectContainer").removeClass("d-none");
    $("#customerSelect").attr("required", "required");
    clearNewCustomerFields();
    $.ajax({
        url: "/Sales/GetSale/" + id,
        type: "GET",
        success: function (data) {
            $("#saleId").val(data.id);
            $("#saleQty").val(data.quantity);
            $("#unitPrice").val(data.unitPrice);
            $("#totalAmount").val(data.totalAmount.toFixed(2));
            $("#notes").val(data.notes);

            // Set Customer Select2
            if (data.customerId) {
                var custOpt = new Option(data.customerName, data.customerId, true, true);
                $("#customerSelect").append(custOpt).trigger('change');
            } else {
                $("#customerSelect").val(null).trigger('change');
            }

            // Set Product Select2
            if (data.productId) {
                var prodOpt = new Option(data.productName, data.productId, true, true);
                $("#productSelect").append(prodOpt).trigger('change');
                if (data.batchNumber && data.batchNumber !== "N/A") {
                    setTimeout(function() {
                        $("#saleBatchSelect").val(data.batchNumber);
                    }, 300);
                }
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

            $("#availableStockContainer").addClass("d-none");
            $("#availableStockQty").text("0");

            $("#saleModalLabel").text("Edit Sale / Stock Out");
            $("#saleModal").modal("show");
        },
        error: function () {
            Swal.fire({
                title: 'Error!',
                text: 'Could not fetch sale details.',
                icon: 'error',
                confirmButtonColor: '#d33'
            });
        }
    });
}

function clearNewCustomerFields() {
    $("#newCustomerFirstName").val("");
    $("#newCustomerLastName").val("");
    $("#newCustomerEmail").val("");
    $("#newCustomerPhone").val("");
    $("#newCustomerFields .text-danger").text("");
}

function calculateLeaseEstimate() {
    var total = parseFloat($("#totalAmount").val()) || 0;
    var down = parseFloat($("#downPayment").val()) || 0;
    var count = parseInt($("#installmentsCount").val()) || 1;

    if (down >= total) {
        $("#leaseEstimate").html("<span class='text-danger'>Down payment must be less than Total Amount.</span>");
        return;
    }

    var balance = total - down;
    var installment = balance / count;
    $("#leaseEstimate").html("Remaining Balance: PKR " + balance.toFixed(2) + "<br/>Estimated Installment: " + count + " x PKR " + installment.toFixed(2));
}

var currentLeaseSaleId = 0;

function openLeaseModal(saleId, invoiceNo, totalAmount, downPayment) {
    currentLeaseSaleId = saleId;
    $("#leaseInvoiceNo").text(invoiceNo);
    $("#leaseTotalCost").text("PKR " + parseFloat(totalAmount).toFixed(2));
    $("#leaseDownPayment").text("PKR " + parseFloat(downPayment).toFixed(2));
    var balance = parseFloat(totalAmount) - parseFloat(downPayment);
    $("#leaseRemainingBalance").text("PKR " + balance.toFixed(2));

    loadLeaseSchedule(saleId);
}

function loadLeaseSchedule(saleId) {
    $("#leaseScheduleBody").html('<tr><td colspan="7" class="text-center"><span class="spinner-border spinner-border-sm me-2"></span>Loading schedule...</td></tr>');
    $("#leaseScheduleModal").modal("show");

    $.ajax({
        url: "/Sales/GetInstallments?saleId=" + saleId,
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
                                <button class="btn btn-sm btn-info text-white me-1 px-2 py-1" onclick="sendReminderEmail(${item.id})">
                                    <i class="fas fa-envelope"></i> Reminder
                                </button>
                                <button class="btn btn-sm btn-success px-2 py-1" onclick="collectLeaseInstallment(${item.id}, ${remaining})">
                                    <i class="fas fa-hand-holding-dollar"></i> Collect
                                </button>
                            `;
                        } else {
                            actionButton = '<span class="text-success fw-bold"><i class="fas fa-check-circle"></i> Collected</span>';
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

function collectLeaseInstallment(installmentId, remainingAmount) {
    Swal.fire({
        title: 'Collect Installment',
        text: 'Enter the amount collected (Max: PKR ' + remainingAmount.toFixed(2) + '):',
        input: 'number',
        inputValue: remainingAmount.toFixed(2),
        inputAttributes: {
            min: 0.01,
            max: remainingAmount.toFixed(2),
            step: 0.01
        },
        showCancelButton: true,
        confirmButtonText: 'Collect Now',
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
                url: "/Sales/ReceiveInstallment",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(data),
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                }
            }).then(response => {
                if (!response.success) {
                    throw new Error(response.message || 'Collection failed.');
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
                title: 'Collected!',
                text: 'Installment collection recorded successfully.',
                icon: 'success'
            });
            loadLeaseSchedule(currentLeaseSaleId);
        }
    });
}

function printInvoice(id) {
    var printUrl = "/Sales/Print/" + id;
    window.open(printUrl, "_blank", "width=850,height=600");
}

function deleteSale(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "This will restock the items back into inventory. You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/Sales/Delete/" + id,
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

function sendReminderEmail(installmentId) {
    Swal.fire({
        title: 'Send Reminder Email',
        text: 'Are you sure you want to send a payment reminder email to the buyer?',
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Yes, Send It',
        showLoaderOnConfirm: true,
        preConfirm: () => {
            var data = {
                InstallmentId: installmentId
            };
            return $.ajax({
                url: "/Sales/SendReminderEmail",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify(data),
                headers: {
                    "RequestVerificationToken": $('input[name="__RequestVerificationToken"]').val()
                }
            }).then(response => {
                if (!response.success) {
                    throw new Error(response.message || 'Request failed.');
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
                title: 'Sent!',
                text: result.value.message || 'Reminder email sent successfully.',
                icon: 'success'
            });
        }
    });
}
