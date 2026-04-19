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

// API Helper Functions
function refreshCartSummary() {
    $.ajax({
        url: '/v1-api/cart/summary',
        method: 'GET',
        success: function (data) {
            if (data) {
                $('#cart-count').text(data.itemCount);
                if (data.totalAmount > 0) {
                    $('#cart-amount').text('৳' + data.totalAmount.toLocaleString());
                    $('#cart-amount-wrapper').show();
                } else {
                    $('#cart-amount-wrapper').hide();
                }
            }
        },
        error: function (err) {
            console.error("Failed to fetch cart summary from API", err);
        }
    });
}
