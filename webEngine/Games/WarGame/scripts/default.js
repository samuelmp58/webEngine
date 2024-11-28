const gameObjects = [];
const collidableObjects = [];

class GameObject {
    constructor(element) {
        this.element = element;
        this.collision = {
			selfCollision: element,
            isColliding: false,
			collidingObj: null  // Referência ao objeto colidido
        };
        this.updateBoundingBox();
    }

    updateBoundingBox() {
        this.boundingBox = this.collision.selfCollision.getBoundingClientRect();
    }

    IntersectsWithObj(other) {
        const rectA = this.boundingBox;
        const rectB = other.boundingBox;

        if (rectA.left < rectB.right &&
            rectA.right > rectB.left &&
            rectA.top < rectB.bottom &&
            rectA.bottom > rectB.top) {

            this.collision.isColliding = true;
			this.collision.collidingObj = other;

            other.collision.isColliding = true;
			other.collision.collidingObj = this;
			return true;
        } else {
            this.collision.isColliding = false;
			this.collision.collidingObj = null;


            other.collision.isColliding = false;
			other.collision.collidingObj = null;
			return false;
        }
    }
	
	destroy() {
        // Remove the element from the DOM
        this.element.remove();

        // Remove the object from gameObjects array
        const gameObjectIndex = gameObjects.indexOf(this);
        if (gameObjectIndex > -1) {
            gameObjects.splice(gameObjectIndex, 1);
        }

        // Remove the object from collidableObjects array
        const collidableObjectIndex = collidableObjects.indexOf(this);
        if (collidableObjectIndex > -1) {
            collidableObjects.splice(collidableObjectIndex, 1);
        }
    }
}

// Função para verificar colisões entre todos os objetos
function checkAllCollisions() {
    collidableObjects.forEach(obj => obj.updateBoundingBox());
    
    for (let i = 0; i < collidableObjects.length; i++) {
        for (let j = i + 1; j < collidableObjects.length; j++) {
            if(collidableObjects[i].IntersectsWithObj(collidableObjects[j])){
				break;
			}
        }
    }
}

// Function do add GameObject
function addObject(element, isCollidable = false, selfCollision = element) {
    const gameObject = new GameObject(element);
    gameObject.collision.selfCollision = selfCollision;
    gameObjects.push(gameObject);
    if (isCollidable) {
        collidableObjects.push(gameObject);
    }
    return gameObject;
}



// Camera

class Camera {
    static follow(obj) {
		document.getElementById(obj.element.id).scrollIntoView({block: "center", inline: "center"});
    }
    
    static followHorizontal(obj) {
        const element = document.getElementById(obj.element.id);
        const parent = element.parentElement;

        if (parent) {
            const parentRect = parent.getBoundingClientRect();
            const elementRect = element.getBoundingClientRect();

            const offsetX = elementRect.left - parentRect.left - (parentRect.width / 2) + (elementRect.width / 2);
            parent.scrollLeft += offsetX;
        }
    }

    static followVertical(obj) {
        const element = document.getElementById(obj.element.id);
        const parent = element.parentElement;

        if (parent) {
            const parentRect = parent.getBoundingClientRect();
            const elementRect = element.getBoundingClientRect();

            const offsetY = elementRect.top - parentRect.top - (parentRect.height / 2) + (elementRect.height / 2);
            parent.scrollTop += offsetY;
        }
    }
}



// Control

class Control {
    constructor(player, config = {}) {
        this.player = player;
        this.keyboard = {};
        this.mouse = { x: 0, y: 0 };
        this.config = {
            speedMultiplier: 3,
            moveKeys: {
                up: 'w',
                left: 'a',
                down: 's',
                right: 'd',
                shift: 'Shift'
            },
            actions: {},
            mouseFollow: true,
            //collisionCheck: element => null, // Função de verificação de colisão genérica
            ...config
        };
        this.lastAngle = 0;
        this.setupEventListeners();
    }

