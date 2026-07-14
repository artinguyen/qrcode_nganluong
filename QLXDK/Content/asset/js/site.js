document.addEventListener('click', (e) => {
  // chỉ bắt nút đóng alert
  const btn = e.target.closest('[data-bs-dismiss="alert"], .btn-close');
  if (!btn) return;

  // tìm alert cha
  const alertEl = btn.closest('.alert');
  if (!alertEl) return;

  // (tuỳ chọn) nếu bạn muốn đúng theo data-bs-dismiss
  // nếu btn không có data-bs-dismiss="alert" thì bỏ qua
  if (btn.getAttribute('data-bs-dismiss') !== 'alert') return;

  e.preventDefault();

  // fade out
  alertEl.classList.remove('show');
  alertEl.classList.add('is-closing', 'fade');

  // remove sau khi transition xong
  const transitionMs = 150;
  window.setTimeout(() => {
    alertEl.remove();
  }, transitionMs);
});

function deleteConfirm(url_dlt) {
localStorage.setItem("url_dlt", url_dlt);
}
function deleteOK() {
	let url = localStorage.getItem("url_dlt");
	window.location.href = url;
}
