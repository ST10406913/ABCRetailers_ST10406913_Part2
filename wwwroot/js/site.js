// wwwroot/js/site.js
$(function () {
    initializeSearchFunctionality();
    initializeStatusUpdates();
    initializeFileUploads();
    initializeAutoRefresh();
});

// Search functionality with debouncing
function initializeSearchFunctionality() {
    const searchInputs = document.querySelectorAll('.search-input');

    searchInputs.forEach(input => {
        let timeoutId;

        input.addEventListener('input', function (e) {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                performSearch(this.value, this.dataset.searchType);
            }, 500);
        });
    });
}

function performSearch(term, type) {
    if (term.length < 2 && term.length > 0) return;

    // Show loading state
    const resultsContainer = document.getElementById('search-results');
    if (resultsContainer) {
        resultsContainer.innerHTML = '<div class="text-center"><div class="loading-spinner"></div> Searching...</div>';
    }

    // AJAX search implementation
    $.get(`/${type}/Search`, { term: term })
        .done(function (data) {
            displaySearchResults(data.results, type);
        })
        .fail(function () {
            console.error('Search failed');
        });
}

function displaySearchResults(results, type) {
    const container = document.getElementById('search-results');
    if (!container) return;

    if (results.length === 0) {
        container.innerHTML = '<div class="text-muted">No results found</div>';
        return;
    }

    let html = '';
    results.forEach(result => {
        html += `
            <div class="search-result-item p-2 border-bottom">
                <a href="/${type}/Details/${result.id}" class="text-decoration-none">
                    ${result.text}
                </a>
            </div>
        `;
    });

    container.innerHTML = html;
}

// Order status updates
function initializeStatusUpdates() {
    // Status update buttons
    $('.status-update-btn').on('click', function (e) {
        e.preventDefault();
        const button = $(this);
        const orderId = button.data('row-key');
        const newStatus = button.data('status');
        const partitionKey = button.data('partition-key');
        const rowKey = button.data('row-key');

        updateOrderStatus(partitionKey, rowKey, newStatus, button);
    });
}

function updateOrderStatus(partitionKey, rowKey, status, button) {
    const originalText = button.html();
    button.prop('disabled', true).html('<span class="loading-spinner"></span> Updating...');

    $.post('/Orders/UpdateStatus', {
        partitionKey: partitionKey,
        rowKey: rowKey,
        status: status
    })
        .done(function (response) {
            if (response.success) {
                showNotification(response.message, 'success');
                // Update the status display
                const statusBadge = $(`[data-order-id="${rowKey}"] .status-badge`);
                statusBadge.removeClass().addClass(`badge status-${status.toLowerCase()}`).text(status);

                // Reload the page after a short delay to reflect changes
                setTimeout(() => {
                    location.reload();
                }, 1500);
            } else {
                showNotification(response.message, 'error');
                button.prop('disabled', false).html(originalText);
            }
        })
        .fail(function () {
            showNotification('Failed to update order status', 'error');
            button.prop('disabled', false).html(originalText);
        });
}

// File upload functionality
function initializeFileUploads() {
    const fileUploadAreas = document.querySelectorAll('.file-upload-area');

    fileUploadAreas.forEach(area => {
        const input = area.querySelector('input[type="file"]');
        const preview = area.querySelector('.file-preview');
        const label = area.querySelector('.upload-label');

        // Click to select file
        area.addEventListener('click', () => input.click());

        // Drag and drop functionality
        area.addEventListener('dragover', (e) => {
            e.preventDefault();
            area.classList.add('dragover');
        });

        area.addEventListener('dragleave', () => {
            area.classList.remove('dragover');
        });

        area.addEventListener('drop', (e) => {
            e.preventDefault();
            area.classList.remove('dragover');

            if (e.dataTransfer.files.length) {
                input.files = e.dataTransfer.files;
                handleFileSelection(input.files[0], preview, label);
            }
        });

        // File selection via input
        input.addEventListener('change', (e) => {
            if (e.target.files.length) {
                handleFileSelection(e.target.files[0], preview, label);
            }
        });
    });
}

function handleFileSelection(file, previewElement, labelElement) {
    if (!file) return;

    // Validate file type and size
    const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'application/pdf'];
    const maxSize = 10 * 1024 * 1024; // 10MB

    if (!validTypes.includes(file.type)) {
        showNotification('Invalid file type. Please select an image or PDF.', 'error');
        return;
    }

    if (file.size > maxSize) {
        showNotification('File size too large. Maximum size is 10MB.', 'error');
        return;
    }

    // Update label
    if (labelElement) {
        labelElement.textContent = file.name;
    }

    // Show preview for images
    if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = (e) => {
            if (previewElement) {
                previewElement.src = e.target.result;
                previewElement.style.display = 'block';
            }
        };
        reader.readAsDataURL(file);
    }
}

// Auto-refresh for real-time updates
function initializeAutoRefresh() {
    // Auto-refresh orders every 30 seconds if on orders page
    if (window.location.pathname.includes('/Orders')) {
        setInterval(() => {
            refreshOrdersCount();
        }, 30000);
    }
}

function refreshOrdersCount() {
    // Implementation for refreshing order counts
    console.log('Refreshing order data...');
}

// Notification system
function showNotification(message, type = 'info') {
    const alertClass = {
        'success': 'alert-success',
        'error': 'alert-danger',
        'warning': 'alert-warning',
        'info': 'alert-info'
    }[type] || 'alert-info';

    const icon = {
        'success': 'fa-check-circle',
        'error': 'fa-exclamation-circle',
        'warning': 'fa-exclamation-triangle',
        'info': 'fa-info-circle'
    }[type] || 'fa-info-circle';

    const notification = $(`
        <div class="alert ${alertClass} alert-dismissible fade show position-fixed" 
             style="top: 20px; right: 20px; z-index: 1050; min-width: 300px;">
            <i class="fas ${icon} me-2"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `);

    $('body').append(notification);

    // Auto-remove after 5 seconds
    setTimeout(() => {
        notification.alert('close');
    }, 5000);
}

// Utility functions
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-ZA', {
        style: 'currency',
        currency: 'ZAR'
    }).format(amount);
}

function formatDate(dateString) {
    return new Date(dateString).toLocaleDateString('en-ZA', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

// Form validation enhancements
function enhanceFormValidation() {
    // Add real-time validation feedback
    $('form').on('blur', 'input, select, textarea', function () {
        const field = $(this);
        if (field.val().trim() !== '') {
            field.addClass('is-valid').removeClass('is-invalid');
        } else if (field.prop('required')) {
            field.addClass('is-invalid').removeClass('is-valid');
        }
    });
}

// Initialize when document is ready
$(function () {
    enhanceFormValidation();
});