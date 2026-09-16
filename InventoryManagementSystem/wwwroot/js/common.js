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
                        exportOptions: defaultExportOptions,
                        title: '',
                        customize: function (win) {
                            var doc = win.document;
                            var now = new Date();
                            var dateStr = now.toLocaleDateString('en-US', {
                                year: 'numeric',
                                month: 'short',
                                day: 'numeric',
                                hour: '2-digit',
                                minute: '2-digit'
                            });

                            var rawTitle = document.title || 'System Report';
                            var pageTitle = rawTitle.split('-')[0].trim();
                            var rowCount = $(doc.body).find('table.dataTable tbody tr').length;

                            // Inject custom B&W Print CSS
                            $(doc.head).append(`
                                <style>
                                    body {
                                        color: #000000 !important;
                                        margin: 15px !important;
                                        padding: 0 !important;
                                        background: #ffffff !important;
                                    }
                                    .print-container {
                                        width: 100%;
                                    }
                                    .print-header {
                                        border-bottom: 3px double #000000;
                                        padding-bottom: 10px;
                                        margin-bottom: 15px;
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: flex-end;
                                    }
                                    .print-brand {
                                        font-size: 22px;
                                        font-weight: 800;
                                        color: #000000;
                                        text-transform: uppercase;
                                        letter-spacing: 1px;
                                        line-height: 1.1;
                                    }
                                    .print-sub {
                                        font-size: 13px;
                                        color: #333333;
                                        margin-top: 4px;
                                        font-weight: 600;
                                    }
                                    .print-meta-box {
                                        border: 1px solid #000000;
                                        padding: 6px 12px;
                                        font-size: 11px;
                                        line-height: 1.5;
                                        background-color: #fafafa;
                                    }
                                    .print-summary-bar {
                                        display: flex;
                                        justify-content: space-between;
                                        font-size: 11px;
                                        font-weight: 600;
                                        color: #000000;
                                        margin-bottom: 8px;
                                        padding: 4px 0;
                                        border-bottom: 1px solid #000000;
                                    }
                                    table.dataTable {
                                        width: 100% !important;
                                        border-collapse: collapse !important;
                                        margin-top: 10px !important;
                                        margin-bottom: 20px !important;
                                    }
                                    table.dataTable thead {
                                        display: table-header-group !important;
                                    }
                                    table.dataTable thead th {
                                        background-color: #f1f5f9 !important;
                                        color: #000000 !important;
                                        font-weight: 700 !important;
                                        font-size: 11px !important;
                                        text-transform: uppercase !important;
                                        letter-spacing: 0.5px !important;
                                        padding: 8px 10px !important;
                                        border-top: 2px solid #000000 !important;
                                        border-bottom: 2px solid #000000 !important;
                                        border-left: 1px solid #cbd5e1 !important;
                                        border-right: 1px solid #cbd5e1 !important;
                                    }
                                    table.dataTable tbody tr {
                                        page-break-inside: avoid !important;
                                    }
                                    table.dataTable tbody td {
                                        padding: 7px 10px !important;
                                        font-size: 11px !important;
                                        border: 1px solid #cbd5e1 !important;
                                        color: #000000 !important;
                                    }
                                    table.dataTable tbody tr:nth-child(even) {
                                        background-color: #f8fafc !important;
                                    }
                                    .print-sign-row {
                                        margin-top: 40px;
                                        display: flex;
                                        justify-content: space-between;
                                        font-size: 11px;
                                        font-weight: 600;
                                        color: #000000;
                                    }
                                    .print-sign-line {
                                        border-top: 1px solid #000000;
                                        width: 200px;
                                        text-align: center;
                                        padding-top: 4px;
                                    }
                                    .print-footer {
                                        margin-top: 25px;
                                        padding-top: 8px;
                                        border-top: 1px solid #000000;
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: center;
                                        font-size: 10px;
                                        color: #475569;
                                    }
                                    @media print {
                                        body { margin: 0 !important; }
                                    }
                                </style>
                            `);

                            // Inject Header Banner & Meta Summary
                            $(doc.body).prepend(`
                                <div class="print-container">
                                    <div class="print-header">
                                        <div>
                                            <div class="print-brand">IMS PORTAL</div>
                                            <div class="print-sub">INVENTORY MANAGEMENT SYSTEM &bull; ${pageTitle.toUpperCase()}</div>
                                        </div>
                                        <div class="print-meta-box">
                                            <div><strong>DATE:</strong> ${dateStr}</div>
                                            <div><strong>DOCUMENT:</strong> OFFICIAL SYSTEM REPORT</div>
                                        </div>
                                    </div>
                                    <div class="print-summary-bar">
                                        <div>REPORT: ${pageTitle.toUpperCase()}</div>
                                        <div>TOTAL ENTRIES: ${rowCount}</div>
                                    </div>
                                </div>
                            `);

                            // Inject Signature Block & Footer
                            $(doc.body).append(`
                                <div class="print-sign-row">
                                    <div class="print-sign-line">PREPARED BY</div>
                                    <div class="print-sign-line">AUTHORIZED SIGNATURE</div>
                                </div>
                                <div class="print-footer">
                                    <div>CONFIDENTIAL &bull; INVENTORY MANAGEMENT SYSTEM &bull; AUDIT REPORT</div>
                                    <div>SYSTEM GENERATED DOCUMENT</div>
                                </div>
                            `);
                        }
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

// Global CNIC formatting utility (XXXXX-XXXXXXX-X)
function formatCnic(value) {
    if (!value) return "";
    var cleaned = value.toString().replace(/\D/g, "");
    if (cleaned.length > 13) cleaned = cleaned.substring(0, 13);
    
    if (cleaned.length <= 5) {
        return cleaned;
    } else if (cleaned.length <= 12) {
        return cleaned.substring(0, 5) + "-" + cleaned.substring(5);
    } else {
        return cleaned.substring(0, 5) + "-" + cleaned.substring(5, 12) + "-" + cleaned.substring(12);
    }
}
window.formatCnic = formatCnic;

$(document).on("input", ".cnic-input", function () {
    var formatted = formatCnic(this.value);
    this.value = formatted;
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

// Admin User Profile Management Modal Handler
$(document).ready(function () {
    $("#manageProfileForm").on("submit", function (e) {
        e.preventDefault();
        
        var fullName = $("#profileFullName").val();
        var currentPassword = $("#profileCurrentPassword").val();
        var newPassword = $("#profileNewPassword").val();
        var confirmPassword = $("#profileConfirmPassword").val();

        if (newPassword && newPassword !== confirmPassword) {
            Swal.fire({
                title: 'Error!',
                text: 'New password and confirm password do not match.',
                icon: 'error',
                confirmButtonColor: '#d33'
            });
            return false;
        }

        var model = {
            FullName: fullName,
            CurrentPassword: currentPassword || null,
            NewPassword: newPassword || null
        };

        var btn = $("#btnSaveProfile");
        var originalText = btn.html();
        btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

        var layoutToken = $('input[name="__RequestVerificationToken"]').val();

        $.ajax({
            url: "/Account/UpdateProfile",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(model),
            headers: {
                'RequestVerificationToken': layoutToken
            },
            success: function (response) {
                btn.prop("disabled", false).html(originalText);
                if (response.success) {
                    $("#manageProfileModal").modal("hide");
                    $("#profileCurrentPassword").val("");
                    $("#profileNewPassword").val("");
                    $("#profileConfirmPassword").val("");

                    Swal.fire({
                        title: 'Success!',
                        text: response.message,
                        icon: 'success',
                        confirmButtonColor: '#3085d6'
                    }).then(function() {
                        location.reload();
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
});
