// Global variables
const apiBaseUrl = '/api';
let guests = [];
let rooms = [];
let services = [];
let reservations = [];

let authToken = localStorage.getItem('authToken') || '';
let currentUser = JSON.parse(localStorage.getItem('authUser') || 'null');

// Initialize
$(document).ready(function () {
    updateAuthUI();
    applyRbacToUi();
    switchTab('rooms');
});

function parseJwt(token) {
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        return JSON.parse(atob(base64));
    } catch {
        return null;
    }
}

function hasRole(role) {
    return currentUser && currentUser.role === role;
}

function isAuthenticated() {
    return !!authToken;
}

function canManageGuests() {
    return hasRole('Receptionist') || hasRole('Administrator');
}

function canManageCatalogWrite() {
    return hasRole('Administrator');
}

function canReadReservations() {
    return hasRole('Client') || hasRole('Receptionist') || hasRole('Administrator');
}

function canModifyReservations() {
    return hasRole('Client') || hasRole('Receptionist') || hasRole('Administrator');
}

function fillDemoLogin(type) {
    if (type === 'admin') {
        $('#auth-username').val('admin');
        $('#auth-password').val('admin123!');
    } else if (type === 'reception') {
        $('#auth-username').val('reception');
        $('#auth-password').val('reception123!');
    } else if (type === 'client') {
        $('#auth-username').val('client');
        $('#auth-password').val('client123!');
    }
}

function login() {
    const username = $('#auth-username').val().trim();
    const password = $('#auth-password').val().trim();

    if (!username || !password) {
        showAuthMessage('Please enter username and password.', 'warning');
        return;
    }

    $.ajax({
        url: `${apiBaseUrl}/auth/token`,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ username, password }),
        success: function (response) {
            authToken = response.accessToken;
            const payload = parseJwt(authToken) || {};
            const role = payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || response.role;
            const name = payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || response.username || username;
            const guestId = payload.guestId ? parseInt(payload.guestId) : null;

            currentUser = { username: name, role: role || 'Unknown', guestId: Number.isNaN(guestId) ? null : guestId };
            localStorage.setItem('authToken', authToken);
            localStorage.setItem('authUser', JSON.stringify(currentUser));

            $('#auth-password').val('');
            showAuthMessage('Logged in successfully.', 'success');
            updateAuthUI();
            applyRbacToUi();
            switchTab('rooms');
        },
        error: function () {
            showAuthMessage('Invalid login credentials.', 'danger');
        }
    });
}

function logout() {
    authToken = '';
    currentUser = null;
    localStorage.removeItem('authToken');
    localStorage.removeItem('authUser');
    showAuthMessage('Logged out.', 'secondary');
    updateAuthUI();
    applyRbacToUi();
    switchTab('rooms');
}

function updateAuthUI() {
    if (isAuthenticated() && currentUser) {
        $('#auth-logged-out-panel').addClass('d-none');
        $('#auth-logged-in-panel').removeClass('d-none');
        $('#auth-username-display').text(currentUser.username);
        $('#auth-role-display').text(currentUser.role);
    } else {
        $('#auth-logged-out-panel').removeClass('d-none');
        $('#auth-logged-in-panel').addClass('d-none');
        $('#auth-username-display').text('-');
        $('#auth-role-display').text('-');
    }
}

function showAuthMessage(message, type) {
    $('#auth-message').html(`<div class="alert alert-${type} py-2 mb-0">${message}</div>`);
}

function authHeaders() {
    return authToken ? { Authorization: `Bearer ${authToken}` } : {};
}

function handleApiError(xhr, customMessage) {
    if (xhr.status === 401) {
        showAuthMessage('Authentication is required.', 'warning');
        return;
    }
    if (xhr.status === 403) {
        showAuthMessage('You do not have permission for this action.', 'danger');
        return;
    }
    alert(customMessage || `API error: ${xhr.status} ${xhr.statusText}`);
}

