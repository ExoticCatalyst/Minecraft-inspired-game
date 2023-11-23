extends Sprite3D

const CAMERA_DIST = 15.0
const CAMERA_ANGLE = 20.0

@onready var camera: Camera3D = get_parent()
@onready var ui_viewport: SubViewport = $SubViewport
var ui_dist = 10.0

func _ready():
	ui_viewport.size = get_viewport().size
	get_viewport().size_changed.connect(on_size_changed)

func on_size_changed():
	ui_viewport.size = get_viewport().size

func _process(_delta):
	# update ui transform
	var aspect_ratio = float(get_viewport().size.x) / get_viewport().size.y
	position = Vector3(0, 0, -ui_dist)
	scale.x = float(get_viewport().size.x) / ui_viewport.size.x
	scale.y = float(get_viewport().size.y) / ui_viewport.size.y
	pixel_size = 2.0 * (tan(deg_to_rad(camera.fov) / 2) * ui_dist) / get_viewport().size.x * aspect_ratio

#func _unhandled_key_input(event):
#	ui_viewport.push_unhandled_input(event)
