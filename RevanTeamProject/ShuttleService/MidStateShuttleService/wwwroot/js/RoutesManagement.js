document.addEventListener("DOMContentLoaded", function () {

    const pickupFilter = document.getElementById("routePickupFilter");
    const timeFrom = document.getElementById("routeTimeFrom");
    const timeTo = document.getElementById("routeTimeTo");
    const routeCards = document.querySelectorAll(".route-card");

    function filterRoutes() {
        const pickup = pickupFilter.value;
        const from = timeFrom.value;
        const to = timeTo.value;

        routeCards.forEach(card => {
            const cardPickup = card.dataset.pickup;
            const cardTime = card.dataset.time;

            const pickupMatch = pickup === "" || cardPickup === pickup;

            let timeMatch = true;

            if (from && cardTime < from) timeMatch = false;
            if (to && cardTime > to) timeMatch = false;

            card.style.display = (pickupMatch && timeMatch) ? "" : "none";
        });
    }

    pickupFilter.addEventListener("change", filterRoutes);
    timeFrom.addEventListener("input", filterRoutes);
    timeTo.addEventListener("input", filterRoutes);
});