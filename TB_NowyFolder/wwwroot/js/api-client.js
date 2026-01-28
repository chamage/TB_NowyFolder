// Global variables
const apiBaseUrl = '/api';
let guests = [];
let rooms = [];
let services = [];
let reservations = [];

// Initialize
$(document).ready(function() {
    loadGuests();
});

// Navigation
function switchTab(tabName) {
    // Update active tab styling
    $('.list-group-item').removeClass('active');
    $(`#tab-${tabName}`).addClass('active');
    
    // Show selected section
    $('.content-section').addClass('d-none');
    $(`#section-${tabName}`).removeClass('d-none');
    
    // Load data for the section
    switch(tabName) {
        case 'guests': loadGuests(); break;
        case 'rooms': loadRooms(); break;
        case 'services': loadServices(); break;
        case 'reservations': loadReservations(); break;
    }
}

// Formatters
function formatCurrency(amount) {
    let locale = 'en-US';
    if (typeof currentCulture !== 'undefined') {
        locale = currentCulture;
    }
    // Simple mapping: if polish, use PLN, else USD default
    let currency = 'USD';
    if (locale.startsWith('pl') || locale === 'pl') {
        currency = 'PLN';
    }
    
    return new Intl.NumberFormat(locale, { style: 'currency', currency: currency }).format(amount);
}

function formatDate(dateString) {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString();
}

// Helper to get localized string or fallback
function t(key) {
    if (typeof resources !== 'undefined' && resources[key]) {
        return resources[key];
    }
    return key;
}

// Guest Functions
function loadGuests() {
    $.get(`${apiBaseUrl}/guests`, function(data) {
        guests = data;
        let html = `<table class="table table-hover"><thead><tr><th>${t("ID")}</th><th>${t("Name")}</th><th>${t("Email")}</th><th>${t("Phone")}</th><th>${t("Actions")}</th></tr></thead><tbody>`;
        
        if (data.length === 0) {
            html += `<tr><td colspan="5" class="text-center">${t("No guests found")}</td></tr>`;
        } else {
            data.forEach(guest => {
                html += `<tr>
                    <td>${guest.guestID}</td>
                    <td>${guest.firstName} ${guest.lastName}</td>
                    <td>${guest.email}</td>
                    <td>${guest.phone || '-'}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteGuest(${guest.guestID})">${t("Delete")}</button>
                    </td>
                </tr>`;
            });
        }
        
        html += '</tbody></table>';
        $('#guests-list').html(html);
        
        // Update selection list
        updateGuestSelect();
    }).fail(function() {
        $('#guests-list').html('<div class="alert alert-danger">Failed to load guests. Make sure the API is running.</div>');
    });
}

function showAddGuestModal() {
    $('#addGuestForm')[0].reset();
    new bootstrap.Modal('#addGuestModal').show();
}

function createGuest() {
    const formData = {
        firstName: $('#addGuestForm input[name="firstName"]').val(),
        lastName: $('#addGuestForm input[name="lastName"]').val(),
        email: $('#addGuestForm input[name="email"]').val(),
        phone: $('#addGuestForm input[name="phone"]').val()
    };
    
    $.ajax({
        url:(`${apiBaseUrl}/guests`),
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function() {
            bootstrap.Modal.getInstance('#addGuestModal').hide();
            loadGuests();
            alert(t('Guest created successfully!'));
        },
        error: function(xhr) {
            alert(t('Error creating guest: ') + xhr.statusText);
        }
    });
}

function deleteGuest(id) {
    if (!confirm(t('Are you sure you want to delete this guest?'))) return;
    
    $.ajax({
        url: `${apiBaseUrl}/guests/${id}`,
        type: 'DELETE',
        success: function() {
            loadGuests();
        },
        error: function() {
            alert(t('Could not delete guest. They may have active reservations.'));
        }
    });
}

