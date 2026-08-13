// OWNER: Member B. JavaScript used ONLY on Officer pages.
// How to load it on your page: see the @section Scripts block
// at the bottom of Pages/Officer/Index.cshtml.
//
// The dialog itself lives in js/site.js so all three roles ask "are you sure?"
// the same way. What is left here is the one question only an officer needs:
// a decision that cannot be undone.
//
// IMPORTANT: this file only asks. It is NOT security and it is NOT a rule.
// Every real rule is enforced again on the server in
// Services/OfficerService.cs, so nothing breaks if a user turns JavaScript off.

(function (app) {
    "use strict";

    // Statuses that can never be undone once saved.
    var FINAL_STATUSES = ["Approved", "Rejected"];

    // The decision form only asks when the choice is final. Moving to Under
    // review or Shortlisted can still be changed later, so we do not interrupt
    // the officer for those.
    document.addEventListener("submit", function (event) {
        var form = event.target;

        // Already confirmed and being re-sent. site.js clears this flag.
        if (form.dataset.appConfirmed === "yes") { return; }

        if (!form.hasAttribute("data-confirm-decision")) { return; }

        var select = form.querySelector('select[name="newStatus"]');
        if (!select) { return; }

        var chosen = select.value;
        if (FINAL_STATUSES.indexOf(chosen) === -1) { return; }

        // Remember which button was pressed, so re-sending uses the same one.
        var submitter = event.submitter || null;

        event.preventDefault();

        var student = form.getAttribute("data-student") || "this student";
        var wording = chosen === "Approved"
            ? "will be awarded this scholarship"
            : "will be told their application was not successful";

        app.confirm({
            title: "Mark as " + chosen + "?",
            body: student + " " + wording + ".",
            warning: "This is the final decision. It cannot be changed afterwards.",
            confirmLabel: "Yes, mark as " + chosen
        }, function () { app.resubmitForm(form, submitter); });
    });

})(window.Grantify);
