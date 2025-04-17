window.playSound = (elementId) => {
    try {
        console.debug("playing sound", elementId)
        //new Audio("sounds/" + type + ".mp3").play();
        document.getElementById(elementId).play();
    } catch (e) {
        console.exception(e);
    }
};

window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text).then(function () {
        console.log('Async: Copying to clipboard was successful!');
    }, function (err) {
        console.error('Async: Could not copy text: ', err);
    });
//    const el = document.createElement("textarea");
//    el.value = str;
//    document.body.appendChild(el);
//    el.select();
//    document.execCommand("copy");
//    document.body.removeChild(el);
};