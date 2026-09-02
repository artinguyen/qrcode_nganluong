var currentOrderCode = '';
var currentAmount = 0;
var currentDate = getCurrentDate();
// Handle waiting time
let ajaxLoadingTimer;
const DEBOUNCE_DELAY = 3000;

$(document).ajaxStart(function () {
    ajaxLoadingTimer = setTimeout(() => {
        showLoadingModal(true);
    }, DEBOUNCE_DELAY);
});

$(document).ajaxStop(function () {
    clearTimeout(ajaxLoadingTimer);
    showLoadingModal(false);
});

// Remove localStorage when reload page
localStorage.removeItem('qr-code');
// Init socket
var paymentHub = $.connection.paymentHub;
// Check expire of session
$.ajaxSetup({
    statusCode: {
        401: function () {
            // Xóa mọi kết nối ngầm để tránh trình duyệt bị lặp vô hạn
            if ($.connection && $.connection.hub) {
                $.connection.hub.stop();
            }
            // Chuyển hướng ngay lập tức
            window.location.href = "/User/Login";
        }
    }
});


/** Generate QrCode **/
$("#generate-btn").click(function (e) {
    e.preventDefault();
    var $btn = $(this);
    // Valid amount
    let amount = $("#qr-amount").val().replaceAll(',', '');
    currentAmount = amount;
    if (amount == 0 || amount == "") return; // No use space
    // If existing QrCode, alert
    let localQr = localStorage.getItem('qr-code');
    if (localQr && localQr != null) {
        let message = 'Bạn muốn tạo QR Code mới khi mã hiện tại chưa thanh toán, vui lòng nhấn Huỷ và tạo lại mã!';
        showModal(message);
        return;
    }

    currentOrderCode = randomCode();

    // Assign into group
    if ($.connection.hub.state === $.signalR.connectionState.connected) {
        paymentHub.server.joinOrderGroup(currentOrderCode);
    }

    // Disable button
    $btn.prop('disabled', true);
    $.ajax({
        url: '/Payment/GenQrCode',
        type: 'POST',
        dataType: 'json',
        data: {
            amount: amount,
            orderCode: currentOrderCode
        },
        success: function (res) {
            if (res.success && res.qrdata) {
                var qrcode = new QRCode(document.getElementById("qrBuffer"), {
                    text: res.qrdata,
                    width: 160,
                    height: 160,
                    correctLevel: QRCode.CorrectLevel.H
                });
                //setTimeout(function () {
                //    var base64Img = document.querySelector("#qrBuffer img").src;
                //    $("#qrImage").attr("src", base64Img).show();
                //}, 500);
                var canvas = document.querySelector("#qrBuffer canvas");
                if (canvas) {
                    var base64Img = canvas.toDataURL("image/png");
                    $("#qrImage").attr("src", base64Img).show();
                    $('.qr-code-box').addClass('qr-active');
                }

                // Show message
                $('#success-msg').text('QR code tạo thành công!');
                setTimeout(function () {
                    $('#success-msg').text('');
                }, 2000);
                
                renderOrders(res.data, 1);
                localStorage.setItem('qr-code', true);
            }
            if (!res.success) {
                resetQrImage();
                $('#success-msg').text(res.message).css('color', 'red');
            }
        },

        error: function (xhr, status, error) {
            if (xhr.status === 401) {
                window.location.href = "/User/Login";
                return;
            }
        },
        
    }).always(function () {
        $btn.prop('disabled', false);
    });

});

init();

function init() {
    $.connection.hub.qs = { "userId": userId };

    paymentHub.client.onPaymentSuccess = function (res) {
        if (res.IsSuccess) {
            // QrCode is existing
            if (res.OrderCode == currentOrderCode) {
                $('.qr-code-box').removeClass('qr-active');
                $("#qrImage").attr("src", checkMarkImageUrl);
                $('#success-msg').text('Giao dịch thành công!');
                setTimeout(function () {
                    // Reset
                    reset();
                }, 2000);
                
            }
            // Payment later
            renderOrders(res.Data, 2);

        } else {
            $('#success-msg').text('Giao dịch thất bại!');
        }
    };

    paymentHub.client.onInvoiceUpdated = function (res) {
        if (!currentOrderCode && res && res.IsSuccess) {
            renderOrders(res.Data, 2);
        }
    };

    
    $.connection.hub.start().done(function () {
        //paymentHub.server.joinOrderGroup(currentOrderCode);
    }).fail(function (reason) {
        if (error && (error.status === 401 || error.message.indexOf("401") > -1)) {
            window.location.href = "/User/Login";
        }
    });
    
}

function renderOrders(orderList, type) {
    if (!orderList || !Array.isArray(orderList) || orderList.length === 0) return;
    const tableBody = document.getElementById("orderTableBody");
    tableBody.innerHTML = "";

    orderList.forEach((order, index) => {
        let statusBadge = "";
        if (order.Status == "3") {
            statusBadge = '<span class="status fulfilled">Đã thanh toán</span>';
        } else if (order.Status == "4") {
            statusBadge = '<span class="status released">Đã huỷ</span>';
        }
        else {
            statusBadge = '<span class="status invoiced">Chờ thanh toán</span>';
        }
        const actionButtonsCell = `
                ${order.QrCode ? `
                    <a class="printQrItem" onclick="inMaQR('${order.OrderCode}', ${order.Amount}, '${order.QrCode}'); return false;"><i class="fa fa-print"></i></a>
                ` : ''} 
        `;
        // Tạo dòng mới
        const row = `
            <tr>
                <td>${index + 1}</td>
                <td>${order.OrderCode}</td>
                <td>${parseJsonDate(order.CreatedDate, type)}</td>
                <td>${parseJsonDate(order.PaymentDate, type)}</td>
                <td>${formatCurrency(order.Amount)}</td>
                <td>${statusBadge}</td>
                <td>${actionButtonsCell}</td>
            </tr>
        `;

        tableBody.insertAdjacentHTML("beforeend", row);
    });
}

/** Check transaction **/
$("#inquiry-btn").click(function (e) {
    e.preventDefault();
    var $btn = $(this);
    let inquiryCode = $('#inquiry-code').val();
    if (inquiryCode.trim() == '') return;
    // Disable button
    $btn.prop('disabled', true);
    $.ajax({
        url: '/Payment/CheckTransaction',
        type: 'POST',
        dataType: 'json',
        data: {
            inquiryCode: inquiryCode
        },
        success: function (res) {
            if (res.success) {
                showModal(res.message);
                renderOrders(res.data, 1);
                return;
            }
            showModal(res.message);
        },

        error: function (xhr, status, error) {
            if (xhr.status === 401) {
                window.location.href = "/User/Login";
                return;
            }
        },
    }).always(function () {
        $btn.prop('disabled', false);
    });
})

/**
Format date
**/
function parseJsonDate(jsonDate, type) {
    if (!jsonDate) return "";
    const timestamp = parseInt(jsonDate.replace(/\/Date\((\d+)\)\//, "$1"));
    let date = new Date(timestamp);
    const pad = (n) => n.toString().padStart(2, '0');
    if (type == 2) {
        date = new Date(jsonDate);
    }
    return `${pad(date.getDate())}/${pad(date.getMonth() + 1)}/${date.getFullYear()} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(date.getSeconds())}`;
}