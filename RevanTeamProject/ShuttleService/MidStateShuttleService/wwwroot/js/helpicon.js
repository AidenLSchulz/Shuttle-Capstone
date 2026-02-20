document.addEventListener("click", function (e) {
    // Remove any existing popup
    const existing = document.querySelector(".help-popup");
    if (existing) existing.remove();

    // If clicked element is a help icon
    if (e.target.classList.contains("help-icon")) {
        const popup = document.createElement("div");
        popup.className = "help-popup";
        popup.innerText = e.target.dataset.help;

        document.body.appendChild(popup);

        const rect = e.target.getBoundingClientRect();
        popup.style.top = `${rect.bottom + window.scrollY + 6}px`;
        popup.style.left = `${rect.left + window.scrollX}px`;

        e.stopPropagation();
    }
});