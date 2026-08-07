window.aeroReports = {
    downloadPdf: async function (elementId, fileName) {
        const reportElement = document.getElementById(elementId);

        if (!reportElement) {
            console.error(`PDF report element '${elementId}' was not found.`);
            alert("The performance report could not be found.");
            return;
        }

        if (!window.html2canvas) {
            console.error("html2canvas has not been loaded.");
            alert("The PDF screenshot library has not loaded.");
            return;
        }

        if (!window.jspdf?.jsPDF) {
            console.error("jsPDF has not been loaded.");
            alert("The PDF library has not loaded.");
            return;
        }

        const downloadButton = document.getElementById(
            "download-report-button"
        );

        const originalButtonText = downloadButton?.innerText;

        try {
            if (downloadButton) {
                downloadButton.disabled = true;
                downloadButton.innerText = "Preparing PDF...";
            }

            // Allow the button text and layout to update.
            await new Promise(resolve => setTimeout(resolve, 100));

            const canvas = await window.html2canvas(reportElement, {
                scale: 2,
                useCORS: true,
                allowTaint: false,
                logging: false,
                backgroundColor: "#ffffff",
                scrollX: 0,
                scrollY: -window.scrollY,
                windowWidth: reportElement.scrollWidth,
                windowHeight: reportElement.scrollHeight,
                onclone: function (clonedDocument) {
                    const clonedButton = clonedDocument.getElementById(
                        "download-report-button"
                    );

                    // The download button does not need to appear in the PDF.
                    if (clonedButton) {
                        clonedButton.style.display = "none";
                    }

                    const clonedReport =
                        clonedDocument.getElementById(elementId);

                    if (clonedReport) {
                        clonedReport.style.width = "100%";
                        clonedReport.style.maxWidth = "none";
                        clonedReport.style.overflow = "visible";
                    }
                }
            });

            const imageData = canvas.toDataURL("image/png", 1.0);
            const { jsPDF } = window.jspdf;

            // A4 portrait dimensions in millimetres.
            const pdf = new jsPDF({
                orientation: "portrait",
                unit: "mm",
                format: "a4",
                compress: true
            });

            const pageWidth = pdf.internal.pageSize.getWidth();
            const pageHeight = pdf.internal.pageSize.getHeight();

            const margin = 10;
            const printableWidth = pageWidth - margin * 2;
            const printableHeight = pageHeight - margin * 2;

            const imageWidth = printableWidth;
            const imageHeight =
                canvas.height * imageWidth / canvas.width;

            let heightRemaining = imageHeight;
            let imagePosition = margin;

            // Add the first page.
            pdf.addImage(
                imageData,
                "PNG",
                margin,
                imagePosition,
                imageWidth,
                imageHeight,
                undefined,
                "FAST"
            );

            heightRemaining -= printableHeight;

            // Add additional pages when the dashboard is longer than one page.
            while (heightRemaining > 0) {
                imagePosition =
                    margin - (imageHeight - heightRemaining);

                pdf.addPage();

                pdf.addImage(
                    imageData,
                    "PNG",
                    margin,
                    imagePosition,
                    imageWidth,
                    imageHeight,
                    undefined,
                    "FAST"
                );

                heightRemaining -= printableHeight;
            }

            const safeFileName =
                fileName && fileName.trim().length > 0
                    ? fileName.trim()
                    : "AeroResponse-Performance-Report.pdf";

            pdf.save(
                safeFileName.toLowerCase().endsWith(".pdf")
                    ? safeFileName
                    : `${safeFileName}.pdf`
            );
        } catch (error) {
            console.error("PDF generation failed:", error);

            alert(
                "The PDF could not be generated. " +
                "Please check the browser console for details."
            );
        } finally {
            if (downloadButton) {
                downloadButton.disabled = false;
                downloadButton.innerText =
                    originalButtonText || "⇩ Download PDF";
            }
        }
    }
};


// ============================================================
// PASSWORD VISIBILITY TOGGLE
// ============================================================
//
// Login/Register are statically rendered, so the password
// visibility button is handled in browser-side JavaScript.
//
// IMPORTANT: these SVGs include their own stroke/fill styling.
// Blazor CSS isolation adds scope attributes to Razor-rendered
// elements, but SVGs inserted later with innerHTML do not receive
// those attributes. Keeping the SVG styling here prevents the
// replacement icon from becoming a solid black shape.
//
// ============================================================

(() => {
    const eyeSvgStyle = `
        fill="none"
        stroke="currentColor"
        stroke-width="1.7"
        stroke-linecap="round"
        stroke-linejoin="round"
        width="21"
        height="21"
    `;

    // Password hidden: the original outline eye.
    const hiddenIcon = `
        <svg
            viewBox="0 0 24 24"
            aria-hidden="true"
            ${eyeSvgStyle}>
            <path d="M3 12s3.5-5 9-5 9 5 9 5-3.5 5-9 5-9-5-9-5Z"></path>
            <circle cx="12" cy="12" r="2.5"></circle>
        </svg>
    `;

    // Password visible: exactly the same outline eye with a
    // proportional line through it, using the same currentColor.
    const visibleIcon = `
        <svg
            viewBox="0 0 24 24"
            aria-hidden="true"
            ${eyeSvgStyle}>
            <path d="M3 12s3.5-5 9-5 9 5 9 5-3.5 5-9 5-9-5-9-5Z"></path>
            <circle cx="12" cy="12" r="2.5"></circle>
            <line x1="4" y1="4" x2="20" y2="20"></line>
        </svg>
    `;

    document.addEventListener("click", (event) => {
        const toggle = event.target.closest(".password-toggle");

        if (!toggle) {
            return;
        }

        const wrapper = toggle.closest(".auth-input-wrapper");
        const input = wrapper?.querySelector("input.auth-input-password");

        if (!input) {
            return;
        }

        event.preventDefault();

        const shouldShow = input.type === "password";
        input.type = shouldShow ? "text" : "password";

        const isConfirmation = input.id === "Input.ConfirmPassword";

        const label = shouldShow
            ? (isConfirmation ? "Hide confirmation password" : "Hide password")
            : (isConfirmation ? "Show confirmation password" : "Show password");

        toggle.setAttribute("aria-label", label);
        toggle.setAttribute("title", label);

        // Hidden = original eye.
        // Visible = same eye with same-colour diagonal line.
        toggle.innerHTML = shouldShow ? visibleIcon : hiddenIcon;
    });
})();
