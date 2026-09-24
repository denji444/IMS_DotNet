// Global IMS JS Utilities
if (window.jQuery) {
    // Automatic Anti-Forgery Token Injection for all jQuery AJAX POST/PUT/DELETE requests
    $.ajaxSetup({
        beforeSend: function (xhr, settings) {
            if (!/^(GET|HEAD|OPTIONS|TRACE)$/i.test(settings.type) && !this.crossDomain) {
                var token = $('input[name="__RequestVerificationToken"]').val();
                if (token) {
                    xhr.setRequestHeader('RequestVerificationToken', token);
                }
            }
        }
    });
}

function showToast(msg, icon) {
    if (!window.Swal) return;
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 2000,
        timerProgressBar: true
    });
    Toast.fire({
        icon: icon || 'success',
        title: msg
    });
}

function copyToClipboard(elementId, toastMsg) {
    var text = $(elementId).val() || $(elementId).text();
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text).then(function () {
            showToast(toastMsg || 'Copied to clipboard!');
        });
    } else {
        var input = $(elementId)[0];
        if (input && input.select) input.select();
        document.execCommand('copy');
        showToast(toastMsg || 'Copied to clipboard!');
    }
}

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

                            var profile = window.companyProfile || {};
                            var compName = profile.companyName || 'Inventory Management System (IMS)';
                            var tagline = profile.tagline || 'Smart Inventory, Sales & Enterprise Tracking';
                            var address = profile.address || '';
                            var phone = profile.phone || '';
                            var email = profile.email || '';
                            var logoPath = profile.logoPath || '';

                            function escapeHtml(str) {
                                if (!str) return '';
                                return String(str)
                                    .replace(/&/g, '&amp;')
                                    .replace(/</g, '&lt;')
                                    .replace(/>/g, '&gt;')
                                    .replace(/"/g, '&quot;')
                                    .replace(/'/g, '&#039;');
                            }

                            // Dynamic Logo HTML
                            var logoHtml = '';
                            if (logoPath) {
                                var logoSrc = logoPath;
                                if (!logoSrc.startsWith('http') && !logoSrc.startsWith('data:')) {
                                    logoSrc = window.location.origin + (logoSrc.startsWith('/') ? '' : '/') + logoSrc;
                                }
                                logoHtml = `
                                    <div class="letterhead-logo-img-container">
                                        <img src="${logoSrc}" alt="Company Logo" class="letterhead-logo-img" />
                                    </div>
                                `;
                            } else {
                                logoHtml = `
                                    <div class="letterhead-logo-icon">
                                        <svg width="34" height="34" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                                            <path d="M20 7L12 3L4 7M20 7L12 11M20 7V17L12 21M12 11L4 7M12 11V21M4 7V17L12 21" stroke="white" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
                                        </svg>
                                    </div>
                                `;
                            }

                            // Contact grid HTML
                            var contactItems = [];
                            if (address) {
                                contactItems.push(`
                                    <div class="letterhead-contact-item">
                                        <i class="fas fa-map-marker-alt small"></i>
                                        <span>${escapeHtml(address)}</span>
                                    </div>
                                `);
                            }
                            if (phone) {
                                contactItems.push(`
                                    <div class="letterhead-contact-item">
                                        <i class="fas fa-phone small"></i>
                                        <span>${escapeHtml(phone)}</span>
                                    </div>
                                `);
                            }
                            if (email) {
                                contactItems.push(`
                                    <div class="letterhead-contact-item">
                                        <i class="fas fa-envelope small"></i>
                                        <span>${escapeHtml(email)}</span>
                                    </div>
                                `);
                            }
                            var contactHtml = contactItems.length ? `<div class="letterhead-contact-grid">${contactItems.join('')}</div>` : '';

                            // Document meta box HTML
                            var docBadge = `${escapeHtml(pageTitle.toUpperCase())} REPORT`;
                            var docBoxHtml = `
                                <div class="letterhead-doc-box">
                                    <div class="letterhead-doc-badge">${docBadge}</div>
                                    <div class="letterhead-doc-meta">
                                        <div><strong>EXPORT DATE:</strong> ${dateStr}</div>
                                        <div><strong>TOTAL RECORDS:</strong> ${rowCount}</div>
                                        <div class="small text-muted mt-1">OFFICIAL SYSTEM EXPORT</div>
                                    </div>
                                </div>
                            `;

                            // Inject FontAwesome CDN & Print Letterhead CSS
                            $(doc.head).append(`
                                <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css" />
                                <style>
                                    @media print {
                                        * {
                                            -webkit-print-color-adjust: exact !important;
                                            print-color-adjust: exact !important;
                                        }
                                        body {
                                            margin: 0 !important;
                                            padding: 10px !important;
                                        }
                                    }
                                    body {
                                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif !important;
                                        color: #1e293b !important;
                                        margin: 15px !important;
                                        padding: 0 !important;
                                        background: #ffffff !important;
                                    }
                                    .letterhead-container {
                                        font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                                        color: #1e293b;
                                        margin-bottom: 20px;
                                        position: relative;
                                    }
                                    .letterhead-top-bar {
                                        height: 6px;
                                        background: linear-gradient(90deg, #0f172a 0%, #1e293b 55%, #2563eb 100%);
                                        border-radius: 4px 4px 0 0;
                                        margin-bottom: 16px;
                                    }
                                    .letterhead-body {
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: flex-start;
                                        gap: 20px;
                                    }
                                    .letterhead-brand-section {
                                        display: flex;
                                        align-items: center;
                                        gap: 18px;
                                        flex: 1;
                                    }
                                    .letterhead-logo-icon {
                                        width: 110px;
                                        height: 110px;
                                        background: linear-gradient(135deg, #0f172a 0%, #2563eb 100%);
                                        border-radius: 14px;
                                        display: flex;
                                        align-items: center;
                                        justify-content: center;
                                        box-shadow: 0 4px 10px rgba(37, 99, 235, 0.25);
                                        flex-shrink: 0;
                                    }
                                    .letterhead-logo-img-container {
                                        width: 110px;
                                        height: 110px;
                                        max-width: 110px;
                                        max-height: 110px;
                                        display: flex;
                                        align-items: center;
                                        justify-content: center;
                                        background: transparent;
                                        border: none !important;
                                        box-shadow: none !important;
                                        flex-shrink: 0;
                                    }
                                    .letterhead-logo-img {
                                        max-height: 110px;
                                        max-width: 110px;
                                        width: auto;
                                        height: auto;
                                        object-fit: contain;
                                        display: block;
                                    }
                                    .letterhead-brand-details h2 {
                                        font-size: 1.45rem;
                                        font-weight: 800;
                                        color: #0f172a;
                                        letter-spacing: -0.5px;
                                        margin: 0 0 2px 0;
                                        line-height: 1.2;
                                    }
                                    .letterhead-brand-details .tagline {
                                        font-size: 0.8rem;
                                        font-weight: 600;
                                        color: #2563eb;
                                        text-transform: uppercase;
                                        letter-spacing: 0.8px;
                                        margin-bottom: 6px;
                                    }
                                    .letterhead-contact-grid {
                                        display: flex;
                                        flex-wrap: wrap;
                                        column-gap: 18px;
                                        row-gap: 4px;
                                        font-size: 0.78rem;
                                        color: #475569;
                                        margin-top: 6px;
                                    }
                                    .letterhead-contact-item {
                                        display: flex;
                                        align-items: center;
                                        gap: 5px;
                                    }
                                    .letterhead-contact-item i {
                                        color: #2563eb;
                                    }
                                    .letterhead-doc-box {
                                        text-align: right;
                                        min-width: 240px;
                                        background: #f8fafc;
                                        border: 1px solid #e2e8f0;
                                        border-radius: 8px;
                                        padding: 12px 16px;
                                        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.02);
                                    }
                                    .letterhead-doc-badge {
                                        display: inline-block;
                                        background: #0f172a;
                                        color: #ffffff;
                                        font-size: 0.85rem;
                                        font-weight: 700;
                                        text-transform: uppercase;
                                        letter-spacing: 1px;
                                        padding: 4px 12px;
                                        border-radius: 4px;
                                        margin-bottom: 8px;
                                    }
                                    .letterhead-doc-meta {
                                        font-size: 0.82rem;
                                        color: #334155;
                                        line-height: 1.55;
                                    }
                                    .letterhead-doc-meta strong {
                                        color: #0f172a;
                                    }
                                    .letterhead-divider {
                                        margin-top: 16px;
                                        border: 0;
                                        height: 0;
                                        border-top: 2px solid #0f172a;
                                        border-bottom: 1px solid #2563eb;
                                    }
                                    
                                    table.dataTable {
                                        width: 100% !important;
                                        border-collapse: collapse !important;
                                        margin-top: 15px !important;
                                        margin-bottom: 20px !important;
                                    }
                                    table.dataTable thead {
                                        display: table-header-group !important;
                                    }
                                    table.dataTable thead th {
                                        background-color: #0f172a !important;
                                        color: #ffffff !important;
                                        font-weight: 700 !important;
                                        font-size: 11px !important;
                                        text-transform: uppercase !important;
                                        letter-spacing: 0.5px !important;
                                        padding: 9px 12px !important;
                                        border: 1px solid #334155 !important;
                                        text-align: center;
                                    }
                                    table.dataTable tbody tr {
                                        page-break-inside: avoid !important;
                                    }
                                    table.dataTable tbody td {
                                        padding: 8px 12px !important;
                                        font-size: 11.5px !important;
                                        border: 1px solid #e2e8f0 !important;
                                        color: #1e293b !important;
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
                                        color: #0f172a;
                                    }
                                    .print-sign-line {
                                        border-top: 1px solid #0f172a;
                                        width: 200px;
                                        text-align: center;
                                        padding-top: 4px;
                                    }
                                    .print-footer {
                                        margin-top: 25px;
                                        padding-top: 8px;
                                        border-top: 1px solid #e2e8f0;
                                        display: flex;
                                        justify-content: space-between;
                                        align-items: center;
                                        font-size: 10px;
                                        color: #64748b;
                                    }
                                </style>
                            `);

                            // Prepend matching letterhead banner
                            $(doc.body).prepend(`
                                <div class="letterhead-container">
                                    <div class="letterhead-top-bar"></div>
                                    <div class="letterhead-body">
                                        <div class="letterhead-brand-section">
                                            ${logoHtml}
                                            <div class="letterhead-brand-details">
                                                <h2>${escapeHtml(compName)}</h2>
                                                ${tagline ? `<div class="tagline">${escapeHtml(tagline)}</div>` : ''}
                                                ${contactHtml}
                                            </div>
                                        </div>
                                        ${docBoxHtml}
                                    </div>
                                    <div class="letterhead-divider"></div>
                                </div>
                            `);

                            // Append signatures & footer
                            $(doc.body).append(`
                                <div class="print-sign-row">
                                    <div class="print-sign-line">PREPARED BY</div>
                                    <div class="print-sign-line">AUTHORIZED SIGNATURE</div>
                                </div>
                                <div class="print-footer">
                                    <div>CONFIDENTIAL &bull; ${escapeHtml(compName.toUpperCase())} &bull; SYSTEM AUDIT EXPORT</div>
                                    <div>PAGE GENERATED AUTOMATICALLY</div>
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

    // Universal Sidebar Tab Interception & Zero-Reload Switcher
    $('#sidebar-wrapper').on('click', '.sidebar-submenu a', function (e) {
        var linkHref = $(this).attr('href');
        if (!linkHref || linkHref.startsWith('#')) return;

        try {
            var targetUrl = new URL(linkHref, window.location.origin);
            var currentPath = window.location.pathname.replace(/\/+$/, '').toLowerCase();
            var targetPath = targetUrl.pathname.replace(/\/+$/, '').toLowerCase();

            // Check if link points to current page
            if (currentPath === targetPath || (currentPath === '' && targetPath === '/home')) {
                var tabParam = targetUrl.searchParams.get('tab');
                if (tabParam) {
                    var tabBtn = $('#' + tabParam + '-tab');
                    if (tabBtn.length) {
                        e.preventDefault();

                        // Switch active class in sidebar immediately
                        $('.sidebar-submenu a').removeClass('active-sublink');
                        $(this).addClass('active-sublink');

                        // Activate the tab
                        bootstrap.Tab.getOrCreateInstance(tabBtn[0]).show();

                        // Update browser URL query string without full reload
                        if (history.replaceState) {
                            window.history.replaceState({ path: targetUrl.href }, '', targetUrl.href);
                        }
                    }
                }
            }
        } catch (err) {
            // If URL parsing fails, allow standard browser navigation
        }
    });

    // Global listener to sync sidebar active state whenever any tab is shown
    $(document).on('shown.bs.tab', 'button[data-bs-toggle="tab"], a[data-bs-toggle="tab"]', function (e) {
        var rawTarget = $(e.target).data('bs-target') || $(e.target).attr('href') || '';
        var targetId = rawTarget.replace('#', '').replace('-tab', '');
        if (targetId) {
            $('.sidebar-submenu a').removeClass('active-sublink');
            var matchingLink = $('#side-nav-' + targetId);
            if (matchingLink.length) {
                matchingLink.addClass('active-sublink');
                var parentCollapse = matchingLink.closest('.collapse');
                if (parentCollapse.length && !parentCollapse.hasClass('show')) {
                    var bsCollapse = bootstrap.Collapse.getInstance(parentCollapse[0]) || new bootstrap.Collapse(parentCollapse[0], { toggle: false });
                    bsCollapse.show();
                }
            }
        }
    });
});

