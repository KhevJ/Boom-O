const WebSocket = require("ws");

const server = new WebSocket.Server({ port: 3000 });
const gameObjects = {}; // store game state
const messageQueue = []; // queue to process messages one at a time
let isProcessing = false; // flag to track if a message is being processed

server.on("connection", (socket) => {
    console.log("A user connected");

    // send a welcome message
    socket.send(JSON.stringify({ action: "connected", message: "Welcome to UNO Server!" }));

    socket.on("message", (data) => {
        const parsedData = JSON.parse(data);
        console.log("Received:", parsedData);

        // add message to the queue
        messageQueue.push({ socket, parsedData });

        // process queue if not already processing
        processQueue();
    });

    socket.on("close", () => {
        console.log("User disconnected");
    });
});

async function processQueue() {
    if (isProcessing || messageQueue.length === 0) {
        return;
    }

    isProcessing = true; // lock queue processing

    while (messageQueue.length > 0) {
        const { socket, parsedData } = messageQueue.shift(); // get the next message in queue

        console.log("Processing message:", parsedData.action);

        if (parsedData.action === 'drawCard') {
            await sendResponse(socket, { action: "cardDrawn", message: "You drew a card!" });
        } else if (parsedData.action === 'sendDeck') {
            console.log("Received deck:", parsedData.cards);
            gameObjects["deck"] = parsedData.cards;
            await sendResponse(socket, { action: "deckSaved", message: "Deck Saved!" });
        } else if (parsedData.action === 'sendPlayerCards') {
            console.log("Received player cards:", parsedData.cards);
            gameObjects["playerCards"] = parsedData.cards;
            await sendResponse(socket, { action: "playerCardsSaved", message: "Player Cards Saved!" });
        }
    }

    isProcessing = false; // unlock queue processing
}

// helper function to send response with a delay to simulate processing time
function sendResponse(socket, response) {
    return new Promise((resolve) => {
        setTimeout(() => {
            socket.send(JSON.stringify(response));
            resolve();
        }, 500); // simulate processing delay (adjust as needed)
    });
}

console.log("UNO Server running on port 3000");
