using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace webEngine
{
    public class ScriptInject
    {
        public static async void LoadJs(Microsoft.Web.WebView2.Wpf.WebView2 webView)
        {
            if (webView.CoreWebView2 != null)
            {
                string jsContent = @"
          window.addEventListener('load', function() {
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


function onDoubleClickDraggable(e) {
    e.preventDefault();  // Impedir o comportamento padrão do duplo clique

    const draggable = this;

    // Criar e exibir um menu com opções
    const menu = document.createElement('div');
    menu.style.position = 'absolute';
    menu.style.backgroundColor = '#ffffff';
    menu.style.border = '1px solid #ddd';
    menu.style.borderRadius = '8px';  // Borda arredondada
    menu.style.padding = '10px';
    menu.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.1)';
    menu.style.zIndex = '2147483647';  // Garantir que o menu tenha o maior z-index possível
    menu.style.fontFamily = 'Arial, sans-serif';  // Melhorar a legibilidade
    menu.style.fontSize = '14px';  // Tamanho da fonte
    menu.style.minWidth = '200px';  // Largura mínima para o menu
    menu.style.transition = 'all 0.3s ease';  // Transição suave para interações

    menu.innerHTML = `
        <div id=""viewStyle"" style=""padding: 8px; cursor: pointer; transition: background-color 0.3s ease;"">Abrir CSS</div>
        <div id=""setId"" style=""padding: 8px; cursor: pointer; transition: background-color 0.3s ease;"">Definir ID</div>
        <div id=""setZIndex"" style=""padding: 8px; cursor: pointer; transition: background-color 0.3s ease;"">Alterar Z-Index</div>
        <div id=""setBackgroundColor"" style=""padding: 8px; cursor: pointer; transition: background-color 0.3s ease;"">Alterar Cor de Fundo</div>
        <div id=""copyHTML"" style=""padding: 8px; cursor: pointer; transition: background-color 0.3s ease;"">Copiar HTML</div>
        <div id=""removeElement"" style=""padding: 8px; cursor: pointer; transition: background-color 0.3s ease;"">Remover Elemento</div>
        <div id=""addCollision"" style=""padding: 8px; cursor: pointer; transition: background-color 0.3s ease;"">Adicionar Colisão</div>
    `;

    // Posição do menu onde o mouse clicou, considerando o scroll
    const mouseX = e.clientX + window.scrollX;
    const mouseY = e.clientY + window.scrollY;

    menu.style.left = `${mouseX + 10}px`; // 10px de distância para a direita do cursor
    menu.style.top = `${mouseY + 10}px`; // 10px de distância para baixo do cursor

    document.body.appendChild(menu);

    // Ações ao clicar nas opções do menu
    document.getElementById('viewStyle').addEventListener('click', function() {
        const className = draggable.className.split(' ')[0];
        const computedStyle = getComputedStyle(draggable);
        const backgroundImage = computedStyle.backgroundImage;
        const urlMatch = backgroundImage.match(/url\([""""""""']?([^""""""""']*)[""""""""']?\)/);
        let imageUrl = '';
        if (urlMatch) {
            imageUrl = urlMatch[1];
        }

        const message = { className: className, imageUrl: imageUrl };
        const messageJson = JSON.stringify(message);

        if (window.chrome && window.chrome.webview) {
            window.chrome.webview.postMessage(messageJson);
        } else {
            console.error('WebView2 postMessage API is not available.');
        }

        document.body.removeChild(menu);
    });

    document.getElementById('setId').addEventListener('click', function() {
        const newId = prompt(""Digite o novo ID para o elemento:"", draggable.id);
        if (newId) {
            draggable.id = newId;
        }
        document.body.removeChild(menu);
    });

    document.getElementById('setZIndex').addEventListener('click', function() {
        const newZIndex = prompt(""Digite o novo Z-Index para o elemento:"", draggable.style.zIndex);
        if (newZIndex !== null && newZIndex !== '') {
            draggable.style.zIndex = newZIndex;
        }
        document.body.removeChild(menu);
    });

    document.getElementById('setBackgroundColor').addEventListener('click', function() {
        const newColor = prompt(""Digite a nova cor de fundo para o elemento (em formato hexadecimal ou nome):"", draggable.style.backgroundColor);
        if (newColor !== null && newColor !== '') {
            draggable.style.backgroundColor = newColor;
        }
        document.body.removeChild(menu);
    });

    document.getElementById('copyHTML').addEventListener('click', function() {
        const html = draggable.outerHTML;
        navigator.clipboard.writeText(html).then(() => {
            alert(""HTML copiado para a área de transferência!"");
        });
        document.body.removeChild(menu);
    });

    document.getElementById('removeElement').addEventListener('click', function() {
        if (confirm(""Tem certeza de que deseja remover este elemento?"")) {
            draggable.remove();
        }
        document.body.removeChild(menu);
    });

    // Adicionar colisão (movível e redimensionável apenas pelo canto inferior direito)
    document.getElementById('addCollision').addEventListener('click', function() {
        // Criar uma div de colisão sobre o elemento
        const collisionDiv = document.createElement('div');
        collisionDiv.style.position = 'absolute';
        collisionDiv.style.backgroundColor = 'rgba(255, 0, 0, 0.3)'; // Cor semi-transparente
        collisionDiv.style.border = '1px dashed red';  // Borda vermelha para destacar a colisão
        collisionDiv.style.zIndex = '9999'; // Certificar-se de que a colisão tenha um z-index alto
        collisionDiv.style.top = `${0}px`;
        collisionDiv.style.left = `${0}px`;
        collisionDiv.style.width = `${draggable.offsetWidth}px`;
        collisionDiv.style.height = `${draggable.offsetHeight}px`;

        // Variáveis para controle de movimento
        let isDragging = false;
        let offsetX, offsetY;

        // Variáveis para controle de redimensionamento
        let isResizing = false;
        let startWidth, startHeight, startX, startY;

        // Função para redimensionar a colisão
        function resizeCollision(e) {
            if (isResizing) {
                const dx = e.clientX - startX;
                const dy = e.clientY - startY;

                collisionDiv.style.width = `${startWidth + dx}px`;
                collisionDiv.style.height = `${startHeight + dy}px`;
            }
        }

        // Iniciar o redimensionamento
        function startResize(e) {
            isResizing = true;
            startWidth = collisionDiv.offsetWidth;
            startHeight = collisionDiv.offsetHeight;
            startX = e.clientX;
            startY = e.clientY;

            document.addEventListener('mousemove', resizeCollision);
            document.addEventListener('mouseup', stopResize);
        }

        // Parar o redimensionamento
        function stopResize() {
            isResizing = false;
            document.removeEventListener('mousemove', resizeCollision);
            document.removeEventListener('mouseup', stopResize);
        }

        // Adicionar a alça de redimensionamento no canto inferior direito
        const resizeHandle = document.createElement('div');
        resizeHandle.style.position = 'absolute';
        resizeHandle.style.width = '10px';
        resizeHandle.style.height = '10px';
        resizeHandle.style.backgroundColor = 'blue';
        resizeHandle.style.bottom = '0';
        resizeHandle.style.right = '0';
        resizeHandle.style.cursor = 'se-resize';

        resizeHandle.addEventListener('mousedown', startResize);

        collisionDiv.appendChild(resizeHandle);

        // Movimentação da colisão
        collisionDiv.addEventListener('mousedown', function(e) {
            if (e.target !== collisionDiv) return;

            isDragging = true;
            offsetX = e.clientX - collisionDiv.offsetLeft;
            offsetY = e.clientY - collisionDiv.offsetTop;

            document.addEventListener('mousemove', moveCollision);
            document.addEventListener('mouseup', stopDrag);
        });

        // Movendo a colisão
        function moveCollision(e) {
            if (isDragging && !ctrlPressed) {
                collisionDiv.style.left = `${e.clientX - offsetX}px`;
                collisionDiv.style.top = `${e.clientY - offsetY}px`;
            }
        }

        // Parando a movimentação
        function stopDrag() {
            isDragging = false;
            document.removeEventListener('mousemove', moveCollision);
            document.removeEventListener('mouseup', stopDrag);
        }

        draggable.appendChild(collisionDiv);
        document.body.removeChild(menu);
    });

    // Fechar o menu ao clicar fora dele
    const closeMenuIfOutsideClick = function(event) {
        if (!menu.contains(event.target) && !draggable.contains(event.target)) {
            document.body.removeChild(menu); // Remove o menu
            document.removeEventListener('click', closeMenuIfOutsideClick); // Remover o ouvinte de evento após fechar o menu
        }
    };

    // Adicionar o ouvinte de clique no documento
    document.addEventListener('click', closeMenuIfOutsideClick);
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

        body.addEventListener('dragover', function(e) {
            e.preventDefault();
        });

        body.addEventListener('drop', function(e) {
            e.preventDefault();
            const files = e.dataTransfer.files;

            if (files.length > 0) {
                const file = files[0];
                if (file.type.startsWith('image/')) {
                    const reader = new FileReader();
                    reader.onload = function(event) {
                        const img = new Image();
                        img.src = event.target.result;
                        img.onload = function() {
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

        ";
                var menu = @"

";
                try
                {
                    await webView.CoreWebView2.ExecuteScriptAsync(jsContent);
                    await webView.CoreWebView2.ExecuteScriptAsync(menu);

                    System.Windows.MessageBox.Show("JavaScript executed successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred while executing the JavaScript: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("WebView2 is not initialized.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
