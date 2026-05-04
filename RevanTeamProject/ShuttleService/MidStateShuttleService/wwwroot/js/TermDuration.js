//This is used in Registration's Index.cshtml to only allow first/second half to be selectable when Spring or Fall is selected

(function () {
    const termSelect = document.getElementById("Term");
    const lengthSelect = document.getElementById("LengthOfRequest");

    function updateLengthOptions() {
        const selectedTerm = termSelect.value;
        const allowHalf = selectedTerm === "Spring" || selectedTerm === "Fall";

        Array.from(lengthSelect.options).forEach(option => {
            // FirstHalf = 1, SecondHalf = 2
            if (option.value === "1" || option.value === "2") {
                option.disabled = !allowHalf;

                if (!allowHalf && option.selected) {
                    lengthSelect.value = "";
                }
            }
        });
    }

    termSelect.addEventListener("change", updateLengthOptions);
    updateLengthOptions();
})();