extends Camera3D

var MOVING = false
var MOVE_SPEED = 4.0
var ACCELERATION = 0
var rot = Vector3()

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	print(ACCELERATION)
	
	var move_vec = Vector3(0, 0, 0)
	
	if Input.is_key_pressed(KEY_W):
		move_vec.z = -1.0
	
	if Input.is_key_pressed(KEY_S):
		move_vec.z = 1.0
		
	if Input.is_key_pressed(KEY_A):
		move_vec.x = -1.0
	
	if Input.is_key_pressed(KEY_D):
		move_vec.x = 1.0
		
	if Input.is_key_pressed(KEY_E):
		move_vec.y = 1.0
		
	if Input.is_key_pressed(KEY_Q):
		move_vec.y = -1.0
	
	if !(move_vec.x == 0) or !(move_vec.y == 0) or !(move_vec.z == 0):
		MOVING = true
	else:
		MOVING = false
	
	if MOVING:
		ACCELERATION = ACCELERATION + 0.01
	else:
		ACCELERATION = 0
	
	translate_object_local(move_vec * (MOVE_SPEED + ACCELERATION) * delta)
	rotation = Vector3()
	rotate_x(rot.x)
	rotate_y(rot.y)
	
var mouse_start_pos = Vector2()

func _input(event):
	if event is InputEventMouseButton:
		if event.button_index == MOUSE_BUTTON_RIGHT:
			if event.pressed:
				mouse_start_pos = event.position
				Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
			else:
				Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
				get_viewport().warp_mouse(mouse_start_pos)
	
	if event is InputEventMouseMotion:
		if Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT):
			rot.x += event.relative.y / -100.0
			rot.y += event.relative.x / -100.0
