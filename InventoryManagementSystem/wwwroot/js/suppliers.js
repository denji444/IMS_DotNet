var suppliersTable;

$(document).ready(function () {
    // Initialize DataTable
    if ($("#suppliersTable").length) {
        suppliersTable = $("#suppliersTable").DataTable({
        "ajax": {
            "url": "/Suppliers/GetSuppliersData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { 
                "data": "name", 
                "width": "18%",
                "className": "text-center align-middle",
                "render": function(data) {
                    return `<div class="fw-bold text-dark fs-6">${data}</div>`;
                }
            },
            { 
                "data": "products",
                "width": "34%",
                "className": "text-center align-middle",
                "render": function(data) {
                    if (!data || data.length === 0) {
                        return '<span class="badge bg-secondary">None</span>';
                    }
                    var listItems = data.map(function(p) {
                        var prodName = p.variant ? `${p.name} (${p.variant})` : p.name;
                        var skuTag = p.sku ? `<span class="badge bg-light text-dark border ms-1"><i class="fas fa-barcode text-muted me-1"></i>${p.sku}</span>` : '';
                        return `<li class="mb-1"><span class="fw-medium">${prodName}</span> ${skuTag}</li>`;
                    }).join('');
                    return `<ul class="mb-0 ps-3 small text-start d-inline-block">${listItems}</ul>`;
                }
            },
            { 
                "data": "email",
                "width": "20%",
                "className": "text-center align-middle",
                "render": function(data, type, row) {
                    var badgeClass = row.isEmailVerified ? "bg-success" : "bg-secondary";
                    var badgeText = row.isEmailVerified 
                        ? '<i class="fas fa-check-circle me-1"></i>Verified' 
                        : '<i class="fas fa-clock me-1"></i>Unverified';
                    var phoneText = row.phone ? `<div class="small text-muted mt-1"><i class="fas fa-phone me-1"></i>${row.phone}</div>` : '';
                    return `
                        <div>
                            <div class="fw-medium">${data}</div>
                            <span class="badge ${badgeClass} mt-1" style="font-size: 0.7rem;">${badgeText}</span>
                            ${phoneText}
                        </div>
                    `;
                }
            },
            { 
                "data": "cnic",
                "width": "18%",
                "className": "text-center align-middle",
                "render": function(data, type, row) {
                    var cnicBadge = data 
                        ? `<span class="badge bg-light text-dark border"><i class="fas fa-id-card me-1 text-primary"></i>${data}</span>` 
                        : '<span class="badge bg-light text-secondary">No CNIC</span>';
                    var addrText = row.address ? `<div class="small text-muted mt-1 text-truncate mx-auto" style="max-width: 180px;" title="${row.address}">${row.address}</div>` : '';
                    return `<div>${cnicBadge}${addrText}</div>`;
                }
            },
            {
                "data": "id",
                "width": "10%",
                "className": "text-center align-middle text-nowrap",
                "render": function (data) {
                    return `
                        <div class="d-inline-flex gap-1 text-nowrap justify-content-center">
                            <button class="btn btn-sm btn-dark" onclick="openSupplierEditModal(${data})" title="Edit Supplier">
                                <i class="fas fa-edit me-1"></i>Edit
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteSupplier(${data})" title="Delete Supplier">
                                <i class="fas fa-trash me-1"></i>Delete
                            </button>
                        </div>
                    `;
                },
                "orderable": false
            }
        ],
        "language": {
            "emptyTable": "No suppliers found. Click 'Add Supplier' to create one."
        }
    });
    }



    // Form submit AJAX handler
    $("#supplierForm").on("submit", function (e) {
        e.preventDefault();
        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        var id = parseInt($("#supplierId").val());
        var url = id === 0 ? "/Suppliers/Create" : "/Suppliers/Edit/" + id;
        
        var supplierData = {
            Id: id,
            Name: $("#supplierName").val(),
            ContactName: $("#supplierName").val(),
            Email: $("#email").val(),
            Phone: $("#phone").val(),
            Cnic: $("#supplierCnic").val(),
            Address: $("#address").val()
        };

        var btn = $("#btnSaveSupplier");
        var originalText = btn.html();
        btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

        $.ajax({
            url: url,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(supplierData),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false).html(originalText);
                if (response.success) {
                    $("#supplierModal").modal("hide");
                    if (suppliersTable) {
                        suppliersTable.ajax.reload(null, false);
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
});

function openSupplierCreateModal() {
    // Reset Form
    $("#supplierForm")[0].reset();
    $("#supplierId").val(0);
    $("#supplierCnic").val("");
    $(".text-danger").text(""); // Clear validation errors
    $("#supplierModalLabel").text("Add Supplier");
    $("#supplierModal").modal("show");
}

function openSupplierEditModal(id) {
    $(".text-danger").text(""); // Clear validation errors
    $.ajax({
        url: "/Suppliers/GetSupplier/" + id,
        type: "GET",
        success: function (data) {
            $("#supplierId").val(data.id);
            $("#supplierName").val(data.name);
            $("#email").val(data.email);
            $("#phone").val(data.phone);
            $("#supplierCnic").val(data.cnic || "");
            $("#address").val(data.address);

            $("#supplierModalLabel").text("Edit Supplier");
            $("#supplierModal").modal("show");
        },
        error: function () {
            Swal.fire({
                title: 'Error!',
                text: 'Could not fetch supplier details.',
                icon: 'error',
                confirmButtonColor: '#d33'
            });
        }
    });
}

function deleteSupplier(id) {
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
                url: "/Suppliers/Delete/" + id,
                type: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                },
                success: function (response) {
                    if (response.success) {
                        if (suppliersTable) {
                            suppliersTable.ajax.reload(null, false);
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
