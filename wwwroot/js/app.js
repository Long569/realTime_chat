$(document).ready(function () {
    let imageUrls = [];

    // Toggle theme controls visibility
    $('#toggleThemeControls').click(function () {
        $('#themeControls').toggle(); // Toggle visibility
    });

    // Handle color theme changes
    $('#colorThemePicker').on('input', function () {
        const color = $(this).val();
        $('header, footer').css('background-color', color);
        $('main').css('background-color', `rgba(255, 255, 255, 0.8)`);
    });

    // Handle background image changes and broadcast to all users
    $('#backgroundImagePicker').on('change', function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                const imageUrl = e.target.result;
                $('body').css('background-image', `url(${imageUrl})`);
                con.invoke('ChangeBackgroundImage', imageUrl); // Broadcast to other users
            };
            reader.readAsDataURL(file);
        }
    });

    // Handle background music changes
    $('#backgroundMusicPicker').on('change', function () {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function (e) {
                const audio = document.getElementById('backgroundMusic');
                audio.src = e.target.result;
                audio.hidden = false;

                // Play the audio after the user clicks a button
                $('#playMusicButton').show().click(function () {
                    audio.play().catch(error => {
                        console.error('Audio play failed:', error);
                    });
                    $(this).hide(); // Hide the button after playing
                });
            };
            reader.readAsDataURL(file);
        }
    });

    // Hide the play button initially
    $('#playMusicButton').hide();

    // Function to play a sound
    function playSound(src) {
        const sound = new Audio(src);
        sound.play().catch(error => {
            console.error('Audio play failed:', error);
        });
    }

    // General Functions ====================
    function getImageURL(message) {
        const re = /\.(jpg|jpeg|png|webp|bmp|gif)$/i;
        try {
            const url = new URL(message);
            if (re.test(url.pathname)) {
                return url.href;
            }
        } catch {
            // Do nothing
        }
        return null;
    }

    function getYouTubeId(message) {
        try {
            const url = new URL(message);
            if (url.hostname == 'www.youtube.com' && url.pathname == '/watch') {
                return url.searchParams.get('v');
            }
        } catch {
            // Do nothing
        }
        return null;
    }

    function sendImages(files) {
        for (const file of files) {
            if (file && file.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    const url = e.target.result;
                    const messageId = Math.random().toString(36).substring(2, 9); // Generate a random message ID for the image
                    imageUrls.push({ id: messageId, url }); // Store the image URL with an ID
    
                    // Debugging: Log the image URL to ensure it is being read correctly
                    console.log('Image URL:', url);
    
                    // Send the image URL to the server
                    con.invoke('SendImage', userName, url).then(() => {
                        console.log('Image sent to server');
                    }).catch(err => console.error('Error sending image:', err));
                };
                reader.readAsDataURL(file);
            }
        }
    }

    // Function to format the timestamp
    function formatTimestamp(timestamp) {
        const date = new Date(timestamp);
        console.log(timestamp);
        return date.toLocaleString(); // Format date and time as a string
    }

    // Function to calculate relative time
    function getRelativeTime(timestamp) {
        const now = new Date();
        const secondsPast = Math.floor((now - new Date(timestamp)) / 1000);

        if (secondsPast < 60) {
            return secondsPast + " seconds ago";
        }
        if (secondsPast < 3600) {
            return Math.floor(secondsPast / 60) + " minutes ago";
        }
        if (secondsPast < 86400) {
            return Math.floor(secondsPast / 3600) + " hours ago";
        }
        return Math.floor(secondsPast / 86400) + " days ago";
    }

    // Function to append a message with a timestamp
    function appendMessage(name, message, timestamp, isSelf, messageId) {
        const formattedTime = formatTimestamp(timestamp);
        const relativeTime = getRelativeTime(timestamp);
        const convertedMessage = convertLinks(message);

        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${messageId}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}:</b> <span class="message-text" style="white-space: pre-wrap;">${convertedMessage}</span>
                    <div class="timestamp" style="font-size: 0.8em; color: gray;">
                        ${formattedTime} (${relativeTime})
                    </div>
                    ${isSelf ? `
                        <span class="message-options">
                            <button onclick="editMessage('${messageId}', this.closest('.message'))">Edit</button>
                            <button onclick="deleteMessage('${messageId}', this.closest('.message'))">Delete</button>
                        </span>` : ''}
                </div>
            </li>
        `);
    }

    // Function to send files
    function sendFiles(files) {
        for (const f of files) {
            if (f) {
                const fr = new FileReader();
                fr.onload = e => {
                    const url = e.target.result;
                    con.invoke('SendFile', userName, url, f.name); // Send the file to the server
                };
                fr.readAsDataURL(f);
            }
        }
    }

    // Function to edit a message
    window.editMessage = function (id, messageElement) {
        const messageTextElement = messageElement.querySelector('.message-text');

        if (!messageTextElement) {
            console.error('Message text element not found.');
            return;
        }

        const originalMessage = messageTextElement.innerText;
        const input = document.createElement('textarea');
        input.value = originalMessage;
        input.classList.add('edit-input');

        const saveButton = document.createElement('button');
        saveButton.textContent = 'Save';
        saveButton.classList.add('save-btn');

        messageElement.innerHTML = ''; // Clear the message element
        messageElement.appendChild(input);
        messageElement.appendChild(saveButton);

        saveButton.onclick = function () {
            const updatedMessage = input.value.trim();
            if (updatedMessage) {
                console.log('Sending updated message:', updatedMessage);
                con.invoke('EditMessage', id, updatedMessage)
                    .then(() => {
                        console.log('Message updated on server:', updatedMessage);
                        const text = convertLinks(updatedMessage);
                        const el = $(`li[data-id='${id}']`);
                        if (el.length) {
                            el.html(`
                                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                                    <b>${name}:</b> <span class="message-text" style="white-space: pre-wrap;">${text}</span>
                                    <div class="timestamp" style="font-size: 0.8em; color: gray;">
                                        ${formattedTime} (${relativeTime})
                                    </div>
                                    ${isSelf ? `
                                        <span class="message-options">
                                            <button onclick="editMessage('${id}', this.closest('.message'))">Edit</button>
                                            <button onclick="deleteMessage('${id}', this.closest('.message'))">Delete</button>
                                        </span>` : ''}
                                </div>
                            `);
                        }
                    })
                    .catch(err => console.error('Error updating message:', err));
            } else {
                alert("Message cannot be empty.");
            }
        };
    };

     // Function to delete a message (image or text)
    window.deleteMessage = function (id, messageElement) {
        con.invoke('DeleteMessage', id)
            .then(() => {
                messageElement.innerHTML = '<em>This message has been deleted</em>';
                messageElement.classList.add('message-deleted');
            })
            .catch(err => console.error('Error deleting message:', err));
    };

    // Utility function to convert email and phone links
    function convertLinks(message) {
        message = convertToMailtoLink(message);  // Convert emails to mailto links
        message = convertToWhatsAppLink(message); // Convert phone numbers to WhatsApp links
        return message;
    }

    // Function to convert email addresses to mailto links
    function convertToMailtoLink(message) {
        const emailRegex = /\b[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z]{2,}\b/gi;
        return message.replace(emailRegex, email => {
            return `<a href="mailto:${email}">${email}</a>`;
        });
    }

    // Function to convert phone numbers to WhatsApp links
    function convertToWhatsAppLink(message) {
        const phoneRegex = /\b(\+?\d{1,4})?\d{7,12}\b/g;
        return message.replace(phoneRegex, phone => {
            const cleanPhone = phone.replace(/\D/g, ''); // Clean non-digit characters
            return `<a href="https://wa.me/${cleanPhone}" target="_blank">${phone}</a>`;
        });
    }

    // Connection Setup
    const userName = sessionStorage.getItem('name') ?? "";
    const icon = sessionStorage.getItem('icon') ?? "";
    if (userName == "") {
        location = "/";
    }
    const param = $.param({ name: userName, icon, page: "chat" });

    const con = new signalR.HubConnectionBuilder()
        .withUrl('/hub?' + param)
        .build();
    con.start().then(() => {
        console.log('SignalR connection established.');
        main(); // Ensure this function is called after the connection is established
    }).catch(err => console.error("SignalR connection error:", err));

    con.onclose(err => {
        console.error("SignalR connection closed:", err);
        sessionStorage.clear();
        location = '/';
    });



    // Handle file messages
    con.on('ReceiveFile', (name, url, filename, who, id, timestamp) => {
        const isSelf = (name === userName);
        const formattedTime = formatTimestamp(timestamp);  // Format the timestamp
        const relativeTime = getRelativeTime(timestamp);  // Get the relative time

        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${id}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}</b> sent a file<br>
                    <a href="${url}" download="${filename}">${filename}</a>
                    <div class="timestamp" style="font-size: 0.8em; color: gray;">
                        ${formattedTime} (${relativeTime})
                    </div>
                    ${isSelf ? `
                        <span class="message-options">
                            <button onclick="deleteMessage('${id}', this.closest('.message'))">Delete</button>
                        </span>` : ''}
                </div>
            </li>
        `);

        // Play the received sound if it's from others
        if (!isSelf) {
            playSound('sounds/message-received.mp3');
        }
    });

    // Handle text messages
    con.on('ReceiveText', (name, message, who, id, timestamp) => {
        console.log('Received text message:', { name, message, who, id, timestamp });
        const isSelf = (name === userName);
        appendMessage(name, message, timestamp, isSelf, id);
    });

    // Handle message update
    con.on('UpdateMessage', (messageId, newMessage) => {
        const messageElement = $(`[data-id="${messageId}"] .message-text`);
        if (messageElement.length) {
            console.log('Updating message in DOM:', newMessage);
            messageElement.html(convertLinks(newMessage));
        }
    });

    // Handle message removal from the server
    con.on('RemoveMessage', (messageId) => {
        const messageElement = $(`[data-id="${messageId}"]`);
        if (messageElement.length) {
            messageElement.html('<em>This message has been deleted</em>');
            messageElement.addClass('message-deleted');
        }
    });


    // Handle image messages received from the server
    con.on('ReceiveImage', (name, url, who, id, timestamp) => {
        const isSelf = (name === userName);
        const formattedTime = formatTimestamp(timestamp);
        const relativeTime = getRelativeTime(timestamp);
    
        // Debugging: Log the image URL to ensure it is received correctly
        console.log('Received image URL:', url);
    
        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${id}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}</b> sent an image<br>
                    <img src="${url}" class="image">
                    <div class="timestamp" style="font-size: 0.8em; color: gray;">
                        ${formattedTime} (${relativeTime})
                    </div>
                    ${isSelf ? `
                        <span class="message-options">
                            <button onclick="deleteMessage('${id}', this.closest('.message'))">Delete</button>
                        </span>` : ''}
                </div>
            </li>
        `);
    });

    // Handle YouTube videos
    con.on('ReceiveYouTube', (name, id, who, messageId, timestamp) => {
        const isSelf = (name === userName);
        const formattedTime = formatTimestamp(timestamp);  // Format the timestamp
        const relativeTime = getRelativeTime(timestamp);  // Get the relative time

        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${messageId}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}</b> sent a video<br>
                    <iframe width="400" height="300" 
                            src="https://www.youtube.com/embed/${id}"
                            frameborder="0"
                            allowfullscreen></iframe>
                    <div class="timestamp" style="font-size: 0.8em; color: gray;">
                        ${formattedTime} (${relativeTime})
                    </div>
                    ${isSelf ? `
                        <span class="message-options">
                            <button onclick="deleteMessage('${messageId}', this.closest('.message'))">Delete</button>
                        </span>` : ''}
                </div>
            </li>
        `);

        // Play the received sound if it's from others
        if (!isSelf) {
            playSound('sounds/message-received.mp3');
        }
    });

    // Handle status updates (e.g., user count, status messages)
    con.on('UpdateStatus', (count, status, names) => {
        $('#btn > span').text(icon);
        $('#count').text(count);

        $('#chat').append(`
            <li class="status">
                <div>
                    ${status}
                </div>
            </li>
        `);

        $('#pop').html(names.sort().join('<br>'));
    });

    // Handle background image changes from other users
    con.on('ChangeBackgroundImage', (imageUrl) => {
        $('body').css('background-image', `url(${imageUrl})`);
    });

    // Fetch and update the game list
    function updateGameList(games) {
        $('#games').empty();
        games.forEach(game => {
            $('#games').append(`
                <li data-game-id="${game.Id}">
                    ${game.GameType} - ${game.IsWaiting ? "Waiting for player" : "In progress"}
                </li>
            `);
        });
    }

    // Handle game list updates from the server
    // con.on('UpdateList', (games) => {
    //     updateGameList(games);
    // });

    // Click event on a game to join or view details
    $('#games').on('click', 'li', function () {
        const gameId = $(this).data('game-id');
        // Implement game joining or viewing logic here
    });

    // On connection start, request the game list
    con.start().then(() => {
        // Request initial game list
        con.invoke('RequestGameList');
        main();
    }).catch(err => console.error("SignalR connection error:", err));

    function main() {
        // Handle the form submission
        $('footer form').submit(e => {
            e.preventDefault();
            const message = $('#message').val().trim();

            if (message) {
                const url = getImageURL(message);
                const id = getYouTubeId(message);

                if (url) {
                    con.invoke('SendImage', userName, url);
                } else if (id) {
                    con.invoke('SendYouTube', userName, id);
                } else {
                    con.invoke('SendText', userName, message)
                        .then(() => {
                            playSound('sounds/message-sent.mp3');  // Play sound on message sent
                        })
                        .catch(err => console.error('Error sending message:', err));
                }
            }
            $('#message').val('').focus(); // Clear the input field
        });

        // Handle Enter key for sending messages
        $('#message').on('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                $(this).closest('form').submit();
            }
        });

        // Fullscreen image toggle
        $(document).on('click', '.image', e => {
            document.fullscreenElement ?
                document.exitFullscreen() :
                e.target.requestFullscreen();
        });

        // Event listener for the "Leave" button
        $('#leave').click(function () {
            // Disconnect SignalR connection if needed
            if (con) {
                con.stop().then(() => {
                    sessionStorage.clear();
                    window.location.href = 'index.html';
                }).catch(err => {
                    console.error('Error stopping SignalR connection:', err);
                    window.location.href = 'index.html'; // Redirect anyway if an error occurs
                });
            } else {
                sessionStorage.clear();
                window.location.href = 'index.html';
            }
        });

        // Event listener for the image button
        $('#image').click(function () {
            $('#file1').click(); // Trigger the file input
        });

        // Event listener for image file input change
        $('#file1').change(function (e) {
            const files = e.target.files;
            sendImages(files); // Function to handle image sending
            e.target.value = null; // Clear the input value to allow the same file to be selected again
        });

        // Event listener for the file button
        $('#file').off('click').on('click', e => {
            $('#file2').click();
        });

        // Event listener for file input change
        $('#file2').off('change').on('change', e => {
            const files = e.target.files;
            sendFiles(files);
            e.target.value = null; // Clear the input value to allow the same file to be selected again
        });

        // Drag and drop functionality for images and files
        $('main').on('dragenter dragover', e => {
            e.preventDefault();
            $('main').addClass('active');
        });

        $('main').on('dragleave drop', e => {
            e.preventDefault();
            $('main').removeClass('active');
        });

        $('main').on('drop', e => {
            e.preventDefault();
            const files = e.originalEvent.dataTransfer.files;
            sendImages(files);
        });

        // Display gallery with all images sent during the session
        $('#gallery').click(e => {
            if (imageUrls.length === 0) {
                $('#container').html('No images to display');
            } else {
                $('#container').empty();
                imageUrls.forEach(url => {
                    $('#container').append(`<img src="${url}" class="image">`);
                });
            }
            $('#dialog')[0].showModal();
        });

        $('#dialog').on('close', e => {
            $('#container').empty();
        });

        // Optional: Adjust textarea height based on content
        $('#message').on('input', function () {
            this.style.height = 'auto';
            this.style.height = (this.scrollHeight) + 'px';
        });
    }
});