window.playSound = (elementId) => {
    try {
        console.log("playing", elementId)
        const audioElement = document.getElementById(elementId);

        // Create a new Audio object to support concurrent sounds
        const audio = new Audio(audioElement.src);
        audio.oncanplaythrough = () => {
            audio.play();
        };
    } catch (e) {
        console.error(e);
    }
};

window.copyToClipboard = (text) => {
    navigator.clipboard.writeText(text).then(function () {
        console.log(`Copied play URL: ${text}`);
    }, function (err) {
        console.error(`Unable to copy play URL: ${text}`, err);
    });
};