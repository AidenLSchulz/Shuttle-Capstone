(function () {
    // Grab the search input, filter dropdown, sort dropdown,
    // and the little results counter from the page.
    const searchEl = document.getElementById("regSearch");
    const filterEl = document.getElementById("regFilter");
    const sortEl = document.getElementById("regSort");
    const countEl = document.getElementById("regCount");

    // Collect all registration request cards into an array
    // so we can search, filter, sort, and count them.
    const cards = Array.from(document.querySelectorAll(".reg-card"));

    // Helper function:
    // Makes text easier to compare by:
    // - handling null/undefined safely
    // - converting to a string
    // - trimming extra spaces
    // - making everything lowercase
    function normalize(s) {
        return (s || "").toString().trim().toLowerCase();
    }

    // Checks whether a card matches the current search text.
    // Search looks at:
    // - name
    // - student ID
    // - term
    function matchesSearch(card, q) {
        // If there is no search text, every card matches.
        if (!q) return true;

        // Pull searchable values from the card's data-* attributes.
        const name = normalize(card.dataset.name);
        const sid = normalize(card.dataset.studentid);
        const term = normalize(card.dataset.term);

        // Return true if the search text appears in any of those fields.
        return name.includes(q) || sid.includes(q) || term.includes(q);
    }

    // Checks whether a card matches the selected filter.
    // The filter is based on whether the request is marked as custom.
    function matchesFilter(card, filter) {
        const isCustom = card.dataset.iscustom === "true";

        // Show only custom requests
        if (filter === "custom") return isCustom;

        // Show only standard (not custom) requests
        if (filter === "standard") return !isCustom;

        // If filter is something like "all", let everything through
        return true;
    }

    // Sorts only the currently visible cards.
    // Sorting is done by re-appending cards into their parent container
    // in the new order.
    function applySort(sortMode) {
        // Get the parent element that holds all the cards.
        const parent = cards[0]?.parentElement;
        if (!parent) return;

        // Only sort cards that are currently visible.
        const visibleCards = cards.filter(c => c.style.display !== "none");

        visibleCards.sort((a, b) => {
            // Read numeric values from data-* attributes.
            // Fallback to 0 if a value is missing.
            const aCreated = parseInt(a.dataset.created || "0", 10);
            const bCreated = parseInt(b.dataset.created || "0", 10);
            const aRides = parseInt(a.dataset.rides || "0", 10);
            const bRides = parseInt(b.dataset.rides || "0", 10);

            // Decide how to sort based on the selected sort mode.
            switch (sortMode) {
                case "oldest": return aCreated - bCreated;
                case "newest": return bCreated - aCreated;
                case "ridesAsc": return aRides - bRides;
                case "ridesDesc": return bRides - aRides;

                // If no valid sort mode is selected, keep current order.
                default: return 0;
            }
        });

        // Reinsert the visible cards into the DOM in sorted order.
        visibleCards.forEach(c => parent.appendChild(c));
    }

    // Main update function:
    // Runs whenever search/filter/sort changes.
    function update() {
        // Get the current search value and normalize it.
        const q = normalize(searchEl.value);

        // Get the selected filter value.
        const f = filterEl.value;

        // Track how many cards are currently visible.
        let shown = 0;

        cards.forEach(card => {
            // A card is shown only if it passes BOTH:
            // - search check
            // - filter check
            const show = matchesSearch(card, q) && matchesFilter(card, f);

            // Show or hide the card in the UI.
            card.style.display = show ? "" : "none";

            if (show) shown++;
        });

        // After filtering, sort the visible cards.
        applySort(sortEl.value);

        // Update the results counter text if that element exists.
        if (countEl) {
            countEl.textContent = `${shown} of ${cards.length} request(s) shown`;
        }
    }

    // When the user types in the search box, re-run the update.
    if (searchEl) searchEl.addEventListener("input", update);

    // When the user changes the filter dropdown, re-run the update.
    if (filterEl) filterEl.addEventListener("change", update);

    // When the user changes the sort dropdown, re-run the update.
    if (sortEl) sortEl.addEventListener("change", update);

    // Run once on page load so the UI starts in the correct state.
    update();
})();