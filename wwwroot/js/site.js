// Açık / koyu tema düğmesi; seçim tarayıcıda hatırlanır
document.getElementById("theme-toggle")?.addEventListener("click", () => {
    const next = document.documentElement.getAttribute("data-theme") === "dark" ? "light" : "dark";
    document.documentElement.setAttribute("data-theme", next);
    try { localStorage.setItem("theme", next); } catch { }
});

// data-confirm taşıyan formlar göndermeden önce onay ister (silme işlemleri)
document.addEventListener("submit", e => {
    const msg = e.target.dataset?.confirm;
    if (msg && !confirm(msg)) e.preventDefault();
});