function applyRbacToUi() {
    $('#btn-add-guest').toggle(canManageGuests());
    $('#btn-add-room').toggle(canManageCatalogWrite());
    $('#btn-add-service').toggle(canManageCatalogWrite());
    $('#btn-add-reservation').toggle(canModifyReservations());

    $('#tab-guests').toggle(canManageGuests());
    $('#tab-reservations').toggle(canReadReservations());
}

// Navigation
function switchTab(tabName) {
    if (tabName === 'guests' && !canManageGuests()) {
        showAuthMessage('Guests tab requires Receptionist or Administrator role.', 'warning');
        return;
    }

    if (tabName === 'reservations' && !canReadReservations()) {
        showAuthMessage('Reservations tab requires Client, Receptionist or Administrator role.', 'warning');
        return;
    }

    // Update active tab styling
    $('.list-group-item').removeClass('active');
    $(`#tab-${tabName}`).addClass('active');

    // Show selected section
    $('.content-section').addClass('d-none');
    $(`#section-${tabName}`).removeClass('d-none');

    // Load data for the section
    switch (tabName) {
        case 'guests': loadGuests(); break;
        case 'rooms': loadRooms(); break;
        case 'services': loadServices(); break;
        case 'reservations': loadReservations(); break;
    }
}

// Formatters
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' }).format(amount || 0);
}

function formatDate(dateString) {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString();
}

// Guest Functions
function loadGuests() {
    $.ajax({
        url: `${apiBaseUrl}/guests`,
        headers: authHeaders(),
        success: function (data) {
            guests = data;
            let html = '<table class="table table-hover"><thead><tr><th>ID</th><th>Name</th><th>Email</th><th>Phone</th><th>Actions</th></tr></thead><tbody>';

            if (data.length === 0) {
                html += '<tr><td colspan="5" class="text-center">No guests found</td></tr>';
            } else {
                data.forEach(guest => {
                    html += `<tr>
                        <td>${guest.guestID}</td>
                        <td>${guest.firstName} ${guest.lastName}</td>
                        <td>${guest.email}</td>
                        <td>${guest.phone || '-'}</td>
                        <td><button class="btn btn-sm btn-outline-danger" onclick="deleteGuest(${guest.guestID})">Delete</button></td>
                    </tr>`;
                });
            }

            html += '</tbody></table>';
            $('#guests-list').html(html);
            updateGuestSelect();
        },
        error: function (xhr) {
            $('#guests-list').html('<div class="alert alert-warning">Guest list is not available for this role.</div>');
            handleApiError(xhr);
        }
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
        url: `${apiBaseUrl}/guests`,
        type: 'POST',
        headers: authHeaders(),
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function () {
            bootstrap.Modal.getInstance('#addGuestModal').hide();
            loadGuests();
            alert('Guest created successfully.');
        },
        error: function (xhr) {
            handleApiError(xhr, 'Error creating guest.');
        }
    });
}

function deleteGuest(id) {
    if (!confirm('Are you sure you want to delete this guest?')) return;

    $.ajax({
        url: `${apiBaseUrl}/guests/${id}`,
        type: 'DELETE',
        headers: authHeaders(),
        success: function () { loadGuests(); },
        error: function (xhr) { handleApiError(xhr, 'Could not delete guest.'); }
    });
}

