var table;

$(document).ready(function () {
    // Initialize DataTables
    table = $("#productsTable").DataTable({
        "ajax": {
            "url": "/Products/GetProductsData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "id", "width": "5%" },
            { "data": "sku" },
            { "data": "name" },
            { 
                "data": "categoryName", 
                "render": function(d) {
                    return `<span class="badge bg-secondary">${d || 'General Stock'}</span>`;
                }
            },
            { 
                "data": "productType", 
                "render": function(d) {
                    return `<span class="badge bg-primary text-white"><i class="fas fa-tag me-1"></i>${d || 'Standard'}</span>`;
                }
            },
            { "data": "variant", "render": function(data) { return data ? data : "N/A"; } },
            { "data": "description" },
            { 
                "data": "price",
                "render": function(data) {
                    if (data === null || data === undefined || data === "" || isNaN(parseFloat(data))) {
                        return 'N/A';
                    }
                    return "PKR " + parseFloat(data).toFixed(2);
                }
            },
            { "data": "stockQuantity" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center">
                            <button class="btn btn-sm btn-dark me-1" onclick="openEditModal(${data})">
                                <i class="fas fa-edit"></i> Edit
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteProduct(${data})">
                                <i class="fas fa-trash"></i> Delete
                            </button>
                        </div>
                    `;
                },
                "orderable": false,
                "width": "20%"
            }
        ],
        "language": {
            "emptyTable": "No products found. Click 'Add Product' to create one."
        }
    });

    // Fetch categories with dynamic type options on page load
    loadCategories();

    // Dynamic type options when Category changes
    $("#productCategorySelect").on("change", function () {
        var catName = $(this).val();
        populateProductTypeOptions(catName);
    });

    // Dynamic row addition events for additional variants
    $("#btnAddVariantRow").on("click", function () {
        addVariantRow();
    });

    $(document).on("click", ".btn-delete-variant-row", function () {
        $(this).closest(".variant-row").remove();
    });

    // Handle modal form submit via AJAX
    $("#productForm").on("submit", function (e) {
        e.preventDefault();
        var form = $(this);
        
        if (!form.valid()) {
            return false;
        }

        var id = parseInt($("#productId").val());
        var isCreate = id === 0;
        var url = isCreate ? "/Products/Create" : "/Products/Edit/" + id;

        var mainSku = $("#productSku").val().trim();
        var mainVariant = $("#productVariant").val().trim();
        var multipleVariants = [];
        var hasErrors = false;

        // Collect and validate additional variants
        $("#additionalVariantsContainer .variant-row").each(function () {
            var rowSku = $(this).find(".variant-sku").val().trim();
            var rowName = $(this).find(".variant-name").val().trim();
            if (!rowSku || !rowName) {
                Swal.fire({
                    title: 'Error!',
                    text: 'All additional variant rows must have SKU and Variant Name filled out.',
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
                hasErrors = true;
                return false;
            }
            if (rowSku === mainSku) {
                Swal.fire({
                    title: 'Error!',
                    text: `Variant SKU '${rowSku}' cannot be the same as the main SKU.`,
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
                hasErrors = true;
                return false;
            }
            multipleVariants.push({
                Sku: rowSku,
                Variant: rowName
            });
        });

        if (hasErrors) {
            return false;
        }

        var priceRaw = $("#price").val();
        var priceVal = (priceRaw !== "" && !isNaN(parseFloat(priceRaw))) ? parseFloat(priceRaw) : null;

        var productData = {
            Id: id,
            Sku: mainSku,
            Name: $("#productName").val(),
            CategoryName: $("#productCategorySelect").val(),
            ProductType: $("#productTypeSelect").val(),
            Variant: mainVariant,
            Description: $("#description").val(),
            Price: priceVal,
            StockQuantity: parseInt($("#stockQuantity").val()),
            MultipleVariants: multipleVariants.length > 0 ? multipleVariants : null
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

function addVariantRow(sku = "", name = "") {
    var row = `
    <div class="row g-2 mb-2 variant-row align-items-center">
        <div class="col-md-5">
            <div class="form-floating">
                <input class="form-control variant-sku" placeholder="SKU" value="${sku}" required />
                <label>SKU</label>
            </div>
        </div>
        <div class="col-md-6">
            <div class="form-floating">
                <input class="form-control variant-name" placeholder="Variant (e.g. Red, 8/256)" value="${name}" required />
                <label>Variant Name</label>
            </div>
        </div>
        <div class="col-md-1 text-center">
            <button type="button" class="btn btn-outline-danger btn-sm border-0 btn-delete-variant-row">
                <i class="fas fa-trash"></i>
            </button>
        </div>
    </div>`;
    $("#additionalVariantsContainer").append(row);
}

var categoriesData = [];

function loadCategories(callback) {
    $.get("/Products/GetCategoriesWithTypes", function (categories) {
        categoriesData = categories;
        var $catSelect = $("#productCategorySelect");
        var curr = $catSelect.val();
        $catSelect.empty();
        $catSelect.append('<option value="">-- Select Category --</option>');
        $.each(categories, function (i, cat) {
            $catSelect.append(new Option(cat.name, cat.name));
        });
        if (curr) $catSelect.val(curr);
        if (callback) callback();
    });
}

function populateProductTypeOptions(categoryName, selectedType) {
    var $typeSelect = $("#productTypeSelect");
    $typeSelect.empty();

    if (!categoryName) {
        $typeSelect.append('<option value="">Select Category First</option>');
        return;
    }

    var catObj = categoriesData.find(c => c.name === categoryName);
    var options = catObj ? catObj.typeOptions : [];

    $typeSelect.append('<option value="">-- Select Type / Condition --</option>');
    if (options && options.length > 0) {
        $.each(options, function (i, opt) {
            $typeSelect.append(new Option(opt, opt));
        });
    } else {
        $typeSelect.append('<option value="Standard">Standard</option>');
    }

    if (selectedType) {
        $typeSelect.val(selectedType);
    }
}

function openCreateModal() {
    $("#productForm")[0].reset();
    $("#productId").val(0);
    $(".text-danger").text("");
    $("#additionalVariantsContainer").empty();

    loadCategories(function() {
        $("#productCategorySelect").val("");
        populateProductTypeOptions("");
        $("#productModalLabel").text("Add Product");
        $("#productModal").modal("show");
    });
}

function openEditModal(id) {
    $(".text-danger").text("");
    $("#additionalVariantsContainer").empty();

    $.ajax({
        url: "/Products/GetProduct/" + id,
        type: "GET",
        success: function (data) {
            $("#productId").val(data.id);
            $("#productSku").val(data.sku);
            $("#productName").val(data.name);
            $("#productVariant").val(data.variant);
            $("#description").val(data.description);
            $("#price").val(data.price !== null && data.price !== undefined ? data.price : "");
            $("#stockQuantity").val(data.stockQuantity);

            loadCategories(function() {
                $("#productCategorySelect").val(data.categoryName || "");
                populateProductTypeOptions(data.categoryName, data.productType || "");
                $("#productModalLabel").text("Edit Product");
                $("#productModal").modal("show");
            });
        },
        error: function () {
            Swal.fire({
                title: 'Error!',
                text: 'Could not fetch product details.',
                icon: 'error',
                confirmButtonColor: '#d33'
            });
        }
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
