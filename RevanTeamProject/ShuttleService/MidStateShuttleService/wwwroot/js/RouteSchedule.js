(function () {
    // ── Accordion helpers ──────────────────────────────────────────
    function openCard(header) {
        var bodyId = header.getAttribute('data-target').replace('#', '');
        var body = document.getElementById(bodyId);
        if (!body) return;
        header.setAttribute('aria-expanded', 'true');
        body.classList.add('open');
    }

    function closeCard(header) {
        var bodyId = header.getAttribute('data-target').replace('#', '');
        var body = document.getElementById(bodyId);
        if (!body) return;
        header.setAttribute('aria-expanded', 'false');
        body.classList.remove('open');
    }

    function toggleCard(header) {
        var isOpen = header.getAttribute('aria-expanded') === 'true';

        // Close every other open card
        document.querySelectorAll('.route-card-header[aria-expanded="true"]').forEach(function (h) {
            if (h !== header) closeCard(h);
        });

        if (isOpen) {
            closeCard(header);
        } else {
            openCard(header);
        }
    }

    // Attach click + keyboard to all accordion headers
    document.querySelectorAll('.route-card-header').forEach(function (header) {
        header.addEventListener('click', function () { toggleCard(header); });
        header.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                toggleCard(header);
            }
        });
    });

    // ── Filter logic ───────────────────────────────────────────────
    var filterDay = document.getElementById('filter-day');
    var filterLocation = document.getElementById('filter-location');
    var btnClear = document.getElementById('filter-clear');

    function applyFilters(navigateAndOpen) {
        var selDay = filterDay ? filterDay.value.trim() : '';
        var selLoc = filterLocation ? filterLocation.value.trim() : '';

        var allCards = document.querySelectorAll('.route-card');
        var allSections = document.querySelectorAll('.day-section');

        var targetCard = null;

        // Show/hide cards
        allCards.forEach(function (card) {
            var dayMatch = !selDay || card.dataset.day === selDay;
            var locMatch = !selLoc || card.dataset.location === selLoc;
            var visible = dayMatch && locMatch;
            card.style.display = visible ? '' : 'none';

            // Track the first matching card for navigation
            if (visible && !targetCard) targetCard = card;
        });

        // Hide day sections that have no visible cards
        allSections.forEach(function (section) {
            var hasVisible = Array.from(section.querySelectorAll('.route-card'))
                .some(function (c) { return c.style.display !== 'none'; });
            section.setAttribute('data-hidden', hasVisible ? 'false' : 'true');
        });

        // Navigate and open the target card
        if (navigateAndOpen && targetCard) {
            // Close all others first
            document.querySelectorAll('.route-card-header[aria-expanded="true"]').forEach(function (h) {
                closeCard(h);
            });

            var header = targetCard.querySelector('.route-card-header');
            if (header) {
                openCard(header);

                // Scroll into view then briefly highlight
                targetCard.scrollIntoView({ behavior: 'smooth', block: 'start' });
                targetCard.classList.remove('targeted');
                // Force reflow so animation restarts
                void targetCard.offsetWidth;
                targetCard.classList.add('targeted');
                targetCard.addEventListener('animationend', function () {
                    targetCard.classList.remove('targeted');
                }, { once: true });

                // Move focus to the header for accessibility
                header.focus();
            }
        }
    }

    if (filterDay) filterDay.addEventListener('change', function () { applyFilters(false); });
    if (filterLocation) filterLocation.addEventListener('change', function () { applyFilters(false); });

    var btnSearch = document.getElementById('filter-search');
    if (btnSearch) {
        btnSearch.addEventListener('click', function () { applyFilters(true); });
    }

    // Also allow pressing Enter in either select to trigger search
    [filterDay, filterLocation].forEach(function (sel) {
        if (!sel) return;
        sel.addEventListener('keydown', function (e) {
            if (e.key === 'Enter') { e.preventDefault(); applyFilters(true); }
        });
    });

    if (btnClear) {
        btnClear.addEventListener('click', function () {
            if (filterDay) filterDay.value = '';
            if (filterLocation) filterLocation.value = '';

            // Reset visibility
            document.querySelectorAll('.route-card').forEach(function (c) { c.style.display = ''; });
            document.querySelectorAll('.day-section').forEach(function (s) { s.setAttribute('data-hidden', 'false'); });

            // Close all accordions
            document.querySelectorAll('.route-card-header[aria-expanded="true"]').forEach(function (h) {
                closeCard(h);
            });
        });
    }
})();