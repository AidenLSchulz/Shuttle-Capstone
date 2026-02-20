let dayIndex = 0

document.getElementById("addDayBtn").addEventListener("click", () => {
    const template = document.getElementById("dayCardTemplate").innerHTML
        .replaceAll("__dayIndex__", dayIndex)

    const wrapper = document.createElement("div")
    wrapper.innerHTML = template

    const card = wrapper.firstElementChild
    card.dataset.rideCount = 0

    document.getElementById("dayCardsContainer").appendChild(card)
    dayIndex++
})

document.addEventListener("click", function (e) {
    if (!e.target.classList.contains("addRideBtn")) return

    const card = e.target.closest(".day-card")
    const dayIdx = card.dataset.dayIndex
    const rideIdx = card.dataset.rideCount

    let template = document.getElementById("rideTemplate").innerHTML
        .replaceAll("__dayIndex__", dayIdx)
        .replaceAll("__rideIndex__", rideIdx)

    const wrapper = document.createElement("div")
    wrapper.innerHTML = template

    const rideRow = wrapper.firstElementChild
    card.querySelector(".ridesContainer").appendChild(rideRow)

    populateTimes(rideRow)

    card.dataset.rideCount++
})

document.addEventListener("click", function (e) {
    if (!e.target.classList.contains("removeDayBtn")) return

    const card = e.target.closest(".day-card")
    card.remove()
})

document.addEventListener("click", function (e) {
    if (!e.target.classList.contains("removeRideBtn")) return

    const rideRow = e.target.closest(".ride-row")
    rideRow.remove()
})

function populateTimes(row) {
    const select = row.querySelector("select[name*='DropOffTime']")

    const start = 7 * 60 + 30
    const end = 16 * 60

    for (let t = start; t <= end; t += 30) {
        const h = Math.floor(t / 60)
        const m = t % 60
        const ampm = h >= 12 ? "PM" : "AM"
        const hour12 = ((h + 11) % 12) + 1
        const text = `${hour12}:${m.toString().padStart(2, "0")} ${ampm}`

        const opt = document.createElement("option")
        opt.value = text
        opt.textContent = text
        select.appendChild(opt)
    }
}