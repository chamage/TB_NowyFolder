// ═══ Hotel Reservation — API Client ═══
const apiBaseUrl = '/api';
let guests = [], rooms = [], services = [], reservations = [];
let authToken = localStorage.getItem('authToken') || '';
let currentUser = JSON.parse(localStorage.getItem('authUser') || 'null');

$(document).ready(function () { updateAuthUI(); applyRbacToUi(); switchTab('rooms'); });

// ── Toast ──
function showToast(msg, type = 'info') {
    const c = document.getElementById('toast-container'); if (!c) return;
    const el = document.createElement('div');
    el.className = 'toast-item toast-' + type; el.textContent = msg;
    c.appendChild(el); setTimeout(() => el.remove(), 3600);
}

// ── Auth helpers ──
function parseJwt(t) { try { return JSON.parse(atob(t.split('.')[1].replace(/-/g,'+').replace(/_/g,'/'))); } catch { return null; } }
function hasRole(r) { return currentUser && currentUser.role === r; }
function isAuthenticated() { return !!authToken; }
function canManageGuests() { return hasRole('Receptionist') || hasRole('Administrator'); }
function canManageCatalogWrite() { return hasRole('Administrator'); }
function canReadReservations() { return hasRole('Client') || hasRole('Receptionist') || hasRole('Administrator'); }
function canModifyReservations() { return canReadReservations(); }

function fillDemoLogin(type) {
    const c = { admin:['admin','admin123!'], reception:['reception','reception123!'], client:['client','client123!'] };
    if (c[type]) { $('#auth-username').val(c[type][0]); $('#auth-password').val(c[type][1]); }
}

