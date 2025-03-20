
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
            let reverse = 1;
            rooms.set(roomId, {
                roomId,
                players: [{ id: socket.id }],
                reverse// for when we need to reverse the order of player 
                // did you know yugioh is the best turn based game
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
    console.log("Server said received top card:", data);
    // ! should be broacast to everyone except the host
    const room = rooms.get(data.roomId);
    const roomUpdate = {
        ...room,
        topCard: data.topCard
    };
    rooms.set(data.roomId, roomUpdate);

    // console.log(rooms.get(data.roomId));
    socket.broadcast.to(data.roomId).emit('topCardUpdate', data.topCard);

}

function handleSendDeck(socket, data) {
    console.log("Received deck:", data);
    const room = rooms.get(data.roomId);
    let deck = [...data.deck]; 
    gameObjects["deck"] = deck; //! delete this later this is just for testing drawing when drawing is not fully implemented
    const playerHands = {};

    for (const player of room.players) { //player here is the socket
        playerHands[player.id] = deck.splice(0, 7); //each player gets 7 cards
    }

    const roomUpdate = {
        ...room,
        deck: deck,
        playerHands: playerHands
    };
    rooms.set(data.roomId, roomUpdate);

    console.log(rooms.get(data.roomId));

    for (const player of room.players) {
        io.to(player.id).emit("playerCardsSaved", playerHands[player.id]); //each player gets their hand
    }
    // ! should be broacast to everyone except the host Done Brother
    io.to(data.roomId).emit("savedDeck", deck) //send deck to everyone
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


