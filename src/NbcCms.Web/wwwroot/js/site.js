(function () {
    "use strict";

    function onReady(fn) {
        if (document.readyState === "loading") {
            document.addEventListener("DOMContentLoaded", fn);
        } else {
            fn();
        }
    }

    function money(value) {
        return "N$ " + (Number.isFinite(value) ? value : 0).toLocaleString("en-US", {
            minimumFractionDigits: 2,
            maximumFractionDigits: 2
        });
    }

    onReady(function () {
        // Sidebar (mobile)
        var body = document.body;
        var menuBtn = document.getElementById("menuBtn");
        var scrim = document.getElementById("sidebarScrim");

        if (menuBtn) {
            menuBtn.addEventListener("click", function () {
                body.classList.toggle("sidebar-open");
            });
        }

        if (scrim) {
            scrim.addEventListener("click", function () {
                body.classList.remove("sidebar-open");
            });
        }

        // Flash messages
        document.querySelectorAll(".flash").forEach(function (flash) {
            var close = flash.querySelector("button");
            if (close) {
                close.addEventListener("click", function () {
                    flash.remove();
                });
            }

            window.setTimeout(function () {
                flash.style.transition = "opacity .25s ease, transform .25s ease";
                flash.style.opacity = "0";
                flash.style.transform = "translateY(-6px)";
                window.setTimeout(function () {
                    flash.remove();
                }, 260);
            }, 7000);
        });

        // Confirmations
        document.querySelectorAll("form[data-confirm]").forEach(function (form) {
            form.addEventListener("submit", function (event) {
                if (form.dataset.confirmed === "true") {
                    return;
                }

                event.preventDefault();
                if (window.confirm(form.dataset.confirm)) {
                    form.dataset.confirmed = "true";
                    if (typeof form.requestSubmit === "function") {
                        form.requestSubmit();
                    } else {
                        form.submit();
                    }
                }
            });
        });

        // Filter auto-submit
        document.querySelectorAll("select[data-autosubmit]").forEach(function (select) {
            select.addEventListener("change", function () {
                if (select.form) {
                    select.form.submit();
                }
            });
        });

        // File name preview
        document.querySelectorAll("input[type=file][data-file-preview]").forEach(function (input) {
            var target = document.getElementById(input.getAttribute("data-file-preview"));
            if (!target) {
                return;
            }

            input.addEventListener("change", function () {
                var name = input.files && input.files.length ? input.files[0].name : "No file selected";
                target.textContent = name;
            });
        });

        initClaimForm();
    });

    function initClaimForm() {
        var form = document.querySelector("[data-claim-form]");
        if (!form) {
            return;
        }

        var body = form.querySelector("[data-items-body]");
        var template = document.getElementById("itemRowTemplate");
        var addBtn = document.getElementById("addItemBtn");
        var totalEl = document.getElementById("claimTotal");
        var intentInput = form.querySelector("[data-intent-input]");

        function rows() {
            return Array.prototype.slice.call(body.querySelectorAll(".item-row"));
        }

        function reindex() {
            rows().forEach(function (row, index) {
                row.querySelectorAll("input[name], select[name]").forEach(function (input) {
                    input.name = input.name.replace(/Items\[\d+\]/, "Items[" + index + "]");
                });

                row.querySelectorAll("input[id], select[id]").forEach(function (input) {
                    input.id = input.id.replace(/Items_\d+__/, "Items_" + index + "__");
                });

                row.querySelectorAll("[data-valmsg-for]").forEach(function (span) {
                    span.setAttribute("data-valmsg-for", span.getAttribute("data-valmsg-for").replace(/Items\[\d+\]/, "Items[" + index + "]"));
                });
            });
        }

        function updateTotal() {
            var total = 0;
            rows().forEach(function (row) {
                var rate = row.querySelector(".money-input");
                if (rate) {
                    total += parseFloat(rate.value) || 0;
                }
            });

            if (totalEl) {
                totalEl.textContent = money(total);
            }

            var countEl = document.getElementById("claimItemCount");
            if (countEl) {
                countEl.textContent = String(rows().length);
            }

            var sideTotalEl = document.getElementById("claimTotalSide");
            if (sideTotalEl) {
                sideTotalEl.textContent = money(total);
            }
        }

        function clearRowError(row) {
            row.classList.remove("has-error");
            row.querySelectorAll(".row-error").forEach(function (error) {
                error.classList.remove("show");
            });
        }

        function validateRow(row) {
            var date = row.querySelector("input[type=date]");
            var programme = row.querySelector("select");
            var rate = row.querySelector(".money-input");
            var message = "";

            if (!date || !date.value) {
                message = "Choose the recording date.";
            } else if (!programme || !programme.value) {
                message = "Choose a programme.";
            } else if (!rate || !rate.value || parseFloat(rate.value) <= 0) {
                message = "Enter a fee greater than zero.";
            }

            var error = row.querySelector(".row-error");
            if (message && error) {
                error.textContent = message;
                error.classList.add("show");
                row.classList.add("has-error");
                return false;
            }

            clearRowError(row);
            return true;
        }

        function addRow() {
            if (!template) {
                return;
            }

            var index = rows().length;
            var html = template.innerHTML.replace(/__INDEX__/g, index);
            var wrapper = document.createElement("tbody");
            wrapper.innerHTML = html.trim();
            var newRow = wrapper.firstElementChild;
            body.appendChild(newRow);

            reindex();
            updateTotal();

            var firstField = newRow.querySelector("input, select");
            if (firstField) {
                firstField.focus();
            }
        }

        if (addBtn) {
            addBtn.addEventListener("click", addRow);
        }

        body.addEventListener("input", function (event) {
            if (event.target.classList.contains("money-input")) {
                updateTotal();
            }

            var row = event.target.closest(".item-row");
            if (row) {
                clearRowError(row);
            }
        });

        body.addEventListener("change", function (event) {
            var row = event.target.closest(".item-row");
            if (row) {
                clearRowError(row);
            }
        });

        body.addEventListener("click", function (event) {
            var remove = event.target.closest(".remove-row");
            if (!remove) {
                return;
            }

            event.preventDefault();
            var row = remove.closest(".item-row");

            if (rows().length <= 1) {
                row.querySelectorAll("input, select").forEach(function (input) {
                    if (input.tagName === "SELECT") {
                        input.selectedIndex = 0;
                    } else {
                        input.value = "";
                    }
                });
                clearRowError(row);
                updateTotal();
                return;
            }

            row.remove();
            reindex();
            updateTotal();
        });

        form.querySelectorAll("[data-intent]").forEach(function (button) {
            button.addEventListener("click", function () {
                if (intentInput) {
                    intentInput.value = button.getAttribute("data-intent");
                }
            });
        });

        form.addEventListener("submit", function (event) {
            var valid = true;
            rows().forEach(function (row) {
                if (!validateRow(row)) {
                    valid = false;
                }
            });

            if (!valid) {
                event.preventDefault();
                var firstError = body.querySelector(".row-error.show");
                if (firstError) {
                    firstError.scrollIntoView({ behavior: "smooth", block: "center" });
                }
            }
        });

        updateTotal();
    }
})();
