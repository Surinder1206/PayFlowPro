// JavaScript functions for PDF downloads and other utilities

window.downloadFile = async (url) => {
    console.log('=== DOWNLOAD FILE FUNCTION CALLED ===');
    console.log('URL:', url);

    try {
        // For Blazor Server applications, direct navigation approach works better
        // as it includes authentication cookies properly
        console.log('Using direct navigation approach for authentication compatibility...');

        // Create a temporary form to download the file
        // This ensures proper cookie authentication
        const form = document.createElement('form');
        form.method = 'GET';
        form.action = url;
        form.style.display = 'none';

        document.body.appendChild(form);
        console.log('Form created and submitted for download');

        form.submit();

        // Cleanup after a short delay
        setTimeout(() => {
            document.body.removeChild(form);
            console.log('Form cleanup completed');
        }, 1000);

        console.log('Download triggered successfully via form submission!');
        return true;

    } catch (error) {
        console.error('Download error:', error);

        // Show user-friendly error
        alert(`Download failed: ${error.message}\n\nTrying alternative approach...`);

        // Try fallback - direct navigation
        console.log('Attempting fallback - direct navigation...');
        try {
            window.location.href = url;
            console.log('Fallback navigation initiated');
        } catch (fallbackError) {
            console.error('Fallback also failed:', fallbackError);
            // Final fallback - open in new window
            window.open(url, '_blank');
        }

        return false;
    }
};

window.previewFile = (url) => {
    // For preview, window.open should work as it includes cookies
    // but we can also add explicit credentials if needed
    window.open(url, '_blank');
};

// Bulk download functionality
window.downloadBatchPayslips = async (payslipIds) => {
    try {
        const response = await fetch('/api/pdf/payslips/batch', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(payslipIds)
        });

        if (response.ok) {
            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `Payslips_Batch_${new Date().toISOString().slice(0, 10)}.pdf`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            window.URL.revokeObjectURL(url);
        } else {
            throw new Error('Failed to download batch payslips');
        }
    } catch (error) {
        console.error('Error downloading batch payslips:', error);
        alert('Error downloading batch payslips: ' + error.message);
    }
};

// Print functionality
window.printElement = (elementId) => {
    const element = document.getElementById(elementId);
    if (element) {
        const printWindow = window.open('', '_blank');
        printWindow.document.write('<html><head><title>Print</title>');
        printWindow.document.write('<style>');
        printWindow.document.write(`
            body { font-family: Arial, sans-serif; margin: 20px; }
            .no-print { display: none !important; }
            @media print {
                body { margin: 0; }
                .no-print { display: none !important; }
            }
        `);
        printWindow.document.write('</style></head><body>');
        printWindow.document.write(element.innerHTML);
        printWindow.document.write('</body></html>');
        printWindow.document.close();
        printWindow.focus();
        printWindow.print();
        printWindow.close();
    }
};

// Bootstrap modal helpers
window.showModal = (modalId) => {
    const modal = new bootstrap.Modal(document.getElementById(modalId));
    modal.show();
};

window.hideModal = (modalId) => {
    const modalElement = document.getElementById(modalId);
    const modal = bootstrap.Modal.getInstance(modalElement);
    if (modal) {
        modal.hide();
    }
};

// Chart.js helpers
let chartInstances = {};

window.initializeLineChart = (canvasId, chartData) => {
    try {
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return;
        }

        // Destroy existing chart if it exists
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
        }

        chartInstances[canvasId] = new Chart(ctx, {
            type: 'line',
            data: {
                labels: chartData.labels,
                datasets: chartData.datasets.map(dataset => ({
                    label: dataset.label,
                    data: dataset.data,
                    borderColor: dataset.borderColor || 'rgb(75, 192, 192)',
                    backgroundColor: dataset.backgroundColor || 'rgba(75, 192, 192, 0.2)',
                    borderWidth: dataset.borderWidth || 2,
                    fill: dataset.fill || false,
                    tension: 0.1
                }))
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: chartData.title || 'Chart'
                    },
                    legend: {
                        display: true,
                        position: 'top'
                    }
                },
                scales: {
                    x: {
                        display: true,
                        title: {
                            display: true,
                            text: chartData.xAxisLabel || 'X Axis'
                        }
                    },
                    y: {
                        display: true,
                        title: {
                            display: true,
                            text: chartData.yAxisLabel || 'Y Axis'
                        },
                        beginAtZero: true
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error initializing line chart:', error);
    }
};

window.initializeDoughnutChart = (canvasId, chartData) => {
    try {
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return;
        }

        // Destroy existing chart if it exists
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
        }

        chartInstances[canvasId] = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: chartData.labels,
                datasets: chartData.datasets.map(dataset => ({
                    data: dataset.data,
                    backgroundColor: [
                        '#FF6384',
                        '#36A2EB',
                        '#FFCE56',
                        '#4BC0C0',
                        '#9966FF',
                        '#FF9F40',
                        '#FF6384',
                        '#C9CBCF'
                    ],
                    borderWidth: 1
                }))
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: chartData.title || 'Chart'
                    },
                    legend: {
                        display: true,
                        position: 'bottom'
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error initializing doughnut chart:', error);
    }
};

