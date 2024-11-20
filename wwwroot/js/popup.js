// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener("DOMContentLoaded", function () {
    document.getElementById('close-popup').addEventListener('click', function() {
        this.parentElement.remove();
    });

    var popup = document.querySelector(".popup-error, .popup-success");
    if (popup) {
        setTimeout(function () {
        popup.style.opacity = "0";
        }, 5000); // 5 seconds delay
    }
});