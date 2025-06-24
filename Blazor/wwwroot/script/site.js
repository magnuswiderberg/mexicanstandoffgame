window.playSound = (elementId) => {
    try {
        const audioElement = document.getElementById(elementId);
        if (!audioElement) {
            console.error(`Audio element with ID ${elementId} not found.`);
            return;
        }

        // Create a new Audio object to support concurrent sounds
        const audio = new Audio(audioElement.src);
        audio.oncanplaythrough = () => {
            audio
                .play()
                .catch(error => {
                    console.log(`Play sound "${elementId}" failed: ${error}.`);
                });
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

window.Speak = async (text) => {
    console.log(`Speaking "${text}"`);

    await play(text)
        .catch(error => {
            console.error(`Error speaking text "${text}":`, error);
        })
        .then(() => {});

    async function play(text) {
        if (!window.speechSynthesis) {
            console.warn('Speech synthesis not supported in this browser.');
            return;
        }
        const utterance = new SpeechSynthesisUtterance(text);
        utterance.lang = "en-US";
        utterance.rate = 0.6;
        utterance.pitch = 0.5;
        window.speechSynthesis.speak(utterance);

        return new Promise(resolve => {
            utterance.onend = resolve;
        });
    }
}