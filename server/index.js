
'use strict';

const http = require('http');
const socket = require('socket.io');
const server = http.createServer();
const { v4: uuidV4 } = require('uuid');
const port = 3000;


var io = socket(server, {
    pingInterval: 15000,
    pingTimeout: 5000,
    transports: ["websocket"]
});

io.use((socket, next) => {
    if (socket.handshake.query.token === "UNITY") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
});


const gameObjects = {}; // store game state
const messageQueue = []; // queue to process messages in order
let isProcessing = false;
const rooms = new Map();// will store all rooms

io.on("connection", (socket) => {
    console.log("A user connected");
    const playerName = socket.handshake.query.playerName || "Unknown Player";
    console.log(`Player Connected: ${playerName}`);

    socket.emit("welcome", { message: `Welcome, ${playerName}!` });
    // console.log(socket);
    // console.log(messageQueue);

    socket.on('createRoom', async (callback) => {
        messageQueue.push({ socket, action: "createRoom", callback });
        processQueue();
        // const roomId = uuidV4(); // <- 1 create a new uuid
        // await socket.join(roomId); // <- 2 make creating user join the room

        // // set roomId as a key and roomData including players as value in the map
        // rooms.set(roomId, { // <- 3
        //     roomId,
        //     players: [{ id: socket.id, username: socket.data?.username }]
        // });
        // // returns Map(1){'2b5b51a9-707b-42d6-9da8-dc19f863c0d0' => [{id: 'socketid', username: 'username1'}]}

        // callback(roomId);
    });

    socket.on('joinRoom', async (data, callback) => {
        messageQueue.push({ socket, action: "joinRoom", data, callback });
        processQueue();
    });


    //? useless function to test
    // Add incoming messages to the queue 
    socket.on("message", (data) => {
        console.log("yo");
        // messageQueue.push({ socket, data });
        processQueue();
    });

    socket.on("drawCard", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action: "drawCard", data });
        processQueue();
    });

    socket.on("sendDeck", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action: "sendDeck", data });
        processQueue();
    });

    socket.on("sendPlayerCards", (data) => {
        // console.log(data)
        messageQueue.push({ socket, action: "sendPlayerCards", data });
        processQueue();
    });

    socket.on("sendTopCard", (data) => { //the one card that is on top of the pile
        // console.log(data)
        messageQueue.push({ socket, action: "sendTopCard", data });
        processQueue();
    });




    socket.on("disconnect", () => {
        console.log("A user disconnected");
    });
});

async function processQueue() {
    if (isProcessing || messageQueue.length === 0) {
        return;
    }

    isProcessing = true; // lock queue processing

    while (messageQueue.length > 0) {
        const { socket, action, data, callback } = messageQueue.shift(); // get the next message

        console.log("Processing message:", action);
        if (action === 'createRoom') {
            //const roomId = uuidV4();
            const roomId = "Khevin's Room"
            await socket.join(roomId);
            rooms.set(roomId, {
                roomId,
                players: [{ id: socket.id }]
            });

            console.log(`Room created: ${roomId}`);
            if (callback) {
                callback(roomId);
            }
        }

        if (action === 'joinRoom') {
            // check if room exists and has a player waiting
            console.log(data);
            const room = rooms.get(data);
            console.log(room);
            // add the joining user's data to the list of players in the room
            if ( room && room.players.length < 2) {
                await socket.join(data); // make the joining client join the room
                const roomUpdate = {
                    ...room,
                    players: [
                        ...room.players,
                        { id: socket.id },
                    ],
                };
                rooms.set(data, roomUpdate);
                if (callback) {
                    callback(data);
                }
                // console.log(socket.id)
                // console.log(room.players)
                // console.log("Room length " + room.players.length)
                io.to(data).emit("roomLength", rooms.get(data).players.length )
            }
            else {
                if (callback) {
                    callback("Error");
                }
            }


        }
        if (action === 'drawCard') {
            handleDrawCard(socket, data);
        } else if (action === 'sendDeck') {
            handleSendDeck(socket, data);
        } else if (action === 'sendPlayerCards') {
            handleSendPlayerCards(socket, data);
        } else if (action === 'sendTopCard') {
            handleTopCard(socket, data);
        }
    }

    isProcessing = false; // unlock queue processing
}

//example handlers for events 
function handleDrawCard(socket, data) {
    console.log("Received drawn card:", data);

    if (gameObjects.deck) {
        if (gameObjects.deck.length > 0 && gameObjects.deck[0] == data) {
            const topCard = gameObjects.deck.shift();
            //console.log("here")
            socket.emit('drawnCard', "Server said You drew " + topCard);
            //here add something to broadcast to all other players in the room/game
        }
    }
}

function handleTopCard(socket, data) {
    gameObjects["topCard"] = data;
    console.log("Server said received top card:", data);
    // ! should be broacast to everyone except the host
    socket.emit('topCardUpdate', "Server said The top card is " + data);
}

function handleSendDeck(socket, data) {
    console.log("Received deck:", data);
    gameObjects["deck"] = data;
    // ! should be broacast to everyone except the host
    socket.emit('deckSaved', "Server said Yugioh is better than UNO");
}

function handleSendPlayerCards(socket, data) {
    console.log("Received deck:", data);
    gameObjects["playerHand"] = data;
    socket.emit('playerCardsSaved', "Server said why play UNO when Yugioh exists");
}

//helper function to send response with delay for testing
// function sendResponse(socket, response) {
//     return new Promise((resolve) => {
//         setTimeout(() => {
//             socket.emit("response", response);
//             resolve();
//         }, 500); // simulate delay
//     });
// }

io.listen(port);

console.log('UNO Server running on port ' + port);


