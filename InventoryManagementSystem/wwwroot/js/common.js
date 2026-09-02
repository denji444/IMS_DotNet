// Global IMS JS Utilities
if (window.jQuery && $.fn && $.fn.dataTable) {
    var defaultExportOptions = {
        columns: function (idx, data, node) {
            var text = $(node).text().trim().toLowerCase();
            return text !== 'actions' && text !== 'action' && !$(node).hasClass('no-export');
        }
    };

    // Override DataTables Buttons default class to prevent mixing btn-secondary
    if ($.fn.dataTable.Buttons && $.fn.dataTable.Buttons.defaults) {
        $.fn.dataTable.Buttons.defaults.dom.button.className = 'btn';
    }

    $.extend(true, $.fn.dataTable.defaults, {
        dom: "<'row mb-3 align-items-center'<'col-md-6 d-flex align-items-center gap-2'B><'col-md-6'f>>" +
             "<'row'<'col-sm-12'tr>>" +
             "<'row mt-3 align-items-center'<'col-md-5'i><'col-md-7'p>>",
        buttons: [
            {
                extend: 'collection',
                text: '<i class="fas fa-download me-1"></i> Export',
                className: 'btn btn-outline-dark btn-sm dropdown-toggle',
                buttons: [
                    {
                        extend: 'copy',
                        text: '<i class="fas fa-copy me-2 text-primary"></i>Copy',
                        exportOptions: defaultExportOptions
                    },
                    {
                        extend: 'excel',
                        text: '<i class="fas fa-file-excel me-2 text-success"></i>Excel',
                        exportOptions: defaultExportOptions
                    },
                    {
                        extend: 'csv',
                        text: '<i class="fas fa-file-csv me-2 text-info"></i>CSV',
                        exportOptions: defaultExportOptions
                    },
                    {
                        extend: 'print',
                        text: '<i class="fas fa-print me-2 text-dark"></i>Print',
                        exportOptions: defaultExportOptions
                    }
                ]
            }
        ]
    });
}

$(document).ready(function () {
    // Enable Select2 Bootstrap 5 theme defaults if select2 is loaded
    if ($.fn.select2) {
        $.fn.select2.defaults.set("theme", "bootstrap-5");
    }

    // Add anti-forgery token to all AJAX requests automatically if present
    var token = $('input[name="__RequestVerificationToken"]').val();
    if (token) {
        $.ajaxSetup({
            headers: {
                'RequestVerificationToken': token
            }
        });
    }
});

// Helper for Ajax forms submission with SweetAlert2 integration
function handleFormSubmitAjax(formSelector, url, redirectUrlOnSuccess, successMessage) {
    $(formSelector).on("submit", function (e) {
        e.preventDefault();
        var form = $(this);

        if (!form.valid()) {
            return false;
        }

        var submitBtn = form.find("button[type='submit']");
        var originalHtml = submitBtn.html();
        
        submitBtn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Saving...');

        // Serialize fields as object for application/json submittal
        var dataArray = form.serializeArray();
        var dataObj = {};
        $.each(dataArray, function () {
            if (dataObj[this.name] !== undefined) {
                if (!dataObj[this.name].push) {
                    dataObj[this.name] = [dataObj[this.name]];
                }
                dataObj[this.name].push(this.value || '');
            } else {
                dataObj[this.name] = this.value || '';
            }
        });

        // Remove __RequestVerificationToken from JSON payload since it goes in headers
        delete dataObj["__RequestVerificationToken"];

        $.ajax({
            url: url,
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(dataObj),
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                submitBtn.prop("disabled", false).html(originalHtml);
                if (response.success) {
                    Swal.fire({
                        title: 'Success!',
                        text: successMessage || response.message || 'Action completed successfully!',
                        icon: 'success',
                        confirmButtonColor: '#3085d6'
                    }).then(function () {
                        if (redirectUrlOnSuccess) {
                            window.location.href = redirectUrlOnSuccess;
                        } else {
                            location.reload();
                        }
                    });
                } else {
                    Swal.fire({
                        title: 'Error!',
                        text: response.message || 'Operation failed.',
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
                }
            },
            error: function (xhr) {
                submitBtn.prop("disabled", false).html(originalHtml);
                var errorMsg = "An unexpected error occurred.";
                if (xhr.responseJSON && xhr.responseJSON.message) {
                    errorMsg = xhr.responseJSON.message;
                } else if (xhr.responseText) {
                    try {
                        var parsed = JSON.parse(xhr.responseText);
                        if (parsed && parsed.message) errorMsg = parsed.message;
                    } catch (e) {}
                }
                Swal.fire({
                    title: 'Error!',
                    text: errorMsg,
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
            }
        });
    });
}
