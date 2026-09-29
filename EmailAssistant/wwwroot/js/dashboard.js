/**
 * AI Email Assistant - Modern Interactive Dashboard
 * Features: Multi-panel categorization, Live search, Real-time metrics, Native AI reclassification
 */

let allCategoryEmails = {
    'Most Important': [],
    'Important': [],
    'Casual': [],
    'Promotional': []
};
let currentEmailId = null;

$(document).ready(function () {
    // Initial load
    loadDashboard();

    // Provider Card Selection in Fetch Modal
    $('.provider-option-card').on('click', function () {
        $('.provider-option-card').removeClass('active');
        $(this).addClass('active');
        $(this).find('input[type="radio"]').prop('checked', true);
    });

    // Fetch Emails Modal Trigger
    $('#btnFetchEmails').on('click', function () {
        $('#fetchModal').modal('show');
    });

    // Execute Fetch inside Modal
    $('#btnExecuteFetch').on('click', function () {
        executeFetchEmails();
    });

    // Refresh Dashboard with icon animation
    $('#btnRefresh').on('click', function () {
        const $icon = $('#refreshIcon');
        $icon.css('transition', 'transform 0.6s ease');
        $icon.css('transform', 'rotate(360deg)');
        setTimeout(() => $icon.css('transform', 'none'), 600);
        loadDashboard();
    });

    // Live Instant Search Filter
    $('#emailSearchInput').on('input', function () {
        const query = $(this).val().trim().toLowerCase();
        if (query.length > 0) {
            $('#btnClearSearch').show();
        } else {
            $('#btnClearSearch').hide();
        }
        filterEmails(query);
    });

    // Clear Search Button
    $('#btnClearSearch').on('click', function () {
        $('#emailSearchInput').val('').trigger('input');
    });

    // Reclassify Email Button in Detail Modal
    $('#btnReclassify').on('click', function () {
        if (currentEmailId) {
            reclassifyEmail(currentEmailId);
        }
    });
});

/**
 * Load dashboard with all categories and emails
 */
function loadDashboard() {
    showLoading('Syncing Inbox', 'Loading categories and emails...');

    $.ajax({
        url: '/api/Email/categories',
        method: 'GET',
        success: function (categories) {
            let totalEmails = 0;
            let mostImportantCount = 0;
            let importantCount = 0;
            let casualCount = 0;
            let promoCount = 0;

            categories.forEach(function (cat) {
                totalEmails += cat.emailCount;
                if (cat.name === 'Most Important') mostImportantCount = cat.emailCount;
                else if (cat.name === 'Important') importantCount = cat.emailCount;
                else if (cat.name === 'Casual') casualCount = cat.emailCount;
                else if (cat.name === 'Promotional') promoCount = cat.emailCount;

                updateCategoryCount(cat.name, cat.emailCount);
            });

            // Update top stat metrics
            $('#totalEmailsCount').text(totalEmails);
            $('#statCountMostImportant').text(mostImportantCount);
            $('#statCountImportant').text(importantCount);
            $('#statCountCasualPromo').text(casualCount + promoCount);

            // Fetch emails for all 4 categories in parallel
            const categoryNames = ['Most Important', 'Important', 'Casual', 'Promotional'];
            let loaded = 0;

            categoryNames.forEach(function (catName) {
                $.ajax({
                    url: '/api/Email/by-category/' + encodeURIComponent(catName),
                    method: 'GET',
                    success: function (emails) {
                        allCategoryEmails[catName] = emails || [];
                        displayEmailsInPanel(catName, allCategoryEmails[catName]);
                    },
                    error: function (xhr) {
                        console.error('Error fetching category ' + catName, xhr);
                        allCategoryEmails[catName] = [];
                        displayEmailsInPanel(catName, []);
                    },
                    complete: function () {
                        loaded++;
                        if (loaded === categoryNames.length) {
                            hideLoading();
                        }
                    }
                });
            });
        },
        error: function (xhr, status, error) {
            hideLoading();
            console.error('Error loading dashboard:', error);
            showError('Unable to load categories. Please check database connection.');
        }
    });
}

/**
 * Display emails inside a category panel
 */
