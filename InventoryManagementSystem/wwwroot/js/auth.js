$(document).ready(function () {
    // AJAX Login
    $("#loginForm").on("submit", function (e) {
        e.preventDefault();
        
        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        var btn = form.find("button[type='submit']");
        var spinner = $("#loginSpinner");

        btn.prop("disabled", true);
        spinner.removeClass("d-none");

        var token = $('input[name="__RequestVerificationToken"]').val();

        var loginData = {
            Username: $("#inputUsername").val(),
            Password: $("#inputPassword").val(),
            RememberMe: $("#inputRememberMe").is(":checked")
        };

        $.ajax({
            url: "/Account/Login",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(loginData),
            headers: {
                "RequestVerificationToken": token,
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false);
                spinner.addClass("d-none");

                if (response.success) {
                    Swal.fire({
                        title: 'Welcome Back!',
                        text: 'Welcome back!',
                        icon: 'success',
                        confirmButtonColor: '#3085d6'
                    }).then(function () {
                        window.location.href = response.redirectUrl;
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
                btn.prop("disabled", false);
                spinner.addClass("d-none");
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

    // AJAX Register
    $("#registerForm").on("submit", function (e) {
        e.preventDefault();

        var form = $(this);
        if (!form.valid()) {
            return false;
        }

        var btn = form.find("button[type='submit']");
        var spinner = $("#registerSpinner");

        btn.prop("disabled", true);
        spinner.removeClass("d-none");

        var token = $('input[name="__RequestVerificationToken"]').val();

        var registerData = {
            FullName: $("#inputFullName").val(),
            Username: $("#inputUsername").val(),
            Email: $("#inputEmail").val(),
            Phone: $("#inputPhone").val(),
            Password: $("#inputPassword").val(),
            ConfirmPassword: $("#inputConfirmPassword").val()
        };

        $.ajax({
            url: "/Account/Register",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(registerData),
            headers: {
                "RequestVerificationToken": token,
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (response) {
                btn.prop("disabled", false);
                spinner.addClass("d-none");

                if (response.success) {
                    Swal.fire({
                        title: 'Success!',
                        text: response.message,
                        icon: 'success',
                        confirmButtonColor: '#3085d6'
                    }).then(function () {
                        window.location.href = response.redirectUrl;
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
                btn.prop("disabled", false);
                spinner.addClass("d-none");
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
