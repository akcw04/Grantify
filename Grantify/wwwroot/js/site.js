// SHARED JAVASCRIPT — loads on EVERY page. Announce in group chat before changing.
// Scripts that only ONE role needs go in js/student/, js/officer/ or js/admin/.
//
// What is in here: the "are you sure?" dialog, and the plain confirmation any
// page can ask for by putting data-confirm="Your question?" on a <form>.
//
// IMPORTANT: this file only ASKS. It is NOT security and it is NOT a rule.
// Every real rule is enforced again on the server in the Services/ classes, so
// nothing breaks if a user turns JavaScript off.

window.Grantify = window.Grantify || {};

(function (app) {
    "use strict";

    // ---------------------------------------------------------------
    // Our own confirm box.
    //
    // We do NOT use window.confirm: the browser puts "localhost:5199 says"
    // above it, which looks like an error rather than part of the system.
    // This builds a small dialog inside the page instead, styled in site.css,
    // so it matches everything else.
    //
    // Built once and reused. onYes runs only if the user confirms.
    // ---------------------------------------------------------------
    var dialog = null;
    var onYes = null;
    var lastFocused = null;

    function buildDialog() {
        var overlay = document.createElement("div");
        overlay.className = "app-dialog-backdrop";
        overlay.setAttribute("hidden", "hidden");
        overlay.innerHTML =
            '<div class="app-dialog" role="alertdialog" aria-modal="true"' +
            '     aria-labelledby="appDialogTitle" aria-describedby="appDialogBody">' +
            '  <h2 class="app-dialog-title" id="appDialogTitle"></h2>' +
            '  <p class="app-dialog-body" id="appDialogBody"></p>' +
            '  <p class="app-dialog-warning" id="appDialogWarning"></p>' +
            '  <div class="app-dialog-buttons">' +
            '    <button type="button" class="btn btn-outline-secondary" data-dialog="cancel">Cancel</button>' +
            '    <button type="button" class="btn btn-primary" data-dialog="confirm"></button>' +
            '  </div>' +
            '</div>';

        document.body.appendChild(overlay);

        overlay.addEventListener("click", function (e) {
            // Clicking the dark area behind the box cancels, like a real dialog.
            if (e.target === overlay) { closeDialog(); return; }

            var action = e.target.getAttribute("data-dialog");
            if (action === "cancel") { closeDialog(); }
            if (action === "confirm") {
                var run = onYes;
                closeDialog();
                if (run) { run(); }
            }
        });

        return overlay;
    }

    // Opens the dialog. options: { title, body, warning, confirmLabel }
    function openDialog(options, confirmCallback) {
        if (!dialog) { dialog = buildDialog(); }

        dialog.querySelector("#appDialogTitle").textContent = options.title;
        dialog.querySelector("#appDialogBody").textContent = options.body;
        dialog.querySelector("#appDialogWarning").textContent = options.warning || "";
        dialog.querySelector('[data-dialog="confirm"]').textContent = options.confirmLabel || "Confirm";

        onYes = confirmCallback;
        lastFocused = document.activeElement;

        dialog.removeAttribute("hidden");
        document.body.classList.add("app-dialog-open");

        // Focus Cancel, not Confirm: a stray Enter key must not approve or
        // delete anything.
        dialog.querySelector('[data-dialog="cancel"]').focus();
    }

    function closeDialog() {
        if (!dialog) { return; }
        dialog.setAttribute("hidden", "hidden");
        document.body.classList.remove("app-dialog-open");
        onYes = null;
        if (lastFocused && lastFocused.focus) { lastFocused.focus(); }
    }

    // Escape always cancels.
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && dialog && !dialog.hasAttribute("hidden")) {
            closeDialog();
        }
    });

    // Re-send a form the user has confirmed. requestSubmit keeps normal
    // validation and sends the button that was clicked; older browsers fall
    // back to submit().
    function resubmit(form, submitter) {
        form.dataset.appConfirmed = "yes";

        if (typeof form.requestSubmit === "function") {
            form.requestSubmit(submitter || undefined);
        } else {
            form.submit();
        }

        // Clear the flag once every submit handler has seen it — this one and
        // any role script's. Doing it inside a handler would hide it from the
        // handlers that run after, and the question would be asked twice.
        // It also matters when validation blocks the submit: without this the
        // form would never ask again.
        setTimeout(function () { delete form.dataset.appConfirmed; }, 0);
    }

    // ---------------------------------------------------------------
    // Any form can ask for a plain confirmation:
    //   <form method="post" data-confirm="Delete this institution?">
    //
    // A role script that needs a cleverer question (the Officer decision form,
    // for example) handles its own submit event and calls app.confirm itself.
    // ---------------------------------------------------------------
    document.addEventListener("submit", function (event) {
        var form = event.target;

        // Set just before we re-submit a form the user has confirmed, so we do
        // not ask the same question twice. resubmit() clears it, not us.
        if (form.dataset.appConfirmed === "yes") { return; }

        var message = form.getAttribute("data-confirm");
        if (!message) { return; }

        // Remember which button was pressed — a form can have several, and
        // re-sending must use the same one.
        var submitter = event.submitter || null;

        event.preventDefault();
        openDialog({
            title: form.getAttribute("data-confirm-title") || "Please confirm",
            body: message,
            warning: form.getAttribute("data-confirm-warning") || "",
            confirmLabel: form.getAttribute("data-confirm-label") || "Continue"
        }, function () { resubmit(form, submitter); });
    });

    // Shared with the role scripts.
    app.confirm = openDialog;
    app.resubmitForm = resubmit;

})(window.Grantify);