function displayEmailsInPanel(categoryName, emails) {
    const panelId = 'panel' + categoryName.replace(/\s+/g, '');
    const $panel = $('#' + panelId);
    $panel.empty();

    if (!emails || emails.length === 0) {
        let emptyIcon = '📋';
        let emptyMsg = 'No emails in this category';

        if (categoryName === 'Most Important') {
            emptyIcon = '🛡️';
            emptyMsg = 'No urgent emails. All clear!';
        } else if (categoryName === 'Important') {
            emptyIcon = '📁';
            emptyMsg = 'No active business/work items';
        } else if (categoryName === 'Casual') {
            emptyIcon = '☕';
            emptyMsg = 'No casual conversations';
        } else if (categoryName === 'Promotional') {
            emptyIcon = '🛍️';
            emptyMsg = 'No active marketing deals';
        }

        $panel.html(`
            <div class="empty-state text-center py-5">
                <div class="empty-state-icon mb-2">${emptyIcon}</div>
                <p class="empty-state-text text-secondary mb-0 small">${emptyMsg}</p>
            </div>
        `);
        return;
    }

    emails.forEach(function (email, idx) {
        const cardHtml = createEmailCardHtml(email);
        const $card = $(cardHtml);
        $panel.append($card);
    });
}

/**
 * Build modern email card HTML
 */
function createEmailCardHtml(email) {
    const sender = email.senderName || email.senderEmail || 'Unknown';
    const initial = sender.trim().charAt(0).toUpperCase() || 'E';
    const timeAgo = getTimeAgo(email.receivedTime);
    const subject = email.subject || '(No Subject)';
    const bodyPreview = email.body ? email.body.replace(/\n/g, ' ').substring(0, 140) : '';

    return `
        <div class="email-card" data-email-id="${email.id}" onclick="showEmailDetail(${email.id}, '${escapeHtml(email.categoryName)}')" role="button" tabindex="0">
            <div class="d-flex align-items-center justify-content-between mb-2">
                <div class="d-flex align-items-center gap-2 overflow-hidden">
                    <div class="email-avatar">${initial}</div>
                    <span class="email-sender">${escapeHtml(sender)}</span>
                </div>
                <span class="email-time flex-shrink-0 ms-2">${timeAgo}</span>
            </div>
            <div class="email-subject">${escapeHtml(subject)}</div>
            <div class="email-preview">${escapeHtml(bodyPreview)}</div>
            <div class="email-footer">
                <span class="email-address-chip">✉ ${escapeHtml(email.senderEmail)}</span>
                <span class="badge bg-dark bg-opacity-75 text-secondary extra-small border border-secondary border-opacity-25">${escapeHtml(email.emailProvider || 'Email')}</span>
            </div>
        </div>
    `;
}

/**
 * Filter emails across panels based on live search
 */
function filterEmails(query) {
    const categoryNames = ['Most Important', 'Important', 'Casual', 'Promotional'];

    categoryNames.forEach(function (catName) {
        const allEmails = allCategoryEmails[catName] || [];
        if (!query) {
            displayEmailsInPanel(catName, allEmails);
            updateCategoryCount(catName, allEmails.length);
            return;
        }

        const filtered = allEmails.filter(function (e) {
            const subject = (e.subject || '').toLowerCase();
            const sender = (e.senderName || '').toLowerCase();
            const senderEmail = (e.senderEmail || '').toLowerCase();
            const body = (e.body || '').toLowerCase();

            return subject.includes(query) || sender.includes(query) || senderEmail.includes(query) || body.includes(query);
        });

        displayEmailsInPanel(catName, filtered);
        updateCategoryCount(catName, filtered.length);
    });
}

/**
 * Show detailed email inside the modal
 */
function showEmailDetail(emailId, categoryName) {
    const emails = allCategoryEmails[categoryName] || [];
    const email = emails.find(e => e.id === emailId);

    if (!email) {
        // Fallback: fetch directly
        $.ajax({
            url: '/api/Email/by-category/' + encodeURIComponent(categoryName),
            method: 'GET',
            success: function (list) {
                const found = (list || []).find(e => e.id === emailId);
                if (found) renderDetailModal(found);
            }
        });
        return;
    }

    renderDetailModal(email);
}

