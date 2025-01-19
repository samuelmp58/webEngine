

let player;
let control;

let boxOne;
let ak;


//
function setPlayerControl(){
	control  = new Control(player, {
        speedMultiplier: 3,
        moveKeys: {
            up: 'w',
            left: 'a',
            down: 's',
            right: 'd',
            shift: 'Shift'
        },
		
		onKeyUp: (key) => {
			if (key === 'shift') {
			  console.log('shift key released');
			  control.config.speedMultiplier = 3;
			}
		},
		
        actions: {
			walk: {
                key: 'shift',
                callback: () => control.config.speedMultiplier = 1.99
            },
			leftClick: {
				key: 'left-click',
				//callback: () => alert('shooting')
			},
            changeWeapon1: {
                key: '1',
                callback: () => console.log('1')
            },
            changeWeapon2: {
                key: '2',
                callback: () => console.log('2')
            },
            changeWeapon3: {
                key: '3',
                callback: () => console.log('3')
            },
			deleteBox: {
                key: 'g',
                callback: () => alert('g')
            }

        },
        mouseFollow: true
    });
}


function setup()
{
	player = addObject(document.getElementById('thePlayer'), true, document.getElementById('playerCollision'));
	addObject(document.getElementById('boxthree'), true);
	let boxThree2 = addObject(document.getElementById('boxthree2'), true);
	boxOne = addObject(document.getElementById('boxOne'));
	ak = addObject(document.getElementById('ak47_1'));
	
	setPlayerControl();
}

function loop() {

    checkAllCollisions();
	Camera.follow(player);
	control.update();
	
	/*
	if(player.collision.isColliding)
	{
		console.log('coliding with ' + player.collision.collidingObj.element.id);
		
		if(player.collision.collidingObj.element === ak.element){
			player.collision.collidingObj.destroy();
			player.collision.IsColliding = false;
		}
	}
	*/
	
	if(player.IntersectsWithObj(ak)){
		ak.element.style.backgroundColor = 'red';
	}else{
		ak.element.style.backgroundColor = '';
	}
	
}

Game.start();