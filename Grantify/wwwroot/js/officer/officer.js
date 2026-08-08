// OWNER: Member B. JavaScript used ONLY on Officer pages.
// How to load it on your page: see the @section Scripts block
// at the bottom of Pages/Officer/Index.cshtml.
//
// IMPORTANT: this file only asks "are you sure?". It is NOT security and it is
// NOT a rule. Every real rule is enforced again on the server in
// Services/OfficerService.cs, so nothing breaks if a user turns JavaScript off.

(function () {
    "use strict";

    // Statuses that can never be undone once saved.
    var FINAL_STATUSES = ["Approved", "Rejected"];

    // ---------------------------------------------------------------
    // Our own confirm box.
    //
    // We do NOT use window.confirm: the browser puts "localhost:5199 says"
    // above it, which looks like an error rather than part of the system.
    // This builds a small dialog inside the page instead, styled in
    // officer.css, so it matches everything else.
    //
    // Built once and reused. onYes runs only if the officer confirms.
    // ---------------------------------------------------------------
    var dialog = null;
    var onYes = null;
    var lastFocused = null;

    function buildDialog() {
        var overlay = document.createElement("div");
        overlay.className = "officer-dialog-backdrop";
        overlay.setAttribute("hidden", "hidden");
        overlay.innerHTML =
            '<div class="officer-dialog" role="alertdialog" aria-modal="true"' +
            '     aria-labelledby="officerDialogTitle" aria-describedby="officerDialogBody">' +
            '  <h2 class="officer-dialog-title" id="officerDialogTitle"></h2>' +
            '  <p class="officer-dialog-body" id="officerDialogBody"></p>' +
            '  <p class="officer-dialog-warning" id="officerDialogWarning"></p>' +
            '  <div class="officer-dialog-buttons">' +
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

    function openDialog(options, confirmCallback) {
        if (!dialog) { dialog = buildDialog(); }

        dialog.querySelector("#officerDialogTitle").textContent = options.title;
        dialog.querySelector("#officerDialogBody").textContent = options.body;
        dialog.querySelector("#officerDialogWarning").textContent = options.warning || "";
        dialog.querySelector('[data-dialog="confirm"]').textContent = options.confirmLabel || "Confirm";

        onYes = confirmCallback;
        lastFocused = document.activeElement;

        dialog.removeAttribute("hidden");
        document.body.classList.add("officer-dialog-open");

        // Focus Cancel, not Confirm: a stray Enter key must not approve anybody.
        dialog.querySelector('[data-dialog="cancel"]').focus();
    }

    function closeDialog() {
        if (!dialog) { return; }
        dialog.setAttribute("hidden", "hidden");
        document.body.classList.remove("officer-dialog-open");
        onYes = null;
        if (lastFocused && lastFocused.focus) { lastFocused.focus(); }
    }

    // Escape always cancels.
    document.addEventListener("keydown", function (e) {
        if (e.key === "Escape" && dialog && !dialog.hasAttribute("hidden")) {
            closeDialog();
        }
    });

    // ---------------------------------------------------------------
    // Ask before anything that cannot be undone.
    // ---------------------------------------------------------------
    document.addEventListener("submit", function (event) {
        var form = event.target;

        // Set just before we re-submit a form the officer has confirmed,
        // so we do not ask the same question twice.
        if (form.dataset.officerConfirmed === "yes") {
            delete form.dataset.officerConfirmed;
            return;
        }

        // ---- 1. Any form can ask for a plain confirmation ----
        // Add data-confirm="Your question?" to the <form> tag.
        // Remember which button was pressed — a form can have several
        // (Verify / Flag), and re-sending must use the same one.
        var submitter = event.submitter || null;

        var message = form.getAttribute("data-confirm");
        if (message) {
            event.preventDefault();
            openDialog({
                title: "Please confirm",
                body: message,
                confirmLabel: "Continue"
            }, function () { resubmit(form, submitter); });
            return;
        }

        // ---- 2. The decision form only asks when the choice is final ----
        // Moving to Under review or Shortlisted can still be changed later,
        // so we do not interrupt the officer for those.
        if (form.hasAttribute("data-confirm-decision")) {
            var select = form.querySelector('select[name="newStatus"]');
            if (!select) { return; }

            var chosen = select.value;
            if (FINAL_STATUSES.indexOf(chosen) === -1) { return; }

            event.preventDefault();

            var student = form.getAttribute("data-student") || "this student";
            var wording = chosen === "Approved"
                ? "will be awarded this scholarship"
                : "will be told their application was not successful";

            openDialog({
                title: "Mark as " + chosen + "?",
                body: student + " " + wording + ".",
                warning: "This is the final decision. It cannot be changed afterwards.",
                confirmLabel: "Yes, mark as " + chosen
            }, function () { resubmit(form, submitter); });
        }
    });

    // Re-send a form the officer has confirmed. requestSubmit keeps normal
    // validation and sends the button that was clicked; older browsers fall
    // back to submit().
    function resubmit(form, submitter) {
        form.dataset.officerConfirmed = "yes";
        if (typeof form.requestSubmit === "function") {
            form.requestSubmit(submitter || undefined);
        } else {
            form.submit();
        }
    }
})();
