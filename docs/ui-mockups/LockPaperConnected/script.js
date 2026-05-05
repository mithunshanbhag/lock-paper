const logoutTrigger = document.querySelector(".logout-trigger");
const confirmDialog = document.querySelector(".confirm-dialog");
const refreshTrigger = document.querySelector(".refresh-fab");
let refreshTimerId = null;

if (logoutTrigger && confirmDialog) {
  logoutTrigger.addEventListener("click", () => {
    confirmDialog.showModal();
  });

  confirmDialog.addEventListener("click", (event) => {
    const dialogBounds = confirmDialog.getBoundingClientRect();
    const clickedOutsideDialog =
      event.clientX < dialogBounds.left ||
      event.clientX > dialogBounds.right ||
      event.clientY < dialogBounds.top ||
      event.clientY > dialogBounds.bottom;

    if (clickedOutsideDialog) {
      confirmDialog.close("cancel");
    }
  });
}

if (refreshTrigger) {
  refreshTrigger.addEventListener("click", () => {
    if (refreshTrigger.classList.contains("is-refreshing")) {
      return;
    }

    refreshTrigger.classList.add("is-refreshing");
    refreshTrigger.setAttribute("aria-label", "Refreshing lock screen");
    refreshTrigger.setAttribute("title", "Refreshing lock screen");
    refreshTrigger.disabled = true;

    window.clearTimeout(refreshTimerId);
    refreshTimerId = window.setTimeout(() => {
      refreshTrigger.classList.remove("is-refreshing");
      refreshTrigger.setAttribute("aria-label", "Refresh lock screen");
      refreshTrigger.setAttribute("title", "Refresh lock screen");
      refreshTrigger.disabled = false;
    }, 1800);
  });
}