// Room Functions
function loadRooms(availableOnly = false) {
    const endpoint = availableOnly ? `${apiBaseUrl}/rooms/available` : `${apiBaseUrl}/rooms`;

    $.ajax({
        url: endpoint,
        headers: authHeaders(),
        success: function (data) {
            rooms = data;
            let html = '<table class="table table-hover"><thead><tr><th>Room #</th><th>Type</th><th>Capacity</th><th>Price</th><th>Status</th></tr></thead><tbody>';

            if (data.length === 0) {
                html += '<tr><td colspan="5" class="text-center">No rooms found</td></tr>';
            } else {
                data.forEach(room => {
                    const statusBadge = room.status === 'Available' ? 'bg-success' : 'bg-secondary';
                    html += `<tr style="cursor: pointer;" onclick="showRoomDetails(${room.roomID})">
                        <td><strong>${room.roomNumber}</strong></td>
                        <td>${room.roomType ? room.roomType.typeName : 'Unknown'}</td>
                        <td>${room.capacity} pers.</td>
                        <td>${formatCurrency(room.pricePerNight)}</td>
                        <td><span class="badge ${statusBadge}">${room.status}</span></td>
                    </tr>`;
                });
            }

            html += '</tbody></table>';
            $('#rooms-list').html(html);
        },
        error: function (xhr) {
            handleApiError(xhr, 'Failed to load rooms.');
        }
    });
}

function showRoomDetails(id) {
    const room = rooms.find(r => r.roomID === id);
    if (!room) return;

    $('#roomDetailsContent').html(`
        <div class="mb-3">
            <h3>Room ${room.roomNumber}</h3>
            <span class="badge ${room.status === 'Available' ? 'bg-success' : 'bg-secondary'} mb-3">${room.status}</span>
        </div>
        <p>
            <strong>Type:</strong> ${room.roomType ? room.roomType.typeName : 'Unknown'}<br>
            <strong>Standard:</strong> ${room.roomType ? room.roomType.standard : '-'}<br>
            <strong>Capacity:</strong> ${room.capacity} Persons<br>
            <strong>Price per Night:</strong> ${formatCurrency(room.pricePerNight)}
        </p>
        <p class="text-muted small">${room.roomType && room.roomType.description ? room.roomType.description : ''}</p>
    `);
    new bootstrap.Modal('#roomDetailsModal').show();
}

function showAddRoomModal() {
    $.ajax({
        url: `${apiBaseUrl}/roomtypes`,
        headers: authHeaders(),
        success: function (data) {
            const select = $('#roomTypeSelect');
            select.empty().append('<option value="">Select Room Type...</option>');
            data.forEach(type => select.append(`<option value="${type.roomTypeID}">${type.typeName} - ${type.standard}</option>`));
        }
    });

    $('#addRoomForm')[0].reset();
    new bootstrap.Modal('#addRoomModal').show();
}

function createRoom() {
    const formData = {
        roomNumber: $('#addRoomForm input[name="roomNumber"]').val(),
        roomTypeID: parseInt($('#addRoomForm select[name="roomTypeID"]').val()),
        capacity: parseInt($('#addRoomForm input[name="capacity"]').val()),
        pricePerNight: parseFloat($('#addRoomForm input[name="pricePerNight"]').val()),
        status: $('#addRoomForm select[name="status"]').val()
    };

    if (!formData.roomTypeID) {
        alert('Please select a room type.');
        return;
    }

    $.ajax({
        url: `${apiBaseUrl}/rooms`,
        type: 'POST',
        headers: authHeaders(),
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function () {
            bootstrap.Modal.getInstance('#addRoomModal').hide();
            loadRooms();
            alert('Room created successfully.');
        },
        error: function (xhr) { handleApiError(xhr, 'Error creating room.'); }
    });
}

