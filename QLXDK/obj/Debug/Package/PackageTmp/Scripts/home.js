var currentOrderCode = '';
var currentAmount = 0;
const today = new Date();
const dd = String(today.getDate()).padStart(2, '0');
const mm = String(today.getMonth() + 1).padStart(2, '0');
const yyyy = today.getFullYear();
var currentDate = `${dd}/${mm}/${yyyy}`;
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
    if (amount == 0 || amount == "") return;

    // If existing QrCode, alert
    let localQr = localStorage.getItem('qr-code');
    if (localQr && localQr != null) {
        modal.style.display = 'block';
        return;
    }
    // Create random order
    currentOrderCode = "QR" + Date.now();
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
                $("#qrImage").attr("src", res.qrdata).show();
                // Show message
                $('#success-msg').text('QR code tạo thành công!');
                setTimeout(function () {
                    $('#success-msg').text('');
                }, 2000);
                $('.qr-code-box').addClass('qr-active');
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
        //currentOrderCode = "";
        $btn.prop('disabled', false);
    });

});

init();

function init() {
    //currentOrderCode = "QR" + Date.now();
    paymentHub.client.onPaymentSuccess = function (res) {
        if (res.IsSuccess) {
            // QrCode is existing
            if (res.OrderCode == currentOrderCode) {
                $('.qr-code-box').removeClass('qr-active');
                $('#success-msg').text('Giao dịch thành công!');
                $("#qrImage").attr("src", checkMarkImageUrl);
                setTimeout(function () {
                    // Reset
                    cancel();
                }, 2000);
                
                
            }
            // Payment later
            renderOrders(res.Data, 2);

        } else {
            $('#success-msg').text('Giao dịch thất bại!');
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

/** Cancel **/
function cancel() {
    $("#qrImage").attr("src", "");
    $("#qr-amount").val('');
    $('.qr-code-box').removeClass('qr-active');
    $('#success-msg').text('');
    // Reset order, amount
    currentOrderCode = "";
    currentAmount = 0;
    // Remove localStorage
    localStorage.removeItem('qr-code');
}

function resetQrImage() {
    $("#qrImage").attr("src", "");
    //$('.qr-code-box').removeClass('qr-active');
}


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

function formatCurrency(amount) {
    if (!amount) return "0 VNĐ";
    const numericAmount = Number(amount);
    if (isNaN(numericAmount)) return amount + " (VNĐ)";

    return numericAmount.toLocaleString('en-US') + " (VNĐ)";
}

function formatInputCurrency(input) {
    let value = input.value.replace(/\D/g, "");

    if (value !== "") {
        let numericValue = parseInt(value, 10);

        input.value = numericValue.toLocaleString('en-US');
    } else {
        input.value = "";
    }
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
                    <a class="printQrItem" onclick="inMaQR('${order.OrderCode}', ${order.Amount}, '${order.QrCode}'); return false;"><i class="fa fa-print"></i></a><a class ="cancelPayment" onclick="cancelPayment('${order.OrderCode}', this); return false;"><i class ="fa fa-trash"></i></a>
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

const defaultWords = [
"không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín"
];

function docSoBlock(number) {
    let tram = Math.floor(number / 100);
    let chuc = Math.floor((number % 100) / 10);
    let donvi = number % 10;
    let result = "";

    if (tram === 0 && chuc === 0 && donvi === 0) return "";

    if (tram !== 0) {
        result += defaultWords[tram] + " trăm ";
        if (chuc === 0 && donvi !== 0) result += "lẻ ";
    }

    if (chuc !== 0 && chuc !== 1) {
        result += defaultWords[chuc] + " mươi ";
    } else if (chuc === 1) {
        result += "mười ";
    }

    if (chuc !== 0 && donvi === 5) {
        result += "lăm";
    } else if (donvi > 1 || (donvi === 1 && chuc !== 0 && chuc !== 1)) {
        result += defaultWords[donvi];
    } else if (donvi === 1) {
        result += "một";
    }

    return result;
}

function convertText(money) {
    if (typeof money === 'string') {
        money = Number(money.replaceAll(',', ''));
    } else {
        money = Number(money);
    }
    if (money === 0) return "Không đồng";
    if (money < 0 || isNaN(money)) return "";

    let Chuoi = "", ty = 0, trieu = 0, nghin = 0;

    if (money < 0) return "Âm " + docTienBangChu(-money);

    if (money >= 1000000000000) {
        return "Số quá lớn!";
    }

    if (money >= 1000000000) {
        ty = Math.floor(money / 1000000000);
        money = money % 1000000000;
    }

    if (money >= 1000000) {
        trieu = Math.floor(money / 1000000);
        money = money % 1000000;
    }

    if (money >= 1000) {
        nghin = Math.floor(money / 1000);
        money = money % 1000;
    }

    if (ty > 0) {
        Chuoi += docSoBlock(ty) + " tỷ ";
    }

    if (trieu > 0) {
        Chuoi += docSoBlock(trieu) + " triệu ";
    } else if (ty > 0 && (nghin > 0 || money > 0)) {
        Chuoi += "không triệu ";
    }

    if (nghin > 0) {
        Chuoi += docSoBlock(nghin) + " nghìn ";
    } else if (trieu > 0 && money > 0) {
        Chuoi += "không nghìn ";
    }

    if (money > 0) {
        Chuoi += docSoBlock(money) + " ";
    }

    return Chuoi.trim().charAt(0).toUpperCase() + Chuoi.trim().slice(1) + " đồng";
}

/** Copy **/
function copy(text) {
    navigator.clipboard.writeText(text).then();
}

/** Box modal **/
// Get the modal
var modal = document.getElementById("myModal");
var span = document.getElementsByClassName("close")[0];
span.onclick = function () {
    modal.style.display = "none";
}

/** Cancel payment **/
function cancelPayment(orderCode, e) {
    var $btn = $(e);
    // Disable button
    $btn.addClass('disabled-link');
    $.ajax({
        url: '/Payment/Cancel',
        type: 'POST',
        dataType: 'json',
        data: {
            orderCode: orderCode
        },
        success: function (res) {
            if (res.success) {
                renderOrders(res.data, 1);
                // Reset if cancel payment of current order
                if (res.orderCode == currentOrderCode) {
                    cancel();
                }
            }
        },

        error: function (xhr, status, error) {
            if (xhr.status === 401) {
                window.location.href = "/User/Login";
                return;
            }
        },
    }).always(function () {
        setTimeout(function () {
            $btn.removeClass('disabled-link');
        }, 2000);
        
    });
}

/** Set max height of table **/
let bodyHeight = document.body.scrollHeight;
$('.table-container').css('max-height', bodyHeight - 100 + 'px');
