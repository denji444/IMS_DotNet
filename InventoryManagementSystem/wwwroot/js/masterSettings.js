var empTable, deptTable, attTable, leaveTable, categoryTable;
var token = $('input[name="__RequestVerificationToken"]').val();
var currentTypeTags = [];

$(document).ready(function () {
    // Setup CSRF header for Ajax
    $.ajaxSetup({
        headers: {
            'RequestVerificationToken': token
        }
    });

    // Initialize select2 components
    initSelect2();

    // Sync side navbar active selection with on-screen tab switch
    function syncSidebarSelection(tabId) {
        $('#masterSettingsCollapse .list-group-item').removeClass('active-sublink');

        if (tabId === 'employees') {
            $('#side-nav-employees').addClass('active-sublink');
        } else if (tabId === 'departments') {
            $('#side-nav-departments').addClass('active-sublink');
            var urlParams = new URLSearchParams(window.location.search);
            var deptId = urlParams.get('deptId');
            if (deptId && $('#side-nav-dept-' + deptId).length) {
                $('#side-nav-dept-' + deptId).addClass('active-sublink');
            } else {
                $('#side-nav-departments-all').addClass('active-sublink');
            }
            var collapseEl = document.getElementById('deptSubmenuCollapse');
            if (collapseEl && !collapseEl.classList.contains('show')) {
                var bsCollapse = bootstrap.Collapse.getInstance(collapseEl) || new bootstrap.Collapse(collapseEl, { toggle: false });
                bsCollapse.show();
            }
        } else if (tabId === 'attendance') {
            $('#side-nav-attendance').addClass('active-sublink');
        } else if (tabId === 'leaves') {
            $('#side-nav-leaves').addClass('active-sublink');
        } else if (tabId === 'categories') {
            $('#side-nav-categories').addClass('active-sublink');
        } else if (tabId === 'smtp') {
            $('#side-nav-smtp').addClass('active-sublink');
            loadSmtpSettings();
        } else if (tabId === 'company') {
            $('#side-nav-company').addClass('active-sublink');
            loadCompanyProfile();
        }
    }

    // Lazy load tables and sync sidebar on tab switch
    $('#masterSettingsTabs button').on('shown.bs.tab', function (e) {
        var rawTarget = $(e.target).data('bs-target') || '';
        var targetId = rawTarget.replace('#', '');

        syncSidebarSelection(targetId);

        // Update browser URL query string without full page reload
        if (history.replaceState) {
            var urlParams = new URLSearchParams(window.location.search);
            urlParams.set('tab', targetId);
            if (targetId !== 'departments') {
                urlParams.delete('deptId');
            }
            var newUrl = window.location.protocol + "//" + window.location.host + window.location.pathname + '?' + urlParams.toString();
            window.history.replaceState({ path: newUrl }, '', newUrl);
        }

        if (targetId === 'employees') {
            empTable.ajax.reload(null, false);
        } else if (targetId === 'departments') {
            deptTable.ajax.reload(null, false);
        } else if (targetId === 'attendance') {
            attTable.ajax.reload(null, false);
        } else if (targetId === 'leaves') {
            leaveTable.ajax.reload(null, false);
        } else if (targetId === 'categories') {
            categoryTable.ajax.reload(null, false);
        } else if (targetId === 'smtp') {
            loadSmtpSettings();
        } else if (targetId === 'company') {
            loadCompanyProfile();
        }
    });

    // Initialize DataTables
    initTables();

    // Activate tab from URL query parameter if present
    var urlParams = new URLSearchParams(window.location.search);
    var tabParam = urlParams.get('tab');
    if (tabParam) {
        var tabButton = $('#' + tabParam + '-tab');
        if (tabButton.length) {
            tabButton.trigger('click');
        } else {
            syncSidebarSelection('employees');
        }
    } else {
        syncSidebarSelection('employees');
    }

    // Form Submissions
    setupFormHandlers();
});