// Service Functions
function loadServices() {
    $.ajax({
        url: `${apiBaseUrl}/services`,
        headers: authHeaders(),
        success: function (data) {
            services = data;
            let html = '<table class="table table-hover"><thead><tr><th>Service</th><th>Description</th><th>Price</th><th>Availability</th></tr></thead><tbody>';

            if (data.length === 0) {
                html += '<tr><td colspan="4" class="text-center">No services found</td></tr>';
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
        },
        error: function (xhr) { handleApiError(xhr, 'Failed to load services.'); }
    });
}

function showServiceDetails(id) {
    const service = services.find(s => s.serviceID === id);
    if (!service) return;

    $('#serviceDetailsContent').html(`
        <h3>${service.serviceName}</h3>
        <p class="lead">${formatCurrency(service.unitPrice)}</p>
        <p><strong>Availability:</strong> ${service.availability}</p>
        <p>${service.description || 'No description available.'}</p>
    `);

    new bootstrap.Modal('#serviceDetailsModal').show();
}

function showAddServiceModal() {
    $('#addServiceForm')[0].reset();
    new bootstrap.Modal('#addServiceModal').show();
}

function createService() {
    const formData = {
        serviceName: $('#addServiceForm input[name="serviceName"]').val(),
        description: $('#addServiceForm textarea[name="description"]').val(),
        unitPrice: parseFloat($('#addServiceForm input[name="unitPrice"]').val()),
        availability: $('#addServiceForm select[name="availability"]').val()
    };

    $.ajax({
        url: `${apiBaseUrl}/services`,
        type: 'POST',
        headers: authHeaders(),
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function () {
            bootstrap.Modal.getInstance('#addServiceModal').hide();
            loadServices();
            alert('Service created successfully.');
        },
        error: function (xhr) { handleApiError(xhr, 'Error creating service.'); }
    });
}

// Reservation Functions
function loadReservations() {
    const endpoint = hasRole('Client') ? `${apiBaseUrl}/reservations/my` : `${apiBaseUrl}/reservations`;

    $.ajax({
        url: endpoint,
        headers: authHeaders(),
        success: function (data) {
            reservations = data;
            let html = '<table class="table table-hover"><thead><tr><th>ID</th><th>Guest</th><th>Dates</th><th>Rooms</th><th>Total</th><th>Status</th><th>Actions</th></tr></thead><tbody>';

            if (data.length === 0) {
                html += '<tr><td colspan="7" class="text-center">No reservations found</td></tr>';
            } else {
                data.forEach(res => {
                    const guestName = res.guest ? `${res.guest.firstName} ${res.guest.lastName}` : 'Unknown';
                    const roomCount = res.reservationRooms ? res.reservationRooms.length : 0;
                    html += `<tr style="cursor: pointer;" onclick="showReservationDetails(${res.reservationID}, event)">
                        <td>${res.reservationID}</td>
                        <td>${guestName}</td>
                        <td>${formatDate(res.checkInDate)} - ${formatDate(res.checkOutDate)}</td>
                        <td>${roomCount} rooms</td>
                        <td>${formatCurrency(res.totalPrice)}</td>
                        <td>${res.reservationStatus}</td>
                        <td>
                            <button class="btn btn-sm btn-outline-primary me-1" onclick="showAddRoomToReservation(${res.reservationID}, event)">Add Room</button>
                            <button class="btn btn-sm btn-outline-info me-1" onclick="showAddServiceToReservation(${res.reservationID}, event)">Add Service</button>
                            <button class="btn btn-sm btn-outline-danger" onclick="deleteReservation(${res.reservationID}, event)">Delete</button>
                        </td>
                    </tr>`;
                });
            }

            html += '</tbody></table>';
            $('#reservations-list').html(html);
        },
        error: function (xhr) {
            $('#reservations-list').html('<div class="alert alert-warning">Reservation list is not available for this role.</div>');
            handleApiError(xhr);
        }
    });
}

function showReservationDetails(id, event) {
    if (event && (event.target.tagName === 'BUTTON' || event.target.closest('button'))) return;

    const reservation = reservations.find(r => r.reservationID === id);
    if (!reservation) return;

    let html = `
        <div class="row mb-3">
            <div class="col-md-6">
                <h6>Guest Information</h6>
                <p><strong>Name:</strong> ${reservation.guest ? reservation.guest.firstName + ' ' + reservation.guest.lastName : 'Unknown'}<br>
                <strong>Email:</strong> ${reservation.guest ? reservation.guest.email : '-'}<br>
                <strong>Phone:</strong> ${reservation.guest ? reservation.guest.phone || '-' : '-'}</p>
            </div>
            <div class="col-md-6">
                <h6>Reservation Info</h6>
                <p><strong>ID:</strong> #${reservation.reservationID}<br>
                <strong>Dates:</strong> ${formatDate(reservation.checkInDate)} - ${formatDate(reservation.checkOutDate)}<br>
                <strong>Status:</strong> ${reservation.reservationStatus}<br>
                <strong>Total Price:</strong> ${formatCurrency(reservation.totalPrice)}</p>
            </div>
        </div>
        <h6>Rooms</h6>
        <table class="table table-sm table-bordered mb-3"><thead><tr><th>Room #</th><th>Type</th><th>Price/Night</th></tr></thead><tbody>
    `;

    if (reservation.reservationRooms && reservation.reservationRooms.length > 0) {
        reservation.reservationRooms.forEach(rr => {
            html += `<tr><td>${rr.room ? rr.room.roomNumber : '-'}</td><td>${rr.room && rr.room.roomType ? rr.room.roomType.typeName : '-'}</td><td>${formatCurrency(rr.pricePerNight)}</td></tr>`;
        });
    } else {
        html += '<tr><td colspan="3" class="text-center text-muted">No rooms assigned</td></tr>';
    }

    html += '</tbody></table><h6>Services</h6><table class="table table-sm table-bordered"><thead><tr><th>Service</th><th>Date</th><th>Quantity</th><th>Unit Price</th></tr></thead><tbody>';

    if (reservation.reservationServices && reservation.reservationServices.length > 0) {
        reservation.reservationServices.forEach(rs => {
            html += `<tr><td>${rs.service ? rs.service.serviceName : '-'}</td><td>${formatDate(rs.serviceDate)}</td><td>${rs.quantity}</td><td>${rs.service ? formatCurrency(rs.service.unitPrice) : '-'}</td></tr>`;
        });
    } else {
        html += '<tr><td colspan="4" class="text-center text-muted">No extra services</td></tr>';
    }

    $('#reservationDetailsContent').html(`${html}</tbody></table>`);
    new bootstrap.Modal('#reservationDetailsModal').show();
}

function deleteReservation(id, event) {
    if (event) event.stopPropagation();
    if (!confirm('Are you sure you want to delete this reservation?')) return;

    $.ajax({
        url: `${apiBaseUrl}/reservations/${id}`,
        type: 'DELETE',
        headers: authHeaders(),
        success: function () { loadReservations(); },
        error: function (xhr) { handleApiError(xhr, 'Could not delete reservation.'); }
    });
}

function updateGuestSelect() {
    const select = $('#reservationGuestSelect');
    select.empty().append('<option value="">Select Guest...</option>');
    guests.forEach(guest => select.append(`<option value="${guest.guestID}">${guest.firstName} ${guest.lastName}</option>`));
}

function showAddReservationModal() {
    if (!canModifyReservations()) {
        showAuthMessage('You do not have permission to create reservations.', 'danger');
        return;
    }

    const select = $('#reservationGuestSelect');
    select.empty();

    if (hasRole('Client')) {
        if (!currentUser || !currentUser.guestId) {
            showAuthMessage('Client account is missing guestId claim.', 'danger');
            return;
        }

        select.append(`<option value="${currentUser.guestId}" selected>${currentUser.username}</option>`);
        select.prop('disabled', true);
    } else {
        select.prop('disabled', false);
        if (guests.length === 0 && canManageGuests()) loadGuests();
    }

    $('#addReservationForm')[0].reset();
    if (hasRole('Client')) {
        select.empty().append(`<option value="${currentUser.guestId}" selected>${currentUser.username}</option>`);
        select.prop('disabled', true);
    }

    new bootstrap.Modal('#addReservationModal').show();
}

function createReservation() {
    let guestId = $('#reservationGuestSelect').val();

    if (hasRole('Client')) {
        guestId = currentUser && currentUser.guestId ? currentUser.guestId : null;
    }

    if (!guestId) {
        alert('Please select a guest.');
        return;
    }

    const formData = {
        guestID: parseInt(guestId),
        checkInDate: $('#addReservationForm input[name="checkInDate"]').val(),
        checkOutDate: $('#addReservationForm input[name="checkOutDate"]').val(),
        numberOfGuests: parseInt($('#addReservationForm input[name="numberOfGuests"]').val()),
        totalPrice: 0,
        reservationStatus: 'Confirmed'
    };

    $.ajax({
        url: `${apiBaseUrl}/reservations`,
        type: 'POST',
        headers: authHeaders(),
        contentType: 'application/json',
        data: JSON.stringify(formData),
        success: function (response) {
            bootstrap.Modal.getInstance('#addReservationModal').hide();
            if (canReadReservations()) loadReservations();
            alert(`Reservation #${response.reservationID} created.`);
        },
        error: function (xhr) { handleApiError(xhr, 'Error creating reservation.'); }
    });
}

// Add Room to Reservation
function showAddRoomToReservation(reservationId, event) {
    if (event) event.stopPropagation();
    $('#roomReservationId').val(reservationId);

    $.ajax({
        url: `${apiBaseUrl}/rooms/available`,
        headers: authHeaders(),
        success: function (data) {
            const select = $('#roomSelect');
            select.empty().append('<option value="">Select Room...</option>');
            data.forEach(room => select.append(`<option value="${room.roomID}">${room.roomNumber} - ${room.roomType ? room.roomType.typeName : ''} (${formatCurrency(room.pricePerNight)})</option>`));
            new bootstrap.Modal('#addRoomToReservationModal').show();
        },
        error: function (xhr) { handleApiError(xhr); }
    });
}

function submitAddRoom() {
    const reservationId = $('#roomReservationId').val();
    const roomId = $('#roomSelect').val();

    if (!roomId) {
        alert('Please select a room.');
        return;
    }

    $.ajax({
        url: `${apiBaseUrl}/reservations/${reservationId}/rooms/${roomId}`,
        type: 'POST',
        headers: authHeaders(),
        success: function () {
            bootstrap.Modal.getInstance('#addRoomToReservationModal').hide();
            loadReservations();
            alert('Room added successfully.');
        },
        error: function (xhr) { handleApiError(xhr, 'Error adding room.'); }
    });
}

// Add Service to Reservation
function showAddServiceToReservation(reservationId, event) {
    if (event) event.stopPropagation();
    $('#serviceReservationId').val(reservationId);
    $('#serviceDate').val(new Date().toISOString().split('T')[0]);

    $.ajax({
        url: `${apiBaseUrl}/services`,
        headers: authHeaders(),
        success: function (data) {
            const select = $('#serviceSelect');
            select.empty().append('<option value="">Select Service...</option>');
            data.forEach(service => select.append(`<option value="${service.serviceID}">${service.serviceName} (${formatCurrency(service.unitPrice)})</option>`));
            new bootstrap.Modal('#addServiceToReservationModal').show();
        },
        error: function (xhr) { handleApiError(xhr); }
    });
}

function submitAddService() {
    const reservationId = $('#serviceReservationId').val();
    const serviceId = $('#serviceSelect').val();
    const quantity = $('#serviceQuantity').val();
    const serviceDate = $('#serviceDate').val();

    if (!serviceId || !serviceDate) {
        alert('Please fill all required fields.');
        return;
    }

    $.ajax({
        url: `${apiBaseUrl}/reservations/${reservationId}/services/${serviceId}`,
        type: 'POST',
        headers: authHeaders(),
        contentType: 'application/json',
        data: JSON.stringify({ quantity: parseInt(quantity), serviceDate: serviceDate }),
        success: function () {
            bootstrap.Modal.getInstance('#addServiceToReservationModal').hide();
            loadReservations();
            alert('Service added successfully.');
        },
        error: function (xhr) { handleApiError(xhr, 'Error adding service.'); }
    });
}