// Room Functions
function loadRooms(availableOnly = false) {
    const endpoint = availableOnly ? `${apiBaseUrl}/rooms/available` : `${apiBaseUrl}/rooms`;
    
    $.get(endpoint, function(data) {
        rooms = data;
        let html = `<table class="table table-hover"><thead><tr><th>${t("Room #")}</th><th>${t("Type")}</th><th>${t("Capacity")}</th><th>${t("Price")}</th><th>${t("Status")}</th></tr></thead><tbody>`;
        
        if (data.length === 0) {
            html += `<tr><td colspan="5" class="text-center">${t("No rooms found")}</td></tr>`;
        } else {
            data.forEach(room => {
                const statusBadge = room.status === 'Available' ? 'bg-success' : 'bg-secondary';
                html += `<tr style="cursor: pointer;" onclick="showRoomDetails(${room.roomID})">
                    <td><strong>${room.roomNumber}</strong></td>
                    <td>${room.roomType ? room.roomType.typeName : t('Unknown')}</td>
                    <td>${room.capacity} ${t("pers.")}</td>
                    <td>${formatCurrency(room.pricePerNight)}</td>
                    <td><span class="badge ${statusBadge}">${room.status}</span></td>
                </tr>`;
            });
        }
        
        html += '</tbody></table>';
        $('#rooms-list').html(html);
    });
}

function showRoomDetails(id) {
    const room = rooms.find(r => r.roomID === id);
    if (!room) return;
    
    let html = `
        <div class="mb-3">
            <h3>${t("Room #")} ${room.roomNumber}</h3>
            <span class="badge ${room.status === 'Available' ? 'bg-success' : 'bg-secondary'} mb-3">${t(room.status)}</span>
        </div>
        <p>
            <strong>${t("Type")}:</strong> ${room.roomType ? room.roomType.typeName : t('Unknown')}<br>
            <strong>${t("Standard")}:</strong> ${room.roomType ? room.roomType.standard : '-'}<br>
            <strong>${t("Capacity")}:</strong> ${room.capacity} ${t("pers.")}<br>
            <strong>${t("Price per Night")}:</strong> ${formatCurrency(room.pricePerNight)}
        </p>
        <p class="text-muted small">
            ${room.roomType && room.roomType.description ? room.roomType.description : ''}
        </p>
    `;
    
    $('#roomDetailsContent').html(html);
    new bootstrap.Modal('#roomDetailsModal').show();
}

// Service Functions
function loadServices() {
    $.get(`${apiBaseUrl}/services`, function(data) {
        services = data;
        let html = `<table class="table table-hover"><thead><tr><th>${t("Service")}</th><th>${t("Description")}</th><th>${t("Price")}</th><th>${t("Availability")}</th></tr></thead><tbody>`;
        
        if (data.length === 0) {
            html += `<tr><td colspan="4" class="text-center">${t("No services found")}</td></tr>`;
        } else {
            data.forEach(service => {
                html += `<tr style="cursor: pointer;" onclick="showServiceDetails(${service.serviceID})">
                    <td><strong>${service.serviceName}</strong></td>
                    <td>${service.description || '-'}</td>
                    <td>${formatCurrency(service.unitPrice)}</td>
                    <td>${service.availability}</td>
                </tr>`;
            });
        }
        
        html += '</tbody></table>';
        $('#services-list').html(html);
    });
}

function showServiceDetails(id) {
    const service = services.find(s => s.serviceID === id);
    if (!service) return;
    
    let html = `
        <h3>${service.serviceName}</h3>
        <p class="lead">${formatCurrency(service.unitPrice)}</p>
        <p>
            <strong>${t("Availability")}:</strong> ${t(service.availability)}<br>
        </p>
        <p>${service.description || t('No description available.')}</p>
    `;
    
    $('#serviceDetailsContent').html(html);
    new bootstrap.Modal('#serviceDetailsModal').show();
}

// Reservation Functions
function loadReservations() {
    $.get(`${apiBaseUrl}/reservations`, function(data) {
        reservations = data;
        let html = `<table class="table table-hover"><thead><tr><th>${t("ID")}</th><th>${t("Guest")}</th><th>${t("Dates")}</th><th>${t("rooms")}</th><th>${t("Total")}</th><th>${t("Status")}</th><th>${t("Actions")}</th></tr></thead><tbody>`;
        
        if (data.length === 0) {
            html += `<tr><td colspan="7" class="text-center">${t("No reservations found")}</td></tr>`;
        } else {
            data.forEach(res => {
                const guestName = res.guest ? `${res.guest.firstName} ${res.guest.lastName}` : t('Unknown');
                const roomCount = res.reservationRooms ? res.reservationRooms.length : 0;
                
                html += `<tr style="cursor: pointer;" onclick="showReservationDetails(${res.reservationID}, event)">
                    <td>${res.reservationID}</td>
                    <td>${guestName}</td>
                    <td>${formatDate(res.checkInDate)} - ${formatDate(res.checkOutDate)}</td>
                    <td>${roomCount} ${t("rooms")}</td>
                    <td>${formatCurrency(res.totalPrice)}</td>
                    <td>${res.reservationStatus}</td>
                    <td>
                        <button class="btn btn-sm btn-outline-primary me-1" onclick="showAddRoomToReservation(${res.reservationID}, event)">${t("Add Room")}</button>
                        <button class="btn btn-sm btn-outline-info me-1" onclick="showAddServiceToReservation(${res.reservationID}, event)">${t("Add Service")}</button>
                        <button class="btn btn-sm btn-outline-danger" onclick="deleteReservation(${res.reservationID}, event)">${t("Delete")}</button>
                    </td>
                </tr>`;
            });
        }
        
        html += '</tbody></table>';
        $('#reservations-list').html(html);
    });
}

