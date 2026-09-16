var table;

$(document).ready(function () {
    // Initialize DataTable
    table = $("#customersTable").DataTable({
        "ajax": {
            "url": "/Customers/GetCustomersData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { 
                "data": "fullName",
                "width": "22%",
                "className": "text-center align-middle",
                "render": function(data) {
                    return `<div class="fw-bold text-dark fs-6">${data}</div>`;
                }
            },
            { 
                "data": "email",
                "width": "28%",
                "className": "text-center align-middle",
                "render": function(data, type, row) {
                    var phoneText = row.phoneNumber ? `<div class="small text-muted mt-1"><i class="fas fa-phone me-1"></i>${row.phoneNumber}</div>` : '';
                    return `<div>
                                <div class="fw-medium">${data}</div>
                                ${phoneText}
                            </div>`;
                }
            },
            {
                "data": "cnic",
                "width": "20%",
                "className": "text-center align-middle text-nowrap",
                "render": function(data) {
                    return data ? `<span class="badge bg-light text-dark border"><i class="fas fa-id-card me-1 text-primary"></i>${data}</span>` : '<span class="badge bg-light text-secondary">N/A</span>';
                }
            },
            { 
                "data": "createdAt",
                "width": "18%",
                "className": "text-center align-middle text-nowrap",
                "render": function(data) {
                    return `<small class="text-muted">${data}</small>`;
                }
            },
            {
                "data": "id",
                "width": "12%",
                "className": "text-center align-middle text-nowrap",
                "render": function (data) {
                    return `
                        <div class="d-inline-flex gap-1 text-nowrap justify-content-center">
                            <button class="btn btn-sm btn-dark" onclick="openEditModal('${data}')" title="Edit Customer">
                                <i class="fas fa-edit me-1"></i>Edit
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteCustomer('${data}')" title="Delete Customer">
                                <i class="fas fa-trash me-1"></i>Delete
                            </button>
                        </div>
                    `;
                },
                "orderable": false
            }
        ],
        "language": {
            "emptyTable": "No customers found."
        }
    });



    // Form submit AJAX
    $("#customerForm").on("submit", function (e) {
        e.preventDefault();
        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        var id = $("#customerId").val();
        var isCreate = id === "";
        var url = isCreate ? "/Customers/Create" : "/Customers/Edit/" + id;

        var customerData = {
            Id: id,
            FirstName: $("#customerFirstName").val(),
            LastName: $("#customerLastName").val(),
            Email: $("#customerEmail").val(),
            Phone: $("#customerPhone").val(),
            Cnic: $("#customerCnic").val()
        };

        var btn = $("#btnSaveCustomer");
        var originalText = btn.html();
        btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

        $.ajax({
            url: url,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(customerData),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false).html(originalText);
                if (response.success) {
                    $("#customerModal").modal("hide");
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
    $("#customerForm")[0].reset();
    $("#customerId").val("");
    $("#customerCnic").val("");
    $(".text-danger").text("");
    $("#customerModalLabel").text("Add Customer");
    $("#customerModal").modal("show");
}

function openEditModal(id) {
    $(".text-danger").text("");
    $.ajax({
        url: "/Customers/GetCustomer/" + id,
        type: "GET",
        success: function (data) {
            $("#customerId").val(data.id);
            $("#customerFirstName").val(data.firstName);
            $("#customerLastName").val(data.lastName);
            $("#customerEmail").val(data.email);
            $("#customerPhone").val(data.phoneNumber);
            $("#customerCnic").val(data.cnic || "");

            $("#customerModalLabel").text("Edit Customer");
            $("#customerModal").modal("show");
        },
        error: function () {
            Swal.fire({
                title: 'Error!',
                text: 'Could not fetch customer details.',
                icon: 'error',
                confirmButtonColor: '#d33'
            });
        }
    });
}

function deleteCustomer(id) {
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
                url: "/Customers/Delete/" + id,
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
