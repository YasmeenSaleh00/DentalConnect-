// DentBridge – Client-side enhancements

// ── SweetAlert2 theme preset ───────────────────────────────────────────────
const DB = {
  primary: '#0ea5e9',
  danger:  '#ef4444',
  warning: '#f59e0b',
  success: '#10b981',
  muted:   '#94a3b8'
};

// ── Toast mixin ───────────────────────────────────────────────────────────
const Toast = Swal.mixin({
  toast: true,
  position: 'top-end',
  showConfirmButton: false,
  timer: 4500,
  timerProgressBar: true,
  didOpen: toast => {
    toast.addEventListener('mouseenter', Swal.stopTimer);
    toast.addEventListener('mouseleave', Swal.resumeTimer);
  }
});

// ── Flash messages as SweetAlert2 toasts ─────────────────────────────────
if (window.__flashMsgs?.length) {
  window.__flashMsgs.forEach(m => Toast.fire({ icon: m.type, title: m.text }));
}

// ── Confirm dialogs via SweetAlert2 ──────────────────────────────────────
document.querySelectorAll('[data-confirm]').forEach(btn => {
  btn.addEventListener('click', async function (e) {
    e.preventDefault();
    e.stopImmediatePropagation();
    const text = this.dataset.confirm;
    const isDestructive = /deactivate|reject|delete|remove|cancel|unpublish/i.test(text);
    const result = await Swal.fire({
      title: 'Are you sure?',
      text,
      icon: isDestructive ? 'warning' : 'question',
      showCancelButton: true,
      confirmButtonColor: isDestructive ? DB.danger : DB.primary,
      cancelButtonColor: DB.muted,
      confirmButtonText: isDestructive ? '<i class="bi bi-exclamation-triangle me-1"></i> Yes, proceed' : 'Yes, proceed',
      cancelButtonText: 'Cancel',
      reverseButtons: true,
      focusCancel: true,
      customClass: { confirmButton: 'fw-600', cancelButton: 'fw-500' }
    });
    if (result.isConfirmed) {
      const form = this.closest('form');
      if (form) {
        // Show loading state
        Swal.fire({ title: 'Processing...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
        form.submit();
      }
    }
  });
});

// ── Reject student via SweetAlert2 textarea ───────────────────────────────
window.rejectStudentSwal = async function (id, name, actionUrl) {
  const { value: reason, isConfirmed } = await Swal.fire({
    title: 'Reject Application',
    html: `<div style="text-align:left">
             <p>Rejecting <strong>${name}</strong>'s application.</p>
             <small style="color:${DB.muted}">The reason will be emailed to the student.</small>
           </div>`,
    input: 'textarea',
    inputPlaceholder: 'e.g. Documents are unclear, please resubmit...',
    inputAttributes: { rows: '3', style: 'resize:none;margin-top:.75rem;font-size:.875rem' },
    showCancelButton: true,
    confirmButtonColor: DB.danger,
    cancelButtonColor: DB.muted,
    confirmButtonText: '<i class="bi bi-x-circle me-1"></i> Send Rejection',
    cancelButtonText: 'Cancel',
    reverseButtons: true,
    focusConfirm: false,
    customClass: { confirmButton: 'fw-600' },
    inputValidator: v => !v?.trim() ? 'Please provide a rejection reason.' : null
  });
  if (isConfirmed && reason) {
    Swal.fire({ title: 'Processing...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
    const form = document.createElement('form');
    form.method = 'post';
    form.action = actionUrl;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
      const t = document.createElement('input');
      t.type = 'hidden'; t.name = '__RequestVerificationToken'; t.value = token;
      form.appendChild(t);
    }
    const r = document.createElement('input');
    r.type = 'hidden'; r.name = 'reason'; r.value = reason;
    form.appendChild(r);
    document.body.appendChild(form);
    form.submit();
  }
};

// ── Reject testimonial via SweetAlert2 ───────────────────────────────────
window.rejectTestimonialSwal = async function(id, name, actionUrl) {
  const { value: notes, isConfirmed } = await Swal.fire({
    title: 'Reject Testimonial',
    html: `<p style="text-align:left">Rejecting <strong>${name}</strong>'s testimonial.</p>`,
    input: 'textarea',
    inputPlaceholder: 'Optional internal reason...',
    inputAttributes: { rows: '3', style: 'resize:none;margin-top:.75rem;font-size:.875rem' },
    showCancelButton: true,
    confirmButtonColor: DB.danger,
    cancelButtonColor: DB.muted,
    confirmButtonText: 'Reject',
    cancelButtonText: 'Cancel',
    reverseButtons: true
  });
  if (isConfirmed) {
    Swal.fire({ title: 'Processing...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
    const form = document.createElement('form');
    form.method = 'post';
    form.action = actionUrl;
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    if (token) {
      const t = document.createElement('input');
      t.type = 'hidden'; t.name = '__RequestVerificationToken'; t.value = token;
      form.appendChild(t);
    }
    const n = document.createElement('input');
    n.type = 'hidden'; n.name = 'notes'; n.value = notes || '';
    form.appendChild(n);
    document.body.appendChild(form);
    form.submit();
  }
};

// ── Forms with loading states (data-loading-form) ─────────────────────────
document.querySelectorAll('[data-loading-form]').forEach(form => {
  form.addEventListener('submit', function() {
    const msg = this.dataset.loadingForm || 'Processing, please wait...';
    Swal.fire({ title: msg, allowOutsideClick: false, didOpen: () => Swal.showLoading() });
  });
});

// ── Accept-case buttons with loading ─────────────────────────────────────
document.querySelectorAll('[data-accept-form]').forEach(form => {
  form.addEventListener('submit', async function(e) {
    e.preventDefault();
    const result = await Swal.fire({
      title: 'Accept this case?',
      text: 'You will be assigned as the treating student. Are you ready to proceed?',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: DB.success,
      cancelButtonColor: DB.muted,
      confirmButtonText: '<i class="bi bi-check-circle me-1"></i> Yes, Accept Case',
      cancelButtonText: 'Cancel',
      reverseButtons: true,
      customClass: { confirmButton: 'fw-600' }
    });
    if (result.isConfirmed) {
      Swal.fire({ title: 'Accepting case...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
      this.submit();
    }
  });
});

// ── Complete-case forms ────────────────────────────────────────────────────
document.querySelectorAll('[data-complete-form]').forEach(form => {
  form.addEventListener('submit', async function(e) {
    e.preventDefault();
    const result = await Swal.fire({
      title: 'Mark as Completed?',
      text: 'This will notify the patient that their treatment is done.',
      icon: 'success',
      showCancelButton: true,
      confirmButtonColor: DB.primary,
      cancelButtonColor: DB.muted,
      confirmButtonText: '<i class="bi bi-check2-circle me-1"></i> Yes, Complete',
      cancelButtonText: 'Cancel',
      reverseButtons: true,
      customClass: { confirmButton: 'fw-600' }
    });
    if (result.isConfirmed) {
      Swal.fire({ title: 'Marking complete...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
      this.submit();
    }
  });
});

// ── Sign-out confirmation ─────────────────────────────────────────────────
document.querySelectorAll('[data-logout-form]').forEach(form => {
  form.addEventListener('submit', async function(e) {
    e.preventDefault();
    const result = await Swal.fire({
      title: 'Sign out?',
      text: 'You will need to sign in again to access your account.',
      icon: 'question',
      showCancelButton: true,
      confirmButtonColor: DB.danger,
      cancelButtonColor: DB.muted,
      confirmButtonText: 'Yes, sign out',
      cancelButtonText: 'Stay signed in',
      reverseButtons: true
    });
    if (result.isConfirmed) {
      Swal.fire({ title: 'Signing out...', allowOutsideClick: false, didOpen: () => Swal.showLoading() });
      this.submit();
    }
  });
});

// ── Form validation — highlight errors with toast ────────────────────────
document.querySelectorAll('.needs-validation-toast').forEach(form => {
  form.addEventListener('submit', function(e) {
    const errors = this.querySelectorAll('.field-validation-error, .validation-message');
    if (errors.length) {
      e.preventDefault();
      Toast.fire({ icon: 'error', title: 'Please fix the errors below before submitting.' });
    }
  });
});

// Auto-show validation toast if server returned errors
(function () {
  const summary = document.querySelector('.validation-summary-errors');
  if (summary && summary.querySelectorAll('li').length > 0) {
    Toast.fire({ icon: 'error', title: 'Please correct the highlighted errors.' });
  }
})();

// ── Image preview on file input ───────────────────────────────────────────
document.querySelectorAll('[data-preview-target]').forEach(input => {
  input.addEventListener('change', function () {
    const target = document.getElementById(this.dataset.previewTarget);
    if (!target || !this.files[0]) return;
    target.src = URL.createObjectURL(this.files[0]);
    target.style.display = 'block';
  });
});

// ── Upload zone drag-and-drop ─────────────────────────────────────────────
document.querySelectorAll('.upload-zone').forEach(zone => {
  zone.addEventListener('dragover', e => { e.preventDefault(); zone.classList.add('dragover'); });
  zone.addEventListener('dragleave', () => zone.classList.remove('dragover'));
  zone.addEventListener('drop', e => {
    e.preventDefault();
    zone.classList.remove('dragover');
    const fileInput = zone.querySelector('input[type="file"]');
    if (fileInput && e.dataTransfer.files.length) {
      fileInput.files = e.dataTransfer.files;
      fileInput.dispatchEvent(new Event('change', { bubbles: true }));
    }
  });
  zone.addEventListener('click', () => zone.querySelector('input[type="file"]')?.click());
});

// ── Star rating hover effect ──────────────────────────────────────────────
document.querySelectorAll('.star-rating-input label').forEach(label => {
  label.addEventListener('mouseover', function () {
    const siblings = [...this.parentElement.querySelectorAll('label')];
    const idx = siblings.indexOf(this);
    siblings.forEach((s, i) => {
      s.style.color = i >= idx ? 'var(--db-warning)' : 'var(--db-border)';
    });
  });
  label.addEventListener('mouseleave', function() {
    // Reset to checked state
    const container = this.parentElement;
    const checked = container.querySelector('input:checked');
    if (!checked) return;
    const checkedLabel = container.querySelector(`label[for="${checked.id}"]`);
    const siblings = [...container.querySelectorAll('label')];
    const checkedIdx = siblings.indexOf(checkedLabel);
    siblings.forEach((s, i) => {
      s.style.color = i >= checkedIdx ? 'var(--db-warning)' : 'var(--db-border)';
    });
  });
});

// ── Testimonial carousel indicator sync ──────────────────────────────────
(function() {
  const carousel = document.getElementById('testimonialCarousel');
  if (!carousel) return;
  carousel.addEventListener('slid.bs.carousel', function(e) {
    const dots = document.querySelectorAll('.carousel-indicators-custom button');
    dots.forEach((d, i) => {
      d.style.background = i === e.to ? 'var(--db-primary)' : 'var(--db-border)';
    });
  });
})();

// ── Tooltip init ──────────────────────────────────────────────────────────
document.querySelectorAll('[data-bs-toggle="tooltip"]')
  .forEach(el => bootstrap.Tooltip.getOrCreateInstance(el));
