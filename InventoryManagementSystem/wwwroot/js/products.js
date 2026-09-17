var productsTable;
var categoryTable;
var currentTypeTags = [];

$(document).ready(function () {
    // Initialize Products DataTable
    if ($("#productsTable").length) {
        productsTable = $("#productsTable").DataTable({
            "ajax": {
                "url": "/Products/GetProductsData",
                "type": "GET",
                "datatype": "json"
            },
            "columns": [
                { 
                    "data": "sku",
                    "width": "14%",
                    "className": "text-center align-middle",
                    "render": function(data) {
                        return `<span class="fw-bold text-nowrap">${data}</span>`;
                    }
                },
                { 
                    "data": "name",
                    "width": "22%",
                    "className": "text-center align-middle",
                    "render": function(data, type, row) {
                        var variantText = (row.variant && row.variant !== 'N/A' && row.variant !== 'Standard') 
                            ? `<small class="text-muted d-block mt-1">${row.variant}</small>` 
                            : '';
                        return `<div>
                                    <div class="fw-bold text-dark">${data}</div>
                                    ${variantText}
                                </div>`;
                    }
                },
                { 
                    "data": "categoryName", 
                    "width": "18%",
                    "className": "text-center align-middle",
                    "render": function(d, type, row) {
                        var catBadge = `<span class="badge bg-secondary mb-1">${d || 'General Stock'}</span>`;
                        var typeBadge = `<span class="badge bg-primary text-white d-block mx-auto" style="width: max-content;"><i class="fas fa-tag me-1"></i>${row.productType || 'Standard'}</span>`;
                        return `<div class="d-flex flex-column align-items-center">${catBadge}${typeBadge}</div>`;
                    }
                },
                { 
                    "data": "description",
                    "width": "22%",
                    "className": "text-center align-middle",
                    "render": function(data) {
                        if (!data) return `<span class="text-muted small">N/A</span>`;
                        return `<small class="text-muted d-block">${data}</small>`;
                    }
                },
                { 
                    "data": "price",
                    "width": "10%",
                    "className": "text-center align-middle text-nowrap",
                    "render": function(data) {
                        if (data === null || data === undefined || data === "" || isNaN(parseFloat(data))) {
                            return '<span class="text-muted">N/A</span>';
                        }
                        return `<span class="fw-bold text-dark">PKR ${parseFloat(data).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</span>`;
                    }
                },
                { 
                    "data": "stockQuantity",
                    "width": "10%",
                    "className": "text-center align-middle text-nowrap",
                    "render": function(data) {
                        var qty = parseInt(data) || 0;
                        if (qty === 0) {
                            return `<span class="badge bg-danger"><i class="fas fa-circle-xmark me-1"></i>0 (Out of Stock)</span>`;
                        } else if (qty <= 5) {
                            return `<span class="badge bg-warning text-dark"><i class="fas fa-triangle-exclamation me-1"></i>${qty} (Low Stock)</span>`;
                        } else if (qty >= 50) {
                            return `<span class="badge bg-info text-dark fw-bold"><i class="fas fa-boxes-stacked me-1"></i>${qty} (Overbought)</span>`;
                        } else {
                            return `<span class="badge bg-success"><i class="fas fa-check me-1"></i>${qty} Units</span>`;
                        }
                    }
                },
                {
                    "data": "id",
                    "width": "8%",
                    "className": "text-center align-middle text-nowrap",
                    "render": function (data) {
                        return `
                            <div class="d-inline-flex gap-1 text-nowrap justify-content-center">
                                <button class="btn btn-sm btn-dark" onclick="openEditModal(${data})" title="Edit Product">
                                    <i class="fas fa-edit me-1"></i>Edit
                                </button>
                                <button class="btn btn-sm btn-danger" onclick="deleteProduct(${data})" title="Delete Product">
                                    <i class="fas fa-trash me-1"></i>Delete
                                </button>
                            </div>
                        `;
                    },
                    "orderable": false
                }
            ],
            "language": {
                "emptyTable": "No products found in catalog"
            }
        });
    }

    // Initialize Categories DataTable
    if ($("#categoriesTable").length) {
        categoryTable = $("#categoriesTable").DataTable({
            "ajax": {
                "url": "/MasterSettings/GetCategoriesData",
                "type": "GET",
                "datatype": "json"
            },
            "columns": [
                {
                    "data": "name",
                    "width": "20%",
                    "render": function (d) {
                        return `<strong class="text-dark"><i class="fas fa-folder text-warning me-2"></i>${d}</strong>`;
                    }
                },
                {
                    "data": "description",
                    "width": "25%",
                    "render": function (d) {
                        return d ? d : '<span class="text-muted fst-italic">No description</span>';
                    }
                },
                {
                    "data": "typeOptions",
                    "width": "35%",
                    "render": function (types) {
                        if (!types || types.length === 0) {
                            return '<span class="text-muted fst-italic small">No dynamic types configured</span>';
                        }
                        var listItems = types.map(function (t) {
                            return `<li class="py-1 d-flex align-items-center text-dark"><i class="fas fa-check-circle text-primary me-2" style="font-size: 0.78rem;"></i><span>${t}</span></li>`;
                        }).join('');
                        return `<ul class="list-unstyled mb-0 small">${listItems}</ul>`;
                    }
                },
                {
                    "data": "productCount",
                    "className": "text-center",
                    "render": function (d) {
                        return `<span class="badge ${d > 0 ? 'bg-info text-dark' : 'bg-secondary'}">${d} Product(s)</span>`;
                    },
                    "width": "10%"
                },
                {
                    "data": "id",
                    "render": function (data) {
                        return `
                            <div class="text-center text-nowrap">
                                <button class="btn btn-sm btn-dark me-1" onclick="openCategoryModal(${data})" title="Edit Category & Types">
                                    <i class="fas fa-edit"></i>
                                </button>
                                <button class="btn btn-sm btn-danger" onclick="deleteCategory(${data})" title="Delete Category">
                                    <i class="fas fa-trash"></i>
                                </button>
                            </div>
                        `;
                    },
                    "orderable": false,
                    "width": "10%"
                }
            ]
        });
    }

    // Dynamic Category -> Type Selector in Product Modal
    $("#productCategorySelect").on("change", function () {
        var selectedCat = $(this).val();
        loadTypesForCategory(selectedCat);
    });

    // Add Additional Variant Row
    $("#btnAddVariantRow").on("click", function () {
        addVariantRow();
    });

    // Save Product Form Handler
    $("#productForm").on("submit", function (e) {
        e.preventDefault();

        var id = parseInt($("#productId").val()) || 0;
        var url = id === 0 ? "/Products/Create" : "/Products/Edit/" + id;

        var additionalVariants = [];
        $(".variant-row").each(function () {
            var vName = $(this).find(".var-name").val().trim();
            var vSku = $(this).find(".var-sku").val().trim();
            if (vName && vSku) {
                additionalVariants.push({
                    VariantName: vName,
                    Sku: vSku
                });
            }
        });

        var productData = {
            Id: id,
            Name: $("#productName").val().trim(),
            CategoryName: $("#productCategorySelect").val(),
            ProductType: $("#productTypeSelect").val(),
            Sku: $("#productSku").val().trim(),
            Variant: $("#productVariant").val().trim() || "Standard",
            Description: $("#description").val().trim(),
            StockQuantity: parseInt($("#stockQuantity").val()) || 0,
            AdditionalVariants: additionalVariants
        };

        var btn = $("#btnSaveProduct");
        var originalText = btn.html();
        btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

        $.ajax({
            url: url,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(productData),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false).html(originalText);
                if (response.success) {
                    $("#productModal").modal("hide");
                    if (productsTable) {
                        productsTable.ajax.reload(null, false);
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

    // Save Category Form Handler
    $("#categoryForm").on("submit", function (e) {
        e.preventDefault();
        var name = $("#catName").val().trim();
        if (!name) {
            Swal.fire({
                title: 'Required Field Missing',
                text: 'Category Name is required.',
                icon: 'warning',
                confirmButtonColor: '#3085d6'
            });
            return;
        }

        var model = {
            Id: parseInt($("#catId").val()) || 0,
            Name: name,
            Description: $("#catDescription").val().trim(),
            TypeOptions: currentTypeTags
        };

        var btn = $("#btnSaveCategory");
        var originalText = btn.html();
        btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

        $.ajax({
            url: "/MasterSettings/SaveCategory",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(model),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false).html(originalText);
                if (response.success) {
                    $("#categoryModal").modal("hide");
                    if (categoryTable) {
                        categoryTable.ajax.reload(null, false);
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
                var msg = xhr.responseJSON?.message || "An unexpected error occurred.";
                Swal.fire({
                    title: 'Error!',
                    text: msg,
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
            }
        });
    });

    // Inventory Module Tab Routing & Lazy Load
    $('#inventoryTabs button').on('shown.bs.tab', function (e) {
        var rawTarget = $(e.target).data('bs-target') || '';
        var targetId = rawTarget.replace('#', '');
        if (history.replaceState) {
            var urlParams = new URLSearchParams(window.location.search);
            urlParams.set('tab', targetId);
            var newUrl = window.location.protocol + "//" + window.location.host + window.location.pathname + '?' + urlParams.toString();
            window.history.replaceState({ path: newUrl }, '', newUrl);
        }
        if (targetId === 'products' && productsTable) {
            productsTable.ajax.reload(null, false);
        } else if (targetId === 'categories' && categoryTable) {
            categoryTable.ajax.reload(null, false);
        }
    });

    // Activate tab from URL query param
    var urlParams = new URLSearchParams(window.location.search);
    var tabParam = urlParams.get('tab') || 'products';
    var tabButton = $('#' + tabParam + '-tab');
    if (tabButton.length) {
        bootstrap.Tab.getOrCreateInstance(tabButton[0]).show();
    }
});

function loadCategories(selectedCategory, selectedType) {
    $.get("/Products/GetCategoriesWithTypes", function (data) {
        var catSelect = $("#productCategorySelect");
        catSelect.empty();
        catSelect.append('<option value="">-- Select Category --</option>');

        window.cachedCategories = data || [];

        data.forEach(function (cat) {
            var isSelected = (cat.name === selectedCategory) ? "selected" : "";
            catSelect.append(`<option value="${cat.name}" ${isSelected}>${cat.name}</option>`);
        });

        if (selectedCategory) {
            loadTypesForCategory(selectedCategory, selectedType);
        } else {
            $("#productTypeSelect").empty().append('<option value="">Select Category First</option>');
        }
    });
}

function loadTypesForCategory(categoryName, selectedType) {
    var typeSelect = $("#productTypeSelect");
    typeSelect.empty();

    if (!categoryName || !window.cachedCategories) {
        typeSelect.append('<option value="">Select Category First</option>');
        return;
    }

    var cat = window.cachedCategories.find(c => c.name === categoryName);
    if (cat && cat.typeOptions && cat.typeOptions.length > 0) {
        typeSelect.append('<option value="">-- Select Type / Condition --</option>');
        cat.typeOptions.forEach(function (opt) {
            var isSelected = (opt === selectedType) ? "selected" : "";
            typeSelect.append(`<option value="${opt}" ${isSelected}>${opt}</option>`);
        });
    } else {
        typeSelect.append('<option value="Standard" selected>Standard</option>');
    }
}

function addVariantRow(name, sku) {
    var rowId = "var_row_" + Date.now() + "_" + Math.floor(Math.random() * 100);
    var rowHtml = `
        <div class="row g-2 mb-2 variant-row align-items-center" id="${rowId}">
            <div class="col-md-5">
                <input class="form-control form-control-sm var-name" placeholder="Variant Name (e.g. Blue, XL)" value="${name || ''}" required />
            </div>
            <div class="col-md-5">
                <input class="form-control form-control-sm var-sku" placeholder="Variant SKU (Unique)" value="${sku || ''}" required />
            </div>
            <div class="col-md-2 text-center">
                <button type="button" class="btn btn-outline-danger btn-sm w-100" onclick="$('#${rowId}').remove()">
                    <i class="fas fa-trash"></i>
                </button>
            </div>
        </div>
    `;
    $("#additionalVariantsContainer").append(rowHtml);
}

function openCreateModal() {
    $("#productForm")[0].reset();
    $("#productId").val(0);
    $("#productModalLabel").text("Add Product");
    $("#additionalVariantsContainer").empty();
    $("#additionalVariantsSection").removeClass("d-none");
    loadCategories(null, null);
    $("#productModal").modal("show");
}

function openEditModal(id) {
    $("#productForm")[0].reset();
    $("#productId").val(id);
    $("#productModalLabel").text("Edit Product");
    $("#additionalVariantsContainer").empty();
    $("#additionalVariantsSection").addClass("d-none");

    $.get("/Products/GetProduct/" + id, function (data) {
        $("#productName").val(data.name);
        $("#productSku").val(data.sku);
        $("#productVariant").val(data.variant);
        $("#description").val(data.description);
        $("#stockQuantity").val(data.stockQuantity);

        loadCategories(data.categoryName, data.productType);
        $("#productModal").modal("show");
    });
}

function deleteProduct(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "You won't be able to revert this!",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/Products/Delete/" + id,
                type: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                success: function (response) {
                    if (response.success) {
                        if (productsTable) {
                            productsTable.ajax.reload(null, false);
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
                    var msg = xhr.responseJSON?.message || "An unexpected error occurred.";
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

// Category Tag Management & Modal
function openCategoryModal(id) {
    $("#categoryForm")[0].reset();
    $("#catId").val(0);
    $("#catTypeInput").val("");
    currentTypeTags = [];
    renderTypeTags();
    $("#categoryModalLabel").html('<i class="fas fa-tags me-2 text-primary"></i>Add Category');

    if (id > 0) {
        $.get("/MasterSettings/GetCategory/" + id, function (data) {
            $("#catId").val(data.id);
            $("#catName").val(data.name);
            $("#catDescription").val(data.description || "");
            currentTypeTags = data.typeOptions || [];
            renderTypeTags();

            $("#categoryModalLabel").html('<i class="fas fa-edit me-2 text-primary"></i>Edit Category');
            $("#categoryModal").modal("show");
        });
    } else {
        $("#categoryModal").modal("show");
    }
}

function renderTypeTags() {
    var container = $("#typeTagsContainer");
    container.empty();
    $("#typeCountBadge").text(currentTypeTags ? currentTypeTags.length : 0);

    if (!currentTypeTags || currentTypeTags.length === 0) {
        container.html(`
            <div class="list-group-item text-center py-3 text-muted small" id="noTypesNotice">
                <i class="fas fa-info-circle me-1"></i> No types added yet. Use the input above to append options.
            </div>
        `);
        return;
    }

    currentTypeTags.forEach(function (tag, index) {
        var item = $(`
            <div class="list-group-item d-flex justify-content-between align-items-center py-2 px-3 bg-white">
                <div class="d-flex align-items-center">
                    <span class="badge bg-secondary rounded-pill me-2 text-white" style="font-size: 0.75rem; width: 22px; height: 22px; display: inline-flex; align-items: center; justify-content: center;">${index + 1}</span>
                    <span class="fw-medium text-dark">${tag}</span>
                </div>
                <button type="button" class="btn btn-sm btn-outline-danger py-0 px-2 rounded-circle" onclick="removeTypeTag(${index})" title="Remove Option" style="width: 28px; height: 28px; line-height: 26px;">
                    <i class="fas fa-trash-alt" style="font-size: 0.75rem;"></i>
                </button>
            </div>
        `);
        container.append(item);
    });
}

function addTypeTagFromInput() {
    var input = $("#catTypeInput");
    var val = input.val().trim();
    if (!val) return;

    var exists = currentTypeTags.some(function (t) { return t.toLowerCase() === val.toLowerCase(); });
    if (!exists) {
        currentTypeTags.push(val);
        renderTypeTags();
    }
    input.val("").focus();
}

function removeTypeTag(index) {
    currentTypeTags.splice(index, 1);
    renderTypeTags();
}

$(document).on("keydown", "#catTypeInput", function (e) {
    if (e.key === "Enter" || e.keyCode === 13) {
        e.preventDefault();
        addTypeTagFromInput();
    }
});

function deleteCategory(id) {
    Swal.fire({
        title: 'Are you sure?',
        text: "This will delete the category and its dynamic type options.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/MasterSettings/DeleteCategory/" + id,
                type: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                success: function (response) {
                    if (response.success) {
                        if (categoryTable) {
                            categoryTable.ajax.reload(null, false);
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
                    var msg = xhr.responseJSON?.message || "An unexpected error occurred.";
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
