// Shopping List Import JavaScript
"use strict";

// Global variables
let connection = null;
let isProcessing = false;

// Initialize the application when the DOM is ready
document.addEventListener('DOMContentLoaded', function () {
    initializeSignalR();
    initializeEventHandlers();
});

// Initialize SignalR connection
async function initializeSignalR() {
    connection = new signalR.HubConnectionBuilder()
        .withUrl("/progressHub")
        .build();

    // Handle progress updates
    connection.on("ProgressUpdate", function (progressData) {
        updateProgress(progressData);
    });

    try {
        await connection.start();
        console.log("SignalR Connected");
    } catch (err) {
        console.error("SignalR Connection Error: ", err);
        showAlert("Connection to progress hub failed. Real-time updates may not work.", "warning");
    }
}

// Initialize event handlers
function initializeEventHandlers() {
    // Form submission
    document.getElementById('shoppingListForm').addEventListener('submit', handleFormSubmission);
    
    // Test connection button
    document.getElementById('testConnectionButton').addEventListener('click', testConnection);
    
    // Enable/disable form based on processing state
    setFormState(true);
}

// Handle form submission
async function handleFormSubmission(event) {
    event.preventDefault();
    
    if (isProcessing) {
        return;
    }

    const name = document.getElementById('shoppingListName').value.trim();
    const content = document.getElementById('shoppingListContent').value.trim();
    const boardId = document.getElementById('boardId').value.trim();

    // Validate input
    if (!name || !content) {
        showAlert('Please provide both a name and content for the shopping list.', 'danger');
        return;
    }

    // Prepare request data
    const requestData = {
        name: name,
        content: content,
        boardId: boardId || null
    };

    try {
        setFormState(false);
        showProgress(true);
        resetProgress();

        // Make API call
        const response = await fetch('/api/shoppinglist/process', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(requestData)
        });

        const result = await response.json();

        if (response.ok && result.success) {
            // Join the progress group for this connection
            if (connection && connection.state === signalR.HubConnectionState.Connected) {
                await connection.invoke("JoinProgressGroup", result.connectionId);
            }

            // Show success message after processing completes
            setTimeout(() => {
                showResult(result);
                setFormState(true);
            }, 1000);
        } else {
            throw new Error(result.error || result.message || 'An error occurred while processing the shopping list.');
        }
    } catch (error) {
        console.error('Error processing shopping list:', error);
        showAlert(`Error: ${error.message}`, 'danger');
        setFormState(true);
        showProgress(false);
    }
}

// Test Trello connection
async function testConnection() {
    const button = document.getElementById('testConnectionButton');
    const status = document.getElementById('connectionStatus');
    
    button.disabled = true;
    button.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Testing...';
    
    try {
        const response = await fetch('/api/shoppinglist/test-connection');
        const result = await response.json();
        
        if (result.success) {
            status.innerHTML = '<span class="text-success"><i class="fas fa-check me-1"></i>Connection successful!</span>';
        } else {
            status.innerHTML = '<span class="text-danger"><i class="fas fa-times me-1"></i>Connection failed: ' + result.message + '</span>';
        }
    } catch (error) {
        status.innerHTML = '<span class="text-danger"><i class="fas fa-times me-1"></i>Connection test failed: ' + error.message + '</span>';
    } finally {
        button.disabled = false;
        button.innerHTML = '<i class="fas fa-wifi me-2"></i>Test Trello Connection';
    }
}

// Update progress display
function updateProgress(progressData) {
    const progressBar = document.getElementById('progressBar');
    const progressText = document.getElementById('progressText');
    const currentCategory = document.getElementById('currentCategory');
    const currentItem = document.getElementById('currentItem');
    const statusText = document.getElementById('statusText');

    // Update progress bar
    const percentage = Math.round(progressData.progressPercentage);
    progressBar.style.width = percentage + '%';
    progressBar.textContent = percentage + '%';

    // Update text fields
    progressText.textContent = `${progressData.processedItems} / ${progressData.totalItems} items`;
    currentCategory.textContent = progressData.currentCategory || '-';
    currentItem.textContent = progressData.currentItem || '-';

    // Update status
    if (progressData.isCompleted) {
        statusText.innerHTML = '<span class="badge bg-success">Completed!</span>';
        progressBar.classList.remove('progress-bar-animated');
    } else {
        statusText.innerHTML = '<span class="badge bg-info">Processing...</span>';
    }
}

// Show/hide progress card
function showProgress(show) {
    const progressCard = document.getElementById('progressCard');
    progressCard.style.display = show ? 'block' : 'none';
}

// Reset progress display
function resetProgress() {
    updateProgress({
        totalItems: 0,
        processedItems: 0,
        progressPercentage: 0,
        currentCategory: 'Initializing...',
        currentItem: '',
        isCompleted: false
    });
}

// Show result card
function showResult(result) {
    const resultCard = document.getElementById('resultCard');
    const resultHeader = document.getElementById('resultHeader');
    const resultBody = document.getElementById('resultBody');

    resultHeader.innerHTML = `
        <h5 class="mb-0">
            <i class="fas fa-check-circle me-2 text-success"></i>
            Import Completed Successfully
        </h5>
    `;

    resultBody.innerHTML = `
        <div class="row">
            <div class="col-md-6">
                <p><strong>Shopping List:</strong> ${result.message}</p>
                <p><strong>Board:</strong> ${result.boardName}</p>
                <p><strong>Categories:</strong> ${result.categoriesCount}</p>
                <p><strong>Total Items:</strong> ${result.totalItems}</p>
            </div>
            <div class="col-md-6 text-end">
                <a href="${result.boardUrl}" target="_blank" class="btn btn-primary">
                    <i class="fas fa-external-link-alt me-2"></i>
                    Open in Trello
                </a>
            </div>
        </div>
    `;

    resultCard.style.display = 'block';
}

// Set form enabled/disabled state
function setFormState(enabled) {
    isProcessing = !enabled;
    
    const form = document.getElementById('shoppingListForm');
    const inputs = form.querySelectorAll('input, textarea, button');
    
    inputs.forEach(input => {
        input.disabled = !enabled;
    });

    const importButton = document.getElementById('importButton');
    if (enabled) {
        importButton.innerHTML = '<i class="fas fa-upload me-2"></i>Import to Trello';
    } else {
        importButton.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Importing...';
    }
}

// Show alert message
function showAlert(message, type = 'info') {
    // Create alert element
    const alertDiv = document.createElement('div');
    alertDiv.className = `alert alert-${type} alert-dismissible fade show`;
    alertDiv.innerHTML = `
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
    `;

    // Insert at the top of the container
    const container = document.querySelector('.container .row .col-md-8');
    container.insertBefore(alertDiv, container.firstChild);

    // Auto-dismiss after 5 seconds
    setTimeout(() => {
        if (alertDiv.parentNode) {
            alertDiv.remove();
        }
    }, 5000);
}