    setupEventListeners() {
        document.addEventListener("mousemove", (event) => this.handleMouseMove(event));
        document.addEventListener("keydown", (e) => this.handleKeyboard(e));
		document.addEventListener("keyup", (e) => this.handleKeyUp(e));
		document.addEventListener("mousedown", (e) => this.handleMouseDown(e)); 

    }
	
	handleMouseDown(event) {
		if (event.button === 0) { // 0 é o botão esquerdo do mouse
			// Verifica se há uma ação associada ao clique esquerdo
			if (this.config.actions.leftClick) {
				this.config.actions.leftClick.callback();
			}
		}
	}
	
	handleKeyUp(event) {
		const key = event.key.toLowerCase();
		this.keyboard[key] = false;

		if (this.config.onKeyUp) {
		  this.config.onKeyUp(key);
		}
	}

    handleMouseMove(event) {
        this.mouse.x = event.clientX;
        this.mouse.y = event.clientY;

        if (this.config.mouseFollow) {
            const rect = this.player.element.getBoundingClientRect();
            const centerX = rect.left + rect.width / 2;
            const centerY = rect.top + rect.height / 2;
            const deltaX = this.mouse.x - centerX;
            const deltaY = this.mouse.y - centerY;
            let angle = Math.atan2(deltaY, deltaX);
            angle = angle * (180 / Math.PI);

            let angleDifference = angle - this.lastAngle;
            if (angleDifference < -180) angleDifference += 360;
            else if (angleDifference > 180) angleDifference -= 360;

            this.lastAngle = angle;
            const currentRotation = parseFloat(this.player.element.style.transform.slice(7, -3)) || 0;
            this.player.element.style.transform = `rotate(${currentRotation + angleDifference}deg)`;
			
            this.player.element.style.transformOrigin = "50% 50%";
        }
    }

    handleKeyboard(event) {
        const key = event.key.toLowerCase();
        this.keyboard[key] = event.type === 'keydown';

        if (event.preventDefault) event.preventDefault();
        else event.returnValue = false;

        // Handle custom actions on keydown only
        if (event.type === 'keydown') {
            for (const [action, keyConfig] of Object.entries(this.config.actions)) {
                if (key === keyConfig.key.toLowerCase()) {
                    if (typeof keyConfig.callback === 'function') {
                        keyConfig.callback();
                    }
                    // Reset key state after action is handled
                    this.keyboard[key] = false;
                }
            }
        }
    }

    movePlayer(deltaX, deltaY, colliding = false) {
	
        const newX = parseInt(this.player.element.style.left || 0) + (deltaX || 0) * this.config.speedMultiplier;
        const newY = parseInt(this.player.element.style.top || 0) + (deltaY || 0) * this.config.speedMultiplier;
		
        this.player.element.style.left = newX + 'px';
        this.player.element.style.top = newY + 'px';
	
		if(this.player.collision.selfCollision != this.player.element){
			this.player.collision.selfCollision.style.left = newX + 'px'; 
			this.player.collision.selfCollision.style.top = newY + 'px';
		}
		
		checkAllCollisions();
        if (this.player.collision.isColliding) {
			this.player.element.style.left = parseInt(this.player.element.style.left || 0) - (deltaX || 0) * this.config.speedMultiplier;
			this.player.element.style.top = parseInt(this.player.element.style.top || 0) - (deltaY || 0) * this.config.speedMultiplier;
		}
		
    }
	
    update() {
        const { up, left, down, right, shift } = this.config.moveKeys;

        if (this.keyboard[left]) this.movePlayer(-1, 0);
        if (this.keyboard[right]) this.movePlayer(1, 0);
        if (this.keyboard[up]) this.movePlayer(0, -1);
        if (this.keyboard[down]) this.movePlayer(0, 1);
    }

}




// Game

class Game {
	static fps = 1000 / 60;
    static intervalId = null;
	
	static start(fps = 60) {
		this.fps = 1000 / fps;
        setup();
		window.setInterval(function(){
			loop();
		}, this.fps);
    }
	
	static stop() {
        if (this.intervalId) {
            clearInterval(this.intervalId);
            this.intervalId = null;
        }
    }
}