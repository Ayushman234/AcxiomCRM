document.addEventListener("DOMContentLoaded", function () {
    // Mobile sidebar toggle
    const sidebar = document.getElementById("sidebar");
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarClose = document.getElementById("sidebarClose");

    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener("click", function () {
            sidebar.classList.add("show");
        });
    }

    if (sidebarClose && sidebar) {
        sidebarClose.addEventListener("click", function () {
            sidebar.classList.remove("show");
        });
    }

    // Live clock update
    const timeElem = document.getElementById("currentTime");
    if (timeElem) {
        setInterval(function () {
            const now = new Date();
            timeElem.innerText = now.toISOString().replace("T", " ").substring(0, 16) + " UTC";
        }, 30000);
    }

    // Auto-dismiss standard alerts after 5 seconds
    const autoAlerts = document.querySelectorAll('.alert-dismissible');
    autoAlerts.forEach(function (alert) {
        setTimeout(function () {
            const bsAlert = bootstrap.Alert.getOrCreateInstance(alert);
            if (bsAlert) {
                bsAlert.close();
            }
        }, 5000);
    });
});
