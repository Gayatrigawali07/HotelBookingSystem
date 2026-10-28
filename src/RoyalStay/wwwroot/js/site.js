document.addEventListener("DOMContentLoaded", function () {
    const today = new Date();
    const localDate = new Date(today.getTime() - today.getTimezoneOffset() * 60000).toISOString().slice(0,10);
    document.querySelectorAll('input[type="date"]').forEach(input => input.min = localDate);
    function pair(inId, outId) {
        const a = document.getElementById(inId), b = document.getElementById(outId);
        if (!a || !b) return;
        a.addEventListener('change', () => {
            if (!a.value) return;
            const d = new Date(a.value + 'T00:00:00'); d.setDate(d.getDate()+1);
            b.min = new Date(d.getTime()-d.getTimezoneOffset()*60000).toISOString().slice(0,10);
            if (b.value && b.value <= a.value) b.value = '';
        });
    }
    pair('checkIn','checkOut'); pair('detailCheckIn','detailCheckOut'); pair('CheckIn','CheckOut');
});

// Booking form: show estimated total = nights x price (the server recalculates the real total).
document.addEventListener("DOMContentLoaded", function () {
    const total = document.getElementById("estimatedTotal");
    const a = document.getElementById("CheckIn"), b = document.getElementById("CheckOut");
    if (!total || !a || !b) return;
    const price = parseFloat(total.dataset.price || "0");
    function update() {
        const si = document.getElementById("summaryCheckIn"), so = document.getElementById("summaryCheckOut");
        if (si) si.textContent = a.value || "Select date";
        if (so) so.textContent = b.value || "Select date";
        if (!a.value || !b.value) return;
        const nights = Math.round((new Date(b.value + "T00:00:00") - new Date(a.value + "T00:00:00")) / 86400000);
        const text = document.getElementById("nightsText");
        if (nights > 0) {
            total.textContent = "₹" + (nights * price).toLocaleString("en-IN");
            if (text) text.textContent = nights + (nights === 1 ? " night" : " nights");
        } else {
            total.textContent = "—";
            if (text) text.textContent = "check dates";
        }
    }
    a.addEventListener("change", update); b.addEventListener("change", update); update();
});
