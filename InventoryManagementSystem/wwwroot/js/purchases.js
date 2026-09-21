var purchasesTable;
var isModalLoading = false;
var voucherItems = [];

$(document).ready(function () {
    loadCategoryAndTypeFilters();

    // Initialize DataTable
    if ($("#purchasesTable").length) {
        purchasesTable = $("#purchasesTable").DataTable({
        "autoWidth": false,
        "ajax": {
            "url": "/Purchases/GetPurchasesData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { 
                "data": "purchaseNo",
                "width": "18%",
                "className": "text-center align-middle text-nowrap",
                "render": function(data, type, row) {
                    var dt = row.purchaseDate ? row.purchaseDate.split(' ')[0] : '';
                    return `<div>
                                <div class="fw-bold text-dark fs-6">${data}</div>
                                ${dt ? `<small class="text-muted d-block">${dt}</small>` : ''}
                            </div>`;
                }
            },
            { 
                "data": "productName",
                "width": "30%",
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
                        itemsHtml = `<div class="fw-bold text-dark text-wrap mx-auto" style="max-width: 300px;" title="${safeTitle}">${data}</div>`;
                    }

                    var batchBadge = (row.batchNumber && row.batchNumber !== "N/A") 
                        ? `<div class="mt-1"><span class="badge bg-primary text-white"><i class="fas fa-layer-group me-1"></i>${row.batchNumber}</span></div>` 
                        : '';
                        
                    return `<div>${itemsHtml}${batchBadge}</div>`;
                }
            },
            { 
                "data": "supplierName",
                "width": "14%",
                "className": "text-center align-middle",
                "render": function(data) {
                    return `<span class="fw-medium text-dark text-truncate d-block mx-auto" style="max-width: 130px;" title="${data}">${data}</span>`;
                }
            },
            { 
                "data": "quantity",
                "width": "6%",
                "className": "text-center align-middle",
                "render": function(data) {
                    return `<span class="badge bg-secondary fs-6">${data}</span>`;
                }
            },
            { 
                "data": "totalCost",
                "width": "15%",
                "className": "text-center align-middle text-nowrap",
                "render": function(data, type, row) {
                    var unitP = parseFloat(row.unitPrice || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                    var totalP = parseFloat(data || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 });
                    return `<div>
                                <div class="fw-bold text-dark">PKR ${totalP}</div>
                                <small class="text-muted d-block">Unit: PKR ${unitP}</small>
                            </div>`;
                }
            },
            { 
                "data": "notes",
                "width": "15%",
                "className": "text-center align-middle",
                "render": function(data) {
                    if (!data || data === "N/A") return `<span class="text-muted small">N/A</span>`;
                    var safeNotes = data.replace(/"/g, '&quot;');
                    return `<small class="text-muted d-block text-truncate mx-auto" style="max-width: 140px;" title="${safeNotes}">${data}</small>`;
                }
            },
            {
                "data": "id",
                "width": "12%",
                "className": "text-center align-middle text-nowrap",
                "render": function (data, type, row) {
                    var leaseBtn = "";
                    if (row.paymentMode === 1) {
                        leaseBtn = `
                            <button class="btn btn-sm btn-warning text-dark" onclick="openLeaseModal(${data}, '${row.purchaseNo}', ${row.totalCost}, ${row.downPayment || 0})" title="Lease Schedule">
                                <i class="fas fa-calendar-alt"></i> Lease
                            </button>
                        `;
                    }
                    return `
                        <div class="d-inline-flex gap-1 text-nowrap justify-content-center">
                            ${leaseBtn}
                            <button class="btn btn-sm btn-dark" onclick="openPurchaseEditModal(${data})" title="Edit Purchase">
                                <i class="fas fa-edit"></i> Edit
                            </button>
                            <button class="btn btn-sm btn-info text-white" onclick="printVoucher(${data})" title="Print Voucher">
                                <i class="fas fa-print"></i> Print
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deletePurchase(${data})" title="Delete Purchase">
                                <i class="fas fa-trash"></i> Delete
                            </button>
                        </div>
                    `;
                },
                "orderable": false
            }
        ],
        "order": [[0, "desc"]], // Sort by date desc
        "language": {
            "emptyTable": "No purchase history found. Click 'New Purchase' to stock in."
        }
    });
    }

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

    // Initialize Category Filter Select2
    $("#itemCategoryFilter").select2({
        dropdownParent: $("#purchaseModal"),
        placeholder: "-- All Categories --",
        allowClear: true
    });

    // Initialize Product Type Filter Select2
    $("#itemTypeFilter").select2({
        dropdownParent: $("#purchaseModal"),
        placeholder: "-- All Product Types --",
        allowClear: true
    });

    // Initialize Batch Select2 with Tagging
    $("#batchNumber").select2({
        dropdownParent: $("#purchaseModal"),
        placeholder: "Select or Type Batch...",
        allowClear: true,
        tags: true
    });

    // Initialize Product Select2 with Category & Type Filter support
    $("#productSelect").select2({
        dropdownParent: $("#purchaseModal"),
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

    function loadRegisteredBatchesForPurchase(productId) {
        var url = "/Purchases/GetAvailableBatchesForProduct?includeAll=true";
        if (productId) {
            url += "&productId=" + productId;
        }
        $.ajax({
            url: url,
            type: "GET",
            success: function (batches) {
                var $batchSelect = $("#batchNumber");
                var currentVal = $batchSelect.val();
                $batchSelect.empty().append('<option value="">Select or Type Batch...</option>');
                if (batches && batches.length > 0) {
                    var existing = new Set();
                    $.each(batches, function (i, b) {
                        var bVal = b.batchNumber || b.displayName;
                        if (bVal && !existing.has(bVal)) {
                            existing.add(bVal);
                            var text = b.displayName || bVal;
                            $batchSelect.append(new Option(text, bVal));
                        }
                    });
                }
                if (currentVal && $batchSelect.find("option[value='" + currentVal + "']").length > 0) {
                    $batchSelect.val(currentVal);
                } else {
                    $batchSelect.val(null);
                }
                $batchSelect.trigger("change.select2");
            }
        });
    }

    // Load initial registered batches on page ready
    loadRegisteredBatchesForPurchase(null);

    // Listen to product selection to automatically populate unit cost and fetch existing batches
    $("#productSelect").on("change", function () {
        var id = $(this).val();
        loadRegisteredBatchesForPurchase(id);
    });

    $("#productSelect").on("select2:select", function (e) {
        var data = e.params.data;
        if (data && data.price) {
            $("#unitPrice").val(parseFloat(data.price).toFixed(2));
        }
        if (data && data.stockQuantity !== undefined && parseInt(data.stockQuantity) >= 50) {
            Swal.fire({
                toast: true,
                position: 'top-end',
                icon: 'info',
                title: `Overbought Notice: '${data.name || 'Product'}' has ${data.stockQuantity} units in stock.`,
                showConfirmButton: false,
                timer: 4500,
                timerProgressBar: true
            });
        }
    });

    // Add Item to Voucher Draft Array
    $("#btnAddPurchaseItem").on("click", function() {
        var productId = parseInt($("#productSelect").val());
        var productText = $("#productSelect option:selected").text();
        var rawBatch = $("#batchNumber").val();
        var batchNumber = (rawBatch && typeof rawBatch === 'string') ? rawBatch.trim() : (Array.isArray(rawBatch) ? rawBatch.join(', ').trim() : "");
        var qty = parseInt($("#purchaseQty").val()) || 0;
        var unitPrice = parseFloat($("#unitPrice").val()) || 0;
        var demandRateRaw = $("#demandRate").val().trim();
        var fixRateRaw = $("#fixRate").val().trim();

        var demandRate = (demandRateRaw !== "" && !isNaN(parseFloat(demandRateRaw))) ? parseFloat(demandRateRaw) : null;
        var fixRate = (fixRateRaw !== "" && !isNaN(parseFloat(fixRateRaw))) ? parseFloat(fixRateRaw) : null;

        if (!productId) {
            Swal.fire({ title: 'Warning!', text: 'Please select a product first.', icon: 'warning', confirmButtonColor: '#3085d6' });
            return;
        }
        if (!batchNumber) {
            Swal.fire({ title: 'Warning!', text: 'Batch Name / Number is required to bind product rates.', icon: 'warning', confirmButtonColor: '#3085d6' });
            return;
        }
        if (qty <= 0) {
            Swal.fire({ title: 'Warning!', text: 'Quantity must be at least 1.', icon: 'warning', confirmButtonColor: '#3085d6' });
            return;
        }
        if (fixRate !== null && fixRate < unitPrice) {
            Swal.fire({ 
                title: 'Warning!', 
                text: `Fix Rate (PKR ${fixRate.toFixed(2)}) cannot be lower than Purchase Rate (PKR ${unitPrice.toFixed(2)}).`, 
                icon: 'warning', 
                confirmButtonColor: '#3085d6' 
            });
            return;
        }
        if (demandRate !== null && fixRate !== null && demandRate < fixRate) {
            Swal.fire({ 
                title: 'Warning!', 
                text: `Demand Rate (PKR ${demandRate.toFixed(2)}) cannot be lower than Fix Rate (PKR ${fixRate.toFixed(2)}).`, 
                icon: 'warning', 
                confirmButtonColor: '#3085d6' 
            });
            return;
        }
        if (demandRate !== null && fixRate === null && demandRate < unitPrice) {
            Swal.fire({ 
                title: 'Warning!', 
                text: `Demand Rate (PKR ${demandRate.toFixed(2)}) cannot be lower than Purchase Rate (PKR ${unitPrice.toFixed(2)}).`, 
                icon: 'warning', 
                confirmButtonColor: '#3085d6' 
            });
            return;
        }

        voucherItems.push({
            id: 0,
            productId: productId,
            productName: productText,
            batchNumber: batchNumber,
            quantity: qty,
            unitPrice: unitPrice,
            demandRate: demandRate,
            fixRate: fixRate,
            totalCost: qty * unitPrice
        });

        // Clear product inputs
        $("#productSelect").val(null).trigger('change');
        $("#batchNumber").val(null).trigger('change');
        $("#purchaseQty").val(1);
        $("#unitPrice").val("");
        $("#demandRate").val("");
        $("#fixRate").val("");

        renderPurchaseItemsTable();
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

    // Handle form submit
    $("#purchaseForm").on("submit", function (e) {
        e.preventDefault();
        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        if (!voucherItems || voucherItems.length === 0) {
            Swal.fire({
                title: 'Validation Error!',
                text: 'Please add at least one item to the purchase voucher.',
                icon: 'warning',
                confirmButtonColor: '#3085d6'
            });
            return false;
        }

        var isNewSupplier = $("#newSupplierToggle").is(":checked");
        var supplierId = isNewSupplier ? 0 : parseInt($("#supplierSelect").val());
        var newSupplier = null;

        if (isNewSupplier) {
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

            var cnic = $("#newSupplierCnic").val().trim();

            if (hasErrors) {
                return false;
            }

            newSupplier = {
                Name: name,
                Email: email,
                Phone: phone,
                Cnic: cnic || null,
                Address: address
            };
        }

        var id = parseInt($("#purchaseId").val());
        var url = id === 0 ? "/Purchases/Create" : "/Purchases/Edit/" + id;

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

        var purchaseData = {
            Id: id,
            SupplierId: supplierId,
            NewSupplier: newSupplier,
            ProductId: voucherItems.length > 0 ? voucherItems[0].productId : 0,
            Quantity: voucherItems.reduce((acc, c) => acc + c.quantity, 0),
            UnitPrice: voucherItems.length === 1 ? voucherItems[0].unitPrice : 0,
            TotalCost: parseFloat($("#totalCost").val()),
            BatchNumber: voucherItems.length > 0 ? voucherItems[0].batchNumber : "",
            Notes: $("#notes").val(),
            PaymentMode: parseInt($("#paymentMode").val()),
            PaymentMethod: paymentMethodVal,
            PaymentReference: payRef,
            BankName: bankName,
            CheckDate: checkDate,
            DownPayment: parseFloat($("#downPayment").val()) || 0,
            InstallmentsCount: parseInt($("#installmentsCount").val()) || 0,
            InstallmentFrequency: $("#installmentFrequency").val(),
            Items: voucherItems
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
                    if (purchasesTable) {
                        purchasesTable.ajax.reload(null, false);
                    }
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

    // Purchasing Module Tab Routing & Lazy Load
    $('#purchasingTabs button').on('shown.bs.tab', function (e) {
        var rawTarget = $(e.target).data('bs-target') || '';
        var targetId = rawTarget.replace('#', '');
        if (history.replaceState) {
            var urlParams = new URLSearchParams(window.location.search);
            urlParams.set('tab', targetId);
            var newUrl = window.location.protocol + "//" + window.location.host + window.location.pathname + '?' + urlParams.toString();
            window.history.replaceState({ path: newUrl }, '', newUrl);
        }
        if (targetId === 'purchases' && purchasesTable) {
            purchasesTable.ajax.reload(null, false);
        } else if (targetId === 'suppliers' && typeof suppliersTable !== 'undefined' && suppliersTable) {
            suppliersTable.ajax.reload(null, false);
        }
    });

    // Activate tab from URL query param
    var urlParams = new URLSearchParams(window.location.search);
    var tabParam = urlParams.get('tab') || 'purchases';
    var tabButton = $('#' + tabParam + '-tab');
    if (tabButton.length) {
        bootstrap.Tab.getOrCreateInstance(tabButton[0]).show();
    }
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

function renderPurchaseItemsTable() {
    var $tbody = $("#purchaseItemsTableBody");
    $tbody.empty();
    var grandTotal = 0;

    if (!voucherItems || voucherItems.length === 0) {
        $tbody.html('<tr id="emptyPurchaseItemsRow"><td colspan="8" class="text-muted small py-3">No items added to voucher yet. Select a product, batch & rates, then click "Add".</td></tr>');
        $("#totalCost").val("0.00").trigger('change');
        return;
    }

    $.each(voucherItems, function(index, item) {
        var lineTotal = item.quantity * item.unitPrice;
        grandTotal += lineTotal;
        var batchBadge = item.batchNumber ? `<span class="badge bg-primary text-white"><i class="fas fa-layer-group me-1"></i>${item.batchNumber}</span>` : '<span class="badge bg-light text-secondary">General</span>';
        var dRateHtml = (item.demandRate !== null && item.demandRate !== undefined && !isNaN(parseFloat(item.demandRate))) ? `PKR ${parseFloat(item.demandRate).toFixed(2)}` : '<span class="text-muted small">N/A</span>';
        var fRateHtml = (item.fixRate !== null && item.fixRate !== undefined && !isNaN(parseFloat(item.fixRate))) ? `PKR ${parseFloat(item.fixRate).toFixed(2)}` : '<span class="text-muted small">N/A</span>';

        var rowHtml = `
            <tr>
                <td class="text-start fw-bold small">${item.productName}</td>
                <td>${batchBadge}</td>
                <td>${item.quantity}</td>
                <td>PKR ${parseFloat(item.unitPrice).toFixed(2)}</td>
                <td class="text-success fw-semibold">${dRateHtml}</td>
                <td class="text-warning fw-semibold">${fRateHtml}</td>
                <td class="fw-bold text-dark">PKR ${lineTotal.toFixed(2)}</td>
                <td>
                    <button type="button" class="btn btn-sm btn-outline-danger py-0 px-1" onclick="removePurchaseItem(${index})" title="Remove Item">
                        <i class="fas fa-times"></i>
                    </button>
                </td>
            </tr>
        `;
        $tbody.append(rowHtml);
    });

    $("#totalCost").val(grandTotal.toFixed(2)).trigger('change');
}

function removePurchaseItem(index) {
    voucherItems.splice(index, 1);
    renderPurchaseItemsTable();
}

function openPurchaseCreateModal() {
    isModalLoading = true;
    voucherItems = [];
    renderPurchaseItemsTable();
    $("#purchaseForm")[0].reset();
    $("#purchaseId").val(0);
    $("#itemCategoryFilter").val("").trigger('change');
    $("#itemTypeFilter").val("").trigger('change');
    $("#supplierSelect").val(null).trigger('change');
    $("#supplierSelect").attr("required", "required");
    $("#supplierSelectContainer").removeClass("d-none");
    $("#newSupplierToggleContainer").show();
    $("#newSupplierToggle").prop("checked", false);
    $("#newSupplierFields").addClass("d-none");
    clearNewSupplierFields();
    $("#productSelect").val(null).trigger('change');
    $("#batchNumber").val(null).trigger('change');
    $(".text-danger").text("");
    $("#paymentMode").val("0").trigger('change').prop('disabled', false);
    $("#paymentMethod").val("0").trigger('change');
    $("#cashReference, #checkBankName, #checkNumber, #checkDate, #transferBankName, #transferTxnId").val("");
    $("#purchaseModalLabel").text("New Purchase / Stock In");
    $("#purchaseModal").modal("show");
    isModalLoading = false;
}

function openPurchaseEditModal(id) {
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
            $("#totalCost").val(data.totalCost.toFixed(2));
            $("#batchNumber").val(data.batchNumber === "N/A" ? "" : (data.batchNumber || ""));
            $("#notes").val(data.notes);

            if (data.items && data.items.length > 0) {
                voucherItems = data.items.map(function(i) {
                    return {
                        id: i.id || 0,
                        productId: i.productId,
                        productName: i.productName || ("Product #" + i.productId),
                        batchNumber: i.batchNumber || "",
                        quantity: i.quantity,
                        unitPrice: i.unitPrice,
                        demandRate: i.demandRate !== undefined ? i.demandRate : null,
                        fixRate: i.fixRate !== undefined ? i.fixRate : null,
                        totalCost: i.totalCost || (i.quantity * i.unitPrice)
                    };
                });
            } else {
                voucherItems = [];
            }
            renderPurchaseItemsTable();

            // Set Supplier
            if (data.supplierId) {
                var supOpt = new Option(data.supplierName, data.supplierId, true, true);
                $("#supplierSelect").append(supOpt).trigger('change');
            } else {
                $("#supplierSelect").val(null).trigger('change');
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
                        if (purchasesTable) {
                            purchasesTable.ajax.reload(null, false);
                        }
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
    $("#newSupplierCnic").val("");
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

                        var methodBadge = "";
                        if (item.status === "Paid") {
                            if (item.paymentMethod === 0) {
                                methodBadge = `<span class="badge bg-success"><i class="fas fa-money-bill-wave me-1"></i>Cash ${item.paymentReference ? '(' + item.paymentReference + ')' : ''}</span>`;
                            } else if (item.paymentMethod === 1) {
                                methodBadge = `<span class="badge bg-warning text-dark"><i class="fas fa-money-check me-1"></i>Check ${item.bankName ? item.bankName : ''} ${item.paymentReference ? '#' + item.paymentReference : ''}</span>`;
                            } else if (item.paymentMethod === 2) {
                                methodBadge = `<span class="badge bg-info text-white"><i class="fas fa-university me-1"></i>Online ${item.bankName ? item.bankName : ''} ${item.paymentReference ? '(' + item.paymentReference + ')' : ''}</span>`;
                            } else {
                                methodBadge = `<span class="badge bg-success">Complete</span>`;
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

function payLeaseInstallment(installmentId, remainingAmount) {
    Swal.fire({
        title: 'Pay Installment',
        html: `
            <div class="text-start">
                <div class="mb-3">
                    <label class="form-label small fw-bold">Payment Amount (Max: PKR ${remainingAmount.toFixed(2)})</label>
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
        confirmButtonText: 'Pay Now',
        showLoaderOnConfirm: true,
        preConfirm: () => {
            var amount = parseFloat($('#swalInstallmentAmount').val());
            if (!amount || amount <= 0 || amount > remainingAmount) {
                Swal.showValidationMessage('Please enter a valid payment amount up to PKR ' + remainingAmount.toFixed(2));
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
