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
            { "data": "fullName" },
            { "data": "email" },
            { 
                "data": "phoneNumber",
                "render": function(data) {
                    return data ? data : "N/A";
                }
            },
            { "data": "createdAt" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center">
                            <button class="btn btn-sm btn-dark me-1" onclick="openEditModal('${data}')">
                                <i class="fas fa-edit"></i> Edit
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteCustomer('${data}')">
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
            Phone: $("#customerPhone").val()
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