function renderDetailModal(email) {
    currentEmailId = email.id;
    const formattedDate = new Date(email.receivedTime).toLocaleString(undefined, {
        dateStyle: 'medium',
        timeStyle: 'short'
    });

    // Update badges
    const categoryBadge = $('#modalCategoryBadge');
    categoryBadge.text(email.categoryDisplayName || email.categoryName);
    categoryBadge.css('background-color', email.categoryColorCode || '#3b82f6');
    categoryBadge.css('color', '#ffffff');

    $('#modalProviderBadge').text(email.emailProvider || 'Provider');

    const html = `
        <div class="email-detail-container">
            <h4 class="fw-bold text-white mb-3">${escapeHtml(email.subject)}</h4>
            
            <div class="d-flex align-items-center justify-content-between p-3 rounded-3 bg-dark bg-opacity-50 border border-secondary border-opacity-25 mb-4">
                <div class="d-flex align-items-center gap-2.5">
                    <div class="email-avatar fs-6" style="width: 38px; height: 38px;">
                        ${(email.senderName || 'U').charAt(0).toUpperCase()}
                    </div>
                    <div>
                        <div class="fw-bold text-white">${escapeHtml(email.senderName || 'Unknown')}</div>
                        <div class="text-secondary extra-small">${escapeHtml(email.senderEmail)}</div>
                    </div>
                </div>
                <div class="text-end">
                    <span class="text-secondary small d-block">${formattedDate}</span>
                </div>
            </div>

            <div class="email-body-wrapper p-3.5 rounded-3 bg-dark bg-opacity-25 border border-secondary border-opacity-25">
                <div class="text-secondary small fw-bold text-uppercase mb-2">Message Content</div>
                <div class="email-body-text text-light" style="white-space: pre-wrap; line-height: 1.6; font-size: 0.92rem;">${escapeHtml(email.body)}</div>
            </div>
        </div>
    `;

    $('#emailDetailContent').html(html);
    $('#emailDetailModal').modal('show');
}

/**
 * Execute email fetching from Gmail or Outlook
 */
function executeFetchEmails() {
    const provider = $('input[name="emailProviderRadio"]:checked').val() || 'Gmail';
    const count = parseInt($('#fetchCount').val() || '20');

    $('#fetchModal').modal('hide');
    showLoading(`Fetching from ${provider}`, 'Connecting to API and running native AI classification...');

    $.ajax({
        url: '/api/Email/fetch',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({
            provider: provider,
            maxEmails: count,
            fromDate: null,
            toDate: null
        }),
        success: function (response) {
            hideLoading();
            showSuccess(`Successfully fetched and classified ${response.count} emails from ${provider}!`);
            loadDashboard();
        },
        error: function (xhr) {
            hideLoading();
            let msg = `Error fetching emails from ${provider}`;
            let details = '';

            if (xhr.responseJSON) {
                if (xhr.responseJSON.error) msg = xhr.responseJSON.error;
                if (xhr.responseJSON.details) details = xhr.responseJSON.details;
            } else if (xhr.responseText) {
                details = xhr.responseText;
            }

            showError(msg + (details ? `\n\n${details}` : ''));
        }
    });
}

/**
 * Reclassify a specific email on-demand using Native AI
 */
function reclassifyEmail(emailId) {
    showLoading('Reclassifying', 'Running native AI classification engine...');

    $.ajax({
        url: '/api/Email/' + emailId + '/reclassify',
        method: 'POST',
        success: function (updated) {
            hideLoading();
            showSuccess(`Email reclassified as "${updated.categoryName}"!`);
            renderDetailModal(updated);
            loadDashboard();
        },
        error: function () {
            hideLoading();
            showError('Failed to reclassify email.');
        }
    });
}

/**
 * Utility functions
 */
function updateCategoryCount(categoryName, count) {
    const countId = 'count' + categoryName.replace(/\s+/g, '');
    $('#' + countId).text(count);
}

function getTimeAgo(dateString) {
    if (!dateString) return '';
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now - date;
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;
    return date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function showLoading(title, subtitle) {
    if (title) $('#loadingTitle').text(title);
    if (subtitle) $('#loadingSubtitle').text(subtitle);
    $('#loadingSpinner').fadeIn(150);
}

function hideLoading() {
    $('#loadingSpinner').fadeOut(150);
}

function showSuccess(message) {
    alert(`✅ ${message}`);
}

function showError(message) {
    alert(`❌ ${message}`);
}
