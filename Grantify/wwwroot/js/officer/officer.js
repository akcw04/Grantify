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

    // One listener for the whole page, so new forms are covered automatically.
    document.addEventListener("submit", function (event) {
        var form = event.target;

        // ---- 1. Any form can ask for a plain confirmation ----
        // Add data-confirm="Your question?" to the <form> tag.
        var message = form.getAttribute("data-confirm");
        if (message && !window.confirm(message)) {
            event.preventDefault();
            return;
        }

        // ---- 2. The decision form only asks when the choice is final ----
        // Moving to Under review or Shortlisted can still be changed later,
        // so we do not interrupt the officer for those.
        if (form.hasAttribute("data-confirm-decision")) {
            var select = form.querySelector('select[name="newStatus"]');
            if (!select) {
                return;
            }

            var chosen = select.value;
            if (FINAL_STATUSES.indexOf(chosen) === -1) {
                return;
            }

            var student = form.getAttribute("data-student") || "this student";
            var question =
                "Mark " + student + " as " + chosen.toUpperCase() + "?\n\n" +
                "This is the final decision. It cannot be changed afterwards, " +
                "and the student will be told this result.";

            if (!window.confirm(question)) {
                event.preventDefault();
            }
        }
    });
})();