function initSelect2() {
    $('.select2-ajax').each(function () {
        var $select = $(this);
        var $modal = $select.closest('.modal');
        $select.select2({
            theme: 'bootstrap-5',
            dropdownParent: $modal.length ? $modal : null,
            ajax: {
                url: '/MasterSettings/GetEmployeesJson',
                dataType: 'json',
                delay: 250,
                data: function (params) {
                    return { q: params.term };
                },
                processResults: function (data) {
                    return { results: data };
                },
                cache: true
            },
            minimumInputLength: 0,
            placeholder: 'Search employee name...'
        });
    });
}

// Re-init select2 when a modal opens
$('.modal').on('shown.bs.modal', function () {
    initSelect2();
});

function initTables() {
    // Employees Table
    empTable = $("#employeesTable").DataTable({
        "ajax": {
            "url": "/MasterSettings/GetEmployeesData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "fullName", "width": "15%" },
            { "data": "email", "width": "15%" },
            { "data": "phone", "width": "10%" },
            { "data": "departmentName", "width": "15%" },
            { "data": "designation", "width": "12%" },
            { "data": "hireDate", "width": "10%" },
            {
                "data": "salary",
                "render": function (d) { return 'PKR ' + parseFloat(d).toFixed(2); },
                "width": "9%"
            },
            {
                "data": "status",
                "render": function (d) {
                    var badge = "bg-success";
                    if (d === "Inactive") badge = "bg-secondary";
                    else if (d === "On Leave") badge = "bg-warning text-dark";
                    else if (d === "Terminated") badge = "bg-danger";
                    return `<span class="badge ${badge}">${d}</span>`;
                },
                "width": "8%"
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center">
                            <button class="btn btn-sm btn-outline-info rounded-circle shadow-sm" style="width: 34px; height: 34px; padding: 0; line-height: 32px;" onclick="openEmployeeAccountModal(${data})" title="User Credentials & Access Control">
                                <i class="fas fa-user-tie"></i>
                            </button>
                        </div>
                    `;
                },
                "orderable": false,
                "width": "6%"
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center text-nowrap">
                            <button class="btn btn-sm btn-dark me-1" onclick="openEmployeeModal(${data})">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteEmployee(${data})">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    `;
                },
                "orderable": false,
                "width": "6%"
            }
        ]
    });

    // Departments Table
    deptTable = $("#departmentsTable").DataTable({
        "ajax": {
            "url": "/MasterSettings/GetDepartmentsData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "id", "width": "10%" },
            { "data": "name", "width": "30%" },
            { "data": "description", "width": "40%" },
            {
                "data": "employeeCount",
                "render": function (d) { return `<span class="badge bg-secondary">${d} Employee(s)</span>`; },
                "width": "10%"
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center text-nowrap">
                            <button class="btn btn-sm btn-dark me-1" onclick="openDeptModal(${data})">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteDept(${data})">
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

    // Attendance Table
    attTable = $("#attendanceTable").DataTable({
        "ajax": {
            "url": "/MasterSettings/GetAttendanceData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "employeeName", "width": "30%" },
            { "data": "date", "width": "20%" },
            { "data": "clockIn", "width": "15%" },
            { "data": "clockOut", "width": "15%" },
            {
                "data": "status",
                "render": function (d) {
                    var badge = "bg-success";
                    if (d === "Absent") badge = "bg-danger";
                    else if (d === "Late") badge = "bg-warning text-dark";
                    else if (d === "Half Day") badge = "bg-info text-white";
                    else if (d && d.startsWith("On Leave")) badge = "bg-primary text-white";
                    return `<span class="badge ${badge}">${d}</span>`;
                },
                "width": "10%"
            },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center text-nowrap">
                            <button class="btn btn-sm btn-dark me-1" onclick="openAttendanceModal(${data})">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteAttendance(${data})">
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

    // Leaves Table
    leaveTable = $("#leavesTable").DataTable({
        "ajax": {
            "url": "/MasterSettings/GetLeavesData",
            "type": "GET",
            "datatype": "json"
        },
        "columns": [
            { "data": "employeeName", "width": "25%" },
            { "data": "startDate", "width": "15%" },
            { "data": "endDate", "width": "15%" },
            { "data": "leaveType", "width": "15%" },
            {
                "data": "status",
                "render": function (d) {
                    var badge = "bg-warning text-dark";
                    if (d === "Approved") badge = "bg-success";
                    else if (d === "Rejected") badge = "bg-danger";
                    return `<span class="badge ${badge}">${d}</span>`;
                },
                "width": "10%"
            },
            { "data": "notes", "width": "12%" },
            {
                "data": "id",
                "render": function (data) {
                    return `
                        <div class="text-center text-nowrap">
                            <button class="btn btn-sm btn-dark me-1" onclick="openLeaveModal(${data})">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-sm btn-danger" onclick="deleteLeave(${data})">
                                <i class="fas fa-trash"></i>
                            </button>
                        </div>
                    `;
                },
                "orderable": false,
                "width": "8%"
            }
        ]
    });

    // Categories & Dynamic Types Table
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

function setupFormHandlers() {
    // Employee Form Save
    $("#employeeForm").on("submit", function (e) {
        e.preventDefault();
        var model = {
            Id: parseInt($("#empId").val()),
            FullName: $("#empFullName").val(),
            Email: $("#empEmail").val(),
            Phone: $("#empPhone").val(),
            DepartmentId: $("#empDepartmentId").val() ? parseInt($("#empDepartmentId").val()) : null,
            Designation: $("#empDesignation").val(),
            HireDate: $("#empHireDate").val(),
            Salary: parseFloat($("#empSalary").val()),
            Status: $("#empStatus").val()
        };

        submitForm("/MasterSettings/SaveEmployee", model, "#employeeModal", empTable, "#btnSaveEmployee");
    });

    // Department Form Save
    $("#deptForm").on("submit", function (e) {
        e.preventDefault();
        var model = {
            Id: parseInt($("#deptId").val()),
            Name: $("#deptName").val(),
            Description: $("#deptDescription").val()
        };

        submitForm("/MasterSettings/SaveDepartment", model, "#deptModal", deptTable, "#btnSaveDept");
    });

    // Category Form Save
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

        submitForm("/MasterSettings/SaveCategory", model, "#categoryModal", categoryTable, "#btnSaveCategory");
    });

    // Attendance Form Save
    $("#attendanceForm").on("submit", function (e) {
        e.preventDefault();
        var empId = parseInt($("#attEmployeeId").val());
        if (!empId || isNaN(empId)) {
            Swal.fire({
                title: 'Required Field Missing',
                text: 'Please select an employee.',
                icon: 'warning',
                confirmButtonColor: '#3085d6'
            });
            return;
        }

        var clockIn = $("#attClockIn").val();
        var clockOut = $("#attClockOut").val();
        if (clockIn && clockOut && clockIn >= clockOut) {
            Swal.fire({
                title: 'Invalid Time Range',
                text: 'Clock Out time must be after Clock In time.',
                icon: 'warning',
                confirmButtonColor: '#3085d6'
            });
            return;
        }

        var model = {
            Id: parseInt($("#attId").val()) || 0,
            EmployeeId: empId,
            Date: $("#attDate").val(),
            ClockIn: clockIn || null,
            ClockOut: clockOut || null,
            Status: $("#attStatus").val()
        };

        submitForm("/MasterSettings/SaveAttendance", model, "#attendanceModal", attTable, "#btnSaveAttendance");
    });

    // Leave Form Save
    $("#leaveForm").on("submit", function (e) {
        e.preventDefault();
        var empId = parseInt($("#leaveEmployeeId").val());
        if (!empId || isNaN(empId)) {
            Swal.fire({
                title: 'Required Field Missing',
                text: 'Please select an employee.',
                icon: 'warning',
                confirmButtonColor: '#3085d6'
            });
            return;
        }
        var model = {
            Id: parseInt($("#leaveId").val()) || 0,
            EmployeeId: empId,
            StartDate: $("#leaveStartDate").val(),
            EndDate: $("#leaveEndDate").val(),
            LeaveType: $("#leaveType").val(),
            Status: $("#leaveStatus").val(),
            Notes: $("#leaveNotes").val()
        };

        submitForm("/MasterSettings/SaveLeave", model, "#leaveModal", leaveTable, "#btnSaveLeave");
    });

    // SMTP Form Save
    $("#smtpForm").on("submit", function (e) {
        e.preventDefault();

        var data = {
            Id: parseInt($("#smtpId").val()) || 0,
            Server: $("#smtpServer").val().trim(),
            Port: parseInt($("#smtpPort").val()) || 587,
            SenderName: $("#smtpSenderName").val().trim(),
            SenderEmail: $("#smtpSenderEmail").val().trim(),
            Username: $("#smtpUsername").val().trim(),
            Password: $("#smtpPassword").val(),
            EnableSsl: true
        };

        var btn = $("#btnSaveSmtp");
        btn.prop("disabled", true).html('<i class="fas fa-spinner fa-spin me-2"></i> Saving Settings...');

        $.ajax({
            url: "/MasterSettings/SaveSmtpSettings",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(data),
            success: function (res) {
                btn.prop("disabled", false).html('<i class="fas fa-save me-2"></i> Save SMTP Configuration');
                if (res.success) {
                    Swal.fire({
                        title: 'Saved!',
                        text: res.message,
                        icon: 'success',
                        confirmButtonColor: '#28a745'
                    });
                    loadSmtpSettings();
                } else {
                    Swal.fire({
                        title: 'Error!',
                        text: res.message,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
                }
            },
            error: function (xhr) {
                btn.prop("disabled", false).html('<i class="fas fa-save me-2"></i> Save SMTP Configuration');
                var msg = xhr.responseJSON?.message || "Failed to save SMTP configuration.";
                Swal.fire({
                    title: 'Error!',
                    text: msg,
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
            }
        });
    });

    // Test SMTP Form Submit
    $("#testSmtpForm").on("submit", function (e) {
        e.preventDefault();

        var recipientEmail = $("#testRecipientEmail").val().trim();
        if (!recipientEmail) return;

        var testData = {
            Server: $("#smtpServer").val().trim(),
            Port: parseInt($("#smtpPort").val()) || 587,
            SenderName: $("#smtpSenderName").val().trim(),
            SenderEmail: $("#smtpSenderEmail").val().trim(),
            Username: $("#smtpUsername").val().trim(),
            Password: $("#smtpPassword").val(),
            EnableSsl: true,
            TestEmail: recipientEmail
        };

        var btn = $("#btnSendTestEmail");
        var alertDiv = $("#testSmtpResultAlert");

        btn.prop("disabled", true).html('<i class="fas fa-spinner fa-spin me-2"></i> Testing Connection...');
        alertDiv.removeClass("d-none alert-success alert-danger")
            .addClass("alert alert-info")
            .html('<i class="fas fa-sync fa-spin me-2"></i> Connecting to SMTP server and sending test email... Please wait.');

        $.ajax({
            url: "/MasterSettings/TestSmtpConnection",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(testData),
            success: function (res) {
                btn.prop("disabled", false).html('<i class="fas fa-paper-plane me-1"></i> Send Test Email');
                if (res.success) {
                    alertDiv.removeClass("alert-info alert-danger")
                        .addClass("alert alert-success")
                        .html('<i class="fas fa-check-circle me-2"></i>' + res.message);
                } else {
                    alertDiv.removeClass("alert-info alert-success")
                        .addClass("alert alert-danger")
                        .html('<i class="fas fa-exclamation-triangle me-2"></i>' + res.message);
                }
            },
            error: function (xhr) {
                btn.prop("disabled", false).html('<i class="fas fa-paper-plane me-1"></i> Send Test Email');
                var msg = xhr.responseJSON?.message || "Error testing SMTP connection.";
                alertDiv.removeClass("alert-info alert-success")
                    .addClass("alert alert-danger")
                    .html('<i class="fas fa-exclamation-triangle me-2"></i>' + msg);
            }
        });
    });
}

// Helper function for CRUD forms submission
function submitForm(url, model, modalSelector, tableRef, btnSelector) {
    var btn = $(btnSelector);
    var originalText = btn.html();
    btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-2"></span>Saving...');

    $.ajax({
        url: url,
        type: "POST",
        contentType: "application/json",
        data: JSON.stringify(model),
        success: function (response) {
            btn.prop("disabled", false).html(originalText);
            if (response.success) {
                $(modalSelector).modal("hide");
                tableRef.ajax.reload(null, false);
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
}

// Modal Open Functions
function openEmployeeModal(id) {
    $("#employeeForm")[0].reset();
    $("#empId").val(0);
    $("#employeeModalLabel").text("Add Employee");
    $("#empEmail").prop("disabled", false);

    if (id > 0) {
        $.get("/MasterSettings/GetEmployee/" + id, function (data) {
            $("#empId").val(data.id);
            $("#empFullName").val(data.fullName);
            $("#empEmail").val(data.email).prop("disabled", true); // email cannot be edited
            $("#empPhone").val(data.phone);
            $("#empDepartmentId").val(data.departmentId);
            $("#empDesignation").val(data.designation);
            $("#empHireDate").val(data.hireDate);
            $("#empSalary").val(data.salary);
            $("#empStatus").val(data.status);

            $("#employeeModalLabel").text("Edit Employee");
            $("#employeeModal").modal("show");
        });
    } else {
        $("#empHireDate").val(new Date().toISOString().substring(0, 10));
        $("#employeeModal").modal("show");
    }
}

function openDeptModal(id) {
    $("#deptForm")[0].reset();
    $("#deptId").val(0);
    $("#deptModalLabel").text("Add Department");

    if (id > 0) {
        $.get("/MasterSettings/GetDepartment/" + id, function (data) {
            $("#deptId").val(data.id);
            $("#deptName").val(data.name);
            $("#deptDescription").val(data.description);

            $("#deptModalLabel").text("Edit Department");
            $("#deptModal").modal("show");
        });
    } else {
        $("#deptModal").modal("show");
    }
}

function openAttendanceModal(id) {
    $("#attendanceForm")[0].reset();
    $("#attId").val(0);
    $("#attEmployeeId").empty().val(null).trigger('change');
    $("#attendanceModalLabel").text("Log Attendance");

    if (id > 0) {
        $.get("/MasterSettings/GetAttendance/" + id, function (data) {
            $("#attId").val(data.id);
            $("#attDate").val(data.date);
            $("#attClockIn").val(data.clockIn);
            $("#attClockOut").val(data.clockOut);
            $("#attStatus").val(data.status);

            // Fetch employee details to show in select2
            $.get("/MasterSettings/GetEmployee/" + data.employeeId, function (emp) {
                var option = new Option(emp.fullName + " (" + emp.designation + ")", data.employeeId, true, true);
                $("#attEmployeeId").empty().append(option).trigger('change');
                $("#attendanceModalLabel").text("Edit Attendance Log");
                $("#attendanceModal").modal("show");
            });
        });
    } else {
        $("#attDate").val(new Date().toISOString().substring(0, 10));
        $("#attendanceModal").modal("show");
    }
}

function openLeaveModal(id) {
    $("#leaveForm")[0].reset();
    $("#leaveId").val(0);
    $("#leaveEmployeeId").empty().val(null).trigger('change');
    $("#leaveModalLabel").text("Add Leave Request");

    if (id > 0) {
        $.get("/MasterSettings/GetLeave/" + id, function (data) {
            $("#leaveId").val(data.id);
            $("#leaveStartDate").val(data.startDate);
            $("#leaveEndDate").val(data.endDate);
            $("#leaveType").val(data.leaveType);
            $("#leaveStatus").val(data.status);
            $("#leaveNotes").val(data.notes);

            $.get("/MasterSettings/GetEmployee/" + data.employeeId, function (emp) {
                var option = new Option(emp.fullName + " (" + emp.designation + ")", data.employeeId, true, true);
                $("#leaveEmployeeId").empty().append(option).trigger('change');
                $("#leaveModalLabel").text("Edit Leave Request");
                $("#leaveModal").modal("show");
            });
        });
    } else {
        var today = new Date().toISOString().substring(0, 10);
        $("#leaveStartDate").val(today);
        $("#leaveEndDate").val(today);
        $("#leaveModal").modal("show");
    }
}

// Delete Actions
function deleteEmployee(id) {
    confirmDelete("/MasterSettings/DeleteEmployee/" + id, empTable, "This will delete the employee and their corresponding user account!");
}

function deleteDept(id) {
    confirmDelete("/MasterSettings/DeleteDepartment/" + id, deptTable, "Employees in this department will be set to 'Unassigned'.");
}

function deleteAttendance(id) {
    confirmDelete("/MasterSettings/DeleteAttendance/" + id, attTable, "This attendance log entry will be permanently removed.");
}

function deleteLeave(id) {
    confirmDelete("/MasterSettings/DeleteLeave/" + id, leaveTable, "This leave record will be permanently deleted.");
}

function deleteCategory(id) {
    confirmDelete("/MasterSettings/DeleteCategory/" + id, categoryTable, "This will delete the category and its dynamic type options.");
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

    // Prevent duplicate tag
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

// Handle Enter key inside Type input
$(document).on("keydown", "#catTypeInput", function (e) {
    if (e.key === "Enter") {
        e.preventDefault();
        addTypeTagFromInput();
    }
});

function confirmDelete(url, tableRef, warningText) {
    Swal.fire({
        title: 'Are you sure?',
        text: warningText,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#3085d6',
        confirmButtonText: 'Yes, delete it!'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: "POST",
                success: function (response) {
                    if (response.success) {
                        tableRef.ajax.reload(null, false);
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

// User Profile & Employee Account Modal Handlers
function openEmployeeAccountModal(id) {
    $("#empAccEmployeeId").val(id);
    $("#empAccFullName").text("Loading...");
    $("#empAccDesignation").text("");
    $("#empAccDepartment").text("");
    $("#empAccEmail").val("");
    $("#empAccUsername").val("");
    $("#empAccPassword").val("Default@123").attr("type", "password");
    $("#eyeIcon").removeClass("fa-eye-slash").addClass("fa-eye");

    $.get("/MasterSettings/GetEmployeeUserAccount/" + id, function (response) {
        if (!response.success) {
            Swal.fire({
                title: 'Error!',
                text: response.message,
                icon: 'error',
                confirmButtonColor: '#d33'
            });
            return;
        }

        $("#empAccFullName").text(response.fullName);
        $("#empAccDesignation").text(response.designation);
        $("#empAccDepartment").text(response.departmentName);
        $("#empAccEmail").val(response.email);
        $("#empAccUsername").val(response.userName);
        $("#empAccPassword").val(response.defaultPassword);

        var isRestricted = response.isRestricted;
        $("#empAccRestrictedToggle").prop("checked", isRestricted);
        updateStatusBadge(isRestricted);

        $("#employeeAccountModal").modal("show");
    }).fail(function () {
        Swal.fire({
            title: 'Error!',
            text: 'Unable to load employee user account profile.',
            icon: 'error',
            confirmButtonColor: '#d33'
        });
    });
}

function updateStatusBadge(isRestricted) {
    var badge = $("#empAccStatusBadge");
    var helpText = $("#accountStatusHelpText");
    if (isRestricted) {
        badge.removeClass("bg-success").addClass("bg-danger").text("ACCOUNT RESTRICTED");
        helpText.text("Account is currently restricted/locked out. Employee cannot log in.");
    } else {
        badge.removeClass("bg-danger").addClass("bg-success").text("ACCOUNT ACTIVE");
        helpText.text("Account is active. Employee can log in with their credentials.");
    }
}

function togglePasswordVisibility(inputSel, btnElem) {
    var $input = $(inputSel);
    var $icon = $(btnElem).find('i');
    if ($input.attr('type') === 'password') {
        $input.attr('type', 'text');
        $icon.removeClass('fa-eye').addClass('fa-eye-slash');
    } else {
        $input.attr('type', 'password');
        $icon.removeClass('fa-eye-slash').addClass('fa-eye');
    }
}

function copyToClipboard(elementId, toastMsg) {
    var text = $(elementId).val();
    if (navigator.clipboard && window.isSecureContext) {
        navigator.clipboard.writeText(text).then(function () {
            showToast(toastMsg);
        });
    } else {
        var input = $(elementId)[0];
        input.select();
        document.execCommand('copy');
        showToast(toastMsg);
    }
}

function showToast(msg) {
    const Toast = Swal.mixin({
        toast: true,
        position: 'top-end',
        showConfirmButton: false,
        timer: 2000,
        timerProgressBar: true
    });
    Toast.fire({
        icon: 'success',
        title: msg
    });
}

function resetEmployeePassword() {
    var empId = parseInt($("#empAccEmployeeId").val());
    var empName = $("#empAccFullName").text();

    Swal.fire({
        title: 'Reset Password?',
        text: `Are you sure you want to reset password for "${empName}" to default (Default@123)?`,
        icon: 'warning',
        showCancelButton: true,
        confirmButtonColor: '#f0ad4e',
        cancelButtonColor: '#6c757d',
        confirmButtonText: 'Yes, Reset Password'
    }).then((result) => {
        if (result.isConfirmed) {
            var btn = $("#btnResetPassword");
            var origHtml = btn.html();
            btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-1"></span>Resetting...');

            $.ajax({
                url: "/MasterSettings/ResetEmployeePassword",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ employeeId: empId }),
                success: function (response) {
                    btn.prop("disabled", false).html(origHtml);
                    if (response.success) {
                        $("#empAccPassword").val(response.defaultPassword);
                        Swal.fire({
                            title: 'Password Reset!',
                            html: `Password for <strong>${empName}</strong> has been reset.<br/><br/>Default Password: <code class="fs-5">${response.defaultPassword}</code>`,
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
                    btn.prop("disabled", false).html(origHtml);
                    var msg = xhr.responseJSON?.message || "An error occurred while resetting password.";
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

function toggleAccountRestriction(toggleElem) {
    var empId = parseInt($("#empAccEmployeeId").val());
    var isRestricted = $(toggleElem).is(":checked");
    var empName = $("#empAccFullName").text();

    var actionTitle = isRestricted ? "Restrict Account?" : "Activate Account?";
    var actionText = isRestricted
        ? `Are you sure you want to RESTRICT access for "${empName}"? They will not be able to log into the system.`
        : `Are you sure you want to ACTIVATE access for "${empName}"?`;

    Swal.fire({
        title: actionTitle,
        text: actionText,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: isRestricted ? '#d33' : '#28a745',
        cancelButtonColor: '#6c757d',
        confirmButtonText: isRestricted ? 'Yes, Restrict' : 'Yes, Activate'
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/MasterSettings/ToggleEmployeeAccountStatus",
                type: "POST",
                contentType: "application/json",
                data: JSON.stringify({ employeeId: empId, isRestricted: isRestricted }),
                success: function (response) {
                    if (response.success) {
                        updateStatusBadge(response.isRestricted);
                        Swal.fire({
                            title: 'Status Updated!',
                            text: response.message,
                            icon: 'success',
                            confirmButtonColor: '#3085d6'
                        });
                    } else {
                        $(toggleElem).prop("checked", !isRestricted);
                        updateStatusBadge(!isRestricted);
                        Swal.fire({
                            title: 'Error!',
                            text: response.message,
                            icon: 'error',
                            confirmButtonColor: '#d33'
                        });
                    }
                },
                error: function (xhr) {
                    $(toggleElem).prop("checked", !isRestricted);
                    updateStatusBadge(!isRestricted);
                    var msg = xhr.responseJSON?.message || "An error occurred while updating account status.";
                    Swal.fire({
                        title: 'Error!',
                        text: msg,
                        icon: 'error',
                        confirmButtonColor: '#d33'
                    });
                }
            });
        } else {
            $(toggleElem).prop("checked", !isRestricted);
        }
    });
}

function loadSmtpSettings() {
    $.ajax({
        url: "/MasterSettings/GetSmtpSettingsData",
        type: "GET",
        success: function (res) {
            if (res.success && res.data) {
                var d = res.data;
                $("#smtpId").val(d.id || d.Id || 0);
                $("#smtpServer").val(d.server || d.Server || '');
                $("#smtpPort").val(d.port || d.Port || 587);
                $("#smtpSenderName").val(d.senderName || d.SenderName || '');
                $("#smtpSenderEmail").val(d.senderEmail || d.SenderEmail || '');
                $("#smtpUsername").val(d.username || d.Username || '');
                $("#smtpPassword").val(d.password || d.Password || '');
                $("#smtpLastUpdated").text(d.updatedAt || d.UpdatedAt || '-');

                if (d.isFromDatabase || d.IsFromDatabase) {
                    $("#smtpConfigSourceBadge")
                        .removeClass("bg-info bg-warning text-dark")
                        .addClass("bg-success text-white")
                        .html('<i class="fas fa-database me-1"></i> Active (Database Saved)');
                } else {
                    $("#smtpConfigSourceBadge")
                        .removeClass("bg-success text-white")
                        .addClass("bg-warning text-dark")
                        .html('<i class="fas fa-file-code me-1"></i> Default (appsettings.json)');
                }
            }
        },
        error: function (xhr) {
            console.error("Failed to load SMTP settings:", xhr);
        }
    });
}

function openTestSmtpModal() {
    var senderEmail = $("#smtpSenderEmail").val();
    if (senderEmail) {
        $("#testRecipientEmail").val(senderEmail);
    }
    $("#testSmtpResultAlert").addClass("d-none alert-success alert-danger alert-info").html("");
    var testModal = new bootstrap.Modal(document.getElementById('testSmtpModal'));
    testModal.show();
}

// Company Profile Handlers
function loadCompanyProfile() {
    $.ajax({
        url: "/MasterSettings/GetCompanyProfileData",
        type: "GET",
        success: function (res) {
            if (res.success && res.data) {
                var d = res.data;
                $("#companyId").val(d.id || d.Id || 0);
                $("#companyName").val(d.companyName || d.CompanyName || '');
                $("#companyTagline").val(d.tagline || d.Tagline || '');
                $("#companyAddress").val(d.address || d.Address || '');
                $("#companyPhone").val(d.phone || d.Phone || '');
                $("#companyEmail").val(d.email || d.Email || '');
                $("#companyLastUpdated").text(d.updatedAt || d.UpdatedAt || '-');

                var logoPath = d.logoPath || d.LogoPath;
                if (logoPath) {
                    $("#companyLogoPreview").attr("src", logoPath).removeClass("d-none");
                    $("#companyLogoPlaceholder").addClass("d-none");
                } else {
                    $("#companyLogoPreview").addClass("d-none").attr("src", "");
                    $("#companyLogoPlaceholder").removeClass("d-none");
                }
            }
        },
        error: function (xhr) {
            console.error("Failed to load company profile:", xhr);
        }
    });
}

function previewCompanyLogo(input) {
    if (input.files && input.files[0]) {
        var reader = new FileReader();
        reader.onload = function (e) {
            $("#companyLogoPreview").attr("src", e.target.result).removeClass("d-none");
            $("#companyLogoPlaceholder").addClass("d-none");
        };
        reader.readAsDataURL(input.files[0]);
    }
}

$(document).on("submit", "#companyProfileForm", function (e) {
    e.preventDefault();
    var formData = new FormData();
    formData.append("Id", $("#companyId").val());
    formData.append("CompanyName", $("#companyName").val());
    formData.append("Tagline", $("#companyTagline").val());
    formData.append("Address", $("#companyAddress").val());
    formData.append("Phone", $("#companyPhone").val());
    formData.append("Email", $("#companyEmail").val());

    var fileInput = $("#companyLogoFile")[0];
    if (fileInput.files.length > 0) {
        formData.append("logoFile", fileInput.files[0]);
    }

    var btn = $("#btnSaveCompanyProfile");
    var origHtml = btn.html();
    btn.prop("disabled", true).html('<span class="spinner-border spinner-border-sm me-1"></span>Saving...');

    $.ajax({
        url: "/MasterSettings/SaveCompanyProfile",
        type: "POST",
        data: formData,
        processData: false,
        contentType: false,
        headers: {
            'RequestVerificationToken': token
        },
        success: function (res) {
            btn.prop("disabled", false).html(origHtml);
            if (res.success) {
                Swal.fire({
                    title: 'Company Profile Saved!',
                    text: res.message,
                    icon: 'success',
                    confirmButtonColor: '#3085d6'
                });
                loadCompanyProfile();
            } else {
                Swal.fire({
                    title: 'Error!',
                    text: res.message,
                    icon: 'error',
                    confirmButtonColor: '#d33'
                });
            }
        },
        error: function (xhr) {
            btn.prop("disabled", false).html(origHtml);
            var msg = xhr.responseJSON?.message || "Failed to save company profile.";
            Swal.fire({
                title: 'Error!',
                text: msg,
                icon: 'error',
                confirmButtonColor: '#d33'
            });
        }
    });
});