window.initializeBarChart = (canvasId, chartData) => {
    try {
        const ctx = document.getElementById(canvasId);
        if (!ctx) {
            console.error('Canvas element not found:', canvasId);
            return;
        }

        // Destroy existing chart if it exists
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
        }

        chartInstances[canvasId] = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: chartData.labels,
                datasets: chartData.datasets.map(dataset => ({
                    label: dataset.label,
                    data: dataset.data,
                    backgroundColor: dataset.backgroundColor || 'rgba(54, 162, 235, 0.2)',
                    borderColor: dataset.borderColor || 'rgba(54, 162, 235, 1)',
                    borderWidth: dataset.borderWidth || 1
                }))
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    title: {
                        display: true,
                        text: chartData.title || 'Chart'
                    },
                    legend: {
                        display: true,
                        position: 'top'
                    }
                },
                scales: {
                    x: {
                        display: true,
                        title: {
                            display: true,
                            text: chartData.xAxisLabel || 'X Axis'
                        }
                    },
                    y: {
                        display: true,
                        title: {
                            display: true,
                            text: chartData.yAxisLabel || 'Y Axis'
                        },
                        beginAtZero: true
                    }
                }
            }
        });
    } catch (error) {
        console.error('Error initializing bar chart:', error);
    }
};

window.updateChart = (canvasId, chartData) => {
    try {
        if (chartInstances[canvasId]) {
            const chart = chartInstances[canvasId];
            chart.data.labels = chartData.labels;
            chart.data.datasets = chartData.datasets;
            chart.update();
        }
    } catch (error) {
        console.error('Error updating chart:', error);
    }
};

window.destroyChart = (canvasId) => {
    if (chartInstances[canvasId]) {
        chartInstances[canvasId].destroy();
        delete chartInstances[canvasId];
    }
};

// Ensure Bootstrap dropdown functionality works
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded - initializing dropdowns');

    // Force initialize all dropdowns
    var dropdownElementList = [].slice.call(document.querySelectorAll('.dropdown-toggle'));
    var dropdownList = dropdownElementList.map(function (dropdownToggleEl) {
        return new bootstrap.Dropdown(dropdownToggleEl);
    });

    console.log('Initialized', dropdownList.length, 'dropdowns');

    // Add click handler for debugging
    document.addEventListener('click', function(e) {
        if (e.target.closest('.dropdown-toggle')) {
            console.log('Dropdown toggle clicked');
            const dropdown = e.target.closest('.dropdown-toggle');
            const menu = dropdown.nextElementSibling;
            if (menu && menu.classList.contains('dropdown-menu')) {
                console.log('Found dropdown menu, toggling visibility');
                menu.classList.toggle('show');
            }
        }
    });
});

// User dropdown toggle function
window.toggleUserDropdown = function(event) {
    event.preventDefault();
    console.log('User dropdown toggle clicked');

    const dropdown = event.target.closest('.user-dropdown-toggle');
    const menu = dropdown.nextElementSibling;

    if (menu && menu.classList.contains('user-dropdown-menu')) {
        console.log('Found user dropdown menu');

        // Close any other open dropdowns first
        document.querySelectorAll('.user-dropdown-menu.show').forEach(function(openMenu) {
            if (openMenu !== menu) {
                openMenu.classList.remove('show');
            }
        });

        // Toggle this dropdown
        menu.classList.toggle('show');
        dropdown.setAttribute('aria-expanded', menu.classList.contains('show'));

        console.log('Dropdown toggled. Now showing:', menu.classList.contains('show'));
    }
};

// Additional function to manually show dropdown for testing
window.showUserDropdown = function() {
    console.log('Manual dropdown trigger called');
    const dropdown = document.querySelector('.user-dropdown-toggle');
    if (dropdown) {
        const menu = dropdown.nextElementSibling;
        if (menu) {
            menu.classList.add('show');
            console.log('Dropdown menu forced to show');
        }
    }
};

// Close dropdown when clicking outside
document.addEventListener('click', function(event) {
    if (!event.target.closest('.dropdown')) {
        document.querySelectorAll('.user-dropdown-menu.show').forEach(function(menu) {
            menu.classList.remove('show');
        });
    }
});