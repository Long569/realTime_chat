$(document).ready(function () {
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

    // Function to send images
    function sendImages(files) {
        for (const f of files) {
            if (f && f.type.startsWith('image/')) {
                const reader = new FileReader();
                reader.onload = function (e) {
                    const url = e.target.result;
                    imageUrls.push(url); // Store the image URL
                    con.invoke('SendImage', userName, url);
                };
                reader.readAsDataURL(f);
            }
        }
    }

    // Function to send files
    function sendFiles(files) {
        for (const f of files) {
            if (f) {
                const fr = new FileReader();
                fr.onload = e => {
                    const url = e.target.result;
                    con.invoke('SendFile', userName, url, f.name);
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

        messageElement.innerHTML = '';
        messageElement.appendChild(input);
        messageElement.appendChild(saveButton);

        saveButton.onclick = function () {
            const updatedMessage = input.value.trim();
            if (updatedMessage) {
                con.invoke('EditMessage', id, updatedMessage)
                    .then(() => {
                        messageElement.innerHTML = `
                            <b>you:</b> <span class="message-text">${convertLinks(updatedMessage)}</span>
                            <span class="message-options">
                                <button onclick="editMessage('${id}', this.closest('.message'))">Edit</button>
                                <button onclick="deleteMessage('${id}', this.closest('.message'))">Delete</button>
                            </span>
                        `;
                    })
                    .catch(err => console.error('Error updating message:', err));
            } else {
                alert("Message cannot be empty.");
            }
        };
    };

    // Function to delete a message
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
            return `<a href=\"https://wa.me/${cleanPhone}\" target=\"_blank\">${phone}</a>`;
        });
    }

    // Connection Setup =====================
    const userName = sessionStorage.getItem('name') ?? "";
    const icon = sessionStorage.getItem('icon') ?? "";
    if (userName == "") {
        location = "/";
    }
    const param = $.param({ name: userName, icon, page: "chat" });

    const con = new signalR.HubConnectionBuilder()
        .withUrl('/hub?' + param)
        .build();

    con.onclose(err => {
        console.error("SignalR connection closed:", err);
        sessionStorage.clear();
        location = '/';
    });

    con.start().then(main).catch(err => console.error("SignalR connection error:", err));

    // Handle file messages
    con.on('ReceiveFile', (name, url, filename, who, id) => {
        const isSelf = (name === userName);
        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${id}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}</b> sent a file<br>
                    <a href="${url}" download="${filename}">${filename}</a>
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

    // Handle updates to existing messages
    con.on('UpdateMessage', (id, newMessage) => {
        const messageElement = document.querySelector(`li[data-id="${id}"] .message-text`);
        if (messageElement) {
            messageElement.innerHTML = newMessage;
        }
    });

    // Handle deletions of messages
    con.on('RemoveMessage', (id) => {
        const messageElement = document.querySelector(`li[data-id="${id}"]`);
        if (messageElement) {
            messageElement.classList.add('message-deleted');
            messageElement.innerHTML = '<em>This message has been deleted</em>';
        }
    });

    // Handle text messages
    con.on('ReceiveText', (name, message, who, id) => {
        const isSelf = (name === userName);
        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${id}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}:</b> <span class="message-text" style="white-space: pre-wrap;">${message}</span>
                    ${isSelf ? `
                        <span class="message-options">
                            <button onclick="editMessage('${id}', this.closest('.message'))">Edit</button>
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

    // Handle image messages received from the server
    con.on('ReceiveImage', (name, url, who, id) => {
        const isSelf = (name === userName);
        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${id}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}</b> sent an image<br>
                    <img src="${url}" class="image">
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

    // Handle YouTube videos
    con.on('ReceiveYouTube', (name, id, who, messageId) => {
        const isSelf = (name === userName);

        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${messageId}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}</b> sent a video<br>
                    <iframe width="400" height="300" 
                            src="https://www.youtube.com/embed/${id}"
                            frameborder="0"
                            allowfullscreen></iframe>
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

    // Handle file messages
    con.on('ReceiveFile', (name, url, filename, who, id) => {
        const isSelf = (name === userName);

        $('#chat').append(`
            <li class="message ${isSelf ? 'caller' : 'others'}" data-id="${id}">
                <div style="text-align: ${isSelf ? 'right' : 'left'};">
                    <b>${name}</b> sent a file<br>
                    <a href="${url}" download="${filename}">${filename}</a>
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

        // Image file picker
        $('#image').click(e => $('#file1').click());

        $('#file1').change(e => {
            const files = e.target.files;
            sendImages(files);
            e.target.value = null;
        });

        // General file picker
        $('#file').click(e => $('#file2').click());

        $('#file2').change(e => {
            const files = e.target.files;
            sendFiles(files);
            e.target.value = null;
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