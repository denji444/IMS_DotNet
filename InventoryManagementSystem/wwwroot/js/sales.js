var table;
var invoiceItems = [];

$(document).ready(function () {
    loadCategoryAndTypeFilters();

    // Initialize DataTable
    table = $("#salesTable").DataTable({
        "autoWidth": false,
        "ajax": {
            "url": "/Sales/GetSalesData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { 
                "data": "invoiceNo",
                "width": "18%",
                "className": "text-center align-middle text-nowrap",
                "render": function(data, type, row) {
                    var dt = row.saleDate ? row.saleDate.split(' ')[0] : '';
                    return `<div>
                                <div class="fw-bold text-dark fs-6">${data}</div>
                                ${dt ? `<small class="text-muted d-block">${dt}</small>` : ''}
                            </div>`;
                }
            },
            { 
                "data": "productName",
                "width": "35%",
                "className": "text-center align-middle",
                "render": function(data, type, row) {
                    if (!data || data === "N/A") return `<span class="text-muted small">N/A</span>`;
                    
                    var items = data.split(', ');
                    var itemsHtml = '';
                    if (items.length > 1) {
                        itemsHtml = `<ul class="list-unstyled mb-0 text-start d-inline-block small">` +
                            items.map(function(item) {
                                var safeItem = item.replace(/"/g, '&quot;');
                                return `<li class="fw-semibold text-dark mb-1"><i class="fas fa-box text-primary me-1 small"></i><span title="${safeItem}">${item}</span></li>`;
                            }).join('') +
                            `</ul>`;
                    } else {
                        var safeTitle = data.replace(/"/g, '&quot;');
                        itemsHtml = `<div class="fw-bold text-dark text-wrap mx-auto" style="max-width: 320px;" title="${safeTitle}">${data}</div>`;
                    }

                    var batchBadge = (row.batchNumber && row.batchNumber !== "N/A") 
                        ? `<div class="mt-1"><span class="badge bg-primary text-white"><i class="fas fa-layer-group me-1"></i>${row.batchNumber}</span></div>` 
                        : '';
                        
                    return `<div>${itemsHtml}${batchBadge}</div>`;
                }
            },
            { 
                "data": "customerName",
                "width": "15%",
                "className": "text-center align-middle",
                "render": function(data) {
                    if (!data || data === "N/A") return `<span class="text-muted small">N/A</span>`;
                    var safeCust = data.replace(/"/g, '&quot;');
                    return `<span class="fw-medium text-dark text-wrap d-block mx-auto" style="max-width: 160px;" title="${safeCust}">${data}</span>`;
                }
            },
            { 
                "data": "totalAmount",
                "width": "15%",
                "className": "text-center align-middle text-nowrap",
                "render": function(data) {
                    var totalP = parseFloat(data || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                    return `<div>
                                <div class="fw-bold text-dark">PKR ${totalP}</div>
                            </div>`;
                }
            },
            { 
                "data": "notes",
                "width": "10%",
                "className": "text-center align-middle",
                "render": function(data) {
                    if (!data || data === "N/A") return `<span class="text-muted small">N/A</span>`;
                    var safeNotes = data.replace(/"/g, '&quot;');
                    return `<small class="text-muted d-block text-truncate mx-auto" style="max-width: 120px;" title="${safeNotes}">${data}</small>`;
                }
            },
            {
                "data": "id",
                "width": "7%",
                "className": "text-center align-middle text-nowrap",
                "render": function (data, type, row) {
                    var leaseBtn = "";
                    if (row.paymentMode === 1) {
                        leaseBtn = `
                            <button class="btn btn-sm btn-warning text-dark" onclick="openLeaseModal(${data}, '${row.invoiceNo}', ${row.totalAmount}, ${row.downPayment || 0})" title="Lease Schedule">
                                <i class="fas fa-calendar-alt"></i> Lease
                            </button>
                        `;
                    }
                    return `
                        <div class="d-inline-flex gap-1 text-nowrap justify-content-center">
                            ${leaseBtn}
                            <button class="btn btn-sm btn-dark" onclick="openEditModal(${data})" title="Edit Sale">
                                <i class="fas fa-edit"></i> Edit
                            </button>
                            <button class="btn btn-sm btn-info text-white" onclick="printInvoice(${data})" title="Print Invoice">
                                <i class="fas fa-print"></i> Invoice
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteSale(${data})" title="Delete Sale">
                                <i class="fas fa-trash"></i> Delete
                            </button>
                        </div>
                    `;
                },
                "orderable": false
            }
        ],
        "order": [[0, "desc"]], // Order by date desc
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

    // Initialize Category Filter Select2
    $("#itemCategoryFilter").select2({
        dropdownParent: $("#saleModal"),
        placeholder: "-- All Categories --",
        allowClear: true
    });

    // Initialize Product Type Filter Select2
    $("#itemTypeFilter").select2({
        dropdownParent: $("#saleModal"),
        placeholder: "-- All Product Types --",
        allowClear: true
    });

    // Initialize Batch Select2
    $("#saleBatchSelect").select2({
        dropdownParent: $("#saleModal"),
        placeholder: "-- General / Unbatched Stock --",
        allowClear: true
    });

    // Initialize Product Select2 with Category & Type Filter support
    $("#productSelect").select2({
        dropdownParent: $("#saleModal"),
        placeholder: "Search & Select Product SKU",
        allowClear: true,
        ajax: {
            url: "/Products/GetProductsFiltered",
            dataType: 'json',
            delay: 250,
            data: function (params) {
                return { 
                    q: params.term,
                    categoryName: $("#itemCategoryFilter").val(),
                    productType: $("#itemTypeFilter").val()
                };
            },
            processResults: function (data) {
                return { results: data };
            },
            cache: true
        }
    });

    // Handle Category & Product Type filter changes
    $("#itemCategoryFilter").on("change", function() {
        var catName = $(this).val();
        var $typeSelect = $("#itemTypeFilter");
        $typeSelect.empty().append('<option value="">-- All Product Types --</option>');
        
        if (catName) {
            $.ajax({
                url: "/Products/GetTypesByCategory?categoryName=" + encodeURIComponent(catName),
                type: "GET",
                success: function(types) {
                    if (types && types.length > 0) {
                        $.each(types, function(i, t) {
                            $typeSelect.append(new Option(t, t));
                        });
                    }
                    $typeSelect.trigger('change.select2');
                }
            });
        } else {
            $typeSelect.trigger('change.select2');
        }
        $("#productSelect").val(null).trigger('change');
    });

    $("#itemTypeFilter").on("change", function() {
        $("#productSelect").val(null).trigger('change');
    });

    var currentBatchesData = [];
    var currentBatchFixRate = null;

    function loadRegisteredBatchesForSale(productId) {
        var url = "/Purchases/GetAvailableBatchesForProduct?includeAll=true";
        if (productId) {
            url += "&productId=" + productId;
        }
        $.ajax({
            url: url,
            type: "GET",
            success: function (batches) {
                currentBatchesData = batches || [];
                var $batchSelect = $("#saleBatchSelect");
                var currentVal = $batchSelect.val();
                $batchSelect.empty().append('<option value="">-- General / Unbatched Stock --</option>');
                if (batches && batches.length > 0) {
                    $.each(batches, function (i, b) {
                        var bVal = b.batchNumber;
                        if (bVal) {
                            $batchSelect.append(new Option(b.displayName, bVal));
                        }
                    });
                }
                if (currentVal && $batchSelect.find("option[value='" + currentVal + "']").length > 0) {
                    $batchSelect.val(currentVal);
                } else {
                    $batchSelect.val("");
                }
                $batchSelect.trigger("change.select2");
                $batchSelect.trigger("change");
            }
        });
    }

    // Load initial registered batches on page ready
    loadRegisteredBatchesForSale(null);

    // Prefill price and update stock info when Product changes
    $("#productSelect").on("change", function () {
        var id = $(this).val();
        currentBatchesData = [];
        currentBatchFixRate = null;
        if (id) {
            $.ajax({
                url: "/Products/GetProduct/" + id,
                type: "GET",
                success: function (data) {
                    $("#availableStockQty").text(data.stockQuantity);
                }
            });

            loadRegisteredBatchesForSale(id);
        } else {
            $("#batchRatesContainer").addClass("d-none");
            $("#availableStockQty").text("0");
            $("#batchPurchaseRateDisplay").text("N/A");
            $("#batchFixRateDisplay").text("N/A");
            $("#batchDemandRateDisplay").text("N/A");
            $("#unitPrice").val("");
            loadRegisteredBatchesForSale(null);
        }
    });

    // Handle batch selection changes and rate display
    $("#saleBatchSelect").on("change", function () {
        var selectedBatchNo = $(this).val();
        var batch = currentBatchesData.find(b => b.batchNumber === selectedBatchNo);

        if (batch) {
            $("#availableStockQty").text(batch.availableQuantity);
            
            var purRateText = (batch.purchaseRate !== null && batch.purchaseRate !== undefined) ? "PKR " + parseFloat(batch.purchaseRate).toFixed(2) : "N/A";
            var fixRateText = (batch.fixRate !== null && batch.fixRate !== undefined) ? "PKR " + parseFloat(batch.fixRate).toFixed(2) : "N/A";
            var demRateText = (batch.demandRate !== null && batch.demandRate !== undefined) ? "PKR " + parseFloat(batch.demandRate).toFixed(2) : "N/A";

            $("#batchPurchaseRateDisplay").text(purRateText);
            $("#batchFixRateDisplay").text(fixRateText);
            $("#batchDemandRateDisplay").text(demRateText);

            currentBatchFixRate = (batch.fixRate !== null && batch.fixRate !== undefined) ? parseFloat(batch.fixRate) : null;

            if (batch.demandRate !== null && batch.demandRate !== undefined) {
                $("#unitPrice").val(parseFloat(batch.demandRate).toFixed(2));
            } else if (batch.purchaseRate !== null && batch.purchaseRate !== undefined) {
                $("#unitPrice").val(parseFloat(batch.purchaseRate).toFixed(2));
            } else {
                $("#unitPrice").val("");
            }

            $("#batchRatesContainer").removeClass("d-none");
        } else {
            currentBatchFixRate = null;
            if (currentBatchesData.length > 0 && currentBatchesData[0]) {
                var defaultBatch = currentBatchesData[0];
                var purRateText = (defaultBatch.purchaseRate !== null && defaultBatch.purchaseRate !== undefined) ? "PKR " + parseFloat(defaultBatch.purchaseRate).toFixed(2) : "N/A";
                var fixRateText = (defaultBatch.fixRate !== null && defaultBatch.fixRate !== undefined) ? "PKR " + parseFloat(defaultBatch.fixRate).toFixed(2) : "N/A";
                var demRateText = (defaultBatch.demandRate !== null && defaultBatch.demandRate !== undefined) ? "PKR " + parseFloat(defaultBatch.demandRate).toFixed(2) : "N/A";

                $("#batchPurchaseRateDisplay").text(purRateText);
                $("#batchFixRateDisplay").text(fixRateText);
                $("#batchDemandRateDisplay").text(demRateText);
                currentBatchFixRate = (defaultBatch.fixRate !== null && defaultBatch.fixRate !== undefined) ? parseFloat(defaultBatch.fixRate) : null;

                if (defaultBatch.demandRate !== null && defaultBatch.demandRate !== undefined) {
                    $("#unitPrice").val(parseFloat(defaultBatch.demandRate).toFixed(2));
                }
            } else {
                $("#batchPurchaseRateDisplay").text("N/A");
                $("#batchFixRateDisplay").text("N/A");
                $("#batchDemandRateDisplay").text("N/A");
                $("#unitPrice").val("");
            }
            if ($("#productSelect").val()) {
                $("#batchRatesContainer").removeClass("d-none");
            }
        }
    });

    // Add Item to Invoice Draft Array
    $("#btnAddSaleItem").on("click", function() {
        var productId = parseInt($("#productSelect").val());
        var productText = $("#productSelect option:selected").text();
        var batchNumber = $("#saleBatchSelect").val() || "";
        var qty = parseInt($("#saleQty").val()) || 0;
        var unitPrice = parseFloat($("#unitPrice").val()) || 0;

        if (!productId) {
            Swal.fire({ title: 'Warning!', text: 'Please select a product first.', icon: 'warning', confirmButtonColor: '#3085d6' });
            return;
        }
        if (qty <= 0) {
            Swal.fire({ title: 'Warning!', text: 'Quantity must be at least 1.', icon: 'warning', confirmButtonColor: '#3085d6' });
            return;
        }
        if (unitPrice <= 0) {
            Swal.fire({ title: 'Warning!', text: 'Unit Price must be greater than 0.', icon: 'warning', confirmButtonColor: '#3085d6' });
            return;
        }
        if (currentBatchFixRate !== null && unitPrice < currentBatchFixRate) {
            Swal.fire({
                title: 'Below Fix Rate Threshold!',
                text: `Selling price (PKR ${unitPrice.toFixed(2)}) cannot be lower than the minimum Fix Rate threshold of PKR ${currentBatchFixRate.toFixed(2)} for this batch.`,
                icon: 'error',
                confirmButtonColor: '#d33'
            });
            return;
        }

        invoiceItems.push({
            id: 0,
            productId: productId,
            productName: productText,
            batchNumber: batchNumber,
            quantity: qty,
            unitPrice: unitPrice,
            totalAmount: qty * unitPrice
        });

        // Clear product inputs
        $("#productSelect").val(null).trigger('change');
        $("#saleBatchSelect").empty().append('<option value="">-- General / Unbatched Stock --</option>');
        $("#saleQty").val(1);
        $("#unitPrice").val("");
        $("#availableStockContainer").addClass("d-none");

        renderSaleItemsTable();
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

    $("#paymentMethod").on("change", function() {
        var val = $(this).val();
        $(".payment-method-fields").addClass("d-none");
        if (val === "0") {
            $("#cashFields").removeClass("d-none");
        } else if (val === "1") {
            $("#checkFields").removeClass("d-none");
        } else if (val === "2") {
            $("#onlineTransferFields").removeClass("d-none");
        }
    });

    // Handle Form Submit
    $("#saleForm").on("submit", function (e) {
        e.preventDefault();
        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        if (!invoiceItems || invoiceItems.length === 0) {
            Swal.fire({
                title: 'Validation Error!',
                text: 'Please add at least one item to the sale invoice.',
                icon: 'warning',
                confirmButtonColor: '#3085d6'
            });
            return false;
        }

        var isNewCustomer = $("#newCustomerToggle").is(":checked");
        var customerId = isNewCustomer ? "" : $("#customerSelect").val();
        var newCustomerFirstName = null;
        var newCustomerLastName = null;
        var newCustomerEmail = null;
        var newCustomerPhone = null;
        var newCustomerCnic = null;

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

            var cnic = $("#newCustomerCnic").val().trim();

            if (hasErrors) {
                return false;
            }

            newCustomerFirstName = firstName;
            newCustomerLastName = lastName;
            newCustomerEmail = email;
            newCustomerPhone = phone;
            newCustomerCnic = cnic || null;
        }

        var id = parseInt($("#saleId").val());
        var url = id === 0 ? "/Sales/Create" : "/Sales/Edit/" + id;

        var paymentMethodVal = parseInt($("#paymentMethod").val());
        var payRef = null;
        var bankName = null;
        var checkDate = null;

        if (paymentMethodVal === 0) {
            payRef = $("#cashReference").val().trim() || null;
        } else if (paymentMethodVal === 1) {
            bankName = $("#checkBankName").val().trim() || null;
            payRef = $("#checkNumber").val().trim() || null;
            checkDate = $("#checkDate").val() || null;
        } else if (paymentMethodVal === 2) {
            bankName = $("#transferBankName").val().trim() || null;
            payRef = $("#transferTxnId").val().trim() || null;
        }

        var saleData = {
            Id: id,
            CustomerId: customerId,
            ProductId: invoiceItems.length > 0 ? invoiceItems[0].productId : 0,
            Quantity: invoiceItems.reduce((acc, c) => acc + c.quantity, 0),
            UnitPrice: invoiceItems.length === 1 ? invoiceItems[0].unitPrice : 0,
            TotalAmount: parseFloat($("#totalAmount").val()),
            BatchNumber: invoiceItems.length > 0 ? invoiceItems[0].batchNumber : "",
            Notes: $("#notes").val(),
            NewCustomerFirstName: newCustomerFirstName,
            NewCustomerLastName: newCustomerLastName,
            NewCustomerEmail: newCustomerEmail,
            NewCustomerPhone: newCustomerPhone,
            NewCustomerCnic: newCustomerCnic,
            PaymentMode: parseInt($("#paymentMode").val()),
            PaymentMethod: paymentMethodVal,
            PaymentReference: payRef,
            BankName: bankName,
            CheckDate: checkDate,
            DownPayment: parseFloat($("#downPayment").val()) || 0,
            InstallmentsCount: parseInt($("#installmentsCount").val()) || 0,
            InstallmentFrequency: $("#installmentFrequency").val(),
            Items: invoiceItems
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

function loadCategoryAndTypeFilters() {
    $.ajax({
        url: "/Products/GetCategoriesWithTypes",
        type: "GET",
        success: function(categories) {
            var $cat = $("#itemCategoryFilter");
            $cat.empty().append('<option value="">-- All Categories --</option>');
            if (categories && categories.length > 0) {
                $.each(categories, function(i, c) {
                    $cat.append(new Option(c.name, c.name));
                });
            }
            $cat.trigger('change.select2');
        }
    });
}

function renderSaleItemsTable() {
    var $tbody = $("#saleItemsTableBody");
    $tbody.empty();
    var grandTotal = 0;

    if (!invoiceItems || invoiceItems.length === 0) {
        $tbody.html('<tr id="emptySaleItemsRow"><td colspan="6" class="text-muted small py-3">No items added to invoice yet. Select a product and click "Add Item".</td></tr>');
        $("#totalAmount").val("0.00").trigger('change');
        return;
    }

    $.each(invoiceItems, function(index, item) {
        var lineTotal = item.quantity * item.unitPrice;
        grandTotal += lineTotal;
        var batchBadge = item.batchNumber ? `<span class="badge bg-primary text-white">${item.batchNumber}</span>` : '<span class="badge bg-light text-secondary">General</span>';

        var rowHtml = `
            <tr>
                <td class="text-start fw-bold small">${item.productName}</td>
                <td>${batchBadge}</td>
                <td>${item.quantity}</td>
                <td>PKR ${parseFloat(item.unitPrice).toFixed(2)}</td>
                <td class="fw-bold text-dark">PKR ${lineTotal.toFixed(2)}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-outline-danger py-0 px-1" onclick="removeSaleItem(${index})" title="Remove Item">
                        <i class="fas fa-times"></i>
                    </button>
                </td>
            </tr>
        `;
        $tbody.append(rowHtml);
    });

    $("#totalAmount").val(grandTotal.toFixed(2)).trigger('change');
}

function removeSaleItem(index) {
    invoiceItems.splice(index, 1);
    renderSaleItemsTable();
}

function openCreateModal() {
    invoiceItems = [];
    renderSaleItemsTable();
    $("#saleForm")[0].reset();
    $("#saleId").val(0);
    $("#itemCategoryFilter").val("").trigger('change');
    $("#itemTypeFilter").val("").trigger('change');
    $("#customerSelect").val(null).trigger('change');
    $("#customerSelect").attr("required", "required");
    $("#customerSelectContainer").removeClass("d-none");
    $("#newCustomerToggleContainer").show();
    $("#newCustomerToggle").prop("checked", false);
    $("#newCustomerFields").addClass("d-none");
    clearNewCustomerFields();
    $("#productSelect").val(null).trigger('change');
    $("#saleBatchSelect").empty().append('<option value="">-- General / Unbatched Stock --</option>').trigger('change');
    $("#availableStockContainer").addClass("d-none");
    $("#availableStockQty").text("0");
    $(".text-danger").text("");
    $("#paymentMode").val("0").trigger('change').prop('disabled', false);
    $("#paymentMethod").val("0").trigger('change');
    $("#cashReference, #checkBankName, #checkNumber, #checkDate, #transferBankName, #transferTxnId").val("");
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
            $("#totalAmount").val(data.totalAmount.toFixed(2));
            $("#notes").val(data.notes);

            if (data.items && data.items.length > 0) {
                invoiceItems = data.items.map(function(i) {
                    return {
                        id: i.id || 0,
                        productId: i.productId,
                        productName: i.productName || ("Product #" + i.productId),
                        batchNumber: i.batchNumber || "",
                        quantity: i.quantity,
                        unitPrice: i.unitPrice,
                        totalAmount: i.totalAmount || (i.quantity * i.unitPrice)
                    };
                });
            } else {
                invoiceItems = [];
            }
            renderSaleItemsTable();

            // Set Customer Select2
            if (data.customerId) {
                var custOpt = new Option(data.customerName, data.customerId, true, true);
                $("#customerSelect").append(custOpt).trigger('change');
            } else {
                $("#customerSelect").val(null).trigger('change');
            }

            // Set Payment Mode and lease values
            $("#paymentMode").val(data.paymentMode).trigger('change').prop('disabled', true);
            $("#paymentMethod").val(data.paymentMethod !== undefined ? data.paymentMethod : 0).trigger('change');
            $("#cashReference, #checkBankName, #checkNumber, #checkDate, #transferBankName, #transferTxnId").val("");
            if (data.paymentMethod === 0) {
                $("#cashReference").val(data.paymentReference || "");
            } else if (data.paymentMethod === 1) {
                $("#checkBankName").val(data.bankName || "");
                $("#checkNumber").val(data.paymentReference || "");
                $("#checkDate").val(data.checkDate || "");
            } else if (data.paymentMethod === 2) {
                $("#transferBankName").val(data.bankName || "");
                $("#transferTxnId").val(data.paymentReference || "");
            }

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
    $("#newCustomerCnic").val("");
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

                        var methodBadge = "";
                        if (item.status === "Paid") {
                            if (item.paymentMethod === 0) {
                                methodBadge = `<span class="badge bg-success"><i class="fas fa-money-bill-wave me-1"></i>Cash ${item.paymentReference ? '(' + item.paymentReference + ')' : ''}</span>`;
                            } else if (item.paymentMethod === 1) {
                                methodBadge = `<span class="badge bg-warning text-dark"><i class="fas fa-money-check me-1"></i>Check ${item.bankName ? item.bankName : ''} ${item.paymentReference ? '#' + item.paymentReference : ''}</span>`;
                            } else if (item.paymentMethod === 2) {
                                methodBadge = `<span class="badge bg-info text-white"><i class="fas fa-university me-1"></i>Online ${item.bankName ? item.bankName : ''} ${item.paymentReference ? '(' + item.paymentReference + ')' : ''}</span>`;
                            } else {
                                methodBadge = `<span class="badge bg-success">Collected</span>`;
                            }
                        } else {
                            methodBadge = `<span class="badge bg-light text-secondary">-</span>`;
                        }

                        html += `
                            <tr>
                                <td>Installment #${item.installmentNumber}</td>
                                <td>${item.dueDate}</td>
                                <td class="fw-bold">PKR ${parseFloat(item.amount).toFixed(2)}</td>
                                <td class="text-success">PKR ${parseFloat(item.paidAmount).toFixed(2)}</td>
                                <td>${item.paymentDate} <br/>${methodBadge}</td>
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
        html: `
            <div class="text-start">
                <div class="mb-3">
                    <label class="form-label small fw-bold">Collection Amount (Max: PKR ${remainingAmount.toFixed(2)})</label>
                    <input type="number" id="swalInstallmentAmount" class="form-control" value="${remainingAmount.toFixed(2)}" min="0.01" max="${remainingAmount.toFixed(2)}" step="0.01" />
                </div>
                <div class="mb-3">
                    <label class="form-label small fw-bold">Payment Method</label>
                    <select id="swalPaymentMethod" class="form-select">
                        <option value="0">Cash</option>
                        <option value="1">Check</option>
                        <option value="2">Online Transfer</option>
                    </select>
                </div>
                <div id="swalCashFields">
                    <div class="mb-3">
                        <label class="form-label small fw-bold">Cash Receipt / Ref # (Optional)</label>
                        <input type="text" id="swalCashRef" class="form-control" placeholder="Receipt #" />
                    </div>
                </div>
                <div id="swalCheckFields" class="d-none">
                    <div class="mb-2">
                        <label class="form-label small fw-bold">Bank Name</label>
                        <input type="text" id="swalCheckBank" class="form-control" placeholder="e.g. HBL, Meezan Bank" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label small fw-bold">Check Number</label>
                        <input type="text" id="swalCheckNum" class="form-control" placeholder="e.g. CHK-12345" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label small fw-bold">Check Date</label>
                        <input type="date" id="swalCheckDate" class="form-control" />
                    </div>
                </div>
                <div id="swalOnlineFields" class="d-none">
                    <div class="mb-2">
                        <label class="form-label small fw-bold">Bank / Platform Name</label>
                        <input type="text" id="swalTransferBank" class="form-control" placeholder="e.g. Meezan Bank, JazzCash" />
                    </div>
                    <div class="mb-2">
                        <label class="form-label small fw-bold">Transaction / Ref ID</label>
                        <input type="text" id="swalTransferTxn" class="form-control" placeholder="e.g. TRX-987654" />
                    </div>
                </div>
            </div>
        `,
        didOpen: () => {
            $('#swalPaymentMethod').on('change', function() {
                var method = $(this).val();
                if (method == '0') {
                    $('#swalCashFields').removeClass('d-none');
                    $('#swalCheckFields, #swalOnlineFields').addClass('d-none');
                } else if (method == '1') {
                    $('#swalCheckFields').removeClass('d-none');
                    $('#swalCashFields, #swalOnlineFields').addClass('d-none');
                } else {
                    $('#swalOnlineFields').removeClass('d-none');
                    $('#swalCashFields, #swalCheckFields').addClass('d-none');
                }
            });
        },
        showCancelButton: true,
        confirmButtonText: 'Collect Now',
        showLoaderOnConfirm: true,
        preConfirm: () => {
            var amount = parseFloat($('#swalInstallmentAmount').val());
            if (!amount || amount <= 0 || amount > remainingAmount) {
                Swal.showValidationMessage('Please enter a valid collection amount up to PKR ' + remainingAmount.toFixed(2));
                return false;
            }
            var method = parseInt($('#swalPaymentMethod').val());
            var ref = null;
            var bank = null;
            var date = null;

            if (method === 0) {
                ref = $('#swalCashRef').val().trim() || null;
            } else if (method === 1) {
                bank = $('#swalCheckBank').val().trim();
                ref = $('#swalCheckNum').val().trim();
                date = $('#swalCheckDate').val() || null;
                if (!bank || !ref) {
                    Swal.showValidationMessage('Bank Name and Check Number are required for Check payment.');
                    return false;
                }
            } else if (method === 2) {
                bank = $('#swalTransferBank').val().trim();
                ref = $('#swalTransferTxn').val().trim();
                if (!bank || !ref) {
                    Swal.showValidationMessage('Bank/Platform Name and Transaction ID are required for Online Transfer.');
                    return false;
                }
            }

            var data = {
                InstallmentId: installmentId,
                Amount: amount,
                PaymentMethod: method,
                PaymentReference: ref,
                BankName: bank,
                CheckDate: date
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