function login() {
    const u = $('#auth-username').val().trim(), p = $('#auth-password').val().trim();
    if (!u || !p) { showToast('Please enter credentials.', 'warning'); return; }
    $.ajax({ url: apiBaseUrl+'/auth/token', type:'POST', contentType:'application/json',
        data: JSON.stringify({username:u,password:p}),
        success(r) {
            authToken = r.accessToken;
            const pl = parseJwt(authToken) || {};
            const role = pl['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || r.role;
            const name = pl['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] || r.username || u;
            const gid = pl.guestId ? parseInt(pl.guestId) : null;
            currentUser = { username:name, role:role||'Unknown', guestId:Number.isNaN(gid)?null:gid };
            localStorage.setItem('authToken', authToken);
            localStorage.setItem('authUser', JSON.stringify(currentUser));
            $('#auth-password').val('');
            showToast('Signed in as ' + currentUser.username, 'success');
            updateAuthUI(); applyRbacToUi(); switchTab('rooms');
        },
        error() { showToast('Invalid credentials.', 'error'); }
    });
}

function logout() {
    authToken=''; currentUser=null;
    localStorage.removeItem('authToken'); localStorage.removeItem('authUser');
    showToast('Signed out.', 'info');
    updateAuthUI(); applyRbacToUi(); switchTab('rooms');
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
    }
}

function authHeaders() { return authToken ? { Authorization:'Bearer '+authToken } : {}; }

function handleApiError(xhr, msg) {
    if (xhr.status===401) { showToast('Authentication required.','warning'); return; }
    if (xhr.status===403) { showToast('Insufficient permissions.','error'); return; }
    showToast(msg || 'Error: '+xhr.status,'error');
}

function applyRbacToUi() {
    $('#btn-add-guest').toggle(canManageGuests());
    $('#btn-add-room').toggle(canManageCatalogWrite());
    $('#btn-add-service').toggle(canManageCatalogWrite());
    $('#btn-add-reservation').toggle(canModifyReservations());
    $('#tab-guests').toggle(canManageGuests());
    $('#tab-reservations').toggle(canReadReservations());
}

// ── Navigation ──
function switchTab(name) {
    if (name==='guests' && !canManageGuests()) { showToast('Requires Receptionist or Admin role.','warning'); return; }
    if (name==='reservations' && !canReadReservations()) { showToast('Requires sign-in.','warning'); return; }
    $('.sidebar-nav button').removeClass('active');
    $('#tab-'+name).addClass('active');
    $('.content-section').addClass('d-none');
    $('#section-'+name).removeClass('d-none');
    ({guests:loadGuests, rooms:loadRooms, services:loadServices, reservations:loadReservations})[name]();
}

// ── Formatters ──
function fmt$(a) { return new Intl.NumberFormat('en-US',{style:'currency',currency:'USD'}).format(a||0); }
function fmtDate(d) { return d ? new Date(d).toLocaleDateString() : ''; }
function badge(s) {
    const m = {Available:'badge-available',Occupied:'badge-occupied',Maintenance:'badge-maintenance',Confirmed:'badge-confirmed'};
    return '<span class="badge-s '+(m[s]||'badge-confirmed')+'">'+s+'</span>';
}

// ── Guests ──
function loadGuests() {
    $.ajax({ url:apiBaseUrl+'/guests', headers:authHeaders(),
        success(data) {
            guests=data;
            let h='<table class="table"><thead><tr><th>ID</th><th>Name</th><th>Email</th><th>Phone</th><th></th></tr></thead><tbody>';
            if (!data.length) h+='<tr><td colspan="5" style="text-align:center;color:var(--text-muted)">No guests yet</td></tr>';
            else data.forEach(g=>{h+=`<tr><td>${g.guestID}</td><td>${g.firstName} ${g.lastName}</td><td>${g.email}</td><td>${g.phone||'—'}</td><td><button class="btn-danger-sm" onclick="deleteGuest(${g.guestID})">Remove</button></td></tr>`;});
            h+='</tbody></table>'; $('#guests-list').html(h); updateGuestSelect();
        },
        error(xhr){$('#guests-list').html('<div class="alert alert-warning">Not available for this role.</div>');handleApiError(xhr);}
    });
}
function showAddGuestModal(){$('#addGuestForm')[0].reset();new bootstrap.Modal('#addGuestModal').show();}
function createGuest(){
    const d={firstName:$('#addGuestForm input[name="firstName"]').val(),lastName:$('#addGuestForm input[name="lastName"]').val(),email:$('#addGuestForm input[name="email"]').val(),phone:$('#addGuestForm input[name="phone"]').val()};
    $.ajax({url:apiBaseUrl+'/guests',type:'POST',headers:authHeaders(),contentType:'application/json',data:JSON.stringify(d),
        success(){bootstrap.Modal.getInstance('#addGuestModal').hide();loadGuests();showToast('Guest added.','success');},
        error(xhr){handleApiError(xhr,'Could not create guest.');}
    });
}
function deleteGuest(id){
    if(!confirm('Remove this guest?'))return;
    $.ajax({url:apiBaseUrl+'/guests/'+id,type:'DELETE',headers:authHeaders(),
        success(){loadGuests();showToast('Guest removed.','success');},error(xhr){handleApiError(xhr);}});
}

// ── Rooms ──
function loadRooms(avail){
    $.ajax({url:apiBaseUrl+'/rooms'+(avail?'/available':''),headers:authHeaders(),
        success(data){
            rooms=data;
            let h='<table class="table"><thead><tr><th>No.</th><th>Type</th><th>Capacity</th><th>Price/Night</th><th>Status</th></tr></thead><tbody>';
            if(!data.length) h+='<tr><td colspan="5" style="text-align:center;color:var(--text-muted)">No rooms found</td></tr>';
            else data.forEach(r=>{h+=`<tr class="clickable-row" onclick="showRoomDetails(${r.roomID})"><td><strong>${r.roomNumber}</strong></td><td>${r.roomType?r.roomType.typeName:'—'}</td><td>${r.capacity}</td><td>${fmt$(r.pricePerNight)}</td><td>${badge(r.status)}</td></tr>`;});
            h+='</tbody></table>'; $('#rooms-list').html(h);
        },error(xhr){handleApiError(xhr,'Failed to load rooms.');}
    });
}
function showRoomDetails(id){
    const r=rooms.find(x=>x.roomID===id);if(!r)return;
    $('#roomDetailsContent').html(`<h4 style="font-family:var(--serif);margin-bottom:.5rem">Room ${r.roomNumber}</h4>${badge(r.status)}<hr style="border-color:var(--border-light);margin:1rem 0"><p style="margin:0;line-height:2"><strong style="color:var(--text-muted)">Type</strong>&ensp;${r.roomType?r.roomType.typeName:'—'}<br><strong style="color:var(--text-muted)">Standard</strong>&ensp;${r.roomType?r.roomType.standard:'—'}<br><strong style="color:var(--text-muted)">Capacity</strong>&ensp;${r.capacity} guests<br><strong style="color:var(--text-muted)">Rate</strong>&ensp;${fmt$(r.pricePerNight)} / night</p>${r.roomType&&r.roomType.description?'<p style="color:var(--text-muted);font-size:.85rem;margin-top:.75rem">'+r.roomType.description+'</p>':''}`);
    new bootstrap.Modal('#roomDetailsModal').show();
}
function showAddRoomModal(){
    $.ajax({url:apiBaseUrl+'/roomtypes',headers:authHeaders(),success(d){const s=$('#roomTypeSelect');s.empty().append('<option value="">Select type…</option>');d.forEach(t=>s.append(`<option value="${t.roomTypeID}">${t.typeName} — ${t.standard}</option>`));}});
    $('#addRoomForm')[0].reset();new bootstrap.Modal('#addRoomModal').show();
}
function createRoom(){
    const d={roomNumber:$('#addRoomForm input[name="roomNumber"]').val(),roomTypeID:parseInt($('#addRoomForm select[name="roomTypeID"]').val()),capacity:parseInt($('#addRoomForm input[name="capacity"]').val()),pricePerNight:parseFloat($('#addRoomForm input[name="pricePerNight"]').val()),status:$('#addRoomForm select[name="status"]').val()};
    if(!d.roomTypeID){showToast('Select a room type.','warning');return;}
    $.ajax({url:apiBaseUrl+'/rooms',type:'POST',headers:authHeaders(),contentType:'application/json',data:JSON.stringify(d),
        success(){bootstrap.Modal.getInstance('#addRoomModal').hide();loadRooms();showToast('Room created.','success');},
        error(xhr){handleApiError(xhr,'Could not create room.');}
    });
}

// ── Services ──
function loadServices(){
    $.ajax({url:apiBaseUrl+'/services',headers:authHeaders(),
        success(data){
            services=data;
            let h='<table class="table"><thead><tr><th>Name</th><th>Description</th><th>Price</th><th>Status</th></tr></thead><tbody>';
            if(!data.length)h+='<tr><td colspan="4" style="text-align:center;color:var(--text-muted)">No services</td></tr>';
            else data.forEach(s=>{h+=`<tr class="clickable-row" onclick="showServiceDetails(${s.serviceID})"><td><strong>${s.serviceName}</strong></td><td>${s.description||'—'}</td><td>${fmt$(s.unitPrice)}</td><td>${badge(s.availability)}</td></tr>`;});
            h+='</tbody></table>';$('#services-list').html(h);
        },error(xhr){handleApiError(xhr);}
    });
}
function showServiceDetails(id){
    const s=services.find(x=>x.serviceID===id);if(!s)return;
    $('#serviceDetailsContent').html(`<h4 style="font-family:var(--serif)">${s.serviceName}</h4><p style="font-size:1.3rem;color:var(--gold);margin:.5rem 0">${fmt$(s.unitPrice)}</p><p><strong style="color:var(--text-muted)">Availability</strong>&ensp;${s.availability}</p><p style="color:var(--text-light)">${s.description||'No description.'}</p>`);
    new bootstrap.Modal('#serviceDetailsModal').show();
}
function showAddServiceModal(){$('#addServiceForm')[0].reset();new bootstrap.Modal('#addServiceModal').show();}
function createService(){
    const d={serviceName:$('#addServiceForm input[name="serviceName"]').val(),description:$('#addServiceForm textarea[name="description"]').val(),unitPrice:parseFloat($('#addServiceForm input[name="unitPrice"]').val()),availability:$('#addServiceForm select[name="availability"]').val()};
    $.ajax({url:apiBaseUrl+'/services',type:'POST',headers:authHeaders(),contentType:'application/json',data:JSON.stringify(d),
        success(){bootstrap.Modal.getInstance('#addServiceModal').hide();loadServices();showToast('Service added.','success');},
        error(xhr){handleApiError(xhr);}
    });
}

// ── Reservations ──
function loadReservations(){
    const ep=hasRole('Client')?apiBaseUrl+'/reservations/my':apiBaseUrl+'/reservations';
    $.ajax({url:ep,headers:authHeaders(),
        success(data){
            reservations=data;
            let h='<table class="table"><thead><tr><th>#</th><th>Guest</th><th>Stay</th><th>Rooms</th><th>Total</th><th>Status</th><th></th></tr></thead><tbody>';
            if(!data.length)h+='<tr><td colspan="7" style="text-align:center;color:var(--text-muted)">No reservations</td></tr>';
            else data.forEach(r=>{
                const gn=r.guest?r.guest.firstName+' '+r.guest.lastName:'—';
                const rc=r.reservationRooms?r.reservationRooms.length:0;
                h+=`<tr class="clickable-row" onclick="showReservationDetails(${r.reservationID},event)"><td>${r.reservationID}</td><td>${gn}</td><td>${fmtDate(r.checkInDate)} — ${fmtDate(r.checkOutDate)}</td><td>${rc}</td><td>${fmt$(r.totalPrice)}</td><td>${badge(r.reservationStatus)}</td><td><button class="btn-icon" onclick="showAddRoomToReservation(${r.reservationID},event)" title="Add room">🛏</button> <button class="btn-icon" onclick="showAddServiceToReservation(${r.reservationID},event)" title="Add service">🍽</button> <button class="btn-danger-sm" onclick="deleteReservation(${r.reservationID},event)" style="font-size:.72rem;padding:.25rem .55rem">✕</button></td></tr>`;
            });
            h+='</tbody></table>';$('#reservations-list').html(h);
        },
        error(xhr){$('#reservations-list').html('<div class="alert alert-warning">Not available.</div>');handleApiError(xhr);}
    });
}

function showReservationDetails(id,ev){
    if(ev&&(ev.target.tagName==='BUTTON'||ev.target.closest('button')))return;
    const r=reservations.find(x=>x.reservationID===id);if(!r)return;
    let h=`<div class="row mb-3"><div class="col-md-6"><h6 style="color:var(--gold);font-family:var(--serif)">Guest</h6><p style="line-height:1.9"><strong style="color:var(--text-muted)">Name</strong>&ensp;${r.guest?r.guest.firstName+' '+r.guest.lastName:'—'}<br><strong style="color:var(--text-muted)">Email</strong>&ensp;${r.guest?r.guest.email:'—'}<br><strong style="color:var(--text-muted)">Phone</strong>&ensp;${r.guest?r.guest.phone||'—':'—'}</p></div><div class="col-md-6"><h6 style="color:var(--gold);font-family:var(--serif)">Booking</h6><p style="line-height:1.9"><strong style="color:var(--text-muted)">ID</strong>&ensp;#${r.reservationID}<br><strong style="color:var(--text-muted)">Stay</strong>&ensp;${fmtDate(r.checkInDate)} — ${fmtDate(r.checkOutDate)}<br><strong style="color:var(--text-muted)">Status</strong>&ensp;${r.reservationStatus}<br><strong style="color:var(--text-muted)">Total</strong>&ensp;${fmt$(r.totalPrice)}</p></div></div>`;
    h+='<h6 style="color:var(--gold);font-family:var(--serif)">Rooms</h6><table class="table"><thead><tr><th>No.</th><th>Type</th><th>Rate</th></tr></thead><tbody>';
    if(r.reservationRooms&&r.reservationRooms.length) r.reservationRooms.forEach(rr=>{h+=`<tr><td>${rr.room?rr.room.roomNumber:'—'}</td><td>${rr.room&&rr.room.roomType?rr.room.roomType.typeName:'—'}</td><td>${fmt$(rr.pricePerNight)}</td></tr>`;});
    else h+='<tr><td colspan="3" style="text-align:center;color:var(--text-muted)">None assigned</td></tr>';
    h+='</tbody></table><h6 style="color:var(--gold);font-family:var(--serif)">Services</h6><table class="table"><thead><tr><th>Service</th><th>Date</th><th>Qty</th><th>Price</th></tr></thead><tbody>';
    if(r.reservationServices&&r.reservationServices.length) r.reservationServices.forEach(rs=>{h+=`<tr><td>${rs.service?rs.service.serviceName:'—'}</td><td>${fmtDate(rs.serviceDate)}</td><td>${rs.quantity}</td><td>${rs.service?fmt$(rs.service.unitPrice):'—'}</td></tr>`;});
    else h+='<tr><td colspan="4" style="text-align:center;color:var(--text-muted)">None</td></tr>';
    $('#reservationDetailsContent').html(h+'</tbody></table>');
    new bootstrap.Modal('#reservationDetailsModal').show();
}

function deleteReservation(id,ev){
    if(ev)ev.stopPropagation();if(!confirm('Delete this reservation?'))return;
    $.ajax({url:apiBaseUrl+'/reservations/'+id,type:'DELETE',headers:authHeaders(),
        success(){loadReservations();showToast('Reservation deleted.','success');},error(xhr){handleApiError(xhr);}});
}

function updateGuestSelect(){
    const s=$('#reservationGuestSelect');s.empty().append('<option value="">Select guest…</option>');
    guests.forEach(g=>s.append(`<option value="${g.guestID}">${g.firstName} ${g.lastName}</option>`));
}

function showAddReservationModal(){
    if(!canModifyReservations()){showToast('No permission.','error');return;}
    const s=$('#reservationGuestSelect');s.empty();
    if(hasRole('Client')){
        if(!currentUser||!currentUser.guestId){showToast('Missing guest ID.','error');return;}
        s.append(`<option value="${currentUser.guestId}" selected>${currentUser.username}</option>`);s.prop('disabled',true);
    } else{s.prop('disabled',false);if(!guests.length&&canManageGuests())loadGuests();}
    $('#addReservationForm')[0].reset();
    if(hasRole('Client')){s.empty().append(`<option value="${currentUser.guestId}" selected>${currentUser.username}</option>`);s.prop('disabled',true);}
    new bootstrap.Modal('#addReservationModal').show();
}

function createReservation(){
    let gid=$('#reservationGuestSelect').val();
    if(hasRole('Client'))gid=currentUser&&currentUser.guestId?currentUser.guestId:null;
    if(!gid){showToast('Select a guest.','warning');return;}
    const d={guestID:parseInt(gid),checkInDate:$('#addReservationForm input[name="checkInDate"]').val(),checkOutDate:$('#addReservationForm input[name="checkOutDate"]').val(),numberOfGuests:parseInt($('#addReservationForm input[name="numberOfGuests"]').val()),totalPrice:0,reservationStatus:'Confirmed'};
    $.ajax({url:apiBaseUrl+'/reservations',type:'POST',headers:authHeaders(),contentType:'application/json',data:JSON.stringify(d),
        success(r){bootstrap.Modal.getInstance('#addReservationModal').hide();if(canReadReservations())loadReservations();showToast('Booking #'+r.reservationID+' created.','success');},
        error(xhr){handleApiError(xhr);}
    });
}

// ── Assign room / service to reservation ──
function showAddRoomToReservation(rid,ev){
    if(ev)ev.stopPropagation();$('#roomReservationId').val(rid);
    $.ajax({url:apiBaseUrl+'/rooms/available',headers:authHeaders(),
        success(d){const s=$('#roomSelect');s.empty().append('<option value="">Select room…</option>');d.forEach(r=>s.append(`<option value="${r.roomID}">${r.roomNumber} — ${r.roomType?r.roomType.typeName:''} (${fmt$(r.pricePerNight)})</option>`));new bootstrap.Modal('#addRoomToReservationModal').show();},
        error(xhr){handleApiError(xhr);}
    });
}
function submitAddRoom(){
    const rid=$('#roomReservationId').val(),rmid=$('#roomSelect').val();
    if(!rmid){showToast('Select a room.','warning');return;}
    $.ajax({url:`${apiBaseUrl}/reservations/${rid}/rooms/${rmid}`,type:'POST',headers:authHeaders(),
        success(){bootstrap.Modal.getInstance('#addRoomToReservationModal').hide();loadReservations();showToast('Room assigned.','success');},
        error(xhr){handleApiError(xhr);}
    });
}

function showAddServiceToReservation(rid,ev){
    if(ev)ev.stopPropagation();$('#serviceReservationId').val(rid);
    $('#serviceDate').val(new Date().toISOString().split('T')[0]);
    $.ajax({url:apiBaseUrl+'/services',headers:authHeaders(),
        success(d){const s=$('#serviceSelect');s.empty().append('<option value="">Select service…</option>');d.forEach(sv=>s.append(`<option value="${sv.serviceID}">${sv.serviceName} (${fmt$(sv.unitPrice)})</option>`));new bootstrap.Modal('#addServiceToReservationModal').show();},
        error(xhr){handleApiError(xhr);}
    });
}
function submitAddService(){
    const rid=$('#serviceReservationId').val(),sid=$('#serviceSelect').val(),qty=$('#serviceQuantity').val(),dt=$('#serviceDate').val();
    if(!sid||!dt){showToast('Fill all fields.','warning');return;}
    $.ajax({url:`${apiBaseUrl}/reservations/${rid}/services/${sid}`,type:'POST',headers:authHeaders(),contentType:'application/json',data:JSON.stringify({quantity:parseInt(qty),serviceDate:dt}),
        success(){bootstrap.Modal.getInstance('#addServiceToReservationModal').hide();loadReservations();showToast('Service added.','success');},
        error(xhr){handleApiError(xhr);}
    });
}
