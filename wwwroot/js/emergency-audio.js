window.aeroEmergencyAudio = {
    playWarning: function () {
        const audio =
            document.getElementById(
                "emergency-warning-audio");

        if (!audio) {
            console.warn(
                "Emergency warning audio element was not found.");

            return false;
        }

        audio.currentTime = 0;

        audio.play().catch(error => {
            console.warn(
                "Emergency warning audio could not play:",
                error);
        });

        return true;
    }
};