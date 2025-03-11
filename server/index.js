const WebSocket = require("ws");
//socket.io and websocket 
const server = new WebSocket.Server({ port: 3000 });
const gameObjects = {} //not sure why I am doing this but it should work
 
server.on("connection", (socket) => {
    console.log("A user connected");

    // Send a welcome message
    socket.send(JSON.stringify({ action: "connected", message: "Welcome to UNO Server!" }));

    socket.on("message", (data) => {
        console.log("Received:", data.toString());

        // Example: Handle drawing a card
        const parsedData = JSON.parse(data);
        if (parsedData.action === "drawCard") {
            socket.send(JSON.stringify({ action: "cardDrawn", message: "You drew a card!" }));
        }

        // if (data.action === "sendDeck") {
        //     console.log("Received deck from player:", data.cards);
        //     gameObjects[cards] = data.cards;

        // }
    });

    socket.on("close", () => {
        console.log("User disconnected");
    });
});

console.log("UNO Server running on port 3000");
