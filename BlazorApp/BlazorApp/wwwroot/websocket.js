window.webSocket = {
    connect: function (url, dotNetRef) {
        window.socket = new WebSocket(url);
        window.socket.onmessage = function (event) {
            dotNetRef.invokeMethodAsync('ReceiveMessage', event.data);
        };
    },
    send: function (message) {
        window.socket.send(message);
    },
    close: function () {
        window.socket.close();
    }
};