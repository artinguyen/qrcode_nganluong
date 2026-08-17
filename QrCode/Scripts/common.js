/**
Convert number to text currency
**/

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

/**
Format currency
**/
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
/** Copy **/
function copy(text) {
    navigator.clipboard.writeText(text).then();
}

/**
Create random code
**/
function randomCode() {
    const prefix = "VCBSGL";
    const allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    let result = prefix;

    for (let i = 0; i < 13; i++) {
        const randomIndex = Math.floor(Math.random() * allowedChars.length);
        result += allowedChars[randomIndex];
    }
    return result;
}
/** Box modal **/
function closeModal() {
    let modal = document.getElementById("myModal");
    var span = document.getElementsByClassName("close")[0];
    span.onclick = function () {
        modal.style.display = "none";
    }
}

function showModal(message) {
    let modal = document.getElementById("myModal");
    modal.style.display = 'block';
    if (message) {
        $('#myModal p').text(message);
    }
}

function showLoadingModal(display) {
    let modal = document.getElementById("loadingModal");
    if (!display) {
        modal.style.display = 'none';
        return;
    }
    modal.style.display = 'block';
}
/**
Get current date
**/
function getCurrentDate() {
    const today = new Date();
    const dd = String(today.getDate()).padStart(2, '0');
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const yyyy = today.getFullYear();
    //return `${dd}/${mm}/${yyyy}`;
    return dd + '/' + mm + '/' + yyyy;
}
/** Reset **/
function reset() {
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
    $('.qr-code-box').removeClass('qr-active');
}
/**
Set height of table
**/
$(window).resize(function () {
    /** Set max height of table **/
    let bodyHeight = document.body.scrollHeight;
    $('.table-container').css('max-height', bodyHeight - 100 + 'px');
});