function showReservationDetails(id, event) {
    // If event is provided, check if it was triggered by a button click
    if (event && (event.target.tagName === 'BUTTON' || event.target.closest('button'))) {
        return;
    }

    const reservation = reservations.find(r => r.reservationID === id);
    if (!reservation) return;

    let html = `
        <div class="row mb-3">
            <div class="col-md-6">
                <h6>${t("Guest Information")}</h6>
                <p>
                    <strong>${t("Name")}:</strong> ${reservation.guest ? reservation.guest.firstName + ' ' + reservation.guest.lastName : t('Unknown')}<br>
                    <strong>${t("Email")}:</strong> ${reservation.guest ? reservation.guest.email : '-'}<br>
                    <strong>${t("Phone")}:</strong> ${reservation.guest ? reservation.guest.phone || '-' : '-'}
                </p>
            </div>
            <div class="col-md-6">
                <h6>${t("Reservation Info")}</h6>
                <p>
                    <strong>${t("ID")}:</strong> #${reservation.reservationID}<br>
                    <strong>${t("Dates")}:</strong> ${formatDate(reservation.checkInDate)} - ${formatDate(reservation.checkOutDate)}<br>
                    <strong>${t("Status")}:</strong> ${reservation.reservationStatus}<br>
                    <strong>${t("Total Price")}:</strong> ${formatCurrency(reservation.totalPrice)}
                </p>
            </div>
        </div>
        
        <h6>${t("Rooms")}</h6>
        <table class="table table-sm table-bordered mb-3">
            <thead><tr><th>${t("Room #")}</th><th>${t("Type")}</th><th>${t("Price/Night")}</th></tr></thead>
            <tbody>
    `;

    if (reservation.reservationRooms && reservation.reservationRooms.length > 0) {
        reservation.reservationRooms.forEach(rr => {
            html += `<tr>
                <td>${rr.room ? rr.room.roomNumber : '-'}</td>
                <td>${rr.room && rr.room.roomType ? rr.room.roomType.typeName : '-'}</td>
                <td>${formatCurrency(rr.pricePerNight)}</td>
            </tr>`;
        });
    } else {
        html += `<tr><td colspan="3" class="text-center text-muted">${t("No rooms assigned")}</td></tr>`;
    }

    html += `
            </tbody>
        </table>

        <h6>${t("Services")}</h6>
        <table class="table table-sm table-bordered">
            <thead><tr><th>${t("Service")}</th><th>${t("Date")}</th><th>${t("Quantity")}</th><th>${t("Unit Price")}</th></tr></thead>
            <tbody>
    `;

    if (reservation.reservationServices && reservation.reservationServices.length > 0) {
        reservation.reservationServices.forEach(rs => {
            html += `<tr>
                <td>${rs.service ? rs.service.serviceName : '-'}</td>
                <td>${formatDate(rs.serviceDate)}</td>
                <td>${rs.quantity}</td>
                <td>${rs.service ? formatCurrency(rs.service.unitPrice) : '-'}</td>
            </tr>`;
        });
    } else {
        html += `<tr><td colspan="4" class="text-center text-muted">${t("No extra services")}</td></tr>`;
    }

    html += `</tbody></table>`;

    $('#reservationDetailsContent').html(html);
    new bootstrap.Modal('#reservationDetailsModal').show();
}

function deleteReservation(id, event) {
    if (event) event.stopPropagation();
    if (!confirm(t('Are you sure you want to delete this reservation?'))) return; // Localized
    
    $.ajax({
        url: `${apiBaseUrl}/reservations/${id}`,
        type: 'DELETE',
        success: function() {
            loadReservations();
        },
        error: function() {
            alert(t('Could not delete reservation.')); // Need to add this key if not present, but for now simple alert.
        }
    });
}

