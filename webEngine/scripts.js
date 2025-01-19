window.addEventListener('load', function () {
    console.log('JavaScript started execution');

    let isDragging = false;
    let isResizing = false;
    let isPanning = false;
    let ctrlPressed = false;
    let offsetX, offsetY;
    let startX, startY, scrollLeft, scrollTop;
    let selectedElements = []; // Array to store selected elements
    let initialPositions = []; // Array to store initial positions of selected elements
    let currentResizer;
    let removedElementsStack = []; // Stack to store removed elements

    console.log('Variables initialized');

    function initializeDraggableElements() {
        document.querySelectorAll('[data-draggable=""true""]').forEach(draggable => {
            draggable.addEventListener('mousedown', onMouseDownDraggable);

            // Add resizers
            const resizers = ['bottom-right', 'middle-right', 'middle-bottom'];

            resizers.forEach(resizer => {
                const resizerElement = document.createElement('div');
                resizerElement.className = `resizer ${resizer}`;
                draggable.appendChild(resizerElement);

                resizerElement.addEventListener('mousedown', onMouseDownResizer);
            });

            draggable.classList.add('resizable');

            // Add double-click event to send message to C#
            draggable.addEventListener('dblclick', onDoubleClickDraggable);
        });
    }

    function onMouseDownDraggable(e) {
        const draggable = e.currentTarget;

        if (ctrlPressed) {
            if (!selectedElements.includes(draggable)) {
                selectedElements.push(draggable);
                draggable.classList.add('selected');
                draggable.style.outline = '2px solid red';
            }
        } else {
            selectedElements = selectedElements.filter(el => el !== draggable);
            draggable.classList.remove('selected');
            draggable.style.outline = 'none';
        }

        if (ctrlPressed && selectedElements.length > 0) {
            isDragging = true;
            offsetX = e.clientX;
            offsetY = e.clientY;
            initialPositions = selectedElements.map(el => {
                const rect = el.getBoundingClientRect();
                return { el, left: rect.left, top: rect.top };
            });
            document.addEventListener('mousemove', onMouseMove);
        }
    }

    function onMouseDownResizer(e) {
        isResizing = true;
        currentResizer = e.currentTarget; // Set the current resizer element
        startX = e.clientX;
        startY = e.clientY;
        e.stopPropagation();
        document.addEventListener('mousemove', onResize);
    }

    function onDoubleClickDraggable() {
        const draggable = this;

        // Get the class name of the element
        const className = draggable.className.split(' ')[0];

        // Get the computed style of the element
        const computedStyle = getComputedStyle(draggable);

        // Get the background image URL
        const backgroundImage = computedStyle.backgroundImage;

        // Extract the URL from the background image string
        const urlMatch = backgroundImage.match(/url\([""']?([^""']*)[""']?\)/);

        let imageUrl = '';
        if (urlMatch) {
            imageUrl = urlMatch[1];
        }

        // Create an object to send the information to C#
        const message = {
            className: className,
            imageUrl: imageUrl
        };

        // Convert the object to a JSON string
        const messageJson = JSON.stringify(message);

        // Send the message to C#
        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(messageJson);
        } else {
            console.error('WebView2 postMessage API is not available.');
        }
    }

    function onMouseMove(e) {
        if (isDragging && selectedElements.length > 0) {
            const deltaX = e.clientX - offsetX;
            const deltaY = e.clientY - offsetY;
            initialPositions.forEach(pos => {
                pos.el.style.position = 'absolute';
                pos.el.style.left = `${pos.left + deltaX + window.scrollX}px`;
                pos.el.style.top = `${pos.top + deltaY + window.scrollY}px`;
            });
        }
    }

    function onResize(e) {
        const deltaX = e.clientX - startX;
        const deltaY = e.clientY - startY;
        const rect = currentResizer.parentElement.getBoundingClientRect();

        let newWidth, newHeight;

        switch (currentResizer.className.split(' ')[1]) {
            case 'bottom-right':
                newWidth = rect.width + deltaX;
                newHeight = rect.height + deltaY;
                break;
            case 'middle-right':
                newWidth = rect.width + deltaX;
                break;
            case 'middle-bottom':
                newHeight = rect.height + deltaY;
                break;
        }

        if (currentResizer.className.split(' ')[1] === 'middle-right') {
            currentResizer.parentElement.style.width = `${newWidth}px`;
        } else if (currentResizer.className.split(' ')[1] === 'middle-bottom') {
            currentResizer.parentElement.style.height = `${newHeight}px`;
        } else {
            // Ensure the aspect ratio remains the same
            const aspectRatio = rect.width / rect.height;
            if (newWidth / newHeight > aspectRatio) {
                newHeight = newWidth / aspectRatio;
            } else {
                newWidth = newHeight * aspectRatio;
            }

            currentResizer.parentElement.style.width = `${newWidth}px`;
            currentResizer.parentElement.style.height = `${newHeight}px`;
        }

        startX = e.clientX;
        startY = e.clientY;
    }

    function onPanMove(e) {
        if (isPanning) {
            const dx = e.clientX - startX;
            const dy = e.clientY - startY;
            window.scrollTo(scrollLeft - dx, scrollTop - dy);
        }
    }

    function initializeDropElements() {
        const body = document.querySelector('body');

        body.addEventListener('dragover', function (e) {
            e.preventDefault();
        });

        body.addEventListener('drop', function (e) {
            e.preventDefault();
            const files = e.dataTransfer.files;

            if (files.length > 0) {
                const file = files[0];
                if (file.type.startsWith('image/')) {
                    const reader = new FileReader();
                    reader.onload = function (event) {
                        const img = new Image();
                        img.src = event.target.result;
                        img.onload = function () {
                            const div = document.createElement('div');
                            div.setAttribute('data-draggable', 'true');

                            // Set the class name to the image name without the .png extension
                            const fileName = file.name.split('.').slice(0, -1).join('.');
                            div.classList.add(fileName);

                            // Calculate the new ID based on the number of existing elements with the same class
                            const existingElements = document.querySelectorAll(`.${fileName}`);
                            const newId = `${fileName}_${existingElements.length + 1}`;
                            div.id = newId;

                            div.title = newId;

                            div.style.position = 'absolute';
                            div.style.top = `${e.clientY + window.scrollY}px`; // Add window.scrollY to adjust for page scrolling
                            div.style.left = `${e.clientX + window.scrollX}px`; // Add window.scrollX to adjust for page scrolling

                            body.appendChild(div);
                            initializeDraggableElements(); // Re-initialize draggable elements
                        };
                    };
                    reader.readAsDataURL(file);
                }
            }
        });
    }


    // Event listeners for the page
    document.addEventListener('mouseup', () => {
        if (isDragging) {
            isDragging = false;
            document.removeEventListener('mousemove', onMouseMove);
        }
        if (isResizing) {
            isResizing = false;
            document.removeEventListener('mousemove', onResize);
        }
        if (isPanning) {
            isPanning = false;
            document.removeEventListener('mousemove', onPanMove);
        }
    });

    document.addEventListener('keydown', (e) => {
        if (e.key === 'Control') {
            ctrlPressed = true;
        } else if ((e.key === 'Delete' || e.key === 'Backspace') && selectedElements.length > 0) {
            e.preventDefault();
            selectedElements.forEach(element => {
                removedElementsStack.push({
                    element: element,
                    parent: element.parentNode,
                    nextSibling: element.nextSibling
                });
                element.remove();
            });
            selectedElements = []; // Reset the selected elements
        } else if (e.key === 'z' && ctrlPressed) {
            const lastRemoved = removedElementsStack.pop();
            if (lastRemoved) {
                lastRemoved.parent.insertBefore(lastRemoved.element, lastRemoved.nextSibling);
            }
        }
    });

    document.addEventListener('keyup', (e) => {
        if (e.key === 'Control') {
            ctrlPressed = false;
        }
    });

    document.addEventListener('mousedown', (e) => {
        if (e.button === 1) {
            isPanning = true;
            startX = e.clientX;
            startY = e.clientY;
            scrollLeft = window.scrollX;
            scrollTop = window.scrollY;
            document.addEventListener('mousemove', onPanMove);
            e.preventDefault();
        }
    });

    // Função para forçar a re-renderização durante a rolagem
    window.addEventListener('scroll', () => {
        document.documentElement.style.transform = 'translateZ(0)';
    });

    // Inicializa os elementos arrastáveis e de arrastar e soltar ao carregar a página
    initializeDraggableElements();
    initializeDropElements();

    console.log('JavaScript execution completed');
});