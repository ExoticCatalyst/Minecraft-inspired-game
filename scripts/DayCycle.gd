extends DirectionalLight3D

const TIME_SPEED = 0.00005

var TIME_ACCUM = 0.0


# Called when the node enters the scene tree for the first time.
func _ready():
	pass


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	TIME_ACCUM += delta
	if TIME_ACCUM >= 0.05:
		rotation.x -= TIME_SPEED
		TIME_ACCUM = 0.0