function updateGuestSelect() {
    const select = $('#reservationGuestSelect');
    select.empty();
    select.append(`<option value="">${t("Select Guest...")}</option>`); // Localized
    
    guests.forEach(guest => {
        select.append(`<option value="${guest.guestID}">${guest.firstName} ${guest.lastName}</option>`);
    });
}

function showAddReservationModal() {
    // Ensure data is loaded
    if (guests.length === 0) loadGuests();
    
    $('#addReservationForm')[0].reset();
    new bootstrap.Modal('#addReservationModal').show();
}

function createReservation() {
    const guestId = $('#reservationGuestSelect').val();
    if (!guestId) {
        alert('Please select a guest');
        return;
    }
    
    const formData = {
        guestID: parseInt(guestId),
        checkInDate: $('#addReservationForm input[name="checkInDate"]').val(),
        checkOutDate: $('#addReservationForm input[name="checkOutDate"]').val(),
        numberOfGuests: parseInt($('#addReservationForm input[name="numberOfGuests"]').val()),
        totalPrice: 0, // Will be calculated based on rooms later
        reservationStatus: 'Confirmed'
    };
    
    $.ajax({
        url: `${apiBaseUrl}/reservations`,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function(response) {
            bootstrap.Modal.getInstance('#addReservationModal').hide();
            loadReservations();
            alert(t('Reservation #') + response.reservationID + t(' created!'));
        },
        error: function(xhr) {
            alert(t('Error creating reservation: ') + xhr.statusText);
        }
    });
}

// Add Room to Reservation
function showAddRoomToReservation(reservationId, event) {
    if (event) event.stopPropagation();
    $('#roomReservationId').val(reservationId);
    
    // Load available rooms
    $.get(`${apiBaseUrl}/rooms/available`, function(data) {
        const select = $('#roomSelect');
        select.empty();
        select.append(`<option value="">${t("Select Room...")}</option>`); // Localized
        
        data.forEach(room => {
            select.append(`<option value="${room.roomID}">${room.roomNumber} - ${room.roomType ? room.roomType.typeName : ''} (${formatCurrency(room.pricePerNight)})</option>`);
        });
        
        new bootstrap.Modal('#addRoomToReservationModal').show();
    });
}

function submitAddRoom() {
    const reservationId = $('#roomReservationId').val();
    const roomId = $('#roomSelect').val();
    
    if (!roomId) {
        alert('Please select a room');
        return;
    }
    
    $.ajax({
        url: `${apiBaseUrl}/reservations/${reservationId}/rooms/${roomId}`,
        type: 'POST',
        success: function() {
            bootstrap.Modal.getInstance('#addRoomToReservationModal').hide();
            loadReservations(); // This reloads the table with updated prices
            alert(t('Room added successfully!'));
        },
        error: function(xhr) {
            alert('Error adding room: ' + xhr.statusText);
        }
    });
}

// Add Service to Reservation
function showAddServiceToReservation(reservationId, event) {
    if (event) event.stopPropagation();
    $('#serviceReservationId').val(reservationId);
    
    // Set default date to today
    const TODAY = new Date().toISOString().split('T')[0];
    $('#serviceDate').val(TODAY);

    // Load services
    $.get(`${apiBaseUrl}/services`, function(data) {
        const select = $('#serviceSelect');
        select.empty();
        select.append(`<option value="">${t("Select Service...")}</option>`); // Localized
        
        data.forEach(service => {
            select.append(`<option value="${service.serviceID}">${service.serviceName} (${formatCurrency(service.unitPrice)})</option>`);
        });
        
        new bootstrap.Modal('#addServiceToReservationModal').show();
    });
}

function submitAddService() {
    const reservationId = $('#serviceReservationId').val();
    const serviceId = $('#serviceSelect').val();
    const quantity = $('#serviceQuantity').val();
    const serviceDate = $('#serviceDate').val();
    
    if (!serviceId || !serviceDate) {
        alert('Please fill all fields');
        return;
    }
    
    const data = {
        quantity: parseInt(quantity),
        serviceDate: serviceDate
    };
    
    $.ajax({
        url: `${apiBaseUrl}/reservations/${reservationId}/services/${serviceId}`,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function() {
            bootstrap.Modal.getInstance('#addServiceToReservationModal').hide();
            loadReservations(); // This reloads the table with updated prices
            alert(t('Service added successfully!'));
        },
        error: function(xhr) {
            alert('Error adding service: ' + xhr.statusText);
        }
    });
}
