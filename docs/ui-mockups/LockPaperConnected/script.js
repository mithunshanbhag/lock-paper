const logoutTrigger = document.querySelector(".logout-trigger");
const confirmDialog = document.querySelector(".confirm-dialog");

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
