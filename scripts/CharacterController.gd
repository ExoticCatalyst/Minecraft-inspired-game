extends CharacterBody3D


const SPEED = 3.8
const JUMP_VELOCITY = 8.94427191
const SENSITIVITY = 0.005

#head bobbing variables
const BOB_FREQ = 4
const BOB_AMP = 0.06
var t_bob = 0.0

# Get the gravity from the project settings to be synced with RigidBody nodes.
var gravity = 32

@onready var head = $Head
@onready var camera = $Head/Camera3D
@onready var voxel_world = get_node("../VoxelWorld")
@onready var raycaster = $Head/Camera3D/RayCast3D

var has_raycast_hit = false
var raycast_grid = Vector3i()
var raycast_normal = Vector3()

func _ready():
	Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)

func _input(event):
	if event is InputEventMouseMotion:
		head.rotate_y(-event.relative.x * SENSITIVITY)
		camera.rotate_x(-event.relative.y * SENSITIVITY)
		camera.rotation.x = clamp(camera.rotation.x, deg_to_rad(-90), deg_to_rad(90))
	
	if event is InputEventMouseButton:
		if event.pressed and has_raycast_hit:
			if event.button_index == MOUSE_BUTTON_LEFT:
				# print(raycast_grid)
				voxel_world.set_block(raycast_grid, 0)
			elif event.button_index == MOUSE_BUTTON_RIGHT:
				voxel_world.set_block(raycast_grid + Vector3i(raycast_normal), 1)

func _physics_process(delta):
	# Add the gravity.
	if not is_on_floor():
		velocity.y -= gravity * delta

	# Unlock/Lock mouse
	if Input.is_action_just_pressed("Esc") and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		Input.set_mouse_mode(Input.MOUSE_MODE_VISIBLE)
	else:
		if Input.is_action_just_pressed("Esc") and Input.mouse_mode == Input.MOUSE_MODE_VISIBLE:
			Input.set_mouse_mode(Input.MOUSE_MODE_CAPTURED)
			

	# Handle Jump.
	if Input.is_action_pressed("space") and is_on_floor():
		velocity.y = JUMP_VELOCITY

	# Get the input direction and handle the movement/deceleration.
	# As good practice, you should replace UI actions with custom gameplay actions.
	var input_dir = Input.get_vector("left", "right", "up", "down")
	var direction = (head.transform.basis * Vector3(input_dir.x, 0, input_dir.y)).normalized()
	if direction:
		velocity.x = direction.x * SPEED
		velocity.z = direction.z * SPEED
	else:
		velocity.x = 0
		velocity.z = 0
	
	#Head bob
	if not is_on_wall():
		t_bob += delta * velocity.length() *  float(is_on_floor())
		camera.transform.origin = _headbob(t_bob)
	
	# raycast
	var mouse_pos = get_viewport().get_mouse_position()
	var from = camera.project_ray_origin(mouse_pos)
	var to = from + camera.project_ray_normal(mouse_pos) * 8
	var query = PhysicsRayQueryParameters3D.create(from, to, 1)
	var result = get_world_3d().direct_space_state.intersect_ray(query)
	
	has_raycast_hit = false
	if result:
		has_raycast_hit = true
		raycast_grid = Vector3i(result.position - result.normal * 0.1)
		raycast_normal = result.normal
	
	move_and_slide()

func _headbob(time) -> Vector3:
	var pos = Vector3.ZERO
	pos.y = sin(time * BOB_FREQ) * BOB_AMP + 0.6
	pos.x = cos(time * BOB_FREQ / 2) * BOB_AMP
	return pos
