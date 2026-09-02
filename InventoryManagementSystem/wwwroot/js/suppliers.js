var table;

$(document).ready(function () {
    // Initialize DataTable
    table = $("#suppliersTable").DataTable({
        "ajax": {
            "url": "/Suppliers/GetSuppliersData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "id", "width": "5%" },
            { "data": "name", "width": "15%" },
            { 
                "data": "products",
                "render": function(data) {
                    if (!data || data.length === 0) {
                        return '<span class="badge bg-secondary">None</span>';
                    }
                    return data.map(p => `<span class="badge bg-dark me-1">${p}</span>`).join(' ');
                },
                "width": "20%"
            },
            { "data": "contactName", "width": "12%" },
            { 
                "data": "email",
                "render": function(data, type, row) {
                    var badgeClass = row.isEmailVerified ? "bg-success" : "bg-secondary";
                    var badgeText = row.isEmailVerified 
                        ? '<i class="fas fa-check-circle me-1"></i>Verified' 
                        : '<i class="fas fa-clock me-1"></i>Unverified';
                    return `
                        <div>
                            <div>${data}</div>
                            <span class="badge ${badgeClass} mt-1" style="font-size: 0.7rem;">${badgeText}</span>
                        </div>
                    `;
                },
                "width": "15%" 
            },
            { "data": "phone", "width": "10%" },
            { "data": "address", "width": "15%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center text-nowrap">
                            <button class="btn btn-sm btn-dark me-1" onclick="openEditModal(${data})">
                                <i class="fas fa-edit"></i> Edit
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteSupplier(${data})">
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
            "emptyTable": "No suppliers found. Click 'Add Supplier' to create one."
        }
    });

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

function openCreateModal() {
    // Reset Form
    $("#supplierForm")[0].reset();
    $("#supplierId").val(0);
    $(".text-danger").text(""); // Clear validation errors
    $("#supplierModalLabel").text("Add Supplier");
    $("#supplierModal").modal("show");
}

function openEditModal(id) {
    $(".text-danger").text(""); // Clear validation errors
    $.ajax({
        url: "/Suppliers/GetSupplier/" + id,
        type: "GET",
        success: function (data) {
            $("#supplierId").val(data.id);
            $("#supplierName").val(data.name);
            $("#email").val(data.email);
            $("#phone").val(data.phone);
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
