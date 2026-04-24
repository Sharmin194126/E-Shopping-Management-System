// Toggle Password Visibility
$(document).ready(function () {
    $(".password-toggle").click(function () {
        var input = $(this).closest(".password-container, .password-wrapper").find("input");
        if (input.attr("type") === "password") {
            input.attr("type", "text");
            $(this).removeClass("bi-eye").addClass("bi-eye-slash");
        } else {
            input.attr("type", "password");
            $(this).removeClass("bi-eye-slash").addClass("bi-eye");
        }
    });
});